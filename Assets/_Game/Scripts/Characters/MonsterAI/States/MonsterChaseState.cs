using UnityEngine;

namespace ProjectAI.Characters.MonsterAI
{
    /// <summary>
    /// 몬스터가 타겟(플레이어)을 발견하여 거리를 좁히는 추적 상태입니다.
    /// </summary>
    public class MonsterChaseState : AMonsterState
    {
        public override void Tick()
        {
            base.Tick();

            if (Brain.Target == null)
            {
                StateMachine.ChangeState<MonsterIdleState>();
                return;
            }

            float sqrDist = ((Vector2)Brain.transform.position - (Vector2)Brain.Target.position).sqrMagnitude;
            
            if (sqrDist <= Brain.AttackRadius * Brain.AttackRadius)
            {
                StateMachine.ChangeState<MonsterAttackState>();
                return;
            }

            Vector2 dir = ((Vector2)Brain.Target.position - (Vector2)Brain.transform.position).normalized;
            Brain.SetMoveDirection(dir);
        }

        public override void Exit()
        {
            base.Exit();
            Brain.SetMoveDirection(Vector2.zero); // 추적 종료 시 정지
        }
    }
}
