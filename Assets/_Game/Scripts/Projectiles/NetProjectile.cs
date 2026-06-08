using UnityEngine;
using Unity.Netcode;
using ProjectAI.Core;
using ProjectAI.Core.Entities;
using ProjectAI.Core.Stats;

namespace ProjectAI.Projectiles
{
    /// <summary>
    /// 마법탄 등 투사체의 이동 및 충돌 판정을 처리하는 서버 주도형 컴포넌트입니다.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class NetProjectile : NetEntity
    {
        [Header("Projectile Settings")]
        [Tooltip("발사 후 자동 파괴될 때까지의 생존 시간 (초)")]
        [SerializeField]
        private float lifeTime = 5f;

        private ulong ownerPlayerId;
        private NetStatComponent statComponent;

        protected override void Awake()
        {
            base.Awake();
            statComponent = GetComponentInChildren<NetStatComponent>();
            UnityEngine.Assertions.Assert.IsNotNull(statComponent, "NetProjectile은 데미지 처리를 위해 NetStatComponent가 필수입니다.");
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
            if (!base.IsServer)
            {
                return;
            }

            ownerPlayerId = playerId;
            
            if (base.Movement is ProjectAI.Movements.NetServerMovement serverMovement)
            {
                serverMovement.SetDirection(direction);
            }
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

                GameStatics.ApplyDamage(collision.gameObject, statComponent.AttackPower.Value);
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
