using UnityEngine;

namespace ProjectAI.Characters.MonsterAI
{
    /// <summary>
    /// 몬스터 AI 상태의 공통 기반 추상 클래스입니다. (인스펙터 노출용 MonoBehaviour)
    /// </summary>
    public abstract class AMonsterState : MonoBehaviour, IState
    {
        protected NetMonsterBrain Brain { get; private set; }
        protected AIStateMachine StateMachine { get; private set; }

        public virtual void Initialize(NetMonsterBrain brain, AIStateMachine stateMachine)
        {
            this.Brain = brain;
            this.StateMachine = stateMachine;
        }

        public virtual void Enter() { }

        public virtual void Tick() { }

        public virtual void Exit() { }
    }
}
