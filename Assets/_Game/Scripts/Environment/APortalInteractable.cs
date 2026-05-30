using UnityEngine;
using Unity.Netcode;
using PortalBroke.Core.Interfaces;

namespace PortalBroke.Environment
{
    /// <summary>
    /// 조건 검사(IInteractionCondition)와 실제 이동 로직(ExecutePortal)의 흐름을 통제하는 포탈 부모 클래스입니다.
    /// </summary>
    public abstract class APortalInteractable : NetworkBehaviour, IInteractable
    {
        private IInteractionCondition[] conditions;

        #region Unity Lifecycle
        protected virtual void Awake()
        {
            conditions = GetComponents<IInteractionCondition>();
        }
        #endregion

        #region Public Methods
        public void Interact(GameObject interactor)
        {
            Debug.Log("CAn Interact?");
            if (CanInteract(interactor))
            {
                Debug.Log("I Can");
                ExecutePortal(interactor);
            }
        }
        #endregion

        #region Protected Methods
        protected virtual bool CanInteract(GameObject interactor)
        {
            foreach (var condition in conditions)
            {
                if (!condition.CheckCondition(interactor))
                {
                    OnInteractFailed(interactor, condition.GetFailedMessage());
                    return false;
                }
            }
            
            return true;
        }

        protected virtual void OnInteractFailed(GameObject interactor, string message)
        {
            Debug.Log($"[Portal] {message}");
        }

        protected abstract void ExecutePortal(GameObject interactor);
        #endregion
    }
}
