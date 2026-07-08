using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.Assertions;
using ProjectAI.UIs.Cores;
using ProjectAI.Core;

namespace ProjectAI.UIs.Popups
{
    /// <summary>
    /// 환경설정(Settings) 팝업 UI를 제어하는 클래스입니다.
    /// AUIPopup을 상속받아 UIManager에 의해 관리됩니다.
    /// </summary>
    public class SettingsPopup : AUIPopup
    {
        private const string CLOSE_BTN_NAME = "btn-close";
        private Button btnClose;

        /// <summary>
        /// 팝업이 처음 생성될 때 UI 요소를 찾고 이벤트를 바인딩합니다.
        /// </summary>
        protected override void OnInitialize()
        {
            btnClose = base.RootElement.Q<Button>(CLOSE_BTN_NAME);
            Assert.IsNotNull(btnClose, $"[SettingsPopup] '{CLOSE_BTN_NAME}' 버튼을 UXML에서 찾을 수 없습니다. (UXML 설정을 확인하세요)");
            
            btnClose.clicked += OnCloseClicked;
        }

        private void OnCloseClicked()
        {
            // 전역 UIManager를 통해 이 팝업을 안전하게 닫습니다.
            GameStatics.UIManager.ClosePopup(this);
        }

        /// <summary>
        /// 팝업이 화면에 나타날 때 호출됩니다.
        /// </summary>
        protected override void OnShow()
        {
            Debug.Log("[SettingsPopup] 환경설정 팝업이 열렸습니다.");
        }

        /// <summary>
        /// 팝업이 화면에서 사라질 때 호출됩니다.
        /// </summary>
        protected override void OnHide()
        {
            Debug.Log("[SettingsPopup] 환경설정 팝업이 닫혔습니다.");
        }
    }
}
