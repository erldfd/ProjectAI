using System;
using UnityEngine;

namespace ProjectAI.SOs
{
    /// <summary>
    /// 소환 스킬의 고유 설정값을 보관하는 데이터 클래스입니다.
    /// </summary>
    [CreateAssetMenu(fileName = "SummonSkillConfig", menuName = "ProjectAI/SOs/Skills/SummonSkillConfig")]
    public class SummonSkillConfig : BaseSkillConfig
    {
        [Tooltip("소환수 유지 시간 (초)")]
        public float Duration = 10f;

        [Tooltip("주인과의 유지 거리")]
        public float FollowDistance = 2f;
    }
}
