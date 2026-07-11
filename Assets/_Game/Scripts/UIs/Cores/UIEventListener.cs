using ProjectAI.Core;
using ProjectAI.Core.Enums;
using ProjectAI.SOs;
using UnityEngine;
using UnityEngine.Assertions;

namespace ProjectAI.UIs.Cores
{
    /// <summary>
    /// EventManager의 UI 이벤트를 수신하여 UIManager에 전달하는 중개자 역할을 합니다.
    /// UIManager와 동일한 게임 오브젝트에 부착되어야 합니다.
    /// </summary>
    [RequireComponent(typeof(UIManager))]
    public class UIEventListener : MonoBehaviour
    {


        private UIManager uiManager;

        private void Awake()
        {
            uiManager = GetComponent<UIManager>();
            Assert.IsNotNull(uiManager, "[UIEventListener] UIManager 컴포넌트를 찾을 수 없습니다.");
        }

        private void OnEnable()
        {
            EventManager.AddListener<SShowRewardPopupEvent>(OnShowRewardPopupEvent);
        }

        private void OnDisable()
        {
            EventManager.RemoveListener<SShowRewardPopupEvent>(OnShowRewardPopupEvent);
        }

        private void OnShowRewardPopupEvent(SShowRewardPopupEvent evt)
        {
            CorePurificationPopup popup = uiManager.ShowPopup<CorePurificationPopup>(EUIPopupType.CorePurification);
            popup.SetupPopup(evt.SummonRewardIndex, evt.SummonUpgradeRewardIndex, evt.PlayerUpgradeRewardIndex, evt.LocalPlayer);
        }
    }
}
