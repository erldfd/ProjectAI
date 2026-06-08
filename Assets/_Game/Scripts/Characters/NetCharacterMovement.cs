using UnityEngine;
using Unity.Netcode.Components;

namespace ProjectAI.Characters
{
    /// <summary>
    /// 플레이어가 아닌 일반 몬스터/NPC용 이동 컴포넌트입니다.
    /// 내부적으로 NetworkTransform과 NetworkRigidbody2D를 사용하여 위치 및 속도를 동기화합니다.
    /// </summary>
    [RequireComponent(typeof(NetworkTransform))]
    [RequireComponent(typeof(NetworkRigidbody2D))]
    [RequireComponent(typeof(Rigidbody2D))]
    public class NetCharacterMovement : ANetMovement
    {
        private NetworkTransform networkTransform;
        private NetworkRigidbody2D networkRigidbody;
        private Rigidbody2D rb;

        public override Vector2 Velocity => rb.linearVelocity;

        protected override void Awake()
        {
            base.Awake();
            networkTransform = GetComponent<NetworkTransform>();
            networkRigidbody = GetComponent<NetworkRigidbody2D>();
            rb = GetComponent<Rigidbody2D>();
        }
    }
}
