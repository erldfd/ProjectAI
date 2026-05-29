using Unity.Netcode;
using UnityEngine;
using PortalBroke.GameModes;

namespace PortalBroke.Core
{
    /// <summary>
    /// 게임 내 모든 핵심 매니저에 전역적으로 접근하기 위한 Gateway 클래스입니다.
    /// 외부에서의 무단 덮어쓰기를 방지하기 위해 등록 메서드를 통해서만 세팅을 허용합니다.
    /// </summary>
    public static class GameStatics
    {
        public static GameManager GameManager { get; private set; }
        public static GameModeBase CurrentMode { get; private set; }

        public static NetworkManager NetworkManager => NetworkManager.Singleton;

        /// <summary>
        /// GameManager가 최초 생성될 때 자신을 등록합니다. 중복 등록을 철저히 차단합니다.
        /// </summary>
        public static void RegisterManager(GameManager manager)
        {
            if (GameManager != null)
            {
                Debug.LogError("[GameStatics] 누군가 이미 존재하는 GameManager를 덮어쓰려고 시도했습니다!");
                return;
            }
            GameManager = manager;
        }

        /// <summary>
        /// 씬이 변경되어 새로운 GameMode가 켜질 때 자신을 등록합니다.
        /// </summary>
        public static void RegisterMode(GameModeBase mode)
        {
            CurrentMode = mode;
        }
    }
}
