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

        private int ownerNetworkObjectId = -1;
        private Collider2D projectileCollider;
        private NetStatComponent statComponent;
        
        // [Fix] 투사체가 대각선으로 날아갈 경우를 대비해, 이동 중에도 바닥 그림자(Y)를 동적으로 추산하기 위한 초기 높이(오프셋)
        private float heightOffset;

        protected override void Awake()
        {
            base.Awake();
            projectileCollider = GetComponentInChildren<Collider2D>();
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

            ownerNetworkObjectId = (int)ownerNetObjId;
            heightOffset = transform.position.y - depthY;
            Debug.Log($"[NetProjectile] Initialize 호출됨. OwnerNetObjId: {ownerNetworkObjectId}, 방향: {direction}, 설정된 초기 높이 오프셋: {heightOffset}");
            
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
            if (collision == null)
            {
                return;
            }
            
            Debug.Log($"[NetProjectile] 투사체 충돌 감지: {collision.gameObject.name}");
            // 충돌 판정은 오직 서버에서만 처리합니다.
            if (!GameStatics.IsServerAuthorized)
            {
                return;
            }

            // 충돌 대상이 데미지를 받을 수 있는 객체인지 O(1) 룩업 확인
            GameObject rootObj = collision.transform.root.gameObject;
            bool isTargetDamageable = GameStatics.TryGetDamageable(rootObj, out IDamageable damageable);

            // 2.5D 깊이 판정 기준 물리적 두께
            float targetDepthThickness = collision.bounds.extents.y;
            float targetY = collision.bounds.center.y;

            if (isTargetDamageable)
            {
                NetworkObject targetNetObj = damageable.OwnerEntity != null ? damageable.OwnerEntity.NetworkObject : null;
                
                // 자신이 쏜 투사체에 자신이 맞는 것은 무시
                if (targetNetObj != null && targetNetObj.NetworkObjectId == (ulong)ownerNetworkObjectId)
                {
                    Debug.Log($"[NetProjectile] 팀킬/자해 무시 처리. TargetNetObjId: {targetNetObj.NetworkObjectId}");
                    return;
                }
                
                // 타겟의 기준 위치는 가급적 부모의 Root 위치를 사용
                targetY = targetNetObj != null ? targetNetObj.transform.position.y : rootObj.transform.position.y;
            }

            // 2.5D 벨트스크롤 깊이(Z축 역할) 판정
            // 현재 높이 오프셋을 역산하여 타격 시점의 발밑 그림자 깊이(Y)를 구함
            float currentDepthY = transform.position.y - heightOffset;
            
            float projectileDepthThickness = projectileCollider != null ? projectileCollider.bounds.extents.y : 0.5f;
            
            float physicalDepthDiff = Mathf.Abs(currentDepthY - targetY);
            
            // 전역 왜곡 배율(DepthScale) 곱셈을 생략하여 부동소수점 연산 최적화
            float allowedTolerance = projectileDepthThickness + targetDepthThickness;

            if (physicalDepthDiff > allowedTolerance)
            {
                Debug.Log($"[NetProjectile] 깊이(Z축) 차이가 너무 커서 빗나감! 차이: {physicalDepthDiff}, 허용치: {allowedTolerance}");
                return;
            }

            if (isTargetDamageable)
            {
                float damageAmount = statComponent != null ? statComponent.AttackPower.Value : 0f;
                Debug.Log($"[NetProjectile] 데미지 적용 대상 발견: {rootObj.name}, 데미지량: {damageAmount}");
                GameStatics.ApplyDamage(damageable, (int)damageAmount);
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
            // Awake에서 캐싱된 콜라이더 사용 (풀링 최적화)
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
