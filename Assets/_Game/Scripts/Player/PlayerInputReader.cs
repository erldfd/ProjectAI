using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ProjectAI.Player
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
        
        [Tooltip("마우스 포인터 위치에 매핑된 액션 (새 인풋 시스템)")]
        [SerializeField]
        private InputActionReference aimAction;

        [Tooltip("상호작용(E키)에 매핑된 액션")]
        [SerializeField]
        private InputActionReference interactAction;

        public event Action<Vector2> OnMoveInputChanged;
        public event Action<bool> OnInteractInputChanged;
        
        private Vector2 currentScreenPosition;
        private Camera mainCamera;

        public Vector2 MouseWorldPosition 
        { 
            get 
            {
                if (mainCamera == null)
                {
                    mainCamera = Camera.main;
                }
                
                if (mainCamera != null)
                {
                    return mainCamera.ScreenToWorldPoint(currentScreenPosition);
                }
                
                return Vector2.zero;
            }
        }

        #region Unity Lifecycle
        private void Awake()
        {
            mainCamera = Camera.main;
        }
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

            if (aimAction != null && aimAction.action != null)
            {
                aimAction.action.Enable();
                aimAction.action.performed += HandleAimInput;
                aimAction.action.canceled += HandleAimInput;
            }

            if (interactAction != null && interactAction.action != null)
            {
                Debug.Log("Interaction Bindings");
                interactAction.action.Enable();
                interactAction.action.performed += HandleInteractInput;
                interactAction.action.canceled += HandleInteractInput;
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

            if (aimAction != null && aimAction.action != null)
            {
                aimAction.action.performed -= HandleAimInput;
                aimAction.action.canceled -= HandleAimInput;
                aimAction.action.Disable();
            }

            if (interactAction != null && interactAction.action != null)
            {
                interactAction.action.performed -= HandleInteractInput;
                interactAction.action.canceled -= HandleInteractInput;
                interactAction.action.Disable();
            }
        }
        #endregion

        #region Private Methods
        private void HandleMoveInput(InputAction.CallbackContext context)
        {
            Vector2 moveInput = context.ReadValue<Vector2>();
            OnMoveInputChanged?.Invoke(moveInput);
        }

        private void HandleAimInput(InputAction.CallbackContext context)
        {
            currentScreenPosition = context.ReadValue<Vector2>();
        }

        private void HandleInteractInput(InputAction.CallbackContext context)
        {
            if (context.performed)
            {
                Debug.Log("Interaction Start");
                OnInteractInputChanged?.Invoke(true);
            }
            else if (context.canceled)
            {
                OnInteractInputChanged?.Invoke(false);
            }
        }
        #endregion
    }
}
