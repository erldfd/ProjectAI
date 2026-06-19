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
        
        [Tooltip("주인에게 다가가는 멈춤 최소 거리")]
        [SerializeField]
        private float stopDistance = 1.5f;

        [Header("Sensor Override")]
        [Tooltip("감지할 레이어 마스크")]
        [SerializeField]
        private LayerMask customDetectLayer;

        [Tooltip("감지할 태그")]
        [TagSelector]
        [SerializeField]
        private string customDetectTag = ObjectTags.NONE;

        [Tooltip("감지할 반경")]
        [SerializeField]
        private float customDetectRadius = 8f;

        private bool isFollowing = false;

        public void SetFollowDistance(float distance)
        {
            followThreshold = distance;
        }

        public override void Enter()
        {
            base.Enter();
            isFollowing = false;
            Brain.SetMoveDirection(Vector2.zero);

            // 상태 진입 시 Brain의 센서 오버라이딩 적용
            Brain.OverrideSensor(customDetectLayer, customDetectTag, customDetectRadius);
        }

        public override void Exit()
        {
            base.Exit();

            // 상태 탈출 시 센서 설정 롤백
            Brain.ResetSensor();
        }

        public override void Tick()
        {
            base.Tick();

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

            float sqrDist = ((Vector2)Brain.transform.position - (Vector2)summonBrain.Owner.position).sqrMagnitude;

            if (sqrDist > followThreshold * followThreshold)
            {
                isFollowing = true;
            }
            else if (sqrDist <= stopDistance * stopDistance)
            {
                isFollowing = false;
                Brain.SetMoveDirection(Vector2.zero);
                return;
            }

            if (isFollowing)
            {
                // 주인을 향해 이동
                Vector2 dir = ((Vector2)summonBrain.Owner.position - (Vector2)Brain.transform.position).normalized;
                Brain.SetMoveDirection(dir);
            }
        }
    }
}
