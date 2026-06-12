using UnityEngine.Assertions;
using UnityEngine;
using Unity.Netcode;
using ProjectAI.Core;
using ProjectAI.Core.Entities;
using ProjectAI.Core.Stats;
using ProjectAI.Core.Pooling;

namespace ProjectAI.Projectiles
{
    /// <summary>
    /// 마법탄 등 투사체의 이동 및 충돌 판정을 처리하는 서버 주도형 컴포넌트입니다.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class NetProjectile : NetEntity, IPoolable
    {
        [Header("Projectile Settings")]
        [Tooltip("발사 후 자동 파괴될 때까지의 생존 시간 (초)")]
        [SerializeField]
        private float lifeTime = 5f;

        private ulong ownerNetworkObjectId;
        private NetStatComponent statComponent;

        protected override void Awake()
        {
            base.Awake();
            statComponent = GetComponentInChildren<NetStatComponent>();
            Assert.IsNotNull(statComponent, "NetProjectile은 데미지 처리를 위해 NetStatComponent가 필수입니다.");

            Assert.IsNotNull(base.Movement, "NetProjectile은 이동 제어를 위한 ANetMovement 컴포넌트가 필수입니다.");
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            // 서버에서만 수명 관리
            if (GameStatics.IsServerAuthorized)
            {
                Invoke(nameof(DestroyProjectile), lifeTime);
            }
        }

        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();

            if (GameStatics.IsServerAuthorized)
            {
                CancelInvoke(nameof(DestroyProjectile));
            }
        }

        /// <summary>
        /// 서버에서 투사체를 스폰할 때 초기화하는 메서드입니다.
        /// </summary>
        public void Initialize(Vector2 direction, ulong ownerNetObjId)
        {
            Assert.IsTrue(GameStatics.IsServerAuthorized, "[NetProjectile] Initialize는 서버에서만 실행되어야 합니다.");
            
            if (!GameStatics.IsServerAuthorized)
            {
                return;
            }

            ownerNetworkObjectId = ownerNetObjId;
            Debug.Log($"[NetProjectile] Initialize 호출됨. OwnerNetObjId: {ownerNetworkObjectId}, 방향: {direction}");
            
            if (base.Movement is ProjectAI.Movements.NetServerMovement serverMovement)
            {
                serverMovement.SetDirection(direction);
            }
        }

        private void OnTriggerEnter2D(Collider2D collision)
        {
            Debug.Log($"[NetProjectile] 투사체 충돌 감지: {collision.gameObject.name}");
            // 충돌 판정은 오직 서버에서만 처리합니다.
            if (!GameStatics.IsServerAuthorized)
            {
                return;
            }

            // 충돌 대상이 데미지를 받을 수 있는 객체인지 확인
            IDamageable damageable = collision.GetComponentInChildren<IDamageable>();
            
            if (damageable != null)
            {
                // 자신이 쏜 투사체에 자신이 맞는 것은 무시
                NetworkObject targetNetObj = collision.GetComponentInParent<NetworkObject>();
                if (targetNetObj != null && targetNetObj.NetworkObjectId == ownerNetworkObjectId)
                {
                    Debug.Log($"[NetProjectile] 팀킬/자해 무시 처리. TargetNetObjId: {targetNetObj.NetworkObjectId}");
                    return;
                }

                Debug.Log($"[NetProjectile] 데미지 적용 대상 발견: {collision.name}, 데미지량: {statComponent.AttackPower.Value}");

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
            Assert.IsTrue(GameStatics.IsServerAuthorized, "[NetProjectile] DestroyProjectile은 서버에서만 실행되어야 합니다.");
            
            if (!GameStatics.IsServerAuthorized)
            {
                return;
            }
            Debug.Log($"[NetProjectile] 투사체 파괴(Despawn) 시도. IsServerAuthorized: {GameStatics.IsServerAuthorized}, NetworkObjectId: {NetworkObjectId}, IsSpawned: {NetworkObject?.IsSpawned}");
            if (NetworkObject != null && NetworkObject.IsSpawned)
            {
                if (GameStatics.ObjectPool != null)
                {
                    Debug.Log($"[NetProjectile] ObjectPool에 투사체 반환 호출: {NetworkObjectId}");
                    GameStatics.ObjectPool.ReturnNetworkObject(NetworkObject);
                }
                else
                {
                    NetworkObject.Despawn(true);
                }
            }
        }

        public void OnSpawn()
        {
        }

        public void OnDespawn()
        {
            if (base.Movement == null || base.Movement.Rb == null)
            {
                return;
            }

            base.Movement.Rb.linearVelocity = Vector2.zero;
            base.Movement.Rb.angularVelocity = 0f;
        }
    }
}
