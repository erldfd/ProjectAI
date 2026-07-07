using UnityEngine.Assertions;
using UnityEngine;
using Unity.Netcode;
using ProjectAI.Core.Pooling;
using ProjectAI.Characters;
using ProjectAI.Characters.MonsterAI;
using ProjectAI.Core;
using ProjectAI.SOs;
using ProjectAI.Characters.Summons;

namespace ProjectAI.Core.Skills.Abilities
{
    /// <summary>
    /// 소환수 소환 스킬의 실행 로직을 처리하는 클래스입니다.
    /// </summary>
    public class SummonSkillLogic : ISkillLogic
    {
        public ESkillType SkillType => ESkillType.Summon;

        public void Initialize(SkillManager manager)
        {
            // Stateless
        }

        public bool CanExecute(NetCharacter caster, BaseSkillConfig config)
        {
            Debug.Log($"[SummonSkillLogic] CanExecute 호출: CasterID={caster.NetworkObjectId}, SkillID={config.SkillId}");
            if (caster.SkillComponent.HasState(EStateTag.Silenced) || caster.SkillComponent.HasState(EStateTag.Stunned) || caster.SkillComponent.HasState(EStateTag.Casting))
            {
                return false;
            }

            Assert.IsNotNull(GameStatics.NetworkManager, "[SummonSkillLogic] CanExecute: NetworkManager is null.");
            if (GameStatics.NetworkManager == null)
            {
                return false;
            }
            
            Debug.Log($"[SummonSkillLogic] ServerTime={GameStatics.NetworkManager.ServerTime.Time}, LastActivation={caster.SkillComponent.GetServerActivationTime(config.SkillId)}, Cooldown={config.BaseCooldown}");
            if (GameStatics.NetworkManager.ServerTime.Time < caster.SkillComponent.GetServerActivationTime(config.SkillId) + config.BaseCooldown)
            {
                return false;
            }

            return true;
        }

        public void Execute(NetCharacter caster, BaseSkillConfig config)
        {
            Assert.IsTrue(GameStatics.IsServerAuthorized, "[SummonSkillLogic] Execute는 서버에서만 호출되어야 합니다.");
            
            if (!GameStatics.IsServerAuthorized || GameStatics.NetworkManager == null)
            {
                return;
            }

            caster.SkillComponent.SetServerActivationTime(config.SkillId, GameStatics.NetworkManager.ServerTime.Time);

            int animHash = caster.SkillComponent.GetSkillAnimHash(config.SkillId);
            if (animHash == 0)
            {
                Debug.LogWarning($"[SummonSkillLogic] Execute 실패: ID {config.SkillId} 에 해당하는 애니메이션 해시를 찾을 수 없습니다. (CasterID: {caster.NetworkObjectId})");
                return;
            }

            caster.SkillComponent.BroadcastPlayAnimationClientRpc(animHash, 0f);
        }

        public void Action(NetCharacter caster, BaseSkillConfig config)
        {
            Assert.IsTrue(GameStatics.IsServerAuthorized, "[SummonSkillLogic] Action은 서버에서만 호출되어야 합니다.");
            
            if (!GameStatics.IsServerAuthorized)
            {
                return;
            }

            NetworkObject summonPrefab = config.Prefab;
            Assert.IsNotNull(summonPrefab, "[SummonSkillLogic] Summon Prefab is missing!");

            float duration = 10f;
            float followDistance = 2f;
            if (config is SummonSkillConfig summonConfig)
            {
                duration = summonConfig.Duration;
                followDistance = summonConfig.FollowDistance;
            }

            // 플레이어 주변(약간 오프셋)에 스폰
            Vector2 spawnPos = (Vector2)caster.transform.position + new Vector2(1f, 0f);

            NetworkObject summonNetObj = GameStatics.ObjectPool.GetNetworkObject(summonPrefab, spawnPos, Quaternion.identity);
            if (summonNetObj == null)
            {
                Debug.LogWarning($"[SummonSkillLogic] 소환수를 풀에서 가져오지 못했습니다. (CasterID: {caster.NetworkObjectId})");
                return;
            }

            // NetServerMovement가 서버 권한으로 이동(속도/애니메이션)을 제어하므로, 서버가 Owner여야 합니다.
            summonNetObj.Spawn();

            // 두뇌 및 AI 상태 설정
            if (summonNetObj.TryGetComponent(out NetSummonBrain brain))
            {
                brain.Owner = caster.transform;
                SummonFollowState followState = brain.GetComponentInChildren<SummonFollowState>(true);
                if (followState != null)
                {
                    followState.SetFollowDistance(followDistance);
                }
            }

            // 디스폰 타이머 세팅: 소환자(캐스터)의 컨트롤러에 위임
            if (caster is NetPlayerCharacter playerCaster && playerCaster.SummonController != null)
            {
                playerCaster.SummonController.AddSummon(summonNetObj, duration);
            }
            else
            {
                Debug.LogWarning($"[SummonSkillLogic] 캐스터({caster.NetworkObjectId})가 NetPlayerCharacter가 아니거나 SummonController가 없어 지속 시간이 관리되지 않습니다.");
            }
        }

        public void End(NetCharacter caster, BaseSkillConfig config)
        {
            // 특이사항 없음
        }
    }
}
