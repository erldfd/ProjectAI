namespace ProjectAI.Core.Combat
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
    }
}
