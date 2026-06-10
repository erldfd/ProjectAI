namespace ProjectAI.Core.Interfaces
{
    /// <summary>
    /// 스포너 등에서 객체를 스폰할 때 만족해야 하는 조건을 정의하는 인터페이스입니다.
    /// </summary>
    public interface ISpawnCondition
    {
        /// <summary>
        /// 스폰 조건이 충족되었는지 검사합니다.
        /// </summary>
        /// <returns>조건 충족 여부</returns>
        bool CheckCondition();
    }
}
