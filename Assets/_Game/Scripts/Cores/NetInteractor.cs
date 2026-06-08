using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;
using ProjectAI.Core.Interfaces;

namespace ProjectAI.Core
{
    /// <summary>
    /// 반경 내의 IInteractable 오브젝트를 찾아 상호작용을 실행하는 범용 네트워크 컴포넌트입니다.
    /// </summary>
    public class NetInteractor : NetworkBehaviour
    {
        [Header("Interaction Settings")]
        [Tooltip("상호작용을 탐지할 반경")]
        [SerializeField]
        private float interactRadius = 1.5f;

        [Tooltip("상호작용 가능한 레이어(예: Interactable)")]
        [SerializeField]
        private LayerMask interactLayer;

        private ContactFilter2D contactFilter;
        private List<Collider2D> overlapResults = new List<Collider2D>(5);

        #region Unity Lifecycle
        private void Awake()
        {
            contactFilter.SetLayerMask(interactLayer);
            contactFilter.useTriggers = true;
        }

        // 디버깅용: 유니티 에디터 씬 뷰에서 상호작용 반경을 빨간색 원으로 그립니다.
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, interactRadius);
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// 외부(플레이어 키 입력, 펫 AI 등)에서 호출하여 반경 내의 가장 가까운 물체와 상호작용을 시도합니다.
        /// </summary>
        public void TryInteract()
        {
            if (!IsOwner)
            {
                return;
            }

            int count = Physics2D.OverlapCircle(transform.position, interactRadius, contactFilter, overlapResults);
            float closestDistance = float.MaxValue;
            IInteractable closestInteractable = null;

            for (int i = 0; i < count; i++)
            {
                Collider2D col = overlapResults[i];
                IInteractable interactable = col.GetComponentInParent<IInteractable>();
                
                if (interactable == null)
                {
                    continue;
                }

                float distanceSqr = (col.transform.position - transform.position).sqrMagnitude;
                
                if (distanceSqr >= closestDistance)
                {
                    continue;
                }

                closestDistance = distanceSqr;
                closestInteractable = interactable;
            }

            if (closestInteractable == null)
            {
                return;
            }

            if (closestInteractable is not Component component || component.TryGetComponent(out NetworkObject netObj) == false)
            {
                return;
            }

            RequestInteractRpc(netObj.NetworkObjectId);
        }
        #endregion

        #region RPCs
        [Rpc(SendTo.Server)]
        private void RequestInteractRpc(ulong targetObjectId)
        {
            if (!NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(targetObjectId, out NetworkObject targetObj))
            {
                return;
            }

            IInteractable interactable = targetObj.GetComponentInChildren<IInteractable>();
            if (interactable == null)
            {
                return;
            }

            interactable.Interact(gameObject);
        }
        #endregion
    }
}
