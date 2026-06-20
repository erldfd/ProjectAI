using UnityEngine;

namespace ProjectAI.Characters.MonsterAI
{
    /// <summary>
    /// 몬스터가 타겟을 찾기 전 대기하는 비전투 하위 상태입니다.
    /// 타겟 발견 시 상위 상태로의 전환은 부모 상태(PeaceState)가 담당합니다.
    /// </summary>
    public class MonsterIdleState : AMonsterState
    {
        public override void Enter()
        {
            base.Enter();
            Brain.SetMoveDirection(Vector2.zero); // 정지

            Debug.Log($"[{nameof(MonsterIdleState)}] Entered. Monster is idle and waiting for target.");
        }

        public override void Exit()
        {
            base.Exit();
            Debug.Log($"[{nameof(MonsterIdleState)}] Exited. Monster is no longer idle.");
        }

        public override void Tick()
        {
            base.Tick();
            // 부모 상태(MonsterPeaceState)가 타겟 감지 전환을 담당하므로 로직 최소화
        }
    }
}
