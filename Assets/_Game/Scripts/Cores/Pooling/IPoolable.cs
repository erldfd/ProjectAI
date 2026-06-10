namespace ProjectAI.Core.Pooling
{
    /// <summary>
    /// 오브젝트 풀링 대상 객체의 수명 주기를 관리하는 인터페이스입니다.
    /// </summary>
    public interface IPoolable
    {
        /// <summary>
        /// 풀에서 생성되어 활성화될 때 호출됩니다.
        /// </summary>
        void OnSpawn();

        /// <summary>
        /// 풀로 반환되어 비활성화될 때 호출됩니다.
        /// </summary>
        void OnDespawn();
    }
}
