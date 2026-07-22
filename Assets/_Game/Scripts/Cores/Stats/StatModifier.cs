using ProjectAI.Core.Enums;

namespace ProjectAI.Core.Stats
{
    /// <summary>
    /// 스탯 수치 변경(버프/디버프) 정보를 담는 클래스입니다.
    /// </summary>
    public class StatModifier
    {
        public EStatType StatType { get; private set; }
        public float Value { get; private set; }

        /// <summary>
        /// 버프/스탯 변동을 유발한 출처 객체 (스킬, 아이템 등).
        /// </summary>
        public object Source { get; private set; }

        /// <summary>
        /// StatModifier 생성자입니다.
        /// </summary>
        /// <param name="statType">변동할 스탯 종류</param>
        /// <param name="value">변동 수치</param>
        /// <param name="source">[주의] 박싱(Garbage) 방지를 위해 int, enum 등 값 타입(Value Type)이 아닌 반드시 클래스(Reference Type) 객체를 전달하세요.</param>
        public StatModifier(EStatType statType, float value, object source = null)
        {
            this.StatType = statType;
            this.Value = value;
            this.Source = source;
        }
    }
}
