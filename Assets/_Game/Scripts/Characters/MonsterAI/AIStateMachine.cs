using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

namespace ProjectAI.Characters.MonsterAI
{
    /// <summary>
    /// 현재 상태를 보관하고 전이시키는 범용 상태 머신 컨트롤러입니다.
    /// (애니메이터의 StateMachine과 혼동되지 않도록 AIStateMachine으로 명명)
    /// </summary>
    public class AIStateMachine
    {
        private Dictionary<Type, IState> states = new Dictionary<Type, IState>();
        
        public IState CurrentState { get; private set; }

        public void AddState(IState state)
        {
            Assert.IsNotNull(state, "추가하려는 상태가 null입니다.");
            states[state.GetType()] = state;
        }

        public void Initialize<T>() where T : IState
        {
            Initialize(typeof(T));
        }

        public void Initialize(Type type)
        {
            if (!states.TryGetValue(type, out IState state))
            {
                Debug.LogError($"초기 상태 {type.Name}가 추가되어 있지 않습니다.");
                return;
            }
            
            CurrentState = state;
            CurrentState.Enter();
        }

        public void ChangeState<T>() where T : IState
        {
            ChangeState(typeof(T));
        }

        public void ChangeState(Type type)
        {
            if (!states.TryGetValue(type, out IState state))
            {
                Debug.LogError($"전환하려는 상태 {type.Name}가 추가되어 있지 않습니다.");
                return;
            }
            
            CurrentState?.Exit();
            
            CurrentState = state;
            CurrentState.Enter();
        }

        public void Tick()
        {
            if (CurrentState == null)
            {
                return;
            }

            CurrentState.Tick();
        }
    }
}
