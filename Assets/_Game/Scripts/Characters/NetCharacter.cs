using ProjectAI.Core.Entities;
using ProjectAI.Core.Skills;
using UnityEngine.Assertions;

namespace ProjectAI.Characters
{
    /// <summary>
    /// 플레이어나 NPC 등 생명체 캐릭터의 핵심 로직을 연결하는 허브 컴포넌트입니다.
    /// </summary>
    public class NetCharacter : NetEntity
    {
        public NetSkillComponent SkillComponent { get; private set; }

        protected override void Awake()
        {
            base.Awake();
            SkillComponent = GetComponentInChildren<NetSkillComponent>();
            Assert.IsNotNull(SkillComponent, "[NetCharacter] NetSkillComponent를 찾을 수 없습니다.");
        }

        /// <summary>
        /// 외부(컨트롤러 등)에서 캐릭터에게 스킬 사용을 지시하는 퍼사드 메서드입니다.
        /// </summary>
        public void TryActivateSkill(ESkillType skillType)
        {
            // 캐릭터 내부망(EntityEvents)을 통해 각 컴포넌트들에게 지시를 내립니다.
            if (Events != null)
            {
                Events.InvokeSkillTriggered(skillType);
            }
        }
    }
}
