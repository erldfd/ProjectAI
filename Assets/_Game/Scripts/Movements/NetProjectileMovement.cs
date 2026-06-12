using UnityEngine.Scripting.APIUpdating;
using UnityEngine.Assertions;
using UnityEngine;
using Unity.Netcode;
using ProjectAI.Core;

namespace ProjectAI.Movements
{
    /// <summary>
    /// 투사체 전용 이동 컴포넌트입니다.
    /// 서버가 방향과 속도를 세팅하면, NetworkVariable을 통해 클라이언트가 전달받아 보간(NetworkTransform) 없이 로컬 물리 엔진으로 직접 이동시킵니다.
    /// </summary>
    public class NetProjectileMovement : ANetMovement
    {
        [Header("Movement Settings")]
        [Tooltip("기본 이동 속도")]
        [SerializeField]
        private float baseSpeed = 15f;

        private Vector2 currentDirection = Vector2.zero;
        private float currentSpeedModifier = 1f;

        public override Vector2 Velocity => base.Rb.linearVelocity;

        // 투사체 동기화 전용 물리 속도 변수
        public NetworkVariable<Vector2> NetPhysicalVelocity = new NetworkVariable<Vector2>(
            Vector2.zero,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

        protected override void Awake()
        {
            base.Awake();
            Assert.IsNotNull(base.Rb, "Rigidbody2D component is missing in parent.");
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            
            NetPhysicalVelocity.OnValueChanged += ApplyClientPhysicalVelocity;
            
            if (!GameStatics.IsServerAuthorized)
            {
                ApplyClientPhysicalVelocity(Vector2.zero, NetPhysicalVelocity.Value);
            }
        }

        public override void OnNetworkDespawn()
        {
            NetPhysicalVelocity.OnValueChanged -= ApplyClientPhysicalVelocity;
            base.OnNetworkDespawn();
        }

        private void ApplyClientPhysicalVelocity(Vector2 previousValue, Vector2 newValue)
        {
            if (!GameStatics.IsServerAuthorized)
            {
                base.Rb.linearVelocity = newValue;
            }
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

        public void SetDirection(Vector2 direction)
        {
            currentDirection = direction.normalized;
            UpdateVelocity();
        }

        private void UpdateVelocity()
        {
            Assert.IsTrue(GameStatics.IsServerAuthorized, "[NetProjectileMovement] UpdateVelocity는 서버에서만 실행되어야 합니다.");
            
            if (!GameStatics.IsServerAuthorized)
            {
                return;
            }

            Vector2 newVelocity = currentDirection * (baseSpeed * currentSpeedModifier);
            base.Rb.linearVelocity = newVelocity;
            NetPhysicalVelocity.Value = newVelocity; // 클라이언트로 속도 전파
        }
    }
}
