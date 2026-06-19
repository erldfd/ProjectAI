using UnityEngine;
using Unity.Netcode;
using ProjectAI.Core.Interfaces;
using UnityEngine.Assertions;
using ProjectAI.Core;

namespace ProjectAI.Environment
{
    /// <summary>
    /// 조건 검사(IInteractionCondition)와 실제 이동 로직(ExecutePortal)의 흐름을 통제하는 포탈 부모 클래스입니다.
    /// </summary>
    [UnityEngine.Scripting.APIUpdating.MovedFrom(true, "ProjectAI.Environment", "Assembly-CSharp", "APortalInteractable")]
    public abstract class ANetPortalInteractable : NetworkBehaviour, IInteractable
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
            Assert.IsTrue(GameStatics.IsServerAuthorized, "[ANetPortalInteractable] Interact는 서버에서만 호출되어야 합니다.");
            
            if (!GameStatics.IsServerAuthorized)
            {
                return;
            }
            
            if (!CanInteract(interactor))
            {
                return;
            }
            
            ExecutePortal(interactor);
        }
        #endregion

        #region Protected Methods
        protected virtual bool CanInteract(GameObject interactor)
        {
            foreach (IInteractionCondition condition in conditions)
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
