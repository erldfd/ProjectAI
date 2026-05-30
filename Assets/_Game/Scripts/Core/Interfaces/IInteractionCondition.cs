using UnityEngine;

namespace PortalBroke.Core.Interfaces
{
    /// <summary>
    /// 상호작용 성공 여부를 결정하는 조건의 공통 규격입니다.
    /// </summary>
    public interface IInteractionCondition
    {
        bool CheckCondition(GameObject interactor);
        string GetFailedMessage();
    }
}
