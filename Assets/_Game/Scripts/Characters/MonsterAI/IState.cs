namespace ProjectAI.Characters.MonsterAI
{
    /// <summary>
    /// FSM의 각 상태가 가져야 할 필수 인터페이스입니다.
    /// </summary>
    public interface IState
    {
        void Enter();
        void Tick();
        void Exit();
    }
}
