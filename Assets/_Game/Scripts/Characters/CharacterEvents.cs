using System;
using UnityEngine;

namespace ProjectAI.Characters
{
    /// <summary>
    /// 캐릭터 내외부에서 발생하는 각종 이벤트를 중계하는 Event Bus 컴포넌트입니다.
    /// </summary>
    public class CharacterEvents : MonoBehaviour
    {
        /// <summary>
        /// 캐릭터의 이동 속도가 변경되었을 때 발생하는 이벤트
        /// </summary>
        public event Action<Vector2> OnVelocityChanged;

        public void InvokeVelocityChanged(Vector2 velocity)
        {
            OnVelocityChanged?.Invoke(velocity);
        }
    }
}
