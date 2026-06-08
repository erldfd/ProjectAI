namespace ProjectAI.Core.Skills
{
    /// <summary>
    /// 개별 스킬의 실제 발동 로직을 담당하는 인터페이스입니다.
    /// SkillManager에 의해 등록되고 실행됩니다.
    /// </summary>
    public interface ISkillLogic
    {
        /// <summary>
        /// 해당 스킬을 식별하는 고유 타입입니다.
        /// </summary>
        ESkillType SkillType { get; }

        /// <summary>
        /// 초기화 시 SkillManager로부터 데이터(프리팹, 수치 등)를 세팅받습니다.
        /// </summary>
        void Initialize(SkillManager manager);

        /// <summary>
        /// 현재 상태에서 이 스킬을 사용할 수 있는지 검사합니다.
        /// </summary>
        bool CanExecute(NetSkillComponent caster);

        /// <summary>
        /// 스킬의 실제 로직을 서버에서 실행합니다.
        /// (예: 투사체 스폰, 데미지 판정, 애니메이션 RPC 전송 등)
        /// </summary>
        void Execute(NetSkillComponent caster);
    }
}
