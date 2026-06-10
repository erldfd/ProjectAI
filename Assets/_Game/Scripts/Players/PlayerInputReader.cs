using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ProjectAI.Players
{
    /// <summary>
    /// 플레이어의 키보드/마우스 입력을 읽어 이벤트를 발생시키는 클래스입니다.
    /// </summary>
    public class PlayerInputReader : MonoBehaviour
    {
        [Header("Input Actions")]
        [Tooltip("WASD 이동에 매핑된 액션 (새 인풋 시스템)")]
        [SerializeField]
        private InputActionReference moveAction;
        

        [Tooltip("상호작용(E키)에 매핑된 액션")]
        [SerializeField]
        private InputActionReference interactAction;

        [Tooltip("공격(마우스 좌클릭)에 매핑된 액션")]
        [SerializeField]
        private InputActionReference attackAction;

        public event Action<Vector2> OnMoveInputChanged;
        public event Action<bool> OnInteractInputChanged;
        public event Action<bool> OnAttackInputChanged;
        

        #region Unity Lifecycle
        // 마우스 관련 초기화 제거됨
        #endregion

        #region Public Methods
        public void EnableInput()
        {
            if (moveAction != null && moveAction.action != null)
            {
                moveAction.action.Enable();
                moveAction.action.performed += HandleMoveInput;
                moveAction.action.canceled += HandleMoveInput;
            }


            if (interactAction != null && interactAction.action != null)
            {
                interactAction.action.Enable();
                interactAction.action.performed += HandleInteractInput;
                interactAction.action.canceled += HandleInteractInput;
            }

            if (attackAction != null && attackAction.action != null)
            {
                attackAction.action.Enable();
                attackAction.action.performed += HandleAttackInput;
                attackAction.action.canceled += HandleAttackInput;
            }
        }

        public void DisableInput()
        {
            if (moveAction != null && moveAction.action != null)
            {
                moveAction.action.performed -= HandleMoveInput;
                moveAction.action.canceled -= HandleMoveInput;
                moveAction.action.Disable();
            }


            if (interactAction != null && interactAction.action != null)
            {
                interactAction.action.performed -= HandleInteractInput;
                interactAction.action.canceled -= HandleInteractInput;
                interactAction.action.Disable();
            }

            if (attackAction != null && attackAction.action != null)
            {
                attackAction.action.performed -= HandleAttackInput;
                attackAction.action.canceled -= HandleAttackInput;
                attackAction.action.Disable();
            }
        }
        #endregion

        #region Private Methods
        private void HandleMoveInput(InputAction.CallbackContext context)
        {
            Vector2 moveInput = context.ReadValue<Vector2>();
            OnMoveInputChanged?.Invoke(moveInput);
        }


        private void HandleInteractInput(InputAction.CallbackContext context)
        {
            if (context.performed)
            {
                OnInteractInputChanged?.Invoke(true);
            }
            else if (context.canceled)
            {
                OnInteractInputChanged?.Invoke(false);
            }
        }

        private void HandleAttackInput(InputAction.CallbackContext context)
        {
            if (context.performed)
            {
                Debug.Log("Attack input performed");
                OnAttackInputChanged?.Invoke(true);
            }
            else if (context.canceled)
            {
                Debug.Log("Attack input canceled");
                OnAttackInputChanged?.Invoke(false);
            }
        }
        #endregion
    }
}
