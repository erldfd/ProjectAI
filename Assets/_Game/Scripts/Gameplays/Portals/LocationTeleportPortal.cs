using UnityEngine;
using ProjectAI.Core;

namespace ProjectAI.Environment
{
    /// <summary>
    /// 같은 씬 내부에서 특정 위치로 순간이동시켜 주는 장소 이동 전용 포탈입니다.
    /// 개인만 이동할지, 파티 전체를 이동시킬지 선택할 수 있습니다.
    /// </summary>
    public class LocationTeleportPortal : APortalInteractable
    {
        [Header("Teleport Settings")]
        [Tooltip("도착할 목적지 트랜스폼 (맵에 배치된 빈 오브젝트 등을 끌어다 놓으세요)")]
        [SerializeField]
        private Transform destinationPoint;

        [Tooltip("체크 시 상호작용자 혼자만 이동하지 않고, 접속한 모든 파티원을 목적지로 함께 강제 이동시킵니다.")]
        [SerializeField]
        private bool shouldTeleportEntireParty = false;

        #region Protected Methods
        protected override void ExecutePortal(GameObject interactor)
        {
            if (destinationPoint == null)
            {
                Debug.LogWarning("[LocationTeleportPortal] 목적지(destinationPoint)가 설정되지 않았습니다! 인스펙터를 확인해주세요.");
                return;
            }

            Vector3 destPos = destinationPoint.position;
            
            Debug.Log(shouldTeleportEntireParty);
            if (shouldTeleportEntireParty)
            {
                // 1. 파티 전체 강제 이동
                if (GameStatics.NetworkManager != null)
                {
                    foreach (var client in GameStatics.NetworkManager.ConnectedClientsList)
                    {
                        if (client.PlayerObject != null)
                        {
                            client.PlayerObject.transform.position = destPos;
                        }
                    }
                }
            }
            else
            {
                // 2. 상호작용한 개인만 이동
                if (interactor != null)
                {
                    interactor.transform.position = destPos;
                }
            }
        }
        #endregion
    }
}
