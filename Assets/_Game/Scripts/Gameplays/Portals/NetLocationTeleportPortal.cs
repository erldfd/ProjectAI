using UnityEngine;
using ProjectAI.Core;
using Unity.Netcode;
using UnityEngine.Assertions;

namespace ProjectAI.Environment
{
    /// <summary>
    /// 파티 강제 이동 시 적용할 스폰 위치 모드를 정의합니다.
    /// </summary>
    public enum EPartyTeleportMode
    {
        IndividualSpawnPoints, // 개별 스폰 포인트 지정 모드
        SamePosition,          // 동일 좌표 스폰 모드
        AutoOffset             // 자동 오프셋 모드
    }

    /// <summary>
    /// 같은 씬 내부에서 특정 위치로 순간이동시켜 주는 장소 이동 전용 포탈입니다.
    /// 개인만 이동할지, 파티 전체를 이동시킬지 선택할 수 있습니다.
    /// </summary>
    public class NetLocationTeleportPortal : ANetPortalInteractable
    {
        [Header("Teleport Settings")]
        [Tooltip("체크 시 상호작용자 혼자만 이동하지 않고, 접속한 모든 파티원을 강제 이동시킵니다.")]
        [SerializeField]
        private bool shouldTeleportEntireParty = false;

        [Tooltip("파티 전체 이동 시 스폰 방식을 결정합니다.")]
        [SerializeField]
        private EPartyTeleportMode partyTeleportMode = EPartyTeleportMode.AutoOffset;

        [Tooltip("도착할 기본 목적지. 개인 이동이거나 SamePosition / AutoOffset 모드일 때 기준점이 됩니다.")]
        [SerializeField]
        private Transform destinationPoint;

        [Tooltip("IndividualSpawnPoints 모드일 때 사용할 각 파티원별 목적지 배열입니다. 인원수만큼 지정해야 합니다.")]
        [SerializeField]
        private Transform[] partySpawnPoints;

        #region Protected Methods
        protected override void ExecutePortal(GameObject interactor)
        {
            Assert.IsTrue(GameStatics.IsServerAuthorized, "[LocationTeleportPortal] ExecutePortal은 서버에서만 호출되어야 합니다.");
            
            if (!GameStatics.IsServerAuthorized)
            {
                return;
            }
            
            Assert.IsNotNull(destinationPoint, "[LocationTeleportPortal] 기본 목적지(destinationPoint)가 설정되지 않았습니다!");

            Vector3 destPos = destinationPoint.position;
            
            if (shouldTeleportEntireParty)
            {
                // 1. 파티 전체 강제 이동
                Assert.IsNotNull(GameStatics.NetworkManager, "[LocationTeleportPortal] GameStatics.NetworkManager is null.");
                
                int i = 0;
                foreach (NetworkClient client in GameStatics.NetworkManager.ConnectedClientsList)
                {
                    if (client.PlayerObject != null)
                    {
                        Vector3 targetPos = destPos;

                        switch (partyTeleportMode)
                        {
                            case EPartyTeleportMode.IndividualSpawnPoints:
                                if (partySpawnPoints != null && i < partySpawnPoints.Length && partySpawnPoints[i] != null)
                                {
                                    targetPos = partySpawnPoints[i].position;
                                }
                                else
                                {
                                    Debug.LogWarning("[LocationTeleportPortal] 파티 스폰 포인트가 부족하거나 누락되어 기본 목적지로 스폰합니다.");
                                    targetPos = destPos;
                                }
                                
                                break;
                                
                            case EPartyTeleportMode.AutoOffset:
                                targetPos = destPos + new Vector3(i * 1.5f, 0, 0);
                                break;
                                
                            case EPartyTeleportMode.SamePosition:
                                targetPos = destPos;
                                break;
                        }

                        Rigidbody2D rb = client.PlayerObject.GetComponentInChildren<Rigidbody2D>();
                        if (rb != null)
                        {
                            rb.position = targetPos;
                        }
                        else
                        {
                            client.PlayerObject.transform.position = targetPos;
                        }
                        
                        i++;
                    }
                }
            }
            else
            {
                // 2. 상호작용한 개인만 이동
                if (interactor != null)
                {
                    Rigidbody2D rb = interactor.GetComponentInChildren<Rigidbody2D>();
                    if (rb != null)
                    {
                        rb.position = destPos;
                    }
                    else
                    {
                        interactor.transform.position = destPos;
                    }
                }
            }
        }
        #endregion
    }
}
