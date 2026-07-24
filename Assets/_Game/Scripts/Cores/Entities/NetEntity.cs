using UnityEngine;
using UnityEngine.Assertions;
using ProjectAI.Movements;
using ProjectAI.Core.Stats;
using ProjectAI.Core.Skills;
using Unity.Netcode;

namespace ProjectAI.Core.Entities
{
    /// <summary>
    /// 생명체(캐릭터) 및 투사체(마법탄) 등 모든 상호작용 가능한 독립 객체의 최상위 기반 클래스입니다.
    /// 공통적인 상태 이벤트(EntityEvents) 및 상태 비트마스크(ActiveStates)를 가집니다.
    /// </summary>
    public class NetEntity : NetworkBehaviour
    {
        public EntityEvents Events { get; private set; }

        /// <summary>
        /// 물리 이동/동기화를 담당하는 컴포넌트입니다. 투사체 등 이동이 없는 엔티티의 경우 null일 수 있습니다.
        /// </summary>
        public ANetMovement Movement { get; private set; }

        public NetStatComponent StatComponent { get; private set; }

        /// <summary>
        /// 엔티티의 현재 상태를 비트마스크로 네트워크 동기화합니다. (Casting, HitStun, Stunned 등)
        /// 쓰기는 반드시 AddState/RemoveState를 통해서만 수행해야 합니다.
        /// </summary>
        public readonly NetworkVariable<int> ActiveStates = new NetworkVariable<int>(
            0,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

        protected virtual void Awake()
        {
            Events = GetComponentInChildren<EntityEvents>();
            Assert.IsNotNull(Events, "NetEntity는 EntityEvents 오너가 필요합니다.");

            Movement = GetComponentInChildren<ANetMovement>();

            StatComponent = GetComponentInChildren<NetStatComponent>();
            if (StatComponent != null)
            {
                StatComponent.SetOwner(this);
            }
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            Animator animator = GetComponentInChildren<Animator>();
            if (animator != null)
            {
                animator.cullingMode = GameStatics.IsServerAuthorized ? AnimatorCullingMode.AlwaysAnimate : AnimatorCullingMode.CullCompletely;
            }

            ActiveStates.OnValueChanged += HandleActiveStatesChanged;
        }

        public override void OnNetworkDespawn()
        {
            ActiveStates.OnValueChanged -= HandleActiveStatesChanged;
            base.OnNetworkDespawn();
        }

        /// <summary>
        /// 지정된 상태 태그가 현재 활성 상태인지 확인합니다.
        /// </summary>
        public bool HasState(EStateTag tag)
        {
            return (ActiveStates.Value & (int)tag) != 0;
        }

        /// <summary>
        /// 지정된 상태 태그를 활성화합니다. 서버에서만 실행 가능합니다.
        /// </summary>
        public void AddState(EStateTag tag)
        {
            Assert.IsTrue(GameStatics.IsServerAuthorized, "[NetEntity] AddState는 서버에서만 실행되어야 합니다.");

            if (!GameStatics.IsServerAuthorized)
            {
                return;
            }

            ActiveStates.Value |= (int)tag;
        }

        /// <summary>
        /// 지정된 상태 태그를 비활성화합니다. 서버에서만 실행 가능합니다.
        /// </summary>
        public void RemoveState(EStateTag tag)
        {
            Assert.IsTrue(GameStatics.IsServerAuthorized, "[NetEntity] RemoveState는 서버에서만 실행되어야 합니다.");

            if (!GameStatics.IsServerAuthorized)
            {
                return;
            }

            ActiveStates.Value &= ~(int)tag;
        }

        private void HandleActiveStatesChanged(int previousValue, int newValue)
        {
            Assert.IsNotNull(Events);
            Events.InvokeActiveStatesChanged(newValue);
        }
    }
}
