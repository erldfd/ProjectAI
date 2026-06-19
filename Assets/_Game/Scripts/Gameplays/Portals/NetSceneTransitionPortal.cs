using UnityEngine;
using ProjectAI.Core;
using ProjectAI.Core.Enums;
using UnityEngine.Assertions;

namespace ProjectAI.Environment
{
    /// <summary>
    /// 씬(Scene) 이동 전용 포탈입니다.
    /// NetworkManager.SceneManager.LoadScene()을 통해 서버 단위로 씬을 전환합니다.
    /// </summary>
    [UnityEngine.Scripting.APIUpdating.MovedFrom(true, "ProjectAI.Environment", "Assembly-CSharp", "SceneTransitionPortal")]
    public class NetSceneTransitionPortal : ANetPortalInteractable
    {
        [Tooltip("이동할 대상 씬")]
        [SerializeField]
        private ESceneType targetScene = ESceneType.Dungeon;

        [Header("Spawn Settings")]
        [Tooltip("이동한 씬에서 착지할 특정 스폰 포인트의 ID (이 값이 있으면 좌표보다 우선합니다)")]
        [SerializeField]
        private string targetSpawnPointID = "";

        [Tooltip("체크 시 아래의 쌩 좌표(Raw)로 직접 이동합니다 (targetSpawnPointID가 비워져 있어야 작동)")]
        [SerializeField]
        private bool shouldUseRawCoordinates = false;

        [Tooltip("직접 이동할 목표 월드 좌표 (Vector2)")]
        [SerializeField]
        private Vector2 rawTargetPosition = Vector2.zero;

        #region Protected Methods
        protected override void ExecutePortal(GameObject interactor)
        {
            Assert.IsTrue(GameStatics.IsServerAuthorized, "[NetSceneTransitionPortal] ExecutePortal은 서버에서만 호출되어야 합니다.");
            
            if (!GameStatics.IsServerAuthorized)
            {
                return;
            }
            
            SceneTransitionData.NextSpawnPointID = targetSpawnPointID;
            SceneTransitionData.ShouldUseRawCoordinates = shouldUseRawCoordinates;
            SceneTransitionData.RawTargetPosition = rawTargetPosition;

            Assert.IsNotNull(GameStatics.NetworkManager, "[NetSceneTransitionPortal] NetworkManager가 null입니다.");
            string sceneName = targetScene.ToString();
            GameStatics.NetworkManager.SceneManager.LoadScene(sceneName, UnityEngine.SceneManagement.LoadSceneMode.Single);
        }
        #endregion
    }
}
