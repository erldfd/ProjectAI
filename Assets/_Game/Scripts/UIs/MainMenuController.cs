using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;
using UnityEngine.Assertions;
using ProjectAI.Core.Enums;

namespace ProjectAI.UIs
{
    /// <summary>
    /// 메인 메뉴 화면의 UI 요소(UIDocument)를 제어하고, 버튼 이벤트와 씬 전환 로직을 관리하는 컨트롤러 클래스입니다.
    /// </summary>
    public class MainMenuController : MonoBehaviour
    {
        [Header("Scene Navigation")]
        [Tooltip("Start Game 버튼 클릭 시 이동할 씬을 선택합니다.")]
        [SerializeField]
        private ESceneType targetScene = ESceneType.Lobby;

        private UIDocument uiDocument;
        private Button btnStart;
        private Button btnSettings;
        private Button btnExit;

        private void OnEnable()
        {
            uiDocument = GetComponent<UIDocument>();
            Assert.IsNotNull(uiDocument, "[MainMenuController] UIDocument component is missing!");

            VisualElement root = uiDocument.rootVisualElement;
            Assert.IsNotNull(root, "[MainMenuController] rootVisualElement is null! VisualTreeAsset might not be assigned.");

            btnStart = root.Q<Button>("btn-start");
            btnSettings = root.Q<Button>("btn-settings");
            btnExit = root.Q<Button>("btn-exit");

            Assert.IsNotNull(btnStart, "[MainMenuController] 'btn-start' button not found in UXML.");
            Assert.IsNotNull(btnSettings, "[MainMenuController] 'btn-settings' button not found in UXML.");
            Assert.IsNotNull(btnExit, "[MainMenuController] 'btn-exit' button not found in UXML.");

            btnStart.clicked += OnStartClicked;
            btnSettings.clicked += OnSettingsClicked;
            btnExit.clicked += OnExitClicked;
        }

        private void OnDisable()
        {
            Assert.IsNotNull(btnStart, "[MainMenuController] 'btn-start' is null in OnDisable.");
            btnStart.clicked -= OnStartClicked;

            Assert.IsNotNull(btnSettings, "[MainMenuController] 'btn-settings' is null in OnDisable.");
            btnSettings.clicked -= OnSettingsClicked;

            Assert.IsNotNull(btnExit, "[MainMenuController] 'btn-exit' is null in OnDisable.");
            btnExit.clicked -= OnExitClicked;
        }

        private void OnStartClicked()
        {
            Debug.Log($"[MainMenuController] Starting game... Loading scene: {targetScene}");
            SceneManager.LoadScene(targetScene.ToString());
        }

        private void OnSettingsClicked()
        {
            Debug.Log("[MainMenuController] Settings clicked. (Not implemented yet)");
            // TODO: 환경설정 UI 패널 열기 로직 구현 예정
        }

        private void OnExitClicked()
        {
            Debug.Log("[MainMenuController] Exiting game...");
            Application.Quit();

#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#endif
        }
    }
}
