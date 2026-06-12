using UnityEngine.Scripting.APIUpdating;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Assertions;
using ProjectAI.Core.Entities;

namespace ProjectAI.Movements
{
    /// <summary>
    /// 모든 네트워크 엔티티(플레이어, 몬스터, 투사체 등) 이동 컴포넌트의 추상 기반 클래스입니다.
    /// 공통 기능(넉백 등) 및 상태 이벤트(EntityEvents)와 통신하기 위한 허브 역할을 합니다.
    /// </summary>
    [MovedFrom(true, "ProjectAI.Characters", "Assembly-CSharp", "ANetMovement")]
    public abstract class ANetMovement : NetworkBehaviour
    {
        protected EntityEvents _entityEvents;

        /// <summary>
        /// 애니메이션 갱신을 위한 네트워크 동기화 속도입니다.
        /// </summary>
        public NetworkVariable<Vector2> NetAnimVelocity = new NetworkVariable<Vector2>(
            Vector2.zero,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Owner
        );

        /// <summary>
        /// 캐릭터가 현재 오른쪽을 바라보고 있는지 여부입니다. (공격 방향 등에 사용)
        /// </summary>
        public NetworkVariable<bool> NetIsFacingRight = new NetworkVariable<bool>(
            true,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Owner
        );

        /// <summary>
        /// 물리 연산 등에서 참조할 실제 물리/이동 속도
        /// </summary>
        public abstract Vector2 Velocity { get; }

        /// <summary>
        /// 이동에 사용하는 물리 리지드바디 컴포넌트입니다.
        /// </summary>
        public Rigidbody2D Rb { get; protected set; }

        protected virtual void Awake()
        {
            _entityEvents = GetComponentInParent<EntityEvents>();
            Assert.IsNotNull(_entityEvents, "EntityEvents component is missing.");
            Rb = GetComponentInParent<Rigidbody2D>();
            Assert.IsNotNull(Rb, "Rigidbody2D is missing.");
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            NetAnimVelocity.OnValueChanged += HandleVelocityChanged;
            NetIsFacingRight.OnValueChanged += HandleFacingDirectionChanged;
            
            // 구독 직후, 이미 값이 존재할 수 있으므로 초기 상태 동기화 수동 호출
            HandleVelocityChanged(Vector2.zero, NetAnimVelocity.Value);
            HandleFacingDirectionChanged(true, NetIsFacingRight.Value);
        }

        public override void OnNetworkDespawn()
        {
            NetAnimVelocity.OnValueChanged -= HandleVelocityChanged;
            NetIsFacingRight.OnValueChanged -= HandleFacingDirectionChanged;
            base.OnNetworkDespawn();
        }

        private void HandleVelocityChanged(Vector2 previousValue, Vector2 newValue)
        {
            Assert.IsNotNull(_entityEvents);
            _entityEvents.InvokeVelocityChanged(newValue);
        }

        private void HandleFacingDirectionChanged(bool previousValue, bool newValue)
        {
            Assert.IsNotNull(_entityEvents);
            _entityEvents.InvokeFacingDirectionChanged(newValue);
        }

        // TODO: 향후 넉백 등 공통 피격/이동 로직 추가
    }
}
