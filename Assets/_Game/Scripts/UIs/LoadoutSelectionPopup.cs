using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.Assertions;
using ProjectAI.Core;
using ProjectAI.Core.Skills;
using ProjectAI.Players;
using ProjectAI.UIs.Cores;
using Unity.Netcode;

namespace ProjectAI.UIs
{
    /// <summary>
    /// 게임 시작 시 도감(CodexManager)에 해금된 소환수 목록을 보여주고, 최대 3마리를 픽업하는 MVP 임시 팝업입니다.
    /// </summary>
    public class LoadoutSelectionPopup : AUIPopup
    {
        private const int MAX_SELECT = 3;

        public override bool IsOverlay => true;

        private ScrollView skillListScrollView;
        private Button startButton;

        private List<int> selectedSkillIds = new List<int>();
        private NetPlayerController cachedPlayerController;

        protected override void OnInitialize()
        {
            Debug.Log("[LoadoutSelectionPopup] 팝업 초기화 시작...");
            Assert.IsNotNull(base.RootElement, "[LoadoutSelectionPopup] RootElement가 null입니다.");

            skillListScrollView = base.RootElement.Q<ScrollView>("SkillListScrollView");
            Assert.IsNotNull(skillListScrollView, "[LoadoutSelectionPopup] 'SkillListScrollView'를 찾을 수 없습니다.");

            startButton = base.RootElement.Q<Button>("StartButton");
            Assert.IsNotNull(startButton, "[LoadoutSelectionPopup] 'StartButton'을 찾을 수 없습니다.");

            startButton.RegisterCallback<ClickEvent>(OnStartClicked);
            startButton.SetEnabled(false); // 최소 1개라도 고르면 활성화
        }

        protected override void OnShow()
        {
            if (GameStatics.NetworkManager != null && GameStatics.NetworkManager.LocalClient != null)
            {
                NetworkObject playerObj = GameStatics.NetworkManager.LocalClient.PlayerObject;
                if (playerObj != null)
                {
                    cachedPlayerController = playerObj.GetComponentInChildren<NetPlayerController>();
                    if (cachedPlayerController != null)
                    {
                        cachedPlayerController.SetInputActive(false);
                    }
                }
            }
            
            PopulateSkillList();
        }

        protected override void OnHide()
        {
            if (cachedPlayerController != null)
            {
                cachedPlayerController.SetInputActive(true);
                cachedPlayerController = null;
            }
            
            selectedSkillIds.Clear();
            skillListScrollView.Clear();
        }

        private void PopulateSkillList()
        {
            selectedSkillIds.Clear();
            skillListScrollView.Clear();

            List<int> unlockedIds = CodexManager.GetUnlockedSkillIds();

            if (unlockedIds.Count == 0)
            {
                Debug.LogWarning("[LoadoutSelectionPopup] 해금된 소환수가 전혀 없습니다! 팝업을 닫고 기본 상태로 시작합니다.");
                startButton.SetEnabled(false);
                GameStatics.UIManager.ClosePopup(this);
                return;
            }

            List<int> savedLoadout = CodexManager.GetSavedLoadout();
            for (int i = 0; i < savedLoadout.Count; i++)
            {
                if (unlockedIds.Contains(savedLoadout[i]) && !selectedSkillIds.Contains(savedLoadout[i]))
                {
                    selectedSkillIds.Add(savedLoadout[i]);
                }
            }

            startButton.SetEnabled(selectedSkillIds.Count > 0);

            for (int i = 0; i < unlockedIds.Count; i++)
            {
                int skillId = unlockedIds[i];
                Button skillBtn = new Button();
                // TODO: SkillManager를 통해 실제 스킬 이름을 가져올 수 있음
                skillBtn.text = $"스킬 ID : {skillId}";
                skillBtn.style.height = 40;

                if (selectedSkillIds.Contains(skillId))
                {
                    skillBtn.style.backgroundColor = new StyleColor(Color.green);
                }
                
                skillBtn.RegisterCallback<ClickEvent>(evt => 
                {
                    ToggleSkillSelection(skillId, skillBtn);
                });

                skillListScrollView.Add(skillBtn);
            }
        }

        private void ToggleSkillSelection(int skillId, Button btn)
        {
            if (selectedSkillIds.Contains(skillId))
            {
                selectedSkillIds.Remove(skillId);
                btn.style.backgroundColor = new StyleColor(StyleKeyword.Null); // 기본색 복구
                startButton.SetEnabled(selectedSkillIds.Count > 0);
                return;
            }

            if (selectedSkillIds.Count >= MAX_SELECT)
            {
                Debug.Log("[LoadoutSelectionPopup] 이미 최대(3개) 선택했습니다.");
                return;
            }
            
            selectedSkillIds.Add(skillId);
            btn.style.backgroundColor = new StyleColor(Color.green);
            startButton.SetEnabled(selectedSkillIds.Count > 0);
        }

        private void OnStartClicked(ClickEvent evt)
        {
            Debug.Log($"[LoadoutSelectionPopup] 픽업 완료! 장착 스킬 수: {selectedSkillIds.Count}");
            
            if (GameStatics.NetworkManager == null || GameStatics.NetworkManager.LocalClient == null)
            {
                Debug.LogWarning("[LoadoutSelectionPopup] NetworkManager 또는 LocalClient가 null입니다.");
                return;
            }
            
            NetworkObject playerObject = GameStatics.NetworkManager.LocalClient.PlayerObject;
            if (playerObject == null)
            {
                Debug.LogWarning("[LoadoutSelectionPopup] PlayerObject가 null입니다.");
                return;
            }

            NetSkillComponent skillComponent = playerObject.GetComponentInChildren<NetSkillComponent>();
            if (skillComponent == null)
            {
                Debug.LogWarning("[LoadoutSelectionPopup] PlayerObject 내부에서 NetSkillComponent를 찾을 수 없습니다.");
                return;
            }
            
            skillComponent.EquipLoadoutServerRpc(selectedSkillIds.ToArray());
            CodexManager.SaveSelectedLoadout(selectedSkillIds);
            GameStatics.UIManager.ClosePopup(this);
        }
    }
}
