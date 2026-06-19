using UnityEngine;
using UnityEngine.Assertions;
using System.Collections.Generic;

namespace ProjectAI.Characters.MonsterAI
{
    /// <summary>
    /// 몬스터가 타겟을 발견했을 때의 전투 상위 상태(Composite State)입니다.
    /// 내부에 추적(Chase), 공격(Attack) 등의 하위 상태를 가집니다.
    /// </summary>
    public class MonsterCombatState : AMonsterState
    {
        [Tooltip("전투 상태일 때 활성화할 하위 상태들입니다. 리스트의 첫 번째 원소가 처음 진입 시 시작할 하위 상태가 됩니다.")]
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
                    Assert.IsFalse(state.IsRootState, $"[MonsterCombatState] 하위 상태 {state.GetType().Name}의 isRootState가 true로 설정되어 있습니다.");
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
            // 타겟을 잃어버리면 비전투 상태로 전환
            if (Brain.Target == null)
            {
                StateMachine.ChangeState<MonsterPeaceState>();
                return;
            }

            base.Tick();
        }
    }
}
