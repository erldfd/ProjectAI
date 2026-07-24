using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using ProjectAI.Core;
using ProjectAI.Core.Enums;
using ProjectAI.Core.Skills;

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
            EventManager.AddListener<SLocalPlayerSpawnedEvent>(OnLocalPlayerSpawnedEvent);
        }

        private void OnDisable()
        {
            EventManager.RemoveListener<SShowRewardPopupEvent>(OnShowRewardPopupEvent);
            EventManager.RemoveListener<SLocalPlayerSpawnedEvent>(OnLocalPlayerSpawnedEvent);
        }

        private void OnShowRewardPopupEvent(SShowRewardPopupEvent evt)
        {
            CorePurificationPopup popup = uiManager.ShowPopup<CorePurificationPopup>(EUIPopupType.CorePurification);
            popup.SetupPopup(evt.SummonRewardIndex, evt.SummonUpgradeRewardIndex, evt.PlayerUpgradeRewardIndex, evt.LocalPlayer);
        }

        private void OnLocalPlayerSpawnedEvent(SLocalPlayerSpawnedEvent evt)
        {
            if (!CodexManager.HasSavedLoadout())
            {
                uiManager.ShowPopup<LoadoutSelectionPopup>(EUIPopupType.LoadoutSelection);
                return;
            }

            if (evt.PlayerObject == null)
            {
                Debug.LogWarning("[UIEventListener] evt.PlayerObject가 null이어서 선택 팝업을 엽니다.");
                uiManager.ShowPopup<LoadoutSelectionPopup>(EUIPopupType.LoadoutSelection);
                return;
            }

            NetSkillComponent skillComponent = evt.PlayerObject.GetComponentInChildren<NetSkillComponent>();
            if (skillComponent == null)
            {
                Debug.LogWarning("[UIEventListener] evt.PlayerObject에서 NetSkillComponent를 찾지 못해 선택 팝업을 엽니다.");
                uiManager.ShowPopup<LoadoutSelectionPopup>(EUIPopupType.LoadoutSelection);
                return;
            }

            List<int> savedLoadout = CodexManager.GetSavedLoadout();
            List<int> validLoadout = new List<int>();

            for (int i = 0; i < savedLoadout.Count; i++)
            {
                if (CodexManager.IsSkillUnlocked(savedLoadout[i]))
                {
                    validLoadout.Add(savedLoadout[i]);
                }
            }

            if (validLoadout.Count == 0)
            {
                Debug.LogWarning("[UIEventListener] 저장된 로드아웃 중 유효하게 해금된 스킬이 없어 선택 팝업을 엽니다.");
                uiManager.ShowPopup<LoadoutSelectionPopup>(EUIPopupType.LoadoutSelection);
                return;
            }

            skillComponent.EquipLoadoutServerRpc(validLoadout.ToArray());
            Debug.Log($"[UIEventListener] 이전에 저장된 소환수 로드아웃({validLoadout.Count}개)을 자동 장착합니다.");
        }
    }
}
