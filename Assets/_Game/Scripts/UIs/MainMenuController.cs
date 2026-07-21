using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;
using UnityEngine.Assertions;
using ProjectAI.Core;
using ProjectAI.Core.Enums;
using ProjectAI.UIs.Popups;
using ProjectAI.UIs.Visuals;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace ProjectAI.UIs
{
    /// <summary>
    /// 메인 메뉴 화면의 UI 요소(UIDocument)를 제어하고, 버튼 이벤트와 씬 전환 로직을 관리하는 컨트롤러 클래스입니다.
    /// </summary>
    public class MainMenuController : MonoBehaviour
    {
        private const string START_BTN_NAME = "btn-start";
        private const string SETTINGS_BTN_NAME = "btn-settings";
        private const string CLEAR_SAVE_BTN_NAME = "btn-clear-save";
        private const string EXIT_BTN_NAME = "btn-exit";

        [Header("Scene Navigation")]
        [Tooltip("Start Game 버튼 클릭 시 이동할 씬을 선택합니다.")]
        [SerializeField]
        private ESceneType targetScene = ESceneType.Lobby;

        private UIDocument uiDocument;
        private Button btnStart;
        private Button btnSettings;
        private Button btnClearSave;
        private Button btnExit;

        private void Awake()
        {
            uiDocument = GetComponent<UIDocument>();
            Assert.IsNotNull(uiDocument, "[MainMenuController] UIDocument component not found!");

            // 무(無)애셋 배경 파티클 연출기를 동적으로 부착합니다.
            gameObject.AddComponent<MainMenuBackgroundFX>();
        }

        private void OnEnable()
        {
            Assert.IsNotNull(uiDocument, "[MainMenuController] UIDocument component is missing!");

            VisualElement root = uiDocument.rootVisualElement;
            Assert.IsNotNull(root, "[MainMenuController] rootVisualElement is null! VisualTreeAsset might not be assigned.");

            btnStart = root.Q<Button>(START_BTN_NAME);
            Assert.IsNotNull(btnStart, $"[MainMenuController] '{START_BTN_NAME}' button not found in UXML.");

            btnSettings = root.Q<Button>(SETTINGS_BTN_NAME);
            Assert.IsNotNull(btnSettings, $"[MainMenuController] '{SETTINGS_BTN_NAME}' button not found in UXML.");

            btnClearSave = root.Q<Button>(CLEAR_SAVE_BTN_NAME);
            Assert.IsNotNull(btnClearSave, $"[MainMenuController] '{CLEAR_SAVE_BTN_NAME}' button not found in UXML.");

            btnExit = root.Q<Button>(EXIT_BTN_NAME);
            Assert.IsNotNull(btnExit, $"[MainMenuController] '{EXIT_BTN_NAME}' button not found in UXML.");

            btnStart.clicked += OnStartClicked;
            btnSettings.clicked += OnSettingsClicked;
            btnClearSave.clicked += OnClearSaveClicked;
            btnExit.clicked += OnExitClicked;
        }

        private void OnDisable()
        {
            if (btnStart != null)
            {
                btnStart.clicked -= OnStartClicked;
            }
            else
            {
                Assert.IsNotNull(btnStart, $"[MainMenuController] '{START_BTN_NAME}' is null in OnDisable.");
            }

            if (btnSettings != null)
            {
                btnSettings.clicked -= OnSettingsClicked;
            }
            else
            {
                Assert.IsNotNull(btnSettings, $"[MainMenuController] '{SETTINGS_BTN_NAME}' is null in OnDisable.");
            }

            if (btnClearSave != null)
            {
                btnClearSave.clicked -= OnClearSaveClicked;
            }
            else
            {
                Assert.IsNotNull(btnClearSave, $"[MainMenuController] '{CLEAR_SAVE_BTN_NAME}' is null in OnDisable.");
            }

            if (btnExit != null)
            {
                btnExit.clicked -= OnExitClicked;
            }
            else
            {
                Assert.IsNotNull(btnExit, $"[MainMenuController] '{EXIT_BTN_NAME}' is null in OnDisable.");
            }
        }

        private void OnStartClicked()
        {
            Debug.Log($"[MainMenuController] Starting game... Loading scene: {targetScene}");
            SceneManager.LoadScene(targetScene.ToString());
        }

        private void OnSettingsClicked()
        {
            Debug.Log("[MainMenuController] Settings clicked. 환경설정 팝업을 엽니다.");
            GameStatics.UIManager.ShowPopup<SettingsPopup>(EUIPopupType.Settings);
        }

        private void OnClearSaveClicked()
        {
            Debug.Log("[MainMenuController] 세이브 데이터 초기화 요청됨. PlayerPrefs를 삭제합니다.");
            PlayerPrefs.DeleteAll();
            PlayerPrefs.Save();
            Debug.Log("[MainMenuController] 세이브 데이터가 완전히 삭제되었습니다.");
        }

        private void OnExitClicked()
        {
            Debug.Log("[MainMenuController] Exiting game...");
            Application.Quit();

#if UNITY_EDITOR
            EditorApplication.isPlaying = false;
#endif
        }
    }
}
