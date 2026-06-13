using UnityEngine.Scripting.APIUpdating;
using UnityEngine.Assertions;
using UnityEngine;
using Unity.Netcode.Components;
using ProjectAI.Core;

namespace ProjectAI.Movements
{
    /// <summary>
    /// 서버 권한으로 물리 기반 이동을 수행하는 범용 이동 컴포넌트입니다. (몬스터, 투사체 등 공용)
    /// 내부적으로 부모의 Rigidbody2D를 참조하여 위치 및 속도를 동기화합니다.
    /// </summary>
    [MovedFrom(true, "ProjectAI.Characters", "Assembly-CSharp", "NetCharacterMovement")]
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
            Assert.IsNotNull(base.Rb, "Rigidbody2D component is missing in parent.");
        }

        private void OnEnable()
        {
            base._entityEvents.OnMoveSpeedModifierChanged += HandleMoveSpeedModifierChanged;
        }

        private void OnDisable()
        {
            base._entityEvents.OnMoveSpeedModifierChanged -= HandleMoveSpeedModifierChanged;
        }

        private void HandleMoveSpeedModifierChanged(float modifier)
        {
            if (!GameStatics.IsServerAuthorized)
            {
                return;
            }

            currentSpeedModifier = modifier;
            UpdateVelocity();
        }

        /// <summary>
        /// 이동 방향을 설정하고 즉시 속도를 갱신합니다.
        /// </summary>
        public void SetDirection(Vector2 direction)
        {
            if (!GameStatics.IsServerAuthorized)
            {
                return;
            }

            // 좌우 이동에 따른 바라보는 방향 갱신 (위아래 이동 시에는 기존 방향 유지)
            if (direction.x > 0.01f)
            {
                base.NetIsFacingRight.Value = true;
            }
            else if (direction.x < -0.01f)
            {
                base.NetIsFacingRight.Value = false;
            }

            currentDirection = direction.normalized;
            UpdateVelocity();
        }

        private void UpdateVelocity()
        {
            Assert.IsTrue(GameStatics.IsServerAuthorized, "[NetServerMovement] UpdateVelocity는 서버에서만 실행되어야 합니다.");
            
            if (!GameStatics.IsServerAuthorized)
            {
                return;
            }

            base.Rb.linearVelocity = currentDirection * (baseSpeed * currentSpeedModifier);
            // 네트워크 애니메이션 및 공통 이벤트 중계 트리거
            base.NetAnimVelocity.Value = base.Rb.linearVelocity;
        }
    }
}
