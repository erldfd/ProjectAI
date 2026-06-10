using UnityEngine;
using Unity.Netcode;
using ProjectAI.Core;
using ProjectAI.Core.Entities;
using System.Collections.Generic;
using UnityEngine.Scripting.APIUpdating;
using ProjectAI.Characters;

namespace ProjectAI.Core.Skills
{
    [System.Serializable]
    public struct SkillAnimMapping
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
        public List<SkillAnimMapping> SkillAnimations = new List<SkillAnimMapping>();

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

        // 로컬/서버 공용 쿨타임 추적용 딕셔너리
        private Dictionary<ESkillType, double> lastActivationTimes = new Dictionary<ESkillType, double>();

        private Dictionary<ESkillType, int> animHashCache = new Dictionary<ESkillType, int>();

        private EntityEvents entityEvents;
        private NetCharacter ownerCharacter;
        private ESkillType? currentCastingSkill = null;

        private void Awake()
        {
            Debug.Log($"[NetSkillComponent] Awake: Caching animation hashes for {SkillAnimations.Count} skills.");
            foreach (SkillAnimMapping mapping in SkillAnimations)
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
            if (entityEvents == null)
            {
                Debug.LogWarning($"[NetSkillComponent] {gameObject.name}에 EntityEvents를 찾을 수 없습니다.");
            }

            ownerCharacter = GetComponentInParent<NetCharacter>();
            UnityEngine.Assertions.Assert.IsNotNull(ownerCharacter, "[NetSkillComponent] NetCharacter를 찾을 수 없습니다.");
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
            if (!IsServer || currentCastingSkill == null)
            {
                return;
            }
            
            if (eventTag == EAnimationEventTag.Action)
            {
                if (GameStatics.SkillManager != null)
                {
                    GameStatics.SkillManager.ActionSkill(currentCastingSkill.Value, ownerCharacter);
                }
            }
        }

        private void HandleAnimationStateExited(int stateHash)
        {
            if (!IsServer || currentCastingSkill == null)
            {
                return;
            }

            // 현재 시전 중인 스킬의 애니메이션 해시와 일치하는지 검사
            int expectedHash = GetSkillAnimHash(currentCastingSkill.Value);
            if (expectedHash == stateHash)
            {
                if (GameStatics.SkillManager != null)
                {
                    GameStatics.SkillManager.EndSkill(currentCastingSkill.Value, ownerCharacter);
                }

                currentCastingSkill = null;
            }
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            if (base.IsOwner)
            {
                UnityEngine.Assertions.Assert.IsNotNull(entityEvents, "EntityEvents component is missing.");
                entityEvents.OnSkillTriggered += TryActivateSkill;
            }
        }

        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();

            UnityEngine.Assertions.Assert.IsNotNull(entityEvents, "EntityEvents component is missing.");
            entityEvents.OnSkillTriggered -= TryActivateSkill;
        }

        public double GetLastActivationTime(ESkillType type)
        {
            if (lastActivationTimes.TryGetValue(type, out double time))
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

        public void SetLastActivationTime(ESkillType type, double time)
        {
            lastActivationTimes[type] = time;
        }

        public bool HasState(EStateTag tag)
        {
            return (ActiveStates.Value & (int)tag) != 0;
        }

        public void AddState(EStateTag tag)
        {
            if (base.IsServer)
            {
                ActiveStates.Value |= (int)tag;
            }
        }

        public void RemoveState(EStateTag tag)
        {
            if (base.IsServer)
            {
                ActiveStates.Value &= ~(int)tag;
            }
        }

        /// <summary>
        /// 클라이언트(컨트롤러)에서 특정 스킬 사용을 시도합니다.
        /// </summary>
        public void TryActivateSkill(ESkillType skillType)
        {
            if (!OwnedSkills.Contains(skillType))
            {
                return;
            }

            // 로컬 단위 클라이언트 예측(쿨타임, 상태 검사) 로직
            if (GameStatics.SkillManager != null)
            {
                SSkillConfig config = GameStatics.SkillManager.GetConfig(skillType);
                if (NetworkManager.Singleton != null && NetworkManager.Singleton.ServerTime.Time < GetLastActivationTime(skillType) + config.BaseCooldown)
                {
                    return; // 쿨타임 대기 중
                }

                if (HasState(EStateTag.Silenced) || HasState(EStateTag.Stunned))
                {
                    return; // 상태 이상으로 시전 불가
                }
            }

            // 클라이언트 전용(호스트 제외) 쿨타임을 미리 돌림 (예측)
            if (NetworkManager.Singleton != null && !base.IsServer)
            {
                SetLastActivationTime(skillType, NetworkManager.Singleton.ServerTime.Time);
            }

            RequestActivateSkillServerRpc(skillType);
        }

        [Rpc(SendTo.Server)]
        private void RequestActivateSkillServerRpc(ESkillType skillType)
        {
            if (!OwnedSkills.Contains(skillType))
            {
                return;
            }

            if (GameStatics.SkillManager != null)
            {
                if (GameStatics.SkillManager.ExecuteSkill(skillType, ownerCharacter))
                {
                    currentCastingSkill = skillType;
                }
            }
        }

        /// <summary>
        /// 서버가 스킬 발동/로직 실행 후 관련된 애니메이션 재생을 모든 클라이언트에게 지시합니다.
        /// </summary>
        [Rpc(SendTo.ClientsAndHost)]
        public void BroadcastPlayAnimationClientRpc(int stateHash, float transitionDuration)
        {
            UnityEngine.Assertions.Assert.IsNotNull(entityEvents, "EntityEvents component is missing.");
            entityEvents.InvokePlayAnimation(stateHash, transitionDuration, 0);
        }
    }
}
