using UnityEngine;

namespace ProjectAI.Core.Entities
{
    /// <summary>
    /// Animator State에 부착되어 해당 애니메이션이 끝날 때(혹은 중간에 취소될 때)
    /// 무조건 종료 이벤트를 발생시켜 스킬 상태 정리를 보장하는 StateMachineBehaviour입니다.
    /// </summary>
    public class SkillStateBehaviour : StateMachineBehaviour
    {
        public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            // 애니메이터가 부착된 동일한 오브젝트(혹은 부모)에서 EntityEvents를 찾습니다.
            EntityEvents events = animator.GetComponentInParent<EntityEvents>();
            if (events == null)
            {
                Debug.LogWarning("SkillStateBehaviour: EntityEvents component not found in parent hierarchy.");
                return;
            }

            events.InvokeAnimationStateExited(stateInfo.shortNameHash);
        }
    }
}
