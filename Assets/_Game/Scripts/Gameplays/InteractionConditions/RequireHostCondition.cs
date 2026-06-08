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
            // [주의] 이 조건 체크는 "서버"에서 실행됨. 
            // 따라서 NetworkManager.IsServer로 체크하면 무조건 true가 나와 방장 외 클라이언트도 통과되는 버그가 발생함.
            // 상호작용 주체(interactor)의 소유자(Owner)가 서버 자신(ServerClientId)인지 대조해야 정확히 방장만 통과됨.
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
