using UnityEngine;
using UnityEngine.Assertions;

namespace ProjectAI.Core.Entities
{
    /// <summary>
    /// Hit(피격) 애니메이션 State에 부착되어
    /// 피격 애니메이션 진입/종료 시 EntityEvents를 통해 이벤트를 중계하는 StateMachineBehaviour입니다.
    /// 구체 클래스(NetSkillComponent 등)를 알 필요 없이 EntityEvents와만 통신하여 디커플링을 보장합니다.
    /// </summary>
    public class HitStateBehaviour : StateMachineBehaviour
    {
        // StateMachineBehaviour는 AnimatorController 단위 공유 인스턴스입니다.
        // 여러 오브젝트가 동일 Controller를 공유할 경우 인스턴스 필드 캐싱은 참조 오염 버그가 발생하므로
        // 상태 전환 시점에만 호출되는 특성을 고려하여 매번 직접 취득합니다.
        public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            EntityEvents events = animator.GetComponentInParent<EntityEvents>();
            Assert.IsNotNull(events, "[HitStateBehaviour] EntityEvents component not found in parent hierarchy.");
            events.InvokeHitStateEntered();
        }

        public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            EntityEvents events = animator.GetComponentInParent<EntityEvents>();
            Assert.IsNotNull(events, "[HitStateBehaviour] EntityEvents component not found in parent hierarchy.");
            events.InvokeHitStateExited();
        }
    }
}
