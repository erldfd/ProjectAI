#if UNITY_EDITOR
using UnityEngine;
using ProjectAI.Core;
using UnityEngine.Assertions;

namespace ProjectAI.Network
{
    /// <summary>
    /// 개발 중 방 생성(Host) 및 조인 코드(Join Code) 입력 테스트를 위한 임시 GUI 컴포넌트입니다.
    /// </summary>
    public class MultiplayerDemoUI : MonoBehaviour
    {
        private string _joinCodeInput = "";
        private string _hostedCode = "";
        private bool _isConnecting = false;

        private void OnGUI()
        {
            // 좌측 상단에 UI 패널 그리기
            GUILayout.BeginArea(new Rect(10, 10, 300, 300));
            GUILayout.Box("Multiplayer Test UI");

            if (_isConnecting)
            {
                GUILayout.Label("연결 중입니다...");
            }
            else
            {
                // --- 호스트 기능 ---
                if (GameStatics.NetworkManager != null && GameStatics.NetworkManager.IsHost)
                {
                    GUILayout.Label("현재 자동/수동 호스트 중입니다.");
                    string cachedCode = GameStatics.MultiplayerManager != null ? GameStatics.MultiplayerManager.LastJoinCode : "";
                    if (!string.IsNullOrEmpty(cachedCode))
                    {
                        GUILayout.Space(10);
                        GUILayout.Label("발급된 Join Code (복사해서 다른 클라이언트에 전달):");
                        GUILayout.TextField(cachedCode);
                    }
                }
                else
                {
                    if (GUILayout.Button("방 만들기 (Start Host)", GUILayout.Height(40)))
                    {
                        StartHostProcessAsync();
                    }

                    if (!string.IsNullOrEmpty(_hostedCode))
                    {
                        GUILayout.Space(10);
                        GUILayout.Label("발급된 Join Code (복사해서 다른 클라이언트에 전달):");
                        _hostedCode = GUILayout.TextField(_hostedCode);
                    }
                }

                GUILayout.Space(30);

                // --- 클라이언트 참가 기능 ---
                GUILayout.Label("참가할 방의 Join Code 입력:");
                _joinCodeInput = GUILayout.TextField(_joinCodeInput);

                if (GUILayout.Button("방 참가 (Join Client)", GUILayout.Height(40)))
                {
                    StartClientProcessAsync();
                }
            }

            GUILayout.EndArea();
        }

        private async void StartHostProcessAsync()
        {
            Assert.IsNotNull(GameStatics.MultiplayerManager, "[MultiplayerDemoUI] GameStatics.MultiplayerManager가 없습니다.");
            if (GameStatics.MultiplayerManager == null)
            {
                Debug.LogError("GameStatics.MultiplayerManager가 없습니다. GameManager가 씬에 있는지 확인하세요.");
                return;
            }

            _isConnecting = true;
            // 호스트 생성 요청 및 발급된 방 코드 받기
            _hostedCode = await GameStatics.MultiplayerManager.StartHostAsync();
            _isConnecting = false;

            if (string.IsNullOrEmpty(_hostedCode))
            {
                Debug.LogError("방 생성에 실패했습니다.");
            }
            else
            {
                Debug.Log($"방 생성 성공. 코드를 전달하세요: {_hostedCode}");
            }
        }

        private async void StartClientProcessAsync()
        {
            if (string.IsNullOrEmpty(_joinCodeInput))
            {
                Debug.LogWarning("참가할 Join Code를 입력해주세요.");
                return;
            }

            Assert.IsNotNull(GameStatics.MultiplayerManager, "[MultiplayerDemoUI] GameStatics.MultiplayerManager가 없습니다.");
            if (GameStatics.MultiplayerManager == null)
            {
                Debug.LogError("GameStatics.MultiplayerManager가 없습니다.");
                return;
            }

            _isConnecting = true;
            // 입력된 코드로 참가 요청
            bool isSuccess = await GameStatics.MultiplayerManager.StartClientAsync(_joinCodeInput);
            _isConnecting = false;

            if (!isSuccess)
            {
                Debug.LogError("방 참가에 실패했습니다.");
            }
            else
            {
                Debug.Log("방 참가에 성공했습니다!");
            }
        }
    }
}
#endif
