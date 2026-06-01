using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using System.Threading.Tasks;

namespace PortalBroke.Network
{
    public class MultiplayerServiceManager : MonoBehaviour
    {
        [Header("Settings")]
        [Tooltip("사용할 멀티플레이 모드를 선택합니다. (Relay 또는 Steamworks)")]
        [SerializeField]
        private MultiplayerMode targetMode = MultiplayerMode.Relay;

        public MultiplayerMode CurrentMode { get; private set; }
        public IMatchmakingService MatchmakingService { get; private set; }
        public string LastJoinCode { get; private set; } // 발급된 코드 저장용

        private void Start()
        {
            InitializeService();
        }

        private void InitializeService()
        {
            CurrentMode = targetMode;

            if (CurrentMode == MultiplayerMode.Relay)
            {
                MatchmakingService = new RelayMatchmakingService();
                
                if (NetworkManager.Singleton != null)
                {
                    UnityTransport transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
                    if (transport != null)
                    {
                        NetworkManager.Singleton.NetworkConfig.NetworkTransport = transport;
                    }
                    else
                    {
                        Debug.LogError("[MultiplayerServiceManager] UnityTransport component is missing on NetworkManager.");
                    }
                }
                else
                {
                    Debug.LogWarning("[MultiplayerServiceManager] NetworkManager.Singleton is null. Cannot set NetworkTransport.");
                }
            }
            else if (CurrentMode == MultiplayerMode.Steamworks)
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
