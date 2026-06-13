using UnityEngine.Scripting.APIUpdating;
using UnityEngine;
using UnityEngine.Assertions;

namespace ProjectAI.Core.Entities
{
    /// <summary>
    /// EntityEvents를 구독하여 애니메이터를 제어하는 컴포넌트입니다.
    /// 네트워크나 물리 로직과 완전히 분리되어 있습니다.
    /// </summary>
    [RequireComponent(typeof(Animator))]
    [RequireComponent(typeof(SpriteRenderer))]
    [MovedFrom(true, "ProjectAI.Characters", "Assembly-CSharp", "CharacterAnimator")]
    public class EntityAnimator : MonoBehaviour
    {
        private static readonly int hashMoveSpeed = Animator.StringToHash("MoveSpeed");

        [Header("States")]
        [AnimStateSelector] 
        [SerializeField] 
        private string dieStateName;
        private int dieStateHash;

        [AnimStateSelector]
        [SerializeField]
        private string hitStateName;
        private int hitStateHash;
        
        private EntityEvents entityEvents;
        private Animator animator;
        private SpriteRenderer spriteRenderer;

        private void Awake()
        {
            if (!string.IsNullOrEmpty(dieStateName))
            {
                dieStateHash = Animator.StringToHash(dieStateName);
            }

            if (!string.IsNullOrEmpty(hitStateName))
            {
                hitStateHash = Animator.StringToHash(hitStateName);
            }

            animator = GetComponent<Animator>();
            Assert.IsNotNull(animator, "Animator is missing.");

            spriteRenderer = GetComponent<SpriteRenderer>();
            Assert.IsNotNull(spriteRenderer, "SpriteRenderer is missing.");

            entityEvents = GetComponentInParent<EntityEvents>();
            Assert.IsNotNull(entityEvents, "EntityEvents component is missing.");
        }

        private void OnEnable()
        {
            Assert.IsNotNull(entityEvents, "EntityEvents component is missing.");
            entityEvents.OnVelocityChanged += HandleVelocityChanged;
            entityEvents.OnFacingDirectionChanged += HandleFacingDirectionChanged;
            entityEvents.OnPlayAnimation += HandlePlayAnimation;
            entityEvents.OnDeathTriggered += HandleDeathTriggered;
            entityEvents.OnHitTriggered += HandleHitTriggered;
        }

        private void OnDisable()
        {
            if (entityEvents == null)
            {
                Debug.LogWarning("[EntityAnimator] OnDisable: entityEvents가 null이므로 이벤트 해제를 생략합니다.");
                return;
            }
            
            entityEvents.OnVelocityChanged -= HandleVelocityChanged;
            entityEvents.OnFacingDirectionChanged -= HandleFacingDirectionChanged;
            entityEvents.OnPlayAnimation -= HandlePlayAnimation;
            entityEvents.OnDeathTriggered -= HandleDeathTriggered;
            entityEvents.OnHitTriggered -= HandleHitTriggered;
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
                Debug.LogWarning("[EntityAnimator] HandlePlayAnimation: stateHash가 0입니다. 무시합니다.");
                return;
            }

            animator.CrossFade(stateHash, transitionDuration, layer, 0f);
        }

        private void HandleDeathTriggered()
        {
            if (dieStateHash == 0)
            {
                Debug.LogWarning("[EntityAnimator] dieStateHash가 0입니다. 사망 애니메이션을 재생할 수 없습니다.");
                return;
            }
            
            HandlePlayAnimation(dieStateHash, 0f, 0);
        }

        private void HandleHitTriggered(int damage, int remainingHealth)
        {
            if (hitStateHash == 0)
            {
                return; // 피격 애니메이션이 없는 객체일 수 있으므로 로그 생략
            }
            
            HandlePlayAnimation(hitStateHash, 0f, 0);
        }

        /// <summary>
        /// 애니메이션 클립의 Animation Event 창에서 이 메서드를 호출하여 스킬의 Action 타이밍을 전파합니다.
        /// 파라미터가 없는 구체적인 메서드를 사용함으로써 매직 넘버 사용을 원천 차단합니다.
        /// </summary>
        public void TriggerActionAnimationEvent()
        {
            entityEvents.InvokeAnimationEventTriggered(EAnimationEventTag.Action);
        }
    }
}
