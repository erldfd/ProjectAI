using UnityEngine;
using ProjectAI.Core;
using ProjectAI.Core.Attributes;

namespace ProjectAI.Characters.MonsterAI
{
    /// <summary>
    /// 소환수(Summon)가 적을 탐지하지 않았을 때, 주인(Owner)을 따라다니는 하위 상태입니다.
    /// 적 감지 시 상위 상태로의 전환은 부모 상태(PeaceState)가 담당합니다.
    /// </summary>
    public class SummonFollowState : AMonsterState
    {
        [Tooltip("주인과의 거리가 이 값보다 멀어지면 쫓아갑니다")]
        [SerializeField]
        private float followThreshold = 3.0f;

        [Header("Sensor Override")]
        [Tooltip("감지할 레이어 마스크")]
        [SerializeField]
        private LayerMask customDetectLayer;

        [Tooltip("감지할 태그")]
        [TagSelector]
        [SerializeField]
        private string customDetectTag = ObjectTags.ALL;

        [Tooltip("감지할 반경")]
        [SerializeField]
        private float customDetectRadius = 8f;

        [Header("Idle Roaming")]
        [Tooltip("대기 시 무작위 배회 반경")]
        [SerializeField]
        private float roamRadius = 2.0f;

        [Tooltip("배회 목적지 갱신 주기")]
        [SerializeField]
        private float roamInterval = 2.0f;

        [Tooltip("배회 목적지에 도착했다고 판정할 거리")]
        [SerializeField]
        private float roamArrivalDistance = 0.5f;

        [Header("Separation")]
        [Tooltip("다른 소환수 밀어내기 반경")]
        [SerializeField]
        private float separationRadius = 1.0f;

        [Tooltip("밀어내기 가중치")]
        [SerializeField]
        private float separationWeight = 1.0f;

        [Tooltip("최대 밀어내기 힘 (1.0 = 최고 속도)")]
        [SerializeField]
        private float maxSeparationForce = 0.6f;

        [Tooltip("밀어내기 연산 주기 (너무 짧으면 떨림 발생)")]
        [SerializeField]
        private float separationInterval = 0.15f;

        [Tooltip("소환수 판정용 레이어 마스크 (밀어내기 용도)")]
        [SerializeField]
        private LayerMask summonLayerMask;

        [Tooltip("소환수 판정용 태그 (밀어내기 용도)")]
        [TagSelector]
        [SerializeField]
        private string summonTag = ObjectTags.ALL;

        private bool isFollowing = false;
        private float currentRoamTimer = 0f;
        private float currentSeparationTimer = 0f;
        private Vector2 cachedSeparationForce = Vector2.zero;
        private Vector2 roamTargetPosition;
        private Collider2D[] hitColliders = new Collider2D[10];
        private ContactFilter2D summonFilter;

        public void SetFollowDistance(float distance)
        {
            followThreshold = distance;
        }

        public override void Enter()
        {
            base.Enter();
            isFollowing = false;
            Brain.SetMoveDirection(Vector2.zero);

            summonFilter.useTriggers = false;
            summonFilter.SetLayerMask(summonLayerMask);
            summonFilter.useLayerMask = true;

            // 상태 진입 시 Brain의 센서 오버라이딩 적용
            Brain.OverrideSensor(customDetectLayer, customDetectTag, customDetectRadius);

            Debug.Log($"[{nameof(SummonFollowState)}] Entered. Sensor overridden: Layer={customDetectLayer}, Tag={customDetectTag}, Radius={customDetectRadius}");
        }

        public override void Exit()
        {
            base.Exit();

            // 상태 탈출 시 센서 설정 롤백
            Brain.ResetSensor();

            Debug.Log($"[{nameof(SummonFollowState)}] Exited. Sensor reset to default.");
        }

