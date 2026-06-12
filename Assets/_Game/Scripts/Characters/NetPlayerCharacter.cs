using ProjectAI.Core;
using ProjectAI.Movements;
using UnityEngine;
using UnityEngine.Assertions;

namespace ProjectAI.Characters
{
    /// <summary>
    /// 플레이어가 조종하는 캐릭터 전용 로직(직접 이동 명령, 상호작용 등)을 담는 퍼사드 클래스입니다.
    /// NetCharacter의 공통 기능(스킬, 상태 등)을 상속받습니다.
    /// </summary>
    public class NetPlayerCharacter : NetCharacter
    {
        private NetInteractor interactor;

        protected override void Awake()
        {
            base.Awake();
            interactor = GetComponentInChildren<NetInteractor>();
            Assert.IsNotNull(interactor, "[NetPlayerCharacter] NetInteractor를 찾을 수 없습니다.");
        }

        /// <summary>
        /// 외부(컨트롤러)에서 캐릭터의 이동을 지시하는 퍼사드 메서드입니다.
        /// </summary>
        public void Move(Vector2 direction)
        {
            if (base.Movement is not NetPlayerMovement playerMovement)
            {
                Debug.LogWarning($"[NetPlayerCharacter] Move 실패: Movement가 NetPlayerMovement가 아닙니다. (ID: {NetworkObjectId})");
                return;
            }

            playerMovement.SetMoveInput(direction);
        }

        /// <summary>
        /// 외부(컨트롤러)에서 캐릭터의 상호작용을 지시하는 퍼사드 메서드입니다.
        /// </summary>
        public void TryInteract()
        {
            interactor.TryInteract();
        }
    }
}
