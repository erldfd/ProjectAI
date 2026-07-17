#if UNITY_EDITOR
using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using ProjectAI.UIs;

namespace ProjectAI.EditorTools
{
    /// <summary>
    /// [에디터 전용] 유니티 MPPM(Multiplayer Play Mode) 환경에서 가상 플레이어 창의 
    /// UI Toolkit 마우스 입력 좌표가 NaN으로 계산되는 버그를 우회하는 스크립트입니다.
    /// 게임 시작 직후 UIDocument와 관련 컨트롤러들을 껐다 켜서 내부 렌더링/이벤트 바인딩을 강제 리셋합니다.
    /// </summary>
    public class MPPM_UIFixer : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoInitialize()
        {
            // 게임 시작 시 씬에 숨겨진 오브젝트를 자동 생성하여 스크립트 실행
            GameObject fixerObj = new GameObject("[MPPM_UIFixer]");
            fixerObj.AddComponent<MPPM_UIFixer>();
            DontDestroyOnLoad(fixerObj); // 씬 전환 중 파괴 방지
        }

        private bool hasFocused = false;
        private bool hasReset = false;

        private void Update()
        {
            // 1단계: 가상 창이 처음으로 클릭되어 포커스(활성화)를 얻을 때까지 기다립니다.
            if (!hasFocused && Application.isFocused)
            {
                hasFocused = true;
                
                // 2단계: 포커스를 얻은 바로 그 시점부터, UI로 전달될 인풋(NaN 유발)을 낚아채기 위해 훅을 겁니다.
                InputSystem.onEvent += OnFirstInputAfterFocus;
            }
        }

        private void OnDisable()
        {
            InputSystem.onEvent -= OnFirstInputAfterFocus;
        }

        private void OnFirstInputAfterFocus(InputEventPtr eventPtr, InputDevice device)
        {
            // 센서나 내부 상태 업데이트 이벤트가 아닌, 실제 유저의 마우스/포인터/키보드 조작인지 필터링합니다.
            if (!(device is Mouse || device is Pointer || device is Keyboard))
            {
                Debug.Log($"<color=cyan>[MPPM_UIFixer]</color> 시스템 백그라운드 이벤트 무시 (Device: {device?.name})");
                return;
            }

            if (!hasReset)
            {
                hasReset = true;
                
                // 이벤트가 한 번 발동하면 즉시 훅을 제거합니다.
                InputSystem.onEvent -= OnFirstInputAfterFocus;

                // UI 리셋 코루틴을 즉시 실행하여, 포커스 이후 들어온 첫 인풋(NaN)이 UI Toolkit에 닿기 전에 기절시킵니다.
                StartCoroutine(ResetUIRoutine());
            }
        }

        private IEnumerator ResetUIRoutine()
        {
            Debug.Log("<color=cyan>[MPPM_UIFixer]</color> 창 포커스 감지됨! 첫 인풋(NaN)이 들어가기 전에 UIDocument를 강제 기절시킵니다.");

            UIDocument[] allDocuments = FindObjectsByType<UIDocument>(FindObjectsSortMode.None);

            // 1. 포커스가 들어온 첫 프레임: UI를 통째로 꺼버려서 NaN 마우스 클릭 이벤트를 씹어버림(무시함)
            foreach (UIDocument doc in allDocuments)
            {
                if (doc == null) 
                {
                    continue;
                }
                
                doc.enabled = false;
            }

            // 1프레임 대기 (위험한 첫 클릭 이벤트를 안전하게 흘려보냄)
            yield return null;

            // 2. 다음 프레임: 안전해진 상태에서 UI 및 컨트롤러 재활성화
            foreach (UIDocument doc in allDocuments)
            {
                if (doc == null)
                {
                    continue;
                }

                doc.enabled = true;

                MainMenuController mainMenu = doc.GetComponent<MainMenuController>();
                if (mainMenu == null || !mainMenu.enabled)
                {
                    continue;
                }

                mainMenu.enabled = false;
                mainMenu.enabled = true;
            }

            Debug.Log("<color=cyan>[MPPM_UIFixer]</color> 강제 리셋 완료. 스크립트를 자폭(Destroy)합니다.");
            Destroy(gameObject);
        }
    }
}
#endif
