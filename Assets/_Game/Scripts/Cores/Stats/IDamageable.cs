namespace ProjectAI.Core.Stats
{
    /// <summary>
    /// 공격을 받을 수 있는 모든 객체가 구현해야 하는 피해(Damage) 처리 인터페이스입니다.
    /// </summary>
    public interface IDamageable
    {
        /// <summary>
        /// 객체에 데미지를 가합니다.
        /// (호출은 GameStatics.ApplyDamage를 통하는 것을 권장합니다)
        /// </summary>
        /// <param name="damage">최종 적용할 데미지 수치</param>
        void TakeDamage(int damage);

        /// <summary>
        /// 타격 판정에 사용되는 객체의 Z축 깊이(두께) 반경입니다.
        /// </summary>
        float DepthRadius { get; }

        /// <summary>
        /// 객체가 소속된 루트 엔티티 (아군 오인 방지 및 스탯 조회용)
        /// 엔티티가 아닐 경우 null을 반환할 수 있습니다.
        /// </summary>
        Entities.NetEntity OwnerEntity { get; }
    }
}
