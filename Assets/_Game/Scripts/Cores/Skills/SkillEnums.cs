using System;

namespace ProjectAI.Core.Skills
{
    /// <summary>
    /// 시스템에 존재하는 모든 스킬 식별자입니다.
    /// 네트워크 전송에 최적화되어 있습니다.
    /// </summary>
    public enum ESkillType : byte
    {
        None = 0,
        BasicAttack = 1,
        Dash = 2,
        Fireball = 3,
        // 필요 시 추가
    }

    /// <summary>
    /// 캐릭터의 각종 상태(버프/디버프) 태그입니다.
    /// Flags 속성을 통해 비트 연산으로 중첩 관리가 가능합니다.
    /// </summary>
    [Flags]
    public enum EStateTag : int
    {
        None = 0,
        Casting = 1 << 0,       // 스킬 시전 중
        Silenced = 1 << 1,      // 스킬 사용 불가
        Stunned = 1 << 2,       // 이동 및 스킬 불가
        Invincible = 1 << 3,    // 무적
        // 최대 32개까지 확장 가능 (1 << 31)
    }
}
