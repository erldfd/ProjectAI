using UnityEngine.Assertions;
using UnityEngine;
using Unity.Netcode;
using ProjectAI.Core;
using ProjectAI.Core.Entities;
using System.Collections.Generic;
using UnityEngine.Scripting.APIUpdating;
using ProjectAI.Characters;

namespace ProjectAI.Core.Skills
{
    /// <summary>
    /// 스킬 타입과 재생할 애니메이션 상태 이름을 매핑하는 구조체입니다.
    /// </summary>
    [System.Serializable]
    public struct SSkillAnimMapping
    {
        public ESkillType SkillType;

        [AnimStateSelector]
        public string AnimStateName;
    }

    /// <summary>
    /// 캐릭터의 스킬(마법탄 발사 등) 입력을 처리하고 서버로 RPC를 보내는 범용 컴포넌트입니다.
    /// </summary>
    [MovedFrom(true, "ProjectAI.Players", "Assembly-CSharp", "NetPlayerCombat")]
    public class NetSkillComponent : NetworkBehaviour
    {
        [Header("Skill Settings")]
        [Tooltip("이 캐릭터가 사용할 수 있는 스킬 목록")]
        public List<ESkillType> OwnedSkills = new List<ESkillType>();

        [Tooltip("캐릭터가 스킬을 시전할 때 재생할 애니메이션 상태 매핑")]
        public List<SSkillAnimMapping> SkillAnimations = new List<SSkillAnimMapping>();

        [Tooltip("마법탄 등이 발사될 기준 위치 (없으면 자신 Transform 중심)")]
        [SerializeField]
        private Transform firePoint;
        public Transform FirePoint => firePoint;

        /// <summary>
        /// 캐릭터의 현재 상태를 비트마스크로 동기화합니다.
        /// </summary>
        public NetworkVariable<int> ActiveStates = new NetworkVariable<int>(
            0,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

        // 로컬 예측 쿨타임 (UI 및 클라이언트 빠른 검증용)
        private Dictionary<ESkillType, double> localActivationTimes = new Dictionary<ESkillType, double>();
        
        // 서버 보안 검증 쿨타임 (서버 권한용)
        private Dictionary<ESkillType, double> serverActivationTimes = new Dictionary<ESkillType, double>();

        private Dictionary<ESkillType, int> animHashCache = new Dictionary<ESkillType, int>();

        private EntityEvents entityEvents;
        private NetCharacter ownerCharacter;
        private ESkillType currentCastingSkill = ESkillType.None;

        private void Awake()
        {
            Debug.Log($"[NetSkillComponent] Awake: Caching animation hashes for {SkillAnimations.Count} skills.");
            foreach (SSkillAnimMapping mapping in SkillAnimations)
            {
                if (string.IsNullOrEmpty(mapping.AnimStateName))
                {
                    Debug.LogWarning($"[NetSkillComponent] Awake: AnimStateName is empty for skill {mapping.SkillType}. Skipping hash caching.");
                    continue;
                }

                Debug.Log($"[NetSkillComponent] Caching animation hash for skill {mapping.SkillType}: {mapping.AnimStateName}");
                animHashCache[mapping.SkillType] = Animator.StringToHash(mapping.AnimStateName);
            }

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
            if (!GameStatics.IsServerAuthorized || currentCastingSkill == ESkillType.None)
            {
                return;
            }
            
            if (eventTag == EAnimationEventTag.Action)
            {
                if (GameStatics.SkillManager != null)
                {
                    GameStatics.SkillManager.ActionSkill(currentCastingSkill, ownerCharacter);
                }
            }
        }

        private void HandleAnimationStateExited(int stateHash)
        {
            if (!GameStatics.IsServerAuthorized || currentCastingSkill == ESkillType.None)
            {
                return;
            }

            int expectedHash = GetSkillAnimHash(currentCastingSkill);
            if (expectedHash == stateHash)
            {
                if (GameStatics.SkillManager != null)
                {
                    GameStatics.SkillManager.EndSkill(currentCastingSkill, ownerCharacter);
                }

                currentCastingSkill = ESkillType.None;
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

        public double GetLocalActivationTime(ESkillType type)
        {
            if (localActivationTimes.TryGetValue(type, out double time))
            {
                return time;
            }

            return -999.0;
        }

        public double GetServerActivationTime(ESkillType type)
        {
            if (serverActivationTimes.TryGetValue(type, out double time))
            {
                return time;
            }

            return -999.0;
        }

        public int GetSkillAnimHash(ESkillType type)
        {
            if (animHashCache.TryGetValue(type, out int hash))
            {
                return hash;
            }

            return 0;
        }

        public void SetLocalActivationTime(ESkillType type, double time)
        {
            localActivationTimes[type] = time;
        }

        public void SetServerActivationTime(ESkillType type, double time)
        {
            serverActivationTimes[type] = time;
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
        public void TryActivateSkill(ESkillType skillType)
        {
            if (!OwnedSkills.Contains(skillType))
            {
                Debug.Log($"[NetSkillComponent] 미보유 스킬 시도: {skillType}");
                return;
            }

            // 로컬 단위 클라이언트 예측(쿨타임, 상태 검사) 로직
            if (GameStatics.SkillManager != null)
            {
                SSkillConfig config = GameStatics.SkillManager.GetConfig(skillType);
                if (GameStatics.NetworkManager != null && GameStatics.NetworkManager.ServerTime.Time < GetLocalActivationTime(skillType) + config.BaseCooldown)
                {
                    Debug.Log($"[NetSkillComponent] 스킬 {skillType} 로컬 쿨타임 대기 중입니다.");
                    return; // 쿨타임 대기 중
                }

                if (HasState(EStateTag.Silenced) || HasState(EStateTag.Stunned) || HasState(EStateTag.Casting))
                {
                    Debug.Log($"[NetSkillComponent] 상태이상(침묵/기절/시전중)으로 인해 스킬 {skillType} 시전 불가.");
                    return; // 상태 이상으로 시전 불가
                }
            }

            // 로컬(클라이언트/호스트) 쿨타임을 즉시 미리 돌림 (예측 및 즉각적인 UI 반영)
            if (GameStatics.NetworkManager != null)
            {
                SetLocalActivationTime(skillType, GameStatics.NetworkManager.ServerTime.Time);
            }

            RequestActivateSkillServerRpc(skillType);
        }

        [Rpc(SendTo.Server)]
        private void RequestActivateSkillServerRpc(ESkillType skillType)
        {
            Assert.IsTrue(GameStatics.IsServerAuthorized, "[NetSkillComponent] RequestActivateSkillServerRpc는 서버에서만 실행되어야 합니다.");
            
            if (!GameStatics.IsServerAuthorized)
            {
                return;
            }
            
            if (!OwnedSkills.Contains(skillType))
            {
                return;
            }

            if (GameStatics.SkillManager != null)
            {
                if (GameStatics.SkillManager.ExecuteSkill(skillType, ownerCharacter))
                {
                    currentCastingSkill = skillType;
                    AddState(EStateTag.Casting);
                }
                else
                {
                    RollbackLocalCooldownClientRpc(skillType);
                }
            }
        }

        [Rpc(SendTo.Owner)]
        private void RollbackLocalCooldownClientRpc(ESkillType skillType)
        {
            Debug.Log($"[NetSkillComponent] 스킬 {skillType} 서버 발동 실패. 로컬 예측 쿨타임 롤백.");
            SetLocalActivationTime(skillType, -999.0);
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
