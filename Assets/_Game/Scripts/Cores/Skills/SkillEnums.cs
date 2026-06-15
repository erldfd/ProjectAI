using System;

namespace ProjectAI.Core.Skills
{
    /// <summary>
    /// 시스템에 존재하는 모든 스킬 타입
    /// 네트워크 전송에 최적화되어 있습니다.
    /// </summary>
    public enum ESkillType : byte
    {
        None = 0,
        BasicAttack = 1,
        ProjectileAttack = 2,
        Summon = 3,
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

    /// <summary>
    /// 애니메이터의 상태 이름과 동일하게 매핑되는 열거형입니다.
    /// 오타 방지 및 안전한 해시 변환을 위해 사용됩니다.
    /// 사용자가 직접 애니메이터 상태 이름에 맞춰 수정해서 사용합니다.
    /// </summary>
    public enum EAnimState
    {
        None = 0,
        Cast_FireProjectile = 1,
        Melee_BasicAttack = 2,
        Summon_Creature = 4,
    }
}
