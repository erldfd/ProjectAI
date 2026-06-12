namespace ProjectAI.Core.Entities
{
    /// <summary>
    /// 애니메이션 클립의 Animation Event에서 int 파라미터로 넘길 식별자 열거형입니다.
    /// 강타입(Strong Type)을 강제하여 문자열 오타 등을 방지합니다.
    /// </summary>
    public enum EAnimationEventTag
    {
        None = 0,
        
        /// <summary>
        /// 스킬의 핵심 액션(투사체 발사, 타격 판정 등)이 발동되는 타이밍
        /// </summary>
        Action = 1,
        
        Sound = 2,
        Effect = 3
    }
}
