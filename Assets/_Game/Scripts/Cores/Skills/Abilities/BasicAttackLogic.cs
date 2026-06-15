using UnityEngine.Assertions;
using UnityEngine;
using Unity.Netcode;
using ProjectAI.Core.Pooling;
using ProjectAI.Movements;
using ProjectAI.Projectiles;
using ProjectAI.Characters;
using ProjectAI.Core;
using System.Collections.Generic;
using ProjectAI.Core.Stats;
using ProjectAI.SOs;

namespace ProjectAI.Core.Skills.Abilities
{
    /// <summary>
    /// 기본 근접 평타 스킬의 쿨타임 검증 및 동작 로직을 구현하는 클래스입니다.
    /// </summary>
    public class BasicAttackLogic : ISkillLogic
    {
        public ESkillType SkillType => ESkillType.BasicAttack;

        // GC(가비지 컬렉션) 발생을 막기 위해 타격 판정용 메모리를 정적(Static)으로 캐싱하여 재사용
        private static readonly ContactFilter2D filter = new ContactFilter2D { useTriggers = true };
        private static readonly Collider2D[] results = new Collider2D[20];
        private static readonly HashSet<IDamageable> hitTargets = new HashSet<IDamageable>();

        public void Initialize(SkillManager manager)
        {
            // Stateless로 변경되어 단일 캐싱 불필요
        }

        public bool CanExecute(NetCharacter caster, BaseSkillConfig config)
        {
            // 침묵, 기절, 시전 중 상태면 사용 불가
            if (caster.SkillComponent.HasState(EStateTag.Silenced) || caster.SkillComponent.HasState(EStateTag.Stunned) || caster.SkillComponent.HasState(EStateTag.Casting))
            {
                Debug.Log($"[BasicAttackLogic] CanExecute 실패: {caster.NetworkObjectId} 상태 이상(침묵/기절/시전중)");
                return false;
            }

            // 쿨타임 검사 (간단 구현)
            Assert.IsNotNull(GameStatics.NetworkManager, "[BasicAttackLogic] CanExecute: NetworkManager is null.");
            
            if (GameStatics.NetworkManager.ServerTime.Time < caster.SkillComponent.GetServerActivationTime(config.SkillId) + config.BaseCooldown)
            {
                Debug.Log($"[BasicAttackLogic] CanExecute 실패: {caster.NetworkObjectId} 서버 쿨타임 대기 중");
                return false;
            }

            return true;
        }

        public void Execute(NetCharacter caster, BaseSkillConfig config)
        {
            Assert.IsTrue(GameStatics.IsServerAuthorized, "[BasicAttackLogic] Execute는 서버에서만 호출되어야 합니다.");
            
            if (!GameStatics.IsServerAuthorized)
            {
                return;
            }

            // 서버 측 쿨타임 기록 (발동 시점)
            caster.SkillComponent.SetServerActivationTime(config.SkillId, GameStatics.NetworkManager.ServerTime.Time);

            // 애니메이션 재생만 지시
            int animHash = caster.SkillComponent.GetSkillAnimHash(config.SkillId);
            if (animHash == 0)
            {
                Debug.LogWarning($"[BasicAttackLogic] Execute 실패: ID {config.SkillId} 에 해당하는 애니메이션 해시를 찾을 수 없습니다. (CasterID: {caster.NetworkObjectId})");
                return;
            }

            caster.SkillComponent.BroadcastPlayAnimationClientRpc(animHash, 0f);
        }

        public void Action(NetCharacter caster, BaseSkillConfig config)
        {
            Assert.IsTrue(GameStatics.IsServerAuthorized, "[BasicAttackLogic] Action은 서버에서만 호출되어야 합니다.");
            Debug.Log($"[BasicAttackLogic] Action 호출: CasterID {caster.NetworkObjectId}, SkillId {config.SkillId}");
            if (!GameStatics.IsServerAuthorized)
            {
                return;
            }

            Collider2D hitbox = caster.SkillComponent.MeleeHitbox;
            Assert.IsNotNull(hitbox, $"[BasicAttackLogic] Action 실패: {caster.NetworkObjectId}에 MeleeHitbox가 설정되지 않았습니다.");

            // [Hitbox 사용 규칙 가이드]
            // 1. 게임오브젝트 상태: 반드시 켜져 있어야 함 (SetActive(true))
            // 2. 컴포넌트 상태: 꺼둬도 무방함 (Collider2D.enabled = false). OverlapCollider는 꺼진 컴포넌트의 모양도 가져와 검사 검사가능.
            // 3. isTrigger 상태: 무관하나, 만약을 대비해 켜두는 것(true)을 권장.
            
            // 캐릭터 스탯에서 공격력 가져오기
            int attackPower = 10; // Default
            if (caster.StatComponent != null)
            {
                attackPower = caster.StatComponent.AttackPower.Value;
            }

            // 캐싱된 배열을 사용하여 메모리 할당(GC) 0 달성
            int count = Physics2D.OverlapCollider(hitbox, filter, results);
            Debug.Log(hitbox.transform.position);
            Debug.Log(hitbox.bounds.center);

            // 한 번의 휘두르기에 한 타겟이 여러 콜라이더(머리, 몸통)로 중복 피격되는 것을 방지
            hitTargets.Clear();
            Debug.Log($"[BasicAttackLogic] Action: {count}개의 콜라이더와 충돌 감지 (CasterID: {caster.NetworkObjectId})");
            for (int i = 0; i < count; i++)
            {
                Collider2D col = results[i];

                IDamageable damageable = col.GetComponentInParent<IDamageable>() ?? col.GetComponentInChildren<IDamageable>();
                if (damageable == null)
                {
                    Debug.Log($"[BasicAttackLogic] Action: 피격 대상이지만 IDamageable 컴포넌트가 없습니다. 오브젝트: {col.gameObject.name}");
                    continue;
                }

                NetworkObject targetNetObj = col.GetComponentInParent<NetworkObject>();
                Debug.Log($"[BasicAttackLogic] Action: 타격 대상 발견 - {col.gameObject.name} (NetworkObjectId: {targetNetObj?.NetworkObjectId.ToString() ?? "N/A"})");
                // 자신은 타격에서 제외
                if (targetNetObj != null && targetNetObj.NetworkObjectId == caster.NetworkObjectId)
                {
                    continue;
                }

                Debug.Log($"[BasicAttackLogic] Action: 타격 대상이 IDamageable을 구현했습니다. 오브젝트: {col.gameObject.name}");

                // 이미 타격한 대상 제외
                if (hitTargets.Contains(damageable))
                {
                    continue;
                }

                Debug.Log($"[BasicAttackLogic] Action: 타격 대상이 새로 추가되었습니다. 오브젝트: {col.gameObject.name}");

                hitTargets.Add(damageable);
                
                GameObject targetObj = ((Component)damageable).gameObject;
                Debug.Log($"[BasicAttackLogic] 근접 평타 적중! 타겟: {targetObj.name}, 데미지: {attackPower}");
                GameStatics.ApplyDamage(targetObj, attackPower);
            }
        }

        public void End(NetCharacter caster, BaseSkillConfig config)
        {
            // 현재 종료 시점 특이사항 없음
        }
    }
}
