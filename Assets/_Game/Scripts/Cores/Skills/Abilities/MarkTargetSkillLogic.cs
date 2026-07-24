using UnityEngine.Assertions;
using UnityEngine;
using Unity.Netcode;
using ProjectAI.Characters;
using ProjectAI.Core;
using ProjectAI.SOs;

namespace ProjectAI.Core.Skills.Abilities
{
    /// <summary>
    /// 바라보는 방향 전방으로 박스 형태의 레이캐스트를 쏴 가장 가까운 적을 색출하고,
    /// 활성화된 모든 소환수에게 일점사 마킹을 지시하는 스킬 로직입니다.
    /// </summary>
    public class MarkTargetSkillLogic : ISkillLogic
    {
        public ESkillType SkillType => ESkillType.MarkTarget;

        private const int MAX_HIT_RESULTS = 50;
        private const float DEFAULT_CAST_DISTANCE = 15f;
        private readonly Vector2 DEFAULT_BOX_SIZE = new Vector2(3f, 3f);

        private RaycastHit2D[] hitBuffer = new RaycastHit2D[MAX_HIT_RESULTS];

        public void Initialize(SkillManager manager)
        {
        }

        public bool CanExecute(NetCharacter caster, BaseSkillConfig config)
        {
            if (caster.HasState(EStateTag.Silenced) || caster.HasState(EStateTag.Stunned))
            {
                return false;
            }

            Assert.IsNotNull(GameStatics.NetworkManager, "[MarkTargetSkillLogic] NetworkManager is null.");
            
            if (GameStatics.NetworkManager.ServerTime.Time < caster.SkillComponent.GetServerActivationTime(config.SkillId) + config.BaseCooldown)
            {
                return false;
            }

            return true;
        }

        public void Execute(NetCharacter caster, BaseSkillConfig config)
        {
            Assert.IsTrue(GameStatics.IsServerAuthorized, "[MarkTargetSkillLogic] Execute는 서버에서만 호출되어야 합니다.");
            
            if (!GameStatics.IsServerAuthorized)
            {
                return;
            }

            caster.SkillComponent.SetServerActivationTime(config.SkillId, GameStatics.NetworkManager.ServerTime.Time);

            int animHash = caster.SkillComponent.GetSkillAnimHash(config.SkillId);
            if (animHash != 0)
            {
                caster.SkillComponent.BroadcastPlayAnimationClientRpc(animHash, 0f);
            }
        }

        public void Action(NetCharacter caster, BaseSkillConfig config)
        {
            if (!GameStatics.IsServerAuthorized)
            {
                Debug.LogWarning("[MarkTargetSkillLogic] 서버 전용 스킬입니다. 권한이 없어 취소합니다.");
                return;
            }

            if (caster == null)
            {
                Debug.LogWarning("[MarkTargetSkillLogic] Caster가 null입니다. 스킬 실행을 취소합니다.");
                return;
            }

            // 플레이어가 바라보는 방향 기반
            Vector2 aimDir = Vector2.right;
            Assert.IsNotNull(caster.Movement, "[MarkTargetSkillLogic] Caster의 Movement 컴포넌트가 없습니다.");
            if (!caster.Movement.NetIsFacingRight.Value)
            {
                aimDir = Vector2.left;
            }

            float castDistance = DEFAULT_CAST_DISTANCE;
            Vector2 boxSize = DEFAULT_BOX_SIZE;
            LayerMask targetLayer = Physics2D.AllLayers;
            
            if (config is MarkTargetSkillConfig markConfig)
            {
                castDistance = markConfig.CastDistance;
                boxSize = markConfig.BoxSize;
                targetLayer = markConfig.TargetLayer;
            }
            
            // 전방 BoxCast로 적 색출 (가비지 프리 탐색 방식 적용)
            ContactFilter2D filter = new ContactFilter2D();
            filter.useTriggers = false; 
            filter.useLayerMask = true;
            filter.layerMask = targetLayer;

            int hitCount = Physics2D.BoxCast(caster.transform.position, boxSize, 0f, aimDir, filter, hitBuffer, castDistance);
            
            Transform foundTarget = null;
            float closestDist = float.MaxValue;

            for (int i = 0; i < hitCount; i++)
            {
                RaycastHit2D hit = hitBuffer[i];
                if (hit.collider == null || hit.collider.gameObject == caster.gameObject)
                {
                    continue;
                }

                if (hit.collider.CompareTag(ObjectTags.ENEMY))
                {
                    float dist = Vector2.Distance(caster.transform.position, hit.collider.transform.position);
                    if (dist < closestDist)
                    {
                        closestDist = dist;
                        foundTarget = hit.transform;
                    }
                }
            }

            if (foundTarget != null)
            {
                Debug.Log($"[MarkTargetSkillLogic] 타겟 마킹 성공: {foundTarget.name}");
                if (caster is NetPlayerCharacter playerCaster)
                {
                    Assert.IsNotNull(playerCaster.SummonController, "[MarkTargetSkillLogic] SummonController가 없습니다.");
                    if (playerCaster.SummonController != null)
                    {
                        playerCaster.SummonController.SetPriorityTarget(foundTarget);
                    }
                }
                else
                {
                    Debug.LogWarning("[MarkTargetSkillLogic] 캐스터가 NetPlayerCharacter가 아닙니다.");
                }
            }
        }

        public void End(NetCharacter caster, BaseSkillConfig config)
        {
        }
    }
}
