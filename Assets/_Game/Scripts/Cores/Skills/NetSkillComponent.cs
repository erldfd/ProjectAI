using UnityEngine;
using Unity.Netcode;
using ProjectAI.Core;

namespace ProjectAI.Core.Skills
{
    /// <summary>
    /// 캐릭터의 스킬(마법탄 발사 등) 입력을 처리하고 서버로 RPC를 보내는 범용 컴포넌트입니다.
    /// </summary>
    [UnityEngine.Scripting.APIUpdating.MovedFrom(true, "ProjectAI.Players", "Assembly-CSharp", "NetPlayerCombat")]
    public class NetSkillComponent : NetworkBehaviour
    {
        [Header("Combat Settings")]
        [Tooltip("마법탄 프리팹 (서버에만 있어도 되지만, 클라이언트 시뮬레이션을 위해 공용)")]
        [SerializeField]
        private GameObject projectilePrefab;

        [Tooltip("마법탄이 발사될 위치 (Muzzle)")]
        [SerializeField]
        private Transform firePoint;

        [Tooltip("공격 쿨타임 (초)")]
        [SerializeField]
        private float attackCooldown = 0.5f;

        private float lastAttackTime = -999f;
        private float serverLastAttackTime = -999f;
        private ProjectAI.Movements.ANetMovement entityMovement;

        private void Awake()
        {
            entityMovement = GetComponentInChildren<ProjectAI.Movements.ANetMovement>();
            UnityEngine.Assertions.Assert.IsNotNull(entityMovement, "ANetMovement is missing for SkillComponent.");
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
        }

        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();
        }

        public void SetAttackInput(bool isAttacking)
        {
            if (!isAttacking)
            {
                return;
            }

            if (Time.time < lastAttackTime + attackCooldown)
            {
                return; // 쿨타임 대기 중
            }

            lastAttackTime = Time.time;

            // 현재 캐릭터가 바라보는 좌/우 방향 확인
            bool isFacingRight = true;
            if (entityMovement != null)
            {
                isFacingRight = entityMovement.NetIsFacingRight.Value;
            }

            Vector2 direction = isFacingRight ? Vector2.right : Vector2.left;

            RequestFireServerRpc(direction);
        }

        [Rpc(SendTo.Server)]
        private void RequestFireServerRpc(Vector2 direction)
        {
            if (Time.time < serverLastAttackTime + attackCooldown)
            {
                Debug.LogWarning("[NetSkillComponent] RPC Attack too fast. Ignoring.");
                return;
            }
            serverLastAttackTime = Time.time;

            if (projectilePrefab == null)
            {
                Debug.LogWarning("[NetSkillComponent] Projectile Prefab is missing!");
                return;
            }

            // 서버 권한으로 투사체 스폰 (클라이언트 조작 불가하게 서버에서 위치 직접 결정)
            Vector2 origin = firePoint != null ? (Vector2)firePoint.position : (Vector2)transform.position;
            GameObject projectileObj = Instantiate(projectilePrefab, origin, Quaternion.identity);
            
            // 발사 방향으로 회전 적용
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            projectileObj.transform.rotation = Quaternion.Euler(0f, 0f, angle);

            if (projectileObj.TryGetComponent(out NetworkObject netObj))
            {
                // 소유권 없이(서버 소유) 스폰.
                // 투사체는 서버가 직접 물리/충돌 연산을 담당함.
                netObj.Spawn();
            }

            // 투사체의 이동 컴포넌트를 초기화
            if (projectileObj.TryGetComponent(out ProjectAI.Projectiles.NetProjectile projectile))
            {
                projectile.Initialize(direction, base.OwnerClientId);
            }
        }
    }
}
