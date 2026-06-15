using UnityEngine.Assertions;
using UnityEngine;
using Unity.Netcode;
using ProjectAI.Core.Entities;
using System.Collections.Generic;
using UnityEngine.Scripting.APIUpdating;
using ProjectAI.Characters;
using ProjectAI.SOs;

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

        /// <summary>
        /// 캐릭터의 현재 상태를 비트마스크로 동기화합니다.
        /// </summary>
        public NetworkVariable<int> ActiveStates = new NetworkVariable<int>(
            0,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

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
            if (entityEvents != null)
            {
                entityEvents.OnAnimationEventTriggered += HandleAnimationEvent;
                entityEvents.OnAnimationStateExited += HandleAnimationStateExited;
            }
        }

        private void OnDisable()
        {
            if (entityEvents != null)
            {
                entityEvents.OnAnimationEventTriggered -= HandleAnimationEvent;
                entityEvents.OnAnimationStateExited -= HandleAnimationStateExited;
            }
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
                RemoveState(EStateTag.Casting);
            }
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            if (IsOwner)
            {
                Assert.IsNotNull(entityEvents, "EntityEvents component is missing.");
                entityEvents.OnSkillTriggered += TryActivateSkill;
            }
        }

        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();

            Assert.IsNotNull(entityEvents, "EntityEvents component is missing.");
            entityEvents.OnSkillTriggered -= TryActivateSkill;
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

        public bool HasState(EStateTag tag)
        {
            return (ActiveStates.Value & (int)tag) != 0;
        }

        public void AddState(EStateTag tag)
        {
            Assert.IsTrue(GameStatics.IsServerAuthorized, "[NetSkillComponent] AddState는 서버에서만 실행되어야 합니다.");
            
            if (!GameStatics.IsServerAuthorized)
            {
                return;
            }
            
            ActiveStates.Value |= (int)tag;
        }

        public void RemoveState(EStateTag tag)
        {
            Assert.IsTrue(GameStatics.IsServerAuthorized, "[NetSkillComponent] RemoveState는 서버에서만 실행되어야 합니다.");
            
            if (!GameStatics.IsServerAuthorized)
            {
                return;
            }
            
            ActiveStates.Value &= ~(int)tag;
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

                if (HasState(EStateTag.Silenced) || HasState(EStateTag.Stunned) || HasState(EStateTag.Casting))
                {
                    Debug.Log($"[NetSkillComponent] 상태이상(침묵/기절/시전중)으로 인해 스킬 ID {skillId} 시전 불가.");
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

        [Rpc(SendTo.Server)]
        private void RequestActivateSkillServerRpc(int skillId)
        {
            Assert.IsTrue(GameStatics.IsServerAuthorized, "[NetSkillComponent] RequestActivateSkillServerRpc는 서버에서만 실행되어야 합니다.");
            
            if (!GameStatics.IsServerAuthorized)
            {
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
                    AddState(EStateTag.Casting);
                }
                else
                {
                    RollbackLocalCooldownClientRpc(skillId);
                }
            }
        }

        [Rpc(SendTo.Owner)]
        private void RollbackLocalCooldownClientRpc(int skillId)
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
