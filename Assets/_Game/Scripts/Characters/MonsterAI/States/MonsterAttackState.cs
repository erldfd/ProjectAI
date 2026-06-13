using UnityEngine;
using ProjectAI.Core.Skills;

namespace ProjectAI.Characters.MonsterAI
{
    /// <summary>
    /// 사거리 내의 타겟에게 일정 주기로 공격을 시도하는 상태입니다.
    /// </summary>
    public class MonsterAttackState : AMonsterState
    {
        private float lastAttackTime = -999f;

        [Tooltip("공격 쿨다운")]
        [SerializeField]
        private float attackCooldown = 2.0f;

        public override void Enter()
        {
            base.Enter();
            Brain.SetMoveDirection(Vector2.zero); // 공격 시 정지
        }

        public override void Tick()
        {
            base.Tick();

            if (Brain.Target == null)
            {
                StateMachine.ChangeState<MonsterIdleState>();
                return;
            }

            float sqrDist = ((Vector2)Brain.transform.position - (Vector2)Brain.Target.position).sqrMagnitude;
            
            if (sqrDist > Brain.AttackRadius * Brain.AttackRadius)
            {
                StateMachine.ChangeState<MonsterChaseState>();
                return;
            }

            if (Time.time >= lastAttackTime + attackCooldown)
            {
                bool success = Brain.TryAttack();
                if (success)
                {
                    lastAttackTime = Time.time;
                }
            }
        }
    }
}
