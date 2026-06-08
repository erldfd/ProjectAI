using UnityEngine;
using Unity.Netcode;
using ProjectAI.Core;

namespace ProjectAI.Projectiles
{
    /// <summary>
    /// 마법탄 등 투사체의 이동 및 충돌 판정을 처리하는 서버 주도형 컴포넌트입니다.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class NetProjectile : Entity
    {
        [Header("Projectile Settings")]
        [Tooltip("투사체 비행 속도")]
        [SerializeField]
        private float speed = 15f;

        [Tooltip("투사체가 줄 데미지량")]
        [SerializeField]
        private int damage = 10;

        [Tooltip("발사 후 자동 파괴될 때까지의 생존 시간 (초)")]
        [SerializeField]
        private float lifeTime = 5f;

        private Vector2 moveDirection;
        private ulong ownerPlayerId;
        private Rigidbody2D rb;

        protected override void Awake()
        {
            base.Awake();
            rb = GetComponent<Rigidbody2D>();
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            // 서버에서만 수명 관리
            if (base.IsServer)
            {
                Invoke(nameof(DestroyProjectile), lifeTime);
            }
        }

        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();

            if (base.IsServer)
            {
                CancelInvoke(nameof(DestroyProjectile));
            }
        }

        /// <summary>
        /// 서버에서 투사체를 스폰할 때 초기화하는 메서드입니다.
        /// </summary>
        public void Initialize(Vector2 direction, ulong playerId)
        {
            moveDirection = direction.normalized;
            ownerPlayerId = playerId;
            
            // 초기 속도 적용 (NetServerMovement를 상속하지 않고 자체 리지드바디 제어)
            rb.linearVelocity = moveDirection * speed;
        }

        private void FixedUpdate()
        {
            // 투사체의 물리 이동은 서버에서만 제어하며, 클라이언트는 NetworkTransform(또는 Rigidbody 동기화)를 통해 수신받습니다.
            if (!base.IsServer)
            {
                return;
            }

            // Rigidbody.linearVelocity가 이미 값을 가지고 있으므로 지속적인 갱신은 불필요할 수 있지만, 
            // 안전을 위해 속도를 유지시켜 줍니다.
            rb.linearVelocity = moveDirection * speed;
        }

        private void OnTriggerEnter2D(Collider2D collision)
        {
            // 충돌 판정은 오직 서버에서만 처리합니다.
            if (!base.IsServer)
            {
                return;
            }

            // 충돌 대상이 데미지를 받을 수 있는 객체인지 확인
            IDamageable damageable = collision.GetComponentInParent<IDamageable>();
            
            if (damageable != null)
            {
                // 자신을 쏜 주인이 아닐 때만 데미지 적용
                // 주의: damageable.OwnerClientId를 가져오려면 NetworkObject가 필요함.
                NetworkObject targetNetObj = collision.GetComponentInParent<NetworkObject>();
                if (targetNetObj != null && targetNetObj.OwnerClientId == ownerPlayerId)
                {
                    // 자신이 쏜 투사체에 자신이 맞는 것은 무시
                    return;
                }

                GameStatics.ApplyDamage(collision.gameObject, damage);
                DestroyProjectile();
            }
            else
            {
                // 벽이나 장애물에 부딪힘
                // 단순 트리거/센서 등이 아닌 환경 콜라이더인지 체크 필요
                if (!collision.isTrigger)
                {
                    DestroyProjectile();
                }
            }
        }

        private void DestroyProjectile()
        {
            if (base.IsServer && base.NetworkObject != null && base.NetworkObject.IsSpawned)
            {
                base.NetworkObject.Despawn();
                // Despawn 하면 기본적으로 객체가 파괴됨. 
                // 단, DestroyWithScene=true (기본값)인지 확인 필요.
            }
        }
    }
}
