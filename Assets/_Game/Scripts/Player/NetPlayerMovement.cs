using UnityEngine;
using Unity.Netcode;

namespace PortalBroke.Player
{
    /// <summary>
    /// Rigidbody2D를 사용한 캐릭터의 물리적 이동을 전담하는 클래스입니다.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class NetPlayerMovement : NetworkBehaviour
    {
        [Header("Movement Settings")]
        [Tooltip("캐릭터의 기본 이동 속도")]
        [SerializeField]
        private float moveSpeed = 5f;

        private Rigidbody2D rb;
        private Vector2 currentMoveInput;

        #region Unity Lifecycle
        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
        }

        private void FixedUpdate()
        {
            if (!IsOwner)
            {
                return;
            }

            ApplyPhysics();
        }
        #endregion

        #region Public Methods
        public void SetMoveInput(Vector2 input)
        {
            currentMoveInput = input;
        }
        #endregion

        #region Private Methods
        private void ApplyPhysics()
        {
            rb.linearVelocity = currentMoveInput * moveSpeed;
        }
        #endregion
    }
}
