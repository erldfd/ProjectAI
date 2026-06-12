using UnityEngine;
using ProjectAI.Core;

namespace ProjectAI.GameModes
{
    /// <summary>
    /// 로비 씬 전용 게임 모드 클래스입니다.
    /// 호스트/클라이언트 시작 등을 관리합니다.
    /// </summary>
    public class NetLobbyGameMode : ANetGameModeBase
    {
        #region Public Methods
        public void StartHostManually()
        {
            if (GameStatics.NetworkManager != null)
            {
                GameStatics.NetworkManager.StartHost();
                GameStatics.NetworkManager.SceneManager.LoadScene("DungeonScene", UnityEngine.SceneManagement.LoadSceneMode.Single);
            }
        }

        public void StartClientManually()
        {
            if (GameStatics.NetworkManager != null)
            {
                GameStatics.NetworkManager.StartClient();
            }
        }
        #endregion
    }
}
