using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
// using Netcode.Transports.Facepunch; // Facepunch Transport 네임스페이스를 나중에 추가할 예정입니다.

namespace PortalBroke.Network
{
    public class MultiplayerServiceManager : MonoBehaviour
    {
        public static MultiplayerServiceManager Instance { get; private set; }

        [Header("Settings")]
        [Tooltip("사용할 멀티플레이 모드를 선택합니다. (Relay 또는 Steamworks)")]
        [SerializeField]
        private MultiplayerMode targetMode = MultiplayerMode.Relay;

        public MultiplayerMode CurrentMode { get; private set; }
        public IMatchmakingService MatchmakingService { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            InitializeService();
        }

        private void InitializeService()
        {
            CurrentMode = targetMode;

            // TODO: 현재 모드(CurrentMode)에 따라 알맞은 Transport 교체 및 Service(MatchmakingService) 할당 로직 작성
            Debug.Log($"[MultiplayerServiceManager] Initialized with mode: {CurrentMode}");
        }
    }
}
