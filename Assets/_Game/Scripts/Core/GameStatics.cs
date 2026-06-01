using Unity.Netcode;
using UnityEngine;
using PortalBroke.GameModes;
using PortalBroke.Network;

namespace PortalBroke.Core
{
    public static class GameStatics
    {
        public static GameManager GameManager { get; private set; }
        public static ANetGameModeBase CurrentMode { get; private set; }

        public static NetworkManager NetworkManager => NetworkManager.Singleton;

        private static MultiplayerServiceManager _multiplayerManager;
        public static MultiplayerServiceManager MultiplayerManager
        {
            get
            {
                if (_multiplayerManager == null && GameManager != null)
                {
                    _multiplayerManager = GameManager.GetComponent<MultiplayerServiceManager>();
                }
                return _multiplayerManager;
            }
        }

        public static void RegisterManager(GameManager manager)
        {
            if (GameManager != null)
            {
                Debug.LogError("[GameStatics] 누군가 이미 존재하는 GameManager를 덮어쓰려고 시도했습니다!");
                return;
            }

            GameManager = manager;
        }

        public static void RegisterGameMode(ANetGameModeBase mode)
        {
            CurrentMode = mode;
        }
    }
}
