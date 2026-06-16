using UnityEngine;

namespace ProjectAI.Characters.MonsterAI
{
    /// <summary>
    /// 몬스터 AI 상태의 공통 기반 추상 클래스입니다. (인스펙터 노출용 MonoBehaviour)
    /// 필요 시 내부에 또 다른 상태 머신(SubStateMachine)을 두어 HFSM(계층형 상태머신)으로 확장 가능합니다.
    /// </summary>
    public abstract class AMonsterState : MonoBehaviour, IState
    {
        [Tooltip("최상위 머신에서 관리하는 메인 상태인지 여부입니다. 체크 해제 시 하위 머신에서만 작동합니다.")]
        [SerializeField]
        protected bool isRootState = false;

        public bool IsRootState => isRootState;

        protected NetMonsterBrain Brain { get; private set; }
        protected AIStateMachine StateMachine { get; private set; }
        
        /// <summary>
        /// 하위 상태를 관리하기 위한 서브 상태 머신 (선택적 사용)
        /// 사용을 원할 경우 하위 클래스에서 직접 할당(new)하고 초기화해야 합니다.
        /// </summary>
        protected AIStateMachine SubStateMachine { get; set; }

        public virtual void Initialize(NetMonsterBrain brain, AIStateMachine stateMachine)
        {
            Brain = brain;
            StateMachine = stateMachine;
        }

        public virtual void Enter() 
        {
            if (SubStateMachine != null && SubStateMachine.CurrentState != null)
            {
                SubStateMachine.CurrentState.Enter();
            }
        }

        public virtual void Tick() 
        {
            if (SubStateMachine != null)
            {
                SubStateMachine.Tick();
            }
        }

        public virtual void Exit() 
        {
            if (SubStateMachine != null && SubStateMachine.CurrentState != null)
            {
                SubStateMachine.CurrentState.Exit();
            }
        }
    }
}
