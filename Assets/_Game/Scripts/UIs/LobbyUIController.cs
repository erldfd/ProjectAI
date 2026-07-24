using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;
using UnityEngine.Assertions;
using ProjectAI.Core;
using ProjectAI.Core.Enums;

namespace ProjectAI.UIs
{
    /// <summary>
    /// 로비 씬(Lobby Scene) 전용 UI 컨트롤러 클래스입니다.
    /// 로비 UI 템플릿(UXML)에서 배치된 '소환수 변경' 버튼을 쿼리하여 팝업 오픈 이벤트를 연결합니다.
    /// </summary>
    public class LobbyUIController : MonoBehaviour
    {
        private const string BUTTON_NAME = "btn-change-summon";

        private UIDocument uiDocument;
        private Button btnChangeSummon;

        private void Awake()
        {
            uiDocument = GetComponent<UIDocument>();
            Assert.IsNotNull(uiDocument, "[LobbyUIController] UIDocument 컴포넌트를 찾을 수 없습니다. 필수 컴포넌트입니다.");
        }

        private void OnEnable()
        {
            // [BugFix Note: 2026-07-24]
            // 에디터 실행 직후 MPPM_UIFixer에 의해 UIDocument 트리가 강제 리빌드되면서 바인딩이 씹히는(클릭 먹통) 현상이 있었습니다.
            // 현재는 해당 픽서 스크립트에서 LobbyUIController도 함께 재부팅(Disable->Enable)시켜주도록 수정하여 해결되었습니다.
            Assert.IsNotNull(uiDocument, "[LobbyUIController] uiDocument가 null입니다.");

            VisualElement root = uiDocument.rootVisualElement;
            if (root == null)
            {
                Debug.LogWarning("[LobbyUIController] rootVisualElement가 null이어서 UI 바인딩을 취소합니다.");
                return;
            }

            btnChangeSummon = root.Q<Button>(BUTTON_NAME);
            Assert.IsNotNull(btnChangeSummon, $"[LobbyUIController] '{BUTTON_NAME}' 버튼을 UXML에서 찾을 수 없습니다. UIDocument에 알맞은 UXML 템플릿(LobbyUI.uxml)이 할당되어 있는지 확인해 주세요.");

            if (btnChangeSummon != null)
            {
                btnChangeSummon.clicked += OnChangeSummonClicked;
                Debug.Log("[LobbyUIController] UXML 기반 '소환수 변경' UI 버튼 이벤트 바인딩 완료.");
            }
        }


        private void OnDisable()
        {
            Debug.Log("[LobbyUIController] OnDisable 호출됨. '소환수 변경' 버튼 이벤트 바인딩 해제.");
            if (btnChangeSummon != null)
            {
                btnChangeSummon.clicked -= OnChangeSummonClicked;
            }
        }

        private void OnChangeSummonClicked()
        {
            Debug.Log("[LobbyUIController] '소환수 변경' 버튼 클릭됨. LoadoutSelectionPopup을 엽니다.");
            GameStatics.UIManager.ShowPopup<LoadoutSelectionPopup>(EUIPopupType.LoadoutSelection);
        }
    }
}
