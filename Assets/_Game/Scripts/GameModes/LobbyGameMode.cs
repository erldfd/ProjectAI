using UnityEngine;
using PortalBroke.Core;

namespace PortalBroke.GameModes
{
    /// <summary>
    /// 로비 씬 전용 게임 매니저입니다.
    /// 플레이어 접속 처리 및 호스트의 게임 시작 씬 전환을 담당합니다.
    /// </summary>
    public class LobbyGameMode : GameModeBase
    {
        protected override void OnGameModeStart()
        {
            Debug.Log("[LobbyGameMode] 로비 씬 게임 모드 초기화 완료.");
        }

        // 임시 UI 버튼과 연결될 테스트용 메서드
        public void StartHost()
        {
            if (GameStatics.NetworkManager != null)
            {
                GameStatics.NetworkManager.StartHost();
                Debug.Log("[LobbyGameMode] 호스트로 서버를 시작했습니다.");
                
                // 호스트가 되면 던전 씬으로 씬을 전환합니다.
                GameStatics.NetworkManager.SceneManager.LoadScene("DungeonScene", UnityEngine.SceneManagement.LoadSceneMode.Single);
            }
        }

        // 임시 UI 버튼과 연결될 테스트용 메서드
        public void StartClient()
        {
            if (GameStatics.NetworkManager != null)
            {
                GameStatics.NetworkManager.StartClient();
                Debug.Log("[LobbyGameMode] 클라이언트로 서버에 접속을 시도합니다.");
            }
        }
    }
}
