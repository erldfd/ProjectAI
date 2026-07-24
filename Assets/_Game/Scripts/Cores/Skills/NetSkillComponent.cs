using UnityEngine.Assertions;
using UnityEngine;
using Unity.Netcode;
using ProjectAI.Core.Entities;
using System.Collections.Generic;
using UnityEngine.Scripting.APIUpdating;
using ProjectAI.Characters;
using ProjectAI.SOs;
using ProjectAI.Core.Enums;

namespace ProjectAI.Core.Skills
{
    /// <summary>
    /// 캐릭터의 스킬(마법탄 발사 등) 입력을 처리하고 서버로 RPC를 보내는 범용 컴포넌트입니다.
    /// </summary>
    [MovedFrom(true, "ProjectAI.Players", "Assembly-CSharp", "NetPlayerCombat")]
    public class NetSkillComponent : NetworkBehaviour
    {
        [Header("Skill Settings")]
        [Tooltip("이 캐릭터가 사용할 수 있는 스킬 목록")]
        public List<BaseSkillConfig> OwnedSkills = new List<BaseSkillConfig>();

        [Tooltip("마법탄 등이 발사될 기준 위치 (없으면 자신 Transform 중심)")]
        [SerializeField]
        private Transform firePoint;
        public Transform FirePoint => firePoint;

        [Tooltip("근접 평타 범위용 콜라이더.\n[주의] 게임오브젝트는 켜두되, Collider 컴포넌트는 비활성화(enabled=false) 및 isTrigger=true로 설정하는 것을 권장합니다.")]
        [SerializeField]
        private Collider2D meleeHitbox;
        public Collider2D MeleeHitbox => meleeHitbox;

        // 로컬 예측 쿨타임 (UI 및 클라이언트 빠른 검증용)
        private Dictionary<int, double> localActivationTimes = new Dictionary<int, double>();

        // 서버 보안 검증 쿨타임 (서버 권한용)
        private Dictionary<int, double> serverActivationTimes = new Dictionary<int, double>();

        private EntityEvents entityEvents;
        private NetCharacter ownerCharacter;
        private int currentCastingSkillId = 0;

        private void Awake()
        {
            entityEvents = GetComponentInParent<EntityEvents>();
            Assert.IsNotNull(entityEvents, $"[NetSkillComponent] {gameObject.name}에 EntityEvents를 찾을 수 없습니다.");

            ownerCharacter = GetComponentInParent<NetCharacter>();
            Assert.IsNotNull(ownerCharacter, "[NetSkillComponent] NetCharacter를 찾을 수 없습니다.");
        }

        private void OnEnable()
        {
            Assert.IsNotNull(entityEvents);
            entityEvents.OnAnimationEventTriggered += HandleAnimationEvent;
            entityEvents.OnAnimationStateExited += HandleAnimationStateExited;
            entityEvents.OnHitStateEntered += HandleHitStateEntered;
            entityEvents.OnHitStateExited += HandleHitStateExited;
        }

        private void OnDisable()
        {
            Assert.IsNotNull(entityEvents);
            entityEvents.OnAnimationEventTriggered -= HandleAnimationEvent;
            entityEvents.OnAnimationStateExited -= HandleAnimationStateExited;
            entityEvents.OnHitStateEntered -= HandleHitStateEntered;
            entityEvents.OnHitStateExited -= HandleHitStateExited;
        }

        private void HandleHitStateEntered()
        {
            if (!GameStatics.IsServerAuthorized)
            {
                return;
            }

            ownerCharacter.AddState(EStateTag.HitStun);
        }

        private void HandleHitStateExited()
        {
            if (!GameStatics.IsServerAuthorized)
            {
                return;
            }

            ownerCharacter.RemoveState(EStateTag.HitStun);
        }

        private void HandleAnimationEvent(EAnimationEventTag eventTag)
        {
            if (!GameStatics.IsServerAuthorized || currentCastingSkillId == 0)
            {
                return;
            }

            if (eventTag == EAnimationEventTag.Action)
            {
                if (GameStatics.SkillManager != null)
                {
                    GameStatics.SkillManager.ActionSkill(currentCastingSkillId, ownerCharacter);
                }
            }
        }

        private void HandleAnimationStateExited(int stateHash)
        {
            Debug.Log($"[NetSkillComponent] HandleAnimationStateExited: stateHash={stateHash}, currentCastingSkillId={currentCastingSkillId}");
            if (!GameStatics.IsServerAuthorized || currentCastingSkillId == 0)
            {
                return;
            }

            Debug.Log($"[NetSkillComponent] Checking if exited state hash matches current casting skill's animation hash.");
            int expectedHash = GetSkillAnimHash(currentCastingSkillId);
            if (expectedHash == stateHash)
            {
                Debug.Log($"[NetSkillComponent] Animation state exited for skill ID {currentCastingSkillId}");
                if (GameStatics.SkillManager != null)
                {
                    Debug.Log($"[NetSkillComponent] Ending skill ID {currentCastingSkillId} for character {ownerCharacter.NetworkObjectId}");
                    GameStatics.SkillManager.EndSkill(currentCastingSkillId, ownerCharacter);
                }

                currentCastingSkillId = 0;
                ownerCharacter.RemoveState(EStateTag.Casting);
            }
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            Assert.IsNotNull(entityEvents, "EntityEvents component is missing.");

            if (IsOwner)
            {
                entityEvents.OnSkillTriggered += TryActivateSkill;
            }
        }

