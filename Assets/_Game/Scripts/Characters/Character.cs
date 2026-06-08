using UnityEngine;
using UnityEngine.Assertions;

namespace ProjectAI.Characters
{
    /// <summary>
    /// 캐릭터의 상태 데이터 및 핵심 로직을 연결하는 허브 컴포넌트입니다.
    /// </summary>
    [RequireComponent(typeof(CharacterEvents))]
    public class Character : MonoBehaviour
    {
        public CharacterEvents Events { get; private set; }

        private void Awake()
        {
            Events = GetComponent<CharacterEvents>();
        }

        /// <summary>
        /// 외부 이동 컴포넌트에서 호출하여 캐릭터의 속도를 갱신합니다.
        /// </summary>
        public void SetVelocity(Vector2 velocity)
        {
            Assert.IsNotNull(Events);
            Events.InvokeVelocityChanged(velocity);
        }
    }
}
