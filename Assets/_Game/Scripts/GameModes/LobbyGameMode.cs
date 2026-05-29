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
            // 로비는 오프라인 상태에서 작동하는 UI가 대부분이므로 여기서 초기화합니다.
            Debug.Log("[LobbyGameMode] 로비 씬 오프라인 초기화 완료.");
        }

        public void StartHost()
        {
            if (GameStatics.NetworkManager != null)
            {
                GameStatics.NetworkManager.StartHost();
                Debug.Log("[LobbyGameMode] 호스트로 서버를 시작했습니다.");
                GameStatics.NetworkManager.SceneManager.LoadScene("DungeonScene", UnityEngine.SceneManagement.LoadSceneMode.Single);
            }
        }

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
