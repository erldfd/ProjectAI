using UnityEngine;
using UnityEngine.Assertions;

namespace ProjectAI.Characters.MonsterAI
{
    /// <summary>
    /// 몬스터가 타겟(플레이어)을 향해 거리를 좁히는 추적 하위 상태입니다.
    /// 타겟 유무에 따른 상위 상태로의 전환은 부모 상태(CombatState)가 담당합니다.
    /// </summary>
    public class MonsterChaseState : AMonsterState
    {
        public override void Tick()
        {
            base.Tick();
            Assert.IsNotNull(Brain.Target, "[MonsterChaseState] Target이 null입니다. 부모 상태가 null 처리를 누락했습니다.");

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
