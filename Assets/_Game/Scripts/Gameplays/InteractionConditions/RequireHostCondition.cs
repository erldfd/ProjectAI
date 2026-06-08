using UnityEngine;
using ProjectAI.Core;
using ProjectAI.Core.Interfaces;

namespace ProjectAI.Environment.Conditions
{
    /// <summary>
    /// 방장(Host) 권한이 있어야만 상호작용을 허용하는 조건 컴포넌트입니다.
    /// </summary>
    public class RequireHostCondition : MonoBehaviour, IInteractionCondition
    {
        #region Public Methods
        public bool CheckCondition(GameObject interactor)
        {
            Unity.Netcode.NetworkObject netObj = interactor.GetComponentInParent<Unity.Netcode.NetworkObject>();
            if (netObj != null)
            {
                return netObj.OwnerClientId == Unity.Netcode.NetworkManager.ServerClientId;
            }

            return false;
        }

        public string GetFailedMessage()
        {
            return "방장(Host)만 조작할 수 있습니다.";
        }
        #endregion
    }
}
