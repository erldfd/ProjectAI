using UnityEngine.Assertions;
using UnityEngine;
using Unity.Netcode;
using ProjectAI.Core.Pooling;
using ProjectAI.Movements;
using ProjectAI.Projectiles;
using ProjectAI.Characters;
using ProjectAI.Core;

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

            Assert.IsNotNull(projectilePrefab, "[BasicAttackLogic] Initialize: projectilePrefab이 누락되었습니다.");
        }

        public bool CanExecute(NetCharacter caster)
        {
            // 침묵, 기절, 시전 중 상태면 사용 불가
            if (caster.SkillComponent.HasState(EStateTag.Silenced) || caster.SkillComponent.HasState(EStateTag.Stunned) || caster.SkillComponent.HasState(EStateTag.Casting))
            {
                Debug.Log($"[BasicAttackLogic] CanExecute 실패: {caster.NetworkObjectId} 상태 이상(침묵/기절/시전중)");
                return false;
            }

            // 쿨타임 검사 (간단 구현)
            Assert.IsNotNull(GameStatics.NetworkManager, "[BasicAttackLogic] CanExecute: NetworkManager is null.");
            
            if (GameStatics.NetworkManager.ServerTime.Time < caster.SkillComponent.GetServerActivationTime(SkillType) + cooldown)
            {
                Debug.Log($"[BasicAttackLogic] CanExecute 실패: {caster.NetworkObjectId} 서버 쿨타임 대기 중");
                return false;
            }

            return true;
        }

        public void Execute(NetCharacter caster)
        {
            Assert.IsTrue(GameStatics.IsServerAuthorized, "[BasicAttackLogic] Execute는 서버에서만 호출되어야 합니다.");
            
            if (!GameStatics.IsServerAuthorized)
            {
                return;
            }

            // 서버 측 쿨타임 기록 (발동 시점)
            caster.SkillComponent.SetServerActivationTime(SkillType, GameStatics.NetworkManager.ServerTime.Time);

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
            Assert.IsTrue(GameStatics.IsServerAuthorized, "[BasicAttackLogic] Action은 서버에서만 호출되어야 합니다.");
            
            if (!GameStatics.IsServerAuthorized)
            {
                return;
            }

            // 실제 키프레임 도달 시 호출되는 투사체 스폰 로직
            Assert.IsNotNull(projectilePrefab, "[BasicAttackLogic] Projectile Prefab is missing in SkillManager!");

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

            Assert.IsNotNull(GameStatics.ObjectPool, "[BasicAttackLogic] GameStatics.ObjectPool이 등록되어 있지 않습니다!");

            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            Quaternion rotation = Quaternion.Euler(0f, 0f, angle);

            NetworkObject projectileNetObj = GameStatics.ObjectPool.GetNetworkObject(projectilePrefab, origin, rotation);
            if (projectileNetObj == null)
            {
                Debug.LogWarning($"[BasicAttackLogic] Action 실패: 투사체를 풀에서 가져오지 못했습니다. (CasterID: {caster.NetworkObjectId})");
                return;
            }

            projectileNetObj.Spawn();

            if (!projectileNetObj.TryGetComponent(out NetProjectile projectile))
            {
                Debug.LogWarning($"[BasicAttackLogic] 발사체 스폰 실패: NetProjectile 컴포넌트를 찾을 수 없습니다. (CasterID: {caster.NetworkObjectId})");
                return;
            }

            Debug.Log($"[BasicAttackLogic] 발사체 초기화 완료. 방향: {direction}, CasterID: {caster.NetworkObjectId}");
            projectile.Initialize(direction, caster.NetworkObjectId);
        }

        public void End(NetCharacter caster)
        {
            // 현재 종료 시점 특이사항 없음
        }
    }
}
