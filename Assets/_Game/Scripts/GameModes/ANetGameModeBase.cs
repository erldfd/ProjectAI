using System.Collections;
using UnityEngine;
using Unity.Netcode;
using PortalBroke.Core;

namespace PortalBroke.GameModes
{
    public abstract class ANetGameModeBase : NetworkBehaviour
    {
        #region Unity Lifecycle
        protected virtual void Awake()
        {
            GameStatics.RegisterGameMode(this);
        }

        protected virtual void Start()
        {
            StartCoroutine(AutoStartHostRoutine());
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
        #endregion

        #region Protected Methods
        protected virtual void OnGameModeStart() { }

        protected virtual void OnGameModeNetworkSpawn() { }

        protected void TeleportPlayersToPlayerStart()
        {
            Vector3 finalSpawnPosition = Vector3.zero;
            bool isSpawnPositionFound = false;

            // 1. Raw Coordinates 모드가 켜져 있고 ID가 비어있으면 쌩 좌표 사용
            if (string.IsNullOrEmpty(SceneTransitionData.NextSpawnPointID) && SceneTransitionData.UseRawCoordinates)
            {
                finalSpawnPosition = SceneTransitionData.RawTargetPosition;
                isSpawnPositionFound = true;
            }
            else // 2. ID가 있거나 쌩 좌표 모드가 아니면 PlayerStart 매칭 사용
            {
                PlayerStart[] allStarts = FindObjectsByType<PlayerStart>(FindObjectsSortMode.None);
                PlayerStart targetStart = null;
                
                foreach (var start in allStarts)
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
            if (isSpawnPositionFound && GameStatics.NetworkManager != null)
            {
                foreach (var client in GameStatics.NetworkManager.ConnectedClientsList)
                {
                    if (client.PlayerObject == null) continue;
                    client.PlayerObject.transform.position = finalSpawnPosition;
                }
            }

            // 4. 텔레포트 완료 후 데이터 초기화
            SceneTransitionData.NextSpawnPointID = "";
            SceneTransitionData.UseRawCoordinates = false;
        }
        #endregion

        #region Private Methods
        private IEnumerator AutoStartHostRoutine()
        {
            yield return null;

            if (GameStatics.NetworkManager == null)
            {
                yield break;
            }

            if (GameStatics.NetworkManager.IsClient || GameStatics.NetworkManager.IsServer)
            {
                yield break;
            }

            GameStatics.NetworkManager.StartHost();
        }
        #endregion
    }
}
