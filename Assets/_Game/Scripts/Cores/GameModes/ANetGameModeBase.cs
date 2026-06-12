using UnityEngine.Assertions;
using System.Collections;
using UnityEngine;
using Unity.Netcode;
using ProjectAI.Core;

namespace ProjectAI.GameModes
{
    /// <summary>
    /// 네트워크 게임 모드의 기본 베이스 클래스입니다.
    /// 스폰 및 시작 로직을 공통으로 처리합니다.
    /// </summary>
    public abstract class ANetGameModeBase : NetworkBehaviour
    {
        #region Unity Lifecycle
        protected virtual void Awake()
        {
            GameStatics.RegisterGameMode(this);
        }

        protected virtual void Start()
        {
            AutoStartHostAsync();
            OnGameModeStart();
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            if (IsServer)
            {
                TeleportPlayersToPlayerStart();
            }

            OnGameModeNetworkSpawn();
        }

        public override void OnDestroy()
        {
            GameStatics.UnregisterGameMode(this);
            base.OnDestroy();
        }
        #endregion

        #region Protected Methods
        protected virtual void OnGameModeStart() { }

        protected virtual void OnGameModeNetworkSpawn() { }

        public void TeleportPlayersToPlayerStart()
        {
            Assert.IsTrue(GameStatics.IsServerAuthorized, "[ANetGameModeBase] TeleportPlayersToPlayerStart는 서버에서만 호출되어야 합니다.");
            
            if (!GameStatics.IsServerAuthorized)
            {
                return;
            }
            
            Vector3 finalSpawnPosition = Vector3.zero;
            bool isSpawnPositionFound = false;

            // 1. Raw Coordinates 모드가 켜져 있고 ID가 비어있으면 쌩 좌표 사용
            if (string.IsNullOrEmpty(SceneTransitionData.NextSpawnPointID) && SceneTransitionData.ShouldUseRawCoordinates)
            {
                finalSpawnPosition = SceneTransitionData.RawTargetPosition;
                isSpawnPositionFound = true;
            }
            else // 2. ID가 있거나 쌩 좌표 모드가 아니면 PlayerStart 매칭 사용
            {
                PlayerStart[] allStarts = FindObjectsByType<PlayerStart>(FindObjectsSortMode.None);
                PlayerStart targetStart = null;
                
                foreach (PlayerStart start in allStarts)
                {
                    if (start.SpawnPointID == SceneTransitionData.NextSpawnPointID)
                    {
                        targetStart = start;
                        break;
                    }
                }

                if (targetStart == null && allStarts.Length > 0)
                {
                    targetStart = allStarts[0];
                }

                if (targetStart != null)
                {
                    finalSpawnPosition = targetStart.transform.position;
                    isSpawnPositionFound = true;
                }
            }

            // 3. 목적지를 찾았으면 실제 텔레포트 수행
            Assert.IsNotNull(GameStatics.NetworkManager, "[ANetGameModeBase] NetworkManager가 null입니다.");
            
            if (isSpawnPositionFound)
            {
                foreach (Unity.Netcode.NetworkClient client in GameStatics.NetworkManager.ConnectedClientsList)
                {
                    if (client.PlayerObject == null)
                    {
                        continue;
                    }

                    client.PlayerObject.transform.position = finalSpawnPosition;
                }
            }

            // 4. 텔레포트 완료 후 데이터 초기화
            SceneTransitionData.NextSpawnPointID = "";
            SceneTransitionData.ShouldUseRawCoordinates = false;
        }
        #endregion

        #region Private Methods
        private async void AutoStartHostAsync()
        {
            try
            {
                // 1프레임 대기 (기존 코루틴의 yield return null 역할)
                await System.Threading.Tasks.Task.Yield();

                if (GameStatics.NetworkManager == null)
                {
                    return;
                }

                if (GameStatics.NetworkManager.IsClient || GameStatics.NetworkManager.IsServer)
                {
                    return;
                }

                Assert.IsNotNull(GameStatics.MultiplayerManager, "[ANetGameModeBase] GameStatics.MultiplayerManager가 없습니다. 비정상적인 상태입니다.");
                await GameStatics.MultiplayerManager.StartHostAsync();
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[ANetGameModeBase] AutoStartHostAsync 실행 중 오류 발생: {e}");
            }
        }
        #endregion
    }
}
