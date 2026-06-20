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
        
        // [Fix] 투사체가 공중에 스폰될 경우를 대비해 순수한 바닥 Y좌표(깊이)를 캐싱
        private float cachedDepthY;

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
            else
            {
                // 순수 클라이언트는 어차피 충돌 판정을 무시하므로, 물리 콜라이더를 꺼서 부하를 줄임
                Collider2D[] cols = GetComponentsInChildren<Collider2D>();
                foreach (Collider2D col in cols)
                {
                    col.enabled = false;
                }
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
        public void Initialize(Vector2 direction, ulong ownerNetObjId, float depthY)
        {
            Assert.IsTrue(GameStatics.IsServerAuthorized, "[NetProjectile] Initialize는 서버에서만 실행되어야 합니다.");
            
            if (!GameStatics.IsServerAuthorized)
            {
                return;
            }

            ownerNetworkObjectId = ownerNetObjId;
            cachedDepthY = depthY;
            Debug.Log($"[NetProjectile] Initialize 호출됨. OwnerNetObjId: {ownerNetworkObjectId}, 방향: {direction}, 설정된 깊이(Y): {cachedDepthY}");
            
            if (base.Movement is ProjectAI.Movements.NetProjectileMovement projectileMovement)
            {
                projectileMovement.SetDirection(direction);
            }
            else if (base.Movement is ProjectAI.Movements.NetServerMovement serverMovement)
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
            IDamageable damageable = collision.GetComponentInParent<IDamageable>();
            if (damageable == null)
            {
                damageable = collision.GetComponentInChildren<IDamageable>();
            }
            // 2.5D 깊이 판정 기본값 설정 (장애물/벽일 경우를 대비하여 bounds 사용)
            float targetDepthRadius = collision.bounds.extents.y;
            float targetY = collision.bounds.center.y;
            bool isTargetDamageable = false;

            if (damageable != null)
            {
                isTargetDamageable = true;
                targetDepthRadius = damageable.DepthRadius;
                NetworkObject targetNetObj = damageable.OwnerEntity != null ? damageable.OwnerEntity.NetworkObject : null;
                
                // 자신이 쏜 투사체에 자신이 맞는 것은 무시
                if (targetNetObj != null && targetNetObj.NetworkObjectId == ownerNetworkObjectId)
                {
                    Debug.Log($"[NetProjectile] 팀킬/자해 무시 처리. TargetNetObjId: {targetNetObj.NetworkObjectId}");
                    return;
                }
                
                // 타겟의 기준 위치는 가급적 부모의 Root 위치를 사용
                targetY = targetNetObj != null ? targetNetObj.transform.position.y : collision.transform.position.y;
            }

            // 2.5D 벨트스크롤 깊이(Z축 역할) 판정 (캐싱된 깊이 사용)
            float projectileDepthRadius = statComponent != null ? statComponent.DepthRadius : 0.5f;
            float physicalDepthDiff = Mathf.Abs(cachedDepthY - targetY);
            // 전역 왜곡 배율을 적용하여 시각적 논리 거리로 변환
            float logicalDepthDifference = physicalDepthDiff * GameStatics.DepthScale;
            float allowedTolerance = projectileDepthRadius + targetDepthRadius;

            if (logicalDepthDifference > allowedTolerance)
            {
                // 깊이가 다르면 데미지 대상이든 벽이든 무시하고 관통함
                Debug.Log($"[NetProjectile] 깊이(Y축) 차이가 너무 커서 관통(무시)됨! 차이: {logicalDepthDifference}, 허용치: {allowedTolerance}");
                return;
            }

            if (isTargetDamageable)
            {
                float damageAmount = statComponent != null ? statComponent.AttackPower.Value : 0f;
                Debug.Log($"[NetProjectile] 데미지 적용 대상 발견: {collision.name}, 데미지량: {damageAmount}");
                GameStatics.ApplyDamage(collision.gameObject, (int)damageAmount);
                DestroyProjectile();
            }
            else
            {
                // 벽이나 장애물에 부딪힘 (깊이가 일치하는 경우에만 여기까지 도달함)
                if (!collision.isTrigger)
                {
                    Debug.Log($"[NetProjectile] 깊이가 일치하는 물리 장애물(벽)에 부딪혀 파괴됨: {collision.gameObject.name}");
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
