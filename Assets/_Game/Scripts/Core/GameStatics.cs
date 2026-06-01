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

        public static MultiplayerServiceManager MultiplayerManager => GameManager != null ? GameManager.MultiplayerService : null;

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
