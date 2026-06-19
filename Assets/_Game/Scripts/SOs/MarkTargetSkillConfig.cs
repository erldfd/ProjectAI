using UnityEngine;

namespace ProjectAI.SOs
{
    [CreateAssetMenu(fileName = "MarkTargetSkillConfig", menuName = "ProjectAI/SOs/Skills/MarkTargetSkillConfig")]
    public class MarkTargetSkillConfig : BaseSkillConfig
    {
        [Header("Mark Target Settings")]
        [Tooltip("타겟 색출(레이캐스트)의 최대 거리입니다.")]
        public float CastDistance = 15f;

        [Tooltip("색출에 사용할 박스캐스트의 크기입니다.")]
        public Vector2 BoxSize = new Vector2(3f, 3f);

        [Tooltip("마킹할 대상의 레이어 마스크입니다.")]
        public LayerMask TargetLayer;
    }
}
