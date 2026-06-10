using UnityEngine;
using Unity.Netcode;
using ProjectAI.Core.Pooling;

namespace ProjectAI.Core.Skills.Abilities
{
    /// <summary>
    /// 기본 공격(마법탄 발사) 스킬의 쿨타임 검증 및 투사체 풀링 스폰 로직을 구현하는 클래스입니다.
    /// </summary>
    public class BasicAttackLogic : ISkillLogic
    {
        public ESkillType SkillType => ESkillType.BasicAttack;

        private NetworkObject projectilePrefab;
        private double cooldown;

        public void Initialize(SkillManager manager)
        {
            SSkillConfig config = manager.GetConfig(SkillType);
            projectilePrefab = config.Prefab;
            cooldown = config.BaseCooldown;

            UnityEngine.Assertions.Assert.IsNotNull(projectilePrefab, "[BasicAttackLogic] Initialize: projectilePrefab이 누락되었습니다.");
        }

        public bool CanExecute(NetSkillComponent caster)
        {
            // 침묵, 기절 상태면 사용 불가
            if (caster.HasState(EStateTag.Silenced) || caster.HasState(EStateTag.Stunned))
            {
                return false;
            }

            // 쿨타임 검사 (간단 구현)
            if (NetworkManager.Singleton != null && NetworkManager.Singleton.ServerTime.Time < caster.GetLastActivationTime(SkillType) + cooldown)
            {
                return false;
            }

            return true;
        }

        public void Execute(NetSkillComponent caster)
        {
            if (NetworkManager.Singleton != null)
            {
                caster.SetLastActivationTime(SkillType, NetworkManager.Singleton.ServerTime.Time);
            }

            UnityEngine.Assertions.Assert.IsNotNull(projectilePrefab, "[BasicAttackLogic] Projectile Prefab is missing in SkillManager!");

            // 시전자의 바라보는 방향 가져오기
            Vector2 direction = Vector2.right; // 기본값
            ProjectAI.Movements.ANetMovement movement = caster.GetComponentInChildren<ProjectAI.Movements.ANetMovement>();
            if (movement != null)
            {
                direction = movement.NetIsFacingRight.Value ? Vector2.right : Vector2.left;
            }

            // 발사 지점 (Muzzle) 결정
            Vector2 origin = (Vector2)caster.transform.position;
            // TODO: 추후 caster 내부에 발사 위치(firePoint)를 지정할 수 있는 컴포넌트 추가

            // 투사체 스폰 (서버 전용)
            UnityEngine.Assertions.Assert.IsNotNull(projectilePrefab, "[BasicAttackLogic] projectilePrefab에 NetworkObject가 누락되었습니다.");

            UnityEngine.Assertions.Assert.IsNotNull(GameStatics.ObjectPool, "[BasicAttackLogic] GameStatics.ObjectPool이 등록되어 있지 않습니다!");

            GameStatics.ObjectPool.SetupPool(projectilePrefab, 10, true);

            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            Quaternion rotation = Quaternion.Euler(0f, 0f, angle);

            NetworkObject projectileNetObj = GameStatics.ObjectPool.GetNetworkObject(projectilePrefab, origin, rotation);

            if (projectileNetObj != null)
            {
                projectileNetObj.Spawn();

                if (projectileNetObj.TryGetComponent(out ProjectAI.Projectiles.NetProjectile projectile))
                {
                    projectile.Initialize(direction, caster.OwnerClientId);
                }
            }
        }
    }
}
