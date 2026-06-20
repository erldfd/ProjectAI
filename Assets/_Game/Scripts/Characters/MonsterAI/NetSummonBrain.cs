using Unity.Netcode;
using UnityEngine;
using UnityEngine.Assertions;
using ProjectAI.Core;
using ProjectAI.Core.Attributes;
using ProjectAI.Movements;
using ProjectAI.Core.Skills;
using ProjectAI.Characters;
using ProjectAI.SOs;

namespace ProjectAI.Characters.MonsterAI
{
    /// <summary>
    /// 몬스터의 FSM(상태 기계)을 돌리며 판단을 내리고 몸체(NetCharacter)를 조종하는 두뇌 클래스입니다.
    /// 모든 연산은 서버(Host)에서만 구동됩니다.
    /// </summary>
    [RequireComponent(typeof(NetCharacter))]
    public class NetSummonBrain : NetMonsterBrain
    {
        private const float DEFENSIVE_DETECT_RADIUS_MULTIPLIER = 0.25f;

        /// <summary>
        /// 소환수일 경우 주인이 스킬로 지정해준 타겟
        /// </summary>
        public Transform PriorityTarget { get; set; }
        [Tooltip("소환수인 경우 주인(Owner) 할당")]
        public Transform Owner { get; set; }

        [Tooltip("소환수가 주인을 벗어날 수 있는 최대 거리 (테더링 반경)")]
        [SerializeField]
        private float tetherRadius = 15f;

        public float TetherRadius => tetherRadius;

        [Tooltip("최우선 타겟(마킹) 최대 추적 거리 배수")]
        [SerializeField]
        private float priorityChaseMultiplier = 3f;

        private ESummonStance currentStance = ESummonStance.Aggressive;

        public void SetStance(ESummonStance stance)
        {
            currentStance = stance;
            Debug.Log($"[NetSummonBrain] 태세 변경됨: {currentStance}");
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            currentStance = ESummonStance.Aggressive;
            PriorityTarget = null;
        }

        protected override void UpdateSensors()
        {
            Assert.IsTrue(GameStatics.IsServerAuthorized, "[NetSummonBrain] UpdateSensors는 서버에서만 호출되어야 합니다.");

            // 호위 태세 페널티 적용
            float effectiveDetectRadius = (currentStance == ESummonStance.Defensive) 
                ? currentDetectRadius * DEFENSIVE_DETECT_RADIUS_MULTIPLIER 
                : currentDetectRadius;

            float effectiveTetherRadius = (currentStance == ESummonStance.Defensive)
                ? tetherRadius * DEFENSIVE_DETECT_RADIUS_MULTIPLIER
                : tetherRadius;

            float maxReach = effectiveTetherRadius + attackRadius;
            float sqrMaxReach = maxReach * maxReach;

            if (PriorityTarget != null && !PriorityTarget.gameObject.activeInHierarchy)
            {
                PriorityTarget = null;
            }

            if (Target != null && !Target.gameObject.activeInHierarchy)
            {
                Target = null;
            }

            // 타겟 기반 테더링 검사: 타겟이 한계선을 벗어났는지 확인
            if (Owner != null)
            {
                if (Target != null)
                {
                    Vector2 diff = (Vector2)Owner.position - (Vector2)Target.position;
                    float sqrDistTargetToOwner = GameStatics.GetPerspectiveSqrMagnitude(diff);
                    if (sqrDistTargetToOwner > sqrMaxReach)
                    {
                        Debug.Log("[NetSummonBrain] 타겟이 테더 범위를 벗어나 포기합니다.");
                        Target = null;
                    }
                }

                if (PriorityTarget != null)
                {
                    Vector2 diff = (Vector2)Owner.position - (Vector2)PriorityTarget.position;
                    float sqrDistPriorityToOwner = GameStatics.GetPerspectiveSqrMagnitude(diff);
                    if (sqrDistPriorityToOwner > sqrMaxReach)
                    {
                        PriorityTarget = null;
                    }
                }
            }

            if (PriorityTarget != null)
            {
                Vector2 diff = (Vector2)transform.position - (Vector2)PriorityTarget.position;
                float sqrDist = GameStatics.GetPerspectiveSqrMagnitude(diff);
                float priorityThreshold = effectiveDetectRadius * priorityChaseMultiplier;
                
                if (sqrDist > priorityThreshold * priorityThreshold)
                {
                    PriorityTarget = null;
                }
                else
                {
                    Target = PriorityTarget;
                    return;
                }
            }

            if (Target != null)
            {
                Vector2 diff = (Vector2)transform.position - (Vector2)Target.position;
                float sqrDist = GameStatics.GetPerspectiveSqrMagnitude(diff);
                float threshold = effectiveDetectRadius * LOST_TARGET_MULTIPLIER;
                if (sqrDist > threshold * threshold) // 탐지 거리 밖으로 벗어남
                {
                    Target = null;
                }
            }

            if (Target != null)
            {
                return;
            }

            // ContactFilter2D를 이용한 최신 표준 탐색 API
            int count = Physics2D.OverlapCircle(transform.position, effectiveDetectRadius, enemyFilter, hitColliders);
            if (count > 0)
            {
                float minSqrDist = float.MaxValue;
                Transform closestTarget = null;
                Vector2 myPos = transform.position;

                for (int i = 0; i < count; i++)
                {
                    if (hitColliders[i].gameObject == gameObject)
                    {
                        continue; // 자기 자신 제외 (표준)
                    }

                    if (!string.IsNullOrEmpty(currentDetectTag) && currentDetectTag != ObjectTags.ALL && !hitColliders[i].CompareTag(currentDetectTag))
                    {
                        continue; // 태그 교집합 필터링
                    }

                    if (Owner != null)
                    {
                        Vector2 diff = (Vector2)Owner.position - (Vector2)hitColliders[i].transform.position;
                        float sqrDistTargetToOwner = GameStatics.GetPerspectiveSqrMagnitude(diff);
                        
                        if (sqrDistTargetToOwner > sqrMaxReach)
                        {
                            continue; // 테더 한계선 + 공격 사거리 밖의 적은 아예 무시
                        }
                    }

                    Vector2 myDiff = myPos - (Vector2)hitColliders[i].transform.position;
                    float sqrDist = GameStatics.GetPerspectiveSqrMagnitude(myDiff);
                    if (sqrDist < minSqrDist)
                    {
                        minSqrDist = sqrDist;
                        closestTarget = hitColliders[i].transform;
                    }
                }
                
                Target = closestTarget;
            }
        }
    }
}
