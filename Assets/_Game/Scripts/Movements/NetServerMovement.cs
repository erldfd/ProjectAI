using UnityEngine;
using Unity.Netcode.Components;

namespace ProjectAI.Movements
{
    /// <summary>
    /// 서버 권한으로 물리 기반 이동을 수행하는 범용 이동 컴포넌트입니다. (몬스터, 투사체 등 공용)
    /// 내부적으로 부모의 Rigidbody2D를 참조하여 위치 및 속도를 동기화합니다.
    /// </summary>
    [UnityEngine.Scripting.APIUpdating.MovedFrom(true, "ProjectAI.Characters", "Assembly-CSharp", "NetCharacterMovement")]
    public class NetServerMovement : ANetMovement
    {
        [Header("Movement Settings")]
        [Tooltip("기본 이동 속도")]
        [SerializeField]
        private float baseSpeed = 15f;

        private Vector2 currentDirection = Vector2.zero;
        private float currentSpeedModifier = 1f;

        public override Vector2 Velocity => base.Rb.linearVelocity;

        protected override void Awake()
        {
            base.Awake();
            UnityEngine.Assertions.Assert.IsNotNull(base.Rb, "Rigidbody2D component is missing in parent.");
        }

        private void OnEnable()
        {
            if (base._entityEvents != null)
            {
                base._entityEvents.OnMoveSpeedModifierChanged += HandleMoveSpeedModifierChanged;
            }
        }

        private void OnDisable()
        {
            if (base._entityEvents != null)
            {
                base._entityEvents.OnMoveSpeedModifierChanged -= HandleMoveSpeedModifierChanged;
            }
        }

        private void HandleMoveSpeedModifierChanged(float modifier)
        {
            currentSpeedModifier = modifier;
            UpdateVelocity();
        }

        /// <summary>
        /// 이동 방향을 설정하고 즉시 속도를 갱신합니다.
        /// </summary>
        public void SetDirection(Vector2 direction)
        {
            currentDirection = direction.normalized;
            UpdateVelocity();
        }

        private void UpdateVelocity()
        {
            if (!base.IsServer)
            {
                return;
            }

            base.Rb.linearVelocity = currentDirection * (baseSpeed * currentSpeedModifier);
            // 네트워크 애니메이션 및 공통 이벤트 중계 트리거
            base.NetAnimVelocity.Value = base.Rb.linearVelocity;
        }
    }
}
