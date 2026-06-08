using UnityEngine;
using Unity.Netcode.Components;

namespace ProjectAI.Movements
{
    /// <summary>
    /// 서버 권한으로 물리 기반 이동을 수행하는 범용 이동 컴포넌트입니다. (몬스터, 투사체 등 공용)
    /// 내부적으로 부모의 Rigidbody2D를 참조하여 위치 및 속도를 동기화합니다.
    /// </summary>
    [UnityEngine.Scripting.APIUpdating.MovedFrom(true, "ProjectAI.Characters", "Assembly-CSharp", "NetCharacterMovement")]
    public class NetServerMovement : ANetMovement
    {
        private Rigidbody2D rb;

        public override Vector2 Velocity => rb.linearVelocity;

        protected override void Awake()
        {
            base.Awake();
            rb = GetComponentInParent<Rigidbody2D>();
            UnityEngine.Assertions.Assert.IsNotNull(rb, "Rigidbody2D component is missing in parent.");
        }
    }
}
