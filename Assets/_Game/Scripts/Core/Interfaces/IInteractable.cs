using UnityEngine;

namespace ProjectAI.Core.Interfaces
{
    /// <summary>
    /// 플레이어, 펫 등이 상호작용(Interact)할 수 있는 모든 오브젝트(포탈, 상자 등)의 공통 규격입니다.
    /// </summary>
    public interface IInteractable
    {
        void Interact(GameObject interactor);
    }
}
