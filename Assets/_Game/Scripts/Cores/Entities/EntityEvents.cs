using UnityEngine.Scripting.APIUpdating;
using System;
using UnityEngine;
using ProjectAI.Core.Skills;

namespace ProjectAI.Core.Entities
{
    /// <summary>
    /// 엔티티 내외부에서 발생하는 각종 상태 이벤트를 중계하는 범용 Event Bus 컴포넌트입니다.
    /// </summary>
    [MovedFrom(true, "ProjectAI.Characters", "Assembly-CSharp", "CharacterEvents")]
    public class EntityEvents : MonoBehaviour
    {
        /// <summary>
        /// 캐릭터의 이동 속도가 변경되었을 때 발생하는 이벤트
        /// </summary>
        /// <param name="velocity">현재 프레임의 이동 속도 벡터</param>
        public event Action<Vector2> OnVelocityChanged;
        
        private event Action<float> onMoveSpeedModifierChanged;
        
        private float cachedMoveSpeedModifier = 1f;

        private bool cachedIsFacingRight = true;
        private event Action<bool> onFacingDirectionChanged;

        /// <summary>
        /// 외부(뇌)에서 특정 스킬의 발동을 요청했을 때 발생하는 이벤트
        /// </summary>
        /// <param name="skillType">발동을 요청할 스킬의 종류</param>
        public event Action<ESkillType> OnSkillTriggered;

        /// <summary>
        /// 강제로 특정 애니메이션 상태를 재생하라고 지시할 때 발생하는 이벤트
        /// </summary>
        /// <param name="stateHash">재생할 애니메이션 상태의 해시(Hash) 값</param>
        /// <param name="transitionDuration">애니메이션 블렌딩(CrossFade) 소요 시간 (0이면 즉시 재생)</param>
        /// <param name="layer">애니메이터의 레이어 인덱스 (기본값 0)</param>
        public event Action<int, float, int> OnPlayAnimation;

        /// <summary>
        /// 유니티 애니메이션 클립의 Animation Event가 트리거되었을 때 발생하는 이벤트
        /// </summary>
        public event Action<EAnimationEventTag> OnAnimationEventTriggered;

        /// <summary>
        /// StateMachineBehaviour를 통해 애니메이션 상태가 종료(Exit)되었을 때 발생하는 이벤트
        /// </summary>
        public event Action<int> OnAnimationStateExited;

        /// <summary>
        /// 구독 시점에 즉시 최신 값을 한 번 내려줍니다. (Late Subscriber 버그 방지)
        /// </summary>
        /// <param name="modifier">적용 중인 이동 속도 배율 (1f가 기본)</param>
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
        /// <param name="isFacingRight">오른쪽을 바라보면 true, 왼쪽이면 false</param>
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

        /// <summary>
        /// 강제로 특정 애니메이션 상태를 재생하도록 이벤트를 발생시킵니다.
        /// </summary>
        /// <param name="stateHash">재생할 애니메이션 상태의 해시(Hash) 값</param>
        /// <param name="transitionDuration">애니메이션 블렌딩(CrossFade) 소요 시간 (0이면 즉시 재생)</param>
        /// <param name="layer">애니메이터의 레이어 인덱스 (기본값 0)</param>
        public void InvokePlayAnimation(int stateHash, float transitionDuration = 0f, int layer = 0)
        {
            OnPlayAnimation?.Invoke(stateHash, transitionDuration, layer);
        }

        public void InvokeAnimationEventTriggered(EAnimationEventTag eventTag)
        {
            OnAnimationEventTriggered?.Invoke(eventTag);
        }

        public void InvokeAnimationStateExited(int stateHash)
        {
            OnAnimationStateExited?.Invoke(stateHash);
        }
    }
}