        public override void OnNetworkDespawn()
        {
            Assert.IsNotNull(entityEvents);

            if (IsOwner)
            {
                entityEvents.OnSkillTriggered -= TryActivateSkill;
            }

            base.OnNetworkDespawn();
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
        public void EquipLoadoutServerRpc(int[] skillIds)
        {
            Assert.IsTrue(GameStatics.IsServerAuthorized, "[NetSkillComponent] EquipLoadoutServerRpc는 서버에서만 실행되어야 합니다.");
            
            if (!GameStatics.IsServerAuthorized)
            {
                return;
            }

            if (skillIds == null || skillIds.Length > 3)
            {
                Debug.LogWarning($"[NetSkillComponent] 비정상적인 스킬 장착 요청이 차단되었습니다. (길이: {skillIds?.Length})");
                return;
            }
            
            // TODO: 서버 측 도감 검증 로직 추가 가능 (현재 MVP는 클라이언트 신뢰)
            EquipLoadoutClientRpc(skillIds);
        }

        [Rpc(SendTo.ClientsAndHost)]
        private void EquipLoadoutClientRpc(int[] skillIds)
        {
            if (GameStatics.SkillManager == null)
            {
                Debug.LogWarning("[NetSkillComponent] EquipLoadoutClientRpc: SkillManager가 존재하지 않습니다.");
                return;
            }

            // 기존 소환수 슬롯(Summon1 ~ Summon3, 인덱스 1~3)을 먼저 null로 초기화하여 이전 잔존 스킬 제거
            for (int slot = (int)ESkillSlot.Summon1; slot <= (int)ESkillSlot.Summon3; slot++)
            {
                while (OwnedSkills.Count <= slot)
                {
                    OwnedSkills.Add(null);
                }

                OwnedSkills[slot] = null;
            }

            // 기획에 따라 Summon1, Summon2, Summon3 슬롯에 소환수 스킬을 주입
            for (int i = 0; i < skillIds.Length; i++)
            {
                // i=0 -> Summon1 (1), i=1 -> Summon2 (2), i=2 -> Summon3 (3)
                int slotIndex = (int)ESkillSlot.Summon1 + i; 
                BaseSkillConfig config = GameStatics.SkillManager.GetConfig(skillIds[i]);
                
                if (config == null)
                {
                    Debug.LogWarning($"[NetSkillComponent] EquipLoadoutClientRpc: 스킬 ID {skillIds[i]}에 대한 BaseSkillConfig를 찾을 수 없습니다. 슬롯 {slotIndex} 장착 실패.");
                    continue;
                }

                OwnedSkills[slotIndex] = config;
                Debug.Log($"[NetSkillComponent] {gameObject.name} 슬롯({slotIndex})에 스킬({config.name}) 장착 완료.");
            }
            
            Debug.Log($"[NetSkillComponent] {gameObject.name} 로드아웃 장착 완료 (총 {skillIds.Length}개).");
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
        public void TryEquipSkillServerRpc(int skillId, int targetSlotIndex)
        {
            Assert.IsTrue(GameStatics.IsServerAuthorized, "[NetSkillComponent] TryEquipSkillServerRpc는 서버에서만 실행되어야 합니다.");
            
            if (!GameStatics.IsServerAuthorized)
            {
                return;
            }

            if (targetSlotIndex < 0 || targetSlotIndex > 3)
            {
                Debug.LogWarning($"[NetSkillComponent] 유효하지 않은 슬롯 인덱스 요청입니다. (Index: {targetSlotIndex})");
                return;
            }

            // TODO: 서버 측 악의적 호출 검증 로직 추가 가능
            EquipSkillClientRpc(skillId, targetSlotIndex);
        }

        [Rpc(SendTo.ClientsAndHost)]
        private void EquipSkillClientRpc(int skillId, int targetSlotIndex)
        {
            if (GameStatics.SkillManager == null)
            {
                return;
            }

            BaseSkillConfig config = GameStatics.SkillManager.GetConfig(skillId);
            if (config == null)
            {
                return;
            }

            while (OwnedSkills.Count <= targetSlotIndex)
            {
                OwnedSkills.Add(null);
            }
            
            OwnedSkills[targetSlotIndex] = config;
            Debug.Log($"<color=green>[NetSkillComponent]</color> {gameObject.name} 슬롯({targetSlotIndex})에 스킬({config.name}) 장착 완료.");
        }

        public double GetLocalActivationTime(int skillId)
        {
            if (localActivationTimes.TryGetValue(skillId, out double time))
            {
                return time;
            }

            return -999.0;
        }

        public double GetServerActivationTime(int skillId)
        {
            if (serverActivationTimes.TryGetValue(skillId, out double time))
            {
                return time;
            }

            return -999.0;
        }

        public int GetSkillAnimHash(int skillId)
        {
            if (GameStatics.SkillManager != null)
            {
                BaseSkillConfig config = GameStatics.SkillManager.GetConfig(skillId);
                if (config != null)
                {
                    return config.AnimHash;
                }
            }
            
            return 0;
        }

        public void SetLocalActivationTime(int skillId, double time)
        {
            localActivationTimes[skillId] = time;
        }

        public void SetServerActivationTime(int skillId, double time)
        {
            serverActivationTimes[skillId] = time;
        }

        /// <summary>
        /// 클라이언트(컨트롤러)에서 특정 스킬 사용을 시도합니다.
        /// </summary>
        public void TryActivateSkill(int skillId)
        {
            bool hasSkill = false;
            foreach (BaseSkillConfig config in OwnedSkills)
            {
                if (config != null && config.SkillId == skillId)
                {
                    hasSkill = true;
                    break;
                }
            }

            if (!hasSkill)
            {
                Debug.LogWarning($"[NetSkillComponent] 클라이언트 로컬 검증 실패: 미보유 스킬(ID: {skillId}) 시도 차단됨.");
                return;
            }

            // 로컬 단위 클라이언트 예측(쿨타임, 상태 검사) 로직
            if (GameStatics.SkillManager != null)
            {
                BaseSkillConfig config = GameStatics.SkillManager.GetConfig(skillId);
                if (GameStatics.NetworkManager != null && config != null && GameStatics.NetworkManager.ServerTime.Time < GetLocalActivationTime(skillId) + config.BaseCooldown)
                {
                    Debug.Log($"[NetSkillComponent] 스킬 ID {skillId} 로컬 쿨타임 대기 중입니다.");
                    return; // 쿨타임 대기 중
                }

                if (ownerCharacter.HasState(EStateTag.Silenced) || ownerCharacter.HasState(EStateTag.Stunned) || ownerCharacter.HasState(EStateTag.Casting) || ownerCharacter.HasState(EStateTag.HitStun))
                {
                    Debug.Log($"[NetSkillComponent] 상태이상(침묵/기절/피격경직/시전중)으로 인해 스킬 ID {skillId} 시전 불가.");
                    return; // 상태 이상으로 시전 불가
                }
            }

            // 로컬(클라이언트/호스트) 쿨타임을 즉시 미리 돌림 (예측 및 즉각적인 UI 반영)
            if (GameStatics.NetworkManager != null)
            {
                SetLocalActivationTime(skillId, GameStatics.NetworkManager.ServerTime.Time);
            }

            RequestActivateSkillServerRpc(skillId);
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
        private void RequestActivateSkillServerRpc(int skillId)
        {
            Assert.IsTrue(GameStatics.IsServerAuthorized, "[NetSkillComponent] RequestActivateSkillServerRpc는 서버에서만 실행되어야 합니다.");
            
            if (!GameStatics.IsServerAuthorized)
            {
                return;
            }

            if (ownerCharacter.HasState(EStateTag.Silenced) || ownerCharacter.HasState(EStateTag.Stunned) || ownerCharacter.HasState(EStateTag.Casting) || ownerCharacter.HasState(EStateTag.HitStun))
            {
                Debug.LogWarning($"[NetSkillComponent] 서버 보안 검증 실패: 상태이상(침묵/기절/피격경직/시전중) 상태에서 스킬 ID {skillId} 시도를 보냈습니다!");
                return;
            }
            
            bool hasSkill = false;
            foreach (BaseSkillConfig config in OwnedSkills)
            {
                if (config != null && config.SkillId == skillId)
                {
                    hasSkill = true;
                    break;
                }
            }

            if (!hasSkill)
            {
                Debug.LogWarning($"[NetSkillComponent] 서버 보안 검증 실패: 클라이언트(Owner: {OwnerClientId})가 미보유 스킬(ID: {skillId}) 시도를 보냈습니다!");
                return;
            }

            if (GameStatics.SkillManager != null)
            {
                if (GameStatics.SkillManager.ExecuteSkill(skillId, ownerCharacter))
                {
                    currentCastingSkillId = skillId;
                    ownerCharacter.AddState(EStateTag.Casting);
                }
                else
                {
                    RollbackLocalCooldownOwnerRpc(skillId);
                }
            }
        }

        [Rpc(SendTo.Owner)]
        private void RollbackLocalCooldownOwnerRpc(int skillId)
        {
            Debug.Log($"[NetSkillComponent] 스킬 ID {skillId} 서버 발동 실패. 로컬 예측 쿨타임 롤백.");
            SetLocalActivationTime(skillId, -999.0);
        }

        /// <summary>
        /// 서버가 스킬 발동/로직 실행 후 관련된 애니메이션 재생을 모든 클라이언트에게 지시합니다.
        /// </summary>
        [Rpc(SendTo.ClientsAndHost)]
        public void BroadcastPlayAnimationClientRpc(int stateHash, float transitionDuration)
        {
            Assert.IsNotNull(entityEvents, "EntityEvents component is missing.");
            entityEvents.InvokePlayAnimation(stateHash, transitionDuration, 0);
        }
    }
}
