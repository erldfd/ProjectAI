using UnityEngine;
using Unity.Netcode;
using ProjectAI.Core;
using ProjectAI.Core.Entities;

namespace ProjectAI.Core.Skills
{
    /// <summary>
    /// 캐릭터의 스킬(마법탄 발사 등) 입력을 처리하고 서버로 RPC를 보내는 범용 컴포넌트입니다.
    /// </summary>
    [UnityEngine.Scripting.APIUpdating.MovedFrom(true, "ProjectAI.Players", "Assembly-CSharp", "NetPlayerCombat")]
    public class NetSkillComponent : NetworkBehaviour
    {
        [Header("Skill Settings")]
        [Tooltip("이 캐릭터가 사용할 수 있는 스킬 목록")]
        public System.Collections.Generic.List<ESkillType> OwnedSkills = new System.Collections.Generic.List<ESkillType>();

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
        private System.Collections.Generic.Dictionary<ESkillType, double> lastActivationTimes = new System.Collections.Generic.Dictionary<ESkillType, double>();

        private EntityEvents entityEvents;

        private void Awake()
        {
            entityEvents = GetComponentInParent<EntityEvents>();
            if (entityEvents == null)
            {
                entityEvents = GetComponentInChildren<EntityEvents>();
            }
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            if (entityEvents != null && base.IsOwner)
            {
                entityEvents.OnSkillTriggered += TryActivateSkill;
            }
        }

        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();

            if (entityEvents != null)
            {
                entityEvents.OnSkillTriggered -= TryActivateSkill;
            }
        }

        public double GetLastActivationTime(ESkillType type)
        {
            if (lastActivationTimes.TryGetValue(type, out double time))
            {
                return time;
            }
            return -999.0;
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
                GameStatics.SkillManager.ExecuteSkill(skillType, this);
            }
        }
    }
}
