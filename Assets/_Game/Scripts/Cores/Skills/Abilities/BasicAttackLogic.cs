using UnityEngine;
using Unity.Netcode;
using ProjectAI.Core.Pooling;
using ProjectAI.Movements;
using ProjectAI.Projectiles;
using ProjectAI.Characters;

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

        public bool CanExecute(NetCharacter caster)
        {
            // 침묵, 기절 상태면 사용 불가
            if (caster.SkillComponent.HasState(EStateTag.Silenced) || caster.SkillComponent.HasState(EStateTag.Stunned))
            {
                return false;
            }

            // 쿨타임 검사 (간단 구현)
            if (NetworkManager.Singleton != null)
            {
                if (NetworkManager.Singleton.ServerTime.Time < caster.SkillComponent.GetLastActivationTime(SkillType) + cooldown)
                {
                    return false;
                }
            }

            return true;
        }

        public void Execute(NetCharacter caster)
        {
            if (NetworkManager.Singleton != null && !NetworkManager.Singleton.IsServer)
            {
                return;
            }

            // 애니메이션 재생만 지시
            int animHash = caster.SkillComponent.GetSkillAnimHash(SkillType);
            if (animHash == 0)
            {
                Debug.LogWarning($"[BasicAttackLogic] Execute 실패: {SkillType} 에 해당하는 애니메이션 해시를 찾을 수 없습니다. (CasterID: {caster.NetworkObjectId})");
                return;
            }

            caster.SkillComponent.BroadcastPlayAnimationClientRpc(animHash, 0f);
        }

        public void Action(NetCharacter caster)
        {
            if (NetworkManager.Singleton != null && !NetworkManager.Singleton.IsServer)
            {
                return;
            }

            // 실제 키프레임 도달 시 호출되는 투사체 스폰 로직
            UnityEngine.Assertions.Assert.IsNotNull(projectilePrefab, "[BasicAttackLogic] Projectile Prefab is missing in SkillManager!");

            Vector2 direction = Vector2.right;
            if (caster.Movement != null)
            {
                direction = caster.Movement.NetIsFacingRight.Value ? Vector2.right : Vector2.left;
            }

            Vector2 origin = (Vector2)caster.transform.position;
            if (caster.SkillComponent.FirePoint != null)
            {
                origin = caster.SkillComponent.FirePoint.position;
            }

            UnityEngine.Assertions.Assert.IsNotNull(GameStatics.ObjectPool, "[BasicAttackLogic] GameStatics.ObjectPool이 등록되어 있지 않습니다!");

            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            Quaternion rotation = Quaternion.Euler(0f, 0f, angle);

            NetworkObject projectileNetObj = GameStatics.ObjectPool.GetNetworkObject(projectilePrefab, origin, rotation);
            if (projectileNetObj == null)
            {
                Debug.LogWarning($"[BasicAttackLogic] Action 실패: 투사체를 풀에서 가져오지 못했습니다. (CasterID: {caster.NetworkObjectId})");
                return;
            }

            projectileNetObj.Spawn();

            if (projectileNetObj.TryGetComponent(out NetProjectile projectile))
            {
                Debug.Log($"[BasicAttackLogic] 발사체 초기화 완료. 방향: {direction}, CasterID: {caster.NetworkObjectId}");
                projectile.Initialize(direction, caster.NetworkObjectId);
            }
        }

        public void End(NetCharacter caster)
        {
            if (NetworkManager.Singleton != null && !NetworkManager.Singleton.IsServer)
            {
                return;
            }

            // 스킬 종료(또는 취소) 시점에 쿨타임을 세팅하여, 종료 시점부터 쿨타임이 돌도록 함.
            if (NetworkManager.Singleton != null)
            {
                caster.SkillComponent.SetLastActivationTime(SkillType, NetworkManager.Singleton.ServerTime.Time);
            }
        }
    }
}
