using UnityEngine;
using ProjectAI.Core;

namespace ProjectAI.GameModes
{
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
