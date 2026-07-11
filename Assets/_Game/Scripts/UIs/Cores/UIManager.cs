using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.Assertions;
using ProjectAI.Core.Enums;
using ProjectAI.SOs;
using ProjectAI.Core;

namespace ProjectAI.UIs.Cores
{
    /// <summary>
    /// 게임 내 팝업 UI를 전역적으로 관리(스택)하는 매니저입니다.
    /// GameManager에 부착되어 씬 전환에도 파괴되지 않고 유지됩니다.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public class UIManager : MonoBehaviour
    {
        [Header("UI Database")]
        [Tooltip("UXML 에셋들이 매핑된 팝업 데이터베이스")]
        [SerializeField]
        private UIPopupDatabaseSO popupDatabase;

        private UIDocument rootDocument;
        private Stack<AUIPopup> popupStack = new Stack<AUIPopup>();
        private Dictionary<EUIPopupType, AUIPopup> cachedPopups = new Dictionary<EUIPopupType, AUIPopup>();
        
        // 스택 조작 시 발생하는 GC 할당(메모리 가비지) 방지를 위한 전역 임시 스택
        private Stack<AUIPopup> tempStack = new Stack<AUIPopup>();

        private void Awake()
        {
            rootDocument = GetComponent<UIDocument>();
            Assert.IsNotNull(rootDocument, "[UIManager] UIDocument 컴포넌트를 찾을 수 없습니다.");
            Assert.IsNotNull(popupDatabase, "[UIManager] 인스펙터에 UIPopupDatabaseSO가 할당되지 않았습니다.");
        }

        private void Start()
        {
            Assert.IsNotNull(GameStatics.GlobalInput, "[UIManager] GlobalInput이 null입니다. 이벤트 바인딩 불가.");
            GameStatics.GlobalInput.OnCancelInput += CloseTopPopup;
        }

        private void OnDestroy()
        {
            if (GameStatics.GameManager != null && GameStatics.GlobalInput != null)
            {
                GameStatics.GlobalInput.OnCancelInput -= CloseTopPopup;
            }
        }

        /// <summary>
        /// 지정된 타입의 팝업을 생성하고 스택 최상단에 띄웁니다.
        /// </summary>
        /// <typeparam name="T">생성할 팝업의 AUIPopup 상속 클래스</typeparam>
        /// <param name="popupType">데이터베이스에서 찾을 팝업 타입</param>
        /// <returns>생성된 팝업 인스턴스</returns>
        public T ShowPopup<T>(EUIPopupType popupType) where T : AUIPopup, new()
        {
            if (rootDocument == null)
            {
                rootDocument = GetComponent<UIDocument>();
                Assert.IsNotNull(rootDocument, "[UIManager] UIDocument 컴포넌트를 찾을 수 없습니다.");
            }

            Assert.IsNotNull(rootDocument.rootVisualElement, "[UIManager] 아직 UI Toolkit의 rootVisualElement가 준비되지 않았습니다. Awake 단계에서의 호출을 피해주세요.");

            if (cachedPopups.TryGetValue(popupType, out AUIPopup existingPopup))
            {
                existingPopup.RootElement.BringToFront();

                if (existingPopup.IsVisible)
                {
                    Debug.LogWarning($"[UIManager] {popupType} 팝업은 이미 표시되어 있습니다. 최상단으로 올리기만 수행합니다.");
                    RemoveFromStack(existingPopup);
                    if (!existingPopup.IsOverlay)
                    {
                        popupStack.Push(existingPopup);
                    }
                    return (T)existingPopup;
                }

                existingPopup.Show();
                if (!existingPopup.IsOverlay)
                {
                    popupStack.Push(existingPopup);
                }
                return (T)existingPopup;
            }

            VisualTreeAsset uxml = popupDatabase.GetUxmlTemplate(popupType);
            Assert.IsNotNull(uxml, $"[UIManager] {popupType}의 UXML 템플릿을 불러올 수 없습니다.");

            TemplateContainer container = uxml.Instantiate();
            
            // 화면 전체를 덮도록 컨테이너의 절대 위치 및 크기 설정
            container.style.width = Length.Percent(100f);
            container.style.height = Length.Percent(100f);
            container.style.position = Position.Absolute;
            
            // UI Toolkit의 기본값이 Flex이므로, Initialize 시 IsVisible이 강제로 true로 설정되어 Show()가 무시되는 버그를 방지합니다.
            container.style.display = DisplayStyle.None;

            rootDocument.rootVisualElement.Add(container);

            T popup = new T();
            popup.Initialize(container, popupType);
            
            cachedPopups.Add(popupType, popup);

            popup.Show();

            if (!popup.IsOverlay)
            {
                popupStack.Push(popup);
            }

            return popup;
        }

        /// <summary>
        /// 가장 최근에 띄워진(스택 최상단) 팝업을 닫습니다. (ESC 키 등에 연동)
        /// </summary>
        public void CloseTopPopup()
        {
            if (popupStack.Count == 0)
            {
                Debug.LogWarning("[UIManager] 스택이 비어 닫을 팝업이 없습니다. (ESC 연타 무시)");
                return;
            }

            AUIPopup topPopup = popupStack.Pop();
            ClosePopupInternal(topPopup);
        }

        /// <summary>
        /// 특정 팝업을 지정하여 닫습니다. (직접 닫기 버튼을 눌렀을 때 사용)
        /// </summary>
        public void ClosePopup(AUIPopup popup)
        {
            Assert.IsNotNull(popup, "[UIManager] 닫으려는 팝업 객체가 null입니다.");

            if (!popup.IsVisible)
            {
                Debug.LogWarning($"[UIManager] {popup.PopupType} 팝업은 이미 닫혀 있습니다. 중복 호출을 무시합니다.");
                return;
            }

            RemoveFromStack(popup);
            ClosePopupInternal(popup);
        }

        /// <summary>
        /// 특정 팝업을 스택의 중간에서 안전하게 뽑아냅니다.
        /// </summary>
        private void RemoveFromStack(AUIPopup popup)
        {
            if (popupStack.Contains(popup))
            {
                // 재사용을 위해 비어있는지 확실히 보장
                tempStack.Clear();

                while (popupStack.Count > 0)
                {
                    AUIPopup p = popupStack.Pop();
                    if (p == popup)
                    {
                        break;
                    }

                    tempStack.Push(p);
                }

                // 뽑아둔 나머지 팝업들을 원상복구 (Pop하면서 자동으로 비워짐)
                while (tempStack.Count > 0)
                {
                    popupStack.Push(tempStack.Pop());
                }
            }
        }

        /// <summary>
        /// 실제 화면에서 팝업을 숨기고 UI 트리에서 제거합니다.
        /// </summary>
        private void ClosePopupInternal(AUIPopup popup)
        {
            popup.Hide();
            // 캐싱 구조이므로 요소(Remove)를 파괴하지 않고 숨기기(Hide)만 수행하여 재활용합니다.
        }
    }
}
