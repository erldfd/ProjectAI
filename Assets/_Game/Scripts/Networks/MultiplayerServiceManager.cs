using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using System.Threading.Tasks;
using ProjectAI.Core;

namespace ProjectAI.Network
{
    /// <summary>
    /// 지원하는 멀티플레이 연결 모드의 종류를 정의합니다.
    /// </summary>
    public enum EMultiplayerMode
    {
        Relay,
        Steamworks
    }

    /// <summary>
    /// 현재 활성화된 매치메이킹 서비스(Relay, Steam 등)를 생성 및 관리하는 래퍼 매니저입니다.
    /// </summary>
    public class MultiplayerServiceManager : MonoBehaviour
    {
        [Header("Settings")]
        [Tooltip("사용할 멀티플레이 모드를 선택합니다. (Relay 또는 Steamworks)")]
        [SerializeField]
        private EMultiplayerMode targetMode = EMultiplayerMode.Relay;

        public EMultiplayerMode CurrentMode { get; private set; }
        public IMatchmakingService MatchmakingService { get; private set; }
        public string LastJoinCode { get; private set; } // 발급된 코드 저장용

        private void Start()
        {
            InitializeService();
        }

        private void InitializeService()
        {
            CurrentMode = targetMode;

            if (CurrentMode == EMultiplayerMode.Relay)
            {
                MatchmakingService = new RelayMatchmakingService();
                
                if (GameStatics.NetworkManager != null)
                {
                    UnityTransport transport = GameStatics.NetworkManager.GetComponent<UnityTransport>();
                    if (transport != null)
                    {
                        GameStatics.NetworkManager.NetworkConfig.NetworkTransport = transport;
                    }
                    else
                    {
                        Debug.LogError("[MultiplayerServiceManager] UnityTransport component is missing on NetworkManager.");
                    }
                }
                else
                {
                    Debug.LogWarning("[MultiplayerServiceManager] GameStatics.NetworkManager is null. Cannot set NetworkTransport.");
                }
            }
            else if (CurrentMode == EMultiplayerMode.Steamworks)
            {
                // TODO: 3단계에서 SteamMatchmakingService 할당 로직 추가 예정
            }

            Debug.Log($"[MultiplayerServiceManager] Initialized with mode: {CurrentMode}");
        }

        public async Task<string> StartHost()
        {
            if (MatchmakingService == null)
            {
                return null;
            }

            LastJoinCode = await MatchmakingService.StartHostAsync();
            return LastJoinCode;
        }

        public async Task<bool> StartClient(string joinData)
        {
            if (MatchmakingService == null)
            {
                return false;
            }

            return await MatchmakingService.StartClientAsync(joinData);
        }

        public void LeaveGame()
        {
            if (MatchmakingService != null)
            {
                MatchmakingService.LeaveGame();
            }
        }
    }
}
