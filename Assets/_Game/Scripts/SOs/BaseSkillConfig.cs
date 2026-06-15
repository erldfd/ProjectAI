using System;
using Unity.Netcode;
using ProjectAI.Core.Skills;
using UnityEngine;
using ProjectAI.Core.Attributes;

namespace ProjectAI.SOs
{
    /// <summary>
    /// 일반 스킬 및 스킬 데이터의 기반이 되는 ScriptableObject 클래스입니다.
    /// </summary>
    [CreateAssetMenu(fileName = "BaseSkillConfig", menuName = "ProjectAI/SOs/Skills/BaseSkillConfig")]
    public class BaseSkillConfig : ScriptableObject
    {
        [Tooltip("네트워크 상에서 이 스킬을 식별할 고유 ID입니다. (이름을 기반으로 자동 생성됨)")]
        [SerializeField]
        [ReadOnly]
        private int skillId;
        public int SkillId => skillId;

        [Tooltip("스킬 종류 식별자")]
        public ESkillType SkillType;

        [Tooltip("기본 쿨타임")]
        public float BaseCooldown;

        [Tooltip("생성할 프리팹 (필요한 스킬의 경우)")]
        public NetworkObject Prefab;

        [Header("Animation")]
        // 주의: 새로운 스킬을 추가할 때는 반드시 애니메이터 창의 State 이름과 
        // EAnimState 열거형의 이름이 일치하도록 맞춰서 등록해 주어야 합니다.
        [Tooltip("스킬 시전 시 재생할 애니메이션 상태 (EAnimState에서 선택)\n[주의] 애니메이터 창의 State 이름과 정확히 일치하게 맞춰야 합니다!")]
        public EAnimState AnimState;

        [Tooltip("애니메이션 해시 (자동 생성)")]
        [SerializeField]
        [ReadOnly]
        private int animHash;
        public int AnimHash => animHash;

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (!string.IsNullOrEmpty(this.name))
            {
                skillId = Animator.StringToHash(this.name);
            }

            if (AnimState != EAnimState.None)
            {
                animHash = Animator.StringToHash(AnimState.ToString());
            }
            else
            {
                animHash = 0;
            }
        }
#endif
    }
}