        public override void Tick()
        {
            base.Tick();

            if (!GameStatics.IsServerAuthorized)
            {
                return;
            }

            if (!(Brain is NetSummonBrain summonBrain))
            {
                return;
            }

            // 주인이 없으면 제자리에 대기
            if (summonBrain.Owner == null)
            {
                Brain.SetMoveDirection(Vector2.zero);
                return;
            }

            Vector2 myPos = Brain.transform.position;
            float sqrDist = (myPos - (Vector2)summonBrain.Owner.position).sqrMagnitude;
            isFollowing = sqrDist > followThreshold * followThreshold;

            Vector2 desiredDirection = Vector2.zero;

            if (isFollowing)
            {
                // 주인을 쫓아갈 때는 최고 속도(정규화)로 쫓아감
                desiredDirection = ((Vector2)summonBrain.Owner.position - myPos).normalized;
                
                // 주인을 쫓아가는 동안에는 타이머를 초기화하여, 추적이 끝나는 즉시 현재 주인 위치 기반의 새로운 목적지를 잡도록 강제함 (과거의 목적지로 돌아가는 고무줄 버그 방지)
                currentRoamTimer = 0f;
            }
            else
            {
                currentRoamTimer -= Time.deltaTime;

                if (currentRoamTimer <= 0f)
                {
                    Debug.Log($"[{nameof(SummonFollowState)}] Roam target updated. Current Position: {myPos}, Owner Position: {summonBrain.Owner.position}");
                    currentRoamTimer = roamInterval;
                    
                    // 배회 목적지가 추적 임계값(followThreshold) 밖으로 잡히면 고무줄 현상이 발생하므로, 안전하게 반경을 축소 보정
                    float safeRoamRadius = Mathf.Min(roamRadius, Mathf.Max(0.1f, followThreshold - 0.5f));
                    Vector2 randomCircle = Random.insideUnitCircle * safeRoamRadius;
                    
                    roamTargetPosition = (Vector2)summonBrain.Owner.position + randomCircle;
                }

                // AI 뇌(Brain)의 헬퍼 함수를 사용하여, 목적지 부근에서 자연스럽게 감속하며 정지(Arrive)하도록 조향 벡터 획득
                desiredDirection = summonBrain.GetArriveDirection(myPos, roamTargetPosition, roamArrivalDistance, roamArrivalDistance + 1.0f);
            }

            currentSeparationTimer -= Time.deltaTime;
            if (currentSeparationTimer <= 0f)
            {
                currentSeparationTimer = separationInterval;
                cachedSeparationForce = Vector2.zero;

                int count = Physics2D.OverlapCircle(myPos, separationRadius, summonFilter, hitColliders);
                if (count > 0)
                {
                    for (int i = 0; i < count; i++)
                    {
                        if (hitColliders[i].gameObject == gameObject)
                        {
                            continue;
                        }

                        // 태그가 All이 아니면서 대상 태그와 일치하지 않을 경우에만 분산 대상에서 제외(무시)
                        if (!string.IsNullOrEmpty(summonTag) && summonTag != ObjectTags.ALL && !hitColliders[i].CompareTag(summonTag))
                        {
                            continue;
                        }

                        Vector2 diff = myPos - (Vector2)hitColliders[i].transform.position;
                        float dist = diff.magnitude;

                        if (dist > 0.01f && dist < separationRadius)
                        {
                            cachedSeparationForce += (diff.normalized / dist);
                        }
                    }
                }
            }

            if (cachedSeparationForce != Vector2.zero)
            {
                // 밀어내기 힘을 정규화하고, 그 길이를 최대치로 제한하여 너무 빠른 속도로 멀리 밀려나는 현상 방지
                Vector2 sepDir = cachedSeparationForce.normalized;
                float sepMagnitude = Mathf.Clamp(cachedSeparationForce.magnitude * separationWeight, 0f, maxSeparationForce);
                
                desiredDirection += sepDir * sepMagnitude;
            }

            if (isFollowing)
            {
                // 주인을 급하게 쫓아가는 도중에는 밀어내기 힘의 역방향 간섭으로 인해 속도가 깎이지 않도록(항상 최고 속도 1.0 유지) 정규화
                if (desiredDirection != Vector2.zero)
                {
                    desiredDirection = desiredDirection.normalized;
                }
            }
            else
            {
                // 배회 중에는 감속(Arrive) 효과와 부드러운 밀어내기를 위해 벡터 크기를 최대 1.0으로만 제한 (속도 유동성 허용)
                desiredDirection = Vector2.ClampMagnitude(desiredDirection, 1f);
            }

            Brain.SetMoveDirection(desiredDirection);
        }
    }
}
