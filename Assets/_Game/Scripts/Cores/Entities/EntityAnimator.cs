using UnityEngine;
using UnityEngine.Assertions;
using Unity.Netcode;

namespace ProjectAI.Core.Entities
{
    /// <summary>
    /// EntityEvents를 구독하여 애니메이터를 제어하는 컴포넌트입니다.
    /// 네트워크나 물리 로직과 완전히 분리되어 있습니다.
    /// </summary>
    [RequireComponent(typeof(Animator))]
    [RequireComponent(typeof(SpriteRenderer))]
    [UnityEngine.Scripting.APIUpdating.MovedFrom(true, "ProjectAI.Characters", "Assembly-CSharp", "CharacterAnimator")]
    public class EntityAnimator : MonoBehaviour
    {
        private static readonly int hashMoveSpeed = Animator.StringToHash("MoveSpeed");

        [SerializeField]
        private EntityEvents entityEvents;
        
        [SerializeField]
        private Animator animator;

        [SerializeField]
        private SpriteRenderer spriteRenderer;

        private void Awake()
        {
            animator = GetComponent<Animator>();
            spriteRenderer = GetComponent<SpriteRenderer>();

            // EntityEvents가 명시적으로 할당되지 않은 경우 탐색
            if (entityEvents == null)
            {
                entityEvents = GetComponentInParent<EntityEvents>();
                Assert.IsNotNull(entityEvents, "EntityEvents component is missing.");
            }
        }

        private void OnEnable()
        {
            Assert.IsNotNull(entityEvents, "EntityEvents component is missing.");
            entityEvents.OnVelocityChanged += HandleVelocityChanged;
            entityEvents.OnFacingDirectionChanged += HandleFacingDirectionChanged;
            entityEvents.OnPlayAnimation += HandlePlayAnimation;
        }

        private void OnDisable()
        {
            Assert.IsNotNull(entityEvents, "EntityEvents component is missing.");
            entityEvents.OnVelocityChanged -= HandleVelocityChanged;
            entityEvents.OnFacingDirectionChanged -= HandleFacingDirectionChanged;
            entityEvents.OnPlayAnimation -= HandlePlayAnimation;
        }

        private void HandleVelocityChanged(Vector2 velocity)
        {
            animator.SetFloat(hashMoveSpeed, velocity.magnitude);
        }

        private void HandleFacingDirectionChanged(bool isFacingRight)
        {
            // 오른쪽을 보면 flipX = false, 왼쪽을 보면 flipX = true
            spriteRenderer.flipX = !isFacingRight;
        }

        private void HandlePlayAnimation(int stateHash, float transitionDuration, int layer)
        {
            if (stateHash == 0)
            {
                return;
            }

            animator.CrossFade(stateHash, transitionDuration, layer, 0f);
        }

        /// <summary>
        /// 애니메이션 클립의 Animation Event 창에서 이 메서드를 호출하여 스킬의 Action 타이밍을 전파합니다.
        /// 파라미터가 없는 구체적인 메서드를 사용함으로써 매직 넘버 사용을 원천 차단합니다.
        /// </summary>
        public void TriggerActionAnimationEvent()
        {
            if (entityEvents != null)
            {
                entityEvents.InvokeAnimationEventTriggered(EAnimationEventTag.Action);
            }
        }
    }
}
