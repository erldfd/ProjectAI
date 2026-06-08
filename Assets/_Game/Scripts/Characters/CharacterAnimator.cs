using UnityEngine;
using UnityEngine.Assertions;

namespace ProjectAI.Characters
{
    /// <summary>
    /// CharacterEvents를 구독하여 애니메이터를 제어하는 컴포넌트입니다.
    /// 네트워크나 물리 로직과 완전히 분리되어 있습니다.
    /// </summary>
    [RequireComponent(typeof(Animator))]
    [RequireComponent(typeof(SpriteRenderer))]
    public class CharacterAnimator : MonoBehaviour
    {
        private static readonly int hashMoveSpeed = Animator.StringToHash("MoveSpeed");

        [SerializeField]
        private CharacterEvents characterEvents;
        
        [SerializeField]
        private Animator animator;

        [SerializeField]
        private SpriteRenderer spriteRenderer;

        private void Awake()
        {
            if (animator == null)
            {
                animator = GetComponent<Animator>();
            }

            if (spriteRenderer == null)
            {
                spriteRenderer = GetComponent<SpriteRenderer>();
            }

            // CharacterEvents가 명시적으로 할당되지 않은 경우 탐색
            if (characterEvents == null)
            {
                characterEvents = GetComponentInParent<CharacterEvents>();
                Assert.IsNotNull(characterEvents, "CharacterEvents component is missing.");
            }
        }

        private void OnEnable()
        {
            Assert.IsNotNull(characterEvents);
            characterEvents.OnVelocityChanged += HandleVelocityChanged;
        }

        private void OnDisable()
        {
            Assert.IsNotNull(characterEvents);
            characterEvents.OnVelocityChanged -= HandleVelocityChanged;
        }

        private void HandleVelocityChanged(Vector2 velocity)
        {
            animator.SetFloat(hashMoveSpeed, velocity.magnitude);

            // 기본 방향이 오른쪽이므로, 왼쪽 이동 시(음수) flipX를 켬
            if (velocity.x < -0.01f)
            {
                spriteRenderer.flipX = true;
            }
            else if (velocity.x > 0.01f)
            {
                spriteRenderer.flipX = false;
            }
        }
    }
}
