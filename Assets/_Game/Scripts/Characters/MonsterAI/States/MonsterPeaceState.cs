using UnityEngine;
using UnityEngine.Assertions;
using System.Collections.Generic;

namespace ProjectAI.Characters.MonsterAI
{
    /// <summary>
    /// 몬스터가 타겟을 발견하지 못했을 때의 비전투 상위 상태(Composite State)입니다.
    /// 내부에 대기(Idle), 순찰(Patrol), 혹은 소환수 쫓아가기(Follow) 등의 하위 상태를 가질 수 있습니다.
    /// </summary>
    public class MonsterPeaceState : AMonsterState
    {
        [Tooltip("비전투 상태일 때 활성화할 하위 상태들입니다. 리스트의 첫 번째 원소가 처음 진입 시 시작할 하위 상태가 됩니다.")]
        [SerializeField]
        private List<AMonsterState> subStates = new List<AMonsterState>();

        public override void Initialize(NetMonsterBrain brain, AIStateMachine stateMachine)
        {
            base.Initialize(brain, stateMachine);
            
            SubStateMachine = new AIStateMachine();

            foreach (AMonsterState state in subStates)
            {
                if (state != null)
                {
                    Assert.IsFalse(state.IsRootState, $"[MonsterPeaceState] 하위 상태 {state.GetType().Name}의 isRootState가 true로 설정되어 있습니다.");
                    state.Initialize(brain, SubStateMachine);
                    SubStateMachine.AddState(state);
                }
            }
        }

        public override void Enter()
        {
            if (subStates.Count == 0 || subStates[0] == null)
            {
                return;
            }

            if (SubStateMachine.CurrentState == null)
            {
                SubStateMachine.Initialize(subStates[0].GetType());
                return;
            }

            base.Enter();
        }

        public override void Tick()
        {
            // 비전투 중에 타겟이 발견되면 전투 상태로 전환
            // 하위 상태 로직 처리 중 Null 예외 방지를 위해 먼저 검사
            if (Brain.Target != null)
            {
                StateMachine.ChangeState<MonsterCombatState>();
                return;
            }

            base.Tick();
        }
    }
}
