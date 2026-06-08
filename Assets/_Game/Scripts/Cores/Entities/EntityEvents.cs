using System;
using UnityEngine;
using ProjectAI.Core.Skills;

namespace ProjectAI.Core.Entities
{
    /// <summary>
    /// 엔티티 내외부에서 발생하는 각종 상태 이벤트를 중계하는 범용 Event Bus 컴포넌트입니다.
    /// </summary>
    [UnityEngine.Scripting.APIUpdating.MovedFrom(true, "ProjectAI.Characters", "Assembly-CSharp", "CharacterEvents")]
    public class EntityEvents : MonoBehaviour
    {
        /// <summary>
        /// 캐릭터의 이동 속도가 변경되었을 때 발생하는 이벤트
        /// </summary>
        public event Action<Vector2> OnVelocityChanged;
        
        private event Action<float> onMoveSpeedModifierChanged;
        
        private float cachedMoveSpeedModifier = 1f;

        private bool cachedIsFacingRight = true;
        private event Action<bool> onFacingDirectionChanged;

        /// <summary>
        /// 외부(뇌)에서 특정 스킬의 발동을 요청했을 때 발생하는 이벤트
        /// </summary>
        public event Action<ESkillType> OnSkillTriggered;

        /// <summary>
        /// 구독 시점에 즉시 최신 값을 한 번 내려줍니다. (Late Subscriber 버그 방지)
        /// </summary>
        public event Action<float> OnMoveSpeedModifierChanged
        {
            add
            {
                onMoveSpeedModifierChanged += value;
                // value는 방금 이 이벤트를 구독(+=)하려고 전달된 델리게이트 단 하나를 의미함.
                // 구독하는 즉시 캐싱된 최신 값을 해당 델리게이트에게만 1회 강제 호출해 줌.
                value?.Invoke(cachedMoveSpeedModifier);
            }
            remove
            {
                onMoveSpeedModifierChanged -= value;
            }
        }

        /// <summary>
        /// 캐릭터가 바라보는 방향이 변경되었을 때 (또는 후참여자 구독 시) 발생하는 이벤트
        /// </summary>
        public event Action<bool> OnFacingDirectionChanged
        {
            add
            {
                onFacingDirectionChanged += value;
                // value는 방금 이 이벤트를 구독(+=)하려고 전달된 델리게이트 단 하나를 의미함.
                // 구독하는 즉시 캐싱된 최신 값을 해당 델리게이트에게만 1회 강제 호출해 줌.
                value?.Invoke(cachedIsFacingRight);
            }
            remove
            {
                onFacingDirectionChanged -= value;
            }
        }

        public void InvokeVelocityChanged(Vector2 velocity)
        {
            OnVelocityChanged?.Invoke(velocity);
        }

        public void InvokeMoveSpeedModifierChanged(float modifier)
        {
            cachedMoveSpeedModifier = modifier;
            onMoveSpeedModifierChanged?.Invoke(modifier);
        }

        public void InvokeFacingDirectionChanged(bool isFacingRight)
        {
            cachedIsFacingRight = isFacingRight;
            onFacingDirectionChanged?.Invoke(isFacingRight);
        }

        public void InvokeSkillTriggered(ESkillType skillType)
        {
            OnSkillTriggered?.Invoke(skillType);
        }
    }
}
