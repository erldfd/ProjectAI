using UnityEngine;

namespace ProjectAI.Characters.MonsterAI
{
    /// <summary>
    /// 몬스터가 타겟을 찾기 전 대기하는 기본 상태입니다.
    /// </summary>
    public class MonsterIdleState : AMonsterState
    {
        public override void Enter()
        {
            base.Enter();
            Brain.SetMoveDirection(Vector2.zero); // 정지
        }

        public override void Tick()
        {
            base.Tick();

            if (Brain.Target != null)
            {
                StateMachine.ChangeState<MonsterChaseState>();
                return;
            }
        }
    }
}
