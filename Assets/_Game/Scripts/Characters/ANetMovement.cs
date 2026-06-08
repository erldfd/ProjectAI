using Unity.Netcode;
using UnityEngine;
using UnityEngine.Assertions;

namespace ProjectAI.Characters
{
    /// <summary>
    /// 모든 네트워크 캐릭터(플레이어, 몬스터 등) 이동 컴포넌트의 추상 기반 클래스입니다.
    /// 공통 기능(넉백 등) 및 애니메이터와 통신하기 위한 허브 역할을 합니다.
    /// </summary>
    public abstract class ANetMovement : NetworkBehaviour
    {
        protected CharacterEvents _characterEvents;

        /// <summary>
        /// 애니메이션 갱신을 위한 네트워크 동기화 속도입니다.
        /// </summary>
        public NetworkVariable<Vector2> NetAnimVelocity = new NetworkVariable<Vector2>(
            Vector2.zero,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Owner
        );

        /// <summary>
        /// 물리 연산 등에서 참조할 실제 물리/이동 속도
        /// </summary>
        public abstract Vector2 Velocity { get; }

        protected virtual void Awake()
        {
            _characterEvents = GetComponentInParent<CharacterEvents>();
            Assert.IsNotNull(_characterEvents, "CharacterEvents component is missing.");
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            NetAnimVelocity.OnValueChanged += HandleVelocityChanged;
        }

        public override void OnNetworkDespawn()
        {
            NetAnimVelocity.OnValueChanged -= HandleVelocityChanged;
            base.OnNetworkDespawn();
        }

        private void HandleVelocityChanged(Vector2 previousValue, Vector2 newValue)
        {
            Assert.IsNotNull(_characterEvents);
            _characterEvents.InvokeVelocityChanged(newValue);
        }

        // TODO: 향후 넉백 등 공통 피격/이동 로직 추가
    }
}
