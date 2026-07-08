using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ProjectAI.Core.Inputs
{
    /// <summary>
    /// UI 조작(팝업 닫기 등)이나 전역 시스템 단축키 입력을 처리하는 전역 인풋 리더입니다.
    /// GameManager에 부착되어 사용됩니다.
    /// </summary>
    public class GlobalInputReader : MonoBehaviour
    {
        [Header("Input Actions")]
        [Tooltip("UI 팝업 닫기(ESC/Cancel 등)에 매핑된 액션")]
        [SerializeField]
        private InputActionReference cancelAction;

        /// <summary>
        /// 취소/닫기 키가 눌렸을 때 발생하는 이벤트
        /// </summary>
        public event Action OnCancelInput;

        #region Unity Lifecycle
        private void Awake()
        {
            UnityEngine.Assertions.Assert.IsNotNull(cancelAction, "[GlobalInputReader] cancelAction이 인스펙터에 할당되지 않았습니다.");
            UnityEngine.Assertions.Assert.IsNotNull(cancelAction.action, "[GlobalInputReader] cancelAction.action이 유효하지 않습니다.");
        }

        private void OnEnable()
        {
            EnableInput();
        }

        private void OnDisable()
        {
            DisableInput();
        }
        #endregion

        #region Public Methods
        public void EnableInput()
        {
            cancelAction.action.Enable();
            cancelAction.action.performed += HandleCancelInput;
        }

        public void DisableInput()
        {
            cancelAction.action.performed -= HandleCancelInput;
            cancelAction.action.Disable();
        }
        #endregion

        #region Private Methods
        private void HandleCancelInput(InputAction.CallbackContext context)
        {
            if (context.performed)
            {
                OnCancelInput?.Invoke();
            }
        }
        #endregion
    }
}
