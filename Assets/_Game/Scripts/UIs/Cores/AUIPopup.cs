using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.Assertions;
using ProjectAI.Core.Enums;

namespace ProjectAI.UIs.Cores
{
    /// <summary>
    /// UIManager에 의해 동적으로 로드되는 팝업 UI들의 최상위 부모 클래스입니다.
    /// UI Toolkit 기반이므로 MonoBehaviour를 상속받지 않는 순수 C# 클래스로 가볍게 동작합니다.
    /// </summary>
    public abstract class AUIPopup
    {
        /// <summary>
        /// 이 팝업의 최상위 UI 요소 (UXML 템플릿에서 복제된 컨테이너)
        /// </summary>
        public VisualElement RootElement { get; private set; }
        
        /// <summary>
        /// 이 팝업의 고유 타입
        /// </summary>
        public EUIPopupType PopupType { get; private set; }

        /// <summary>
        /// 화면에 항상 떠 있는 오버레이(HUD)인지 여부. true일 경우 UIManager 스택에 쌓이지 않아 ESC로 닫을 수 없습니다.
        /// </summary>
        public virtual bool IsOverlay => false;

        /// <summary>
        /// 팝업이 초기화되었는지 여부를 추적합니다.
        /// </summary>
        public bool IsInitialized { get; private set; }

        /// <summary>
        /// 팝업이 현재 화면에 열려있는지 여부를 추적합니다.
        /// </summary>
        public bool IsVisible { get; private set; }

        /// <summary>
        /// 팝업이 최초 생성될 때 UIManager로부터 호출되어 루트 요소와 타입을 할당받습니다.
        /// </summary>
        /// <param name="root">인스턴스화된 최상위 VisualElement</param>
        /// <param name="type">팝업 타입</param>
        public void Initialize(VisualElement root, EUIPopupType type)
        {
            Assert.IsFalse(IsInitialized, "[AUIPopup] 이미 초기화된 팝업에 대해 Initialize() 중복 호출이 발생했습니다!");
            Assert.IsNotNull(root, "[AUIPopup] 초기화에 실패했습니다. root VisualElement가 null입니다.");
            
            RootElement = root;
            PopupType = type;
            IsInitialized = true;
            IsVisible = (RootElement.style.display.value == DisplayStyle.Flex);
            Debug.Log($"[AUIPopup] {PopupType} 팝업이 초기화되었습니다. IsVisible: {IsVisible}");
            OnInitialize();
        }

        /// <summary>
        /// 자식 클래스에서 오버라이드하여 UI 쿼리(Q) 및 버튼 이벤트 바인딩 등을 수행합니다.
        /// </summary>
        protected abstract void OnInitialize();

        /// <summary>
        /// 팝업을 화면에 표시합니다.
        /// </summary>
        public virtual void Show()
        {
            Assert.IsTrue(IsInitialized, "[AUIPopup] 초기화되지 않은 팝업을 열려고 시도했습니다!");
            Assert.IsNotNull(RootElement, "[AUIPopup] Show 호출 실패. RootElement가 null입니다.");
            
            Debug.Log($"[AUIPopup] {PopupType} 팝업을 화면에 표시합니다, IsVisible: {IsVisible}");
            if (IsVisible)
            {
                Debug.LogWarning($"[AUIPopup] {PopupType} 팝업은 이미 표시되어 있습니다. (중복 Show 방어)");
                return;
            }

            IsVisible = true;
            RootElement.style.display = DisplayStyle.Flex;
            OnShow();
        }

        /// <summary>
        /// 자식 클래스에서 오버라이드하여 팝업이 열릴 때마다 필요한 애니메이션 재생이나 데이터 갱신을 수행합니다.
        /// </summary>
        protected virtual void OnShow() { }

        /// <summary>
        /// 팝업을 화면에서 숨깁니다.
        /// </summary>
        public virtual void Hide()
        {
            Assert.IsTrue(IsInitialized, "[AUIPopup] 초기화되지 않은 팝업을 숨기려고 시도했습니다!");
            Assert.IsNotNull(RootElement, "[AUIPopup] Hide 호출 실패. RootElement가 null입니다.");
            
            if (!IsVisible)
            {
                Debug.LogWarning($"[AUIPopup] {PopupType} 팝업은 이미 숨겨져 있습니다. (중복 Hide 방어)");
                return;
            }

            IsVisible = false;
            RootElement.style.display = DisplayStyle.None;
            OnHide();
        }

        /// <summary>
        /// 자식 클래스에서 오버라이드하여 팝업이 닫힐 때 필요한 데이터 정리 등을 수행합니다.
        /// </summary>
        protected virtual void OnHide() { }
    }
}
