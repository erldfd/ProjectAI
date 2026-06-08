using System;
using UnityEngine;

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
        
        private float cachedMoveSpeedModifier = 1f;
        private event Action<float> onMoveSpeedModifierChanged;

        /// <summary>
        /// 구독 시점에 즉시 최신 값을 한 번 내려줍니다. (Late Subscriber 버그 방지)
        /// </summary>
        public event Action<float> OnMoveSpeedModifierChanged
        {
            add
            {
                onMoveSpeedModifierChanged += value;
                value?.Invoke(cachedMoveSpeedModifier);
            }
            remove
            {
                onMoveSpeedModifierChanged -= value;
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
    }
}
