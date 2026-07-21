using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.Assertions;
using ProjectAI.Core;
using ProjectAI.Core.Enums;
using ProjectAI.Core.Skills;
using ProjectAI.UIs.Cores;
using ProjectAI.SOs;

namespace ProjectAI.UIs.Popups
{
    /// <summary>
    /// 소환수 슬롯이 꽉 찼을 때, 기존 스킬을 교체할 수 있도록 띄우는 팝업입니다.
    /// </summary>
    public class SkillReplacePopup : AUIPopup
    {
        public override bool IsOverlay => true;

        private int pendingNewSkillId;
        private NetSkillComponent playerSkillComponent;

        private Button btnReplace1;
        private Button btnReplace2;
        private Button btnReplace3;
        private Button btnCancel;

        protected override void OnInitialize()
        {
            Assert.IsNotNull(base.RootElement, "[SkillReplacePopup] RootElement가 null입니다.");

            btnReplace1 = base.RootElement.Q<Button>("btn-replace-1");
            btnReplace2 = base.RootElement.Q<Button>("btn-replace-2");
            btnReplace3 = base.RootElement.Q<Button>("btn-replace-3");
            btnCancel = base.RootElement.Q<Button>("btn-cancel");

            Assert.IsNotNull(btnReplace1, "[SkillReplacePopup] btn-replace-1 버튼을 찾을 수 없습니다.");
            Assert.IsNotNull(btnReplace2, "[SkillReplacePopup] btn-replace-2 버튼을 찾을 수 없습니다.");
            Assert.IsNotNull(btnReplace3, "[SkillReplacePopup] btn-replace-3 버튼을 찾을 수 없습니다.");
            Assert.IsNotNull(btnCancel, "[SkillReplacePopup] btn-cancel 버튼을 찾을 수 없습니다.");

            btnReplace1.clicked += () => OnReplaceClicked((int)ESkillSlot.Summon1);
            btnReplace2.clicked += () => OnReplaceClicked((int)ESkillSlot.Summon2);
            btnReplace3.clicked += () => OnReplaceClicked((int)ESkillSlot.Summon3);
            btnCancel.clicked += OnCancelClicked;
        }

        public void Setup(int newSkillId, NetSkillComponent skillComponent)
        {
            Assert.IsNotNull(skillComponent, "[SkillReplacePopup] skillComponent가 null입니다.");
            
            pendingNewSkillId = newSkillId;
            playerSkillComponent = skillComponent;

            SetupButton(btnReplace1, (int)ESkillSlot.Summon1);
            SetupButton(btnReplace2, (int)ESkillSlot.Summon2);
            SetupButton(btnReplace3, (int)ESkillSlot.Summon3);
        }

        private void SetupButton(Button btn, int slotIndex)
        {
            if (playerSkillComponent.OwnedSkills.Count > slotIndex && playerSkillComponent.OwnedSkills[slotIndex] != null)
            {
                BaseSkillConfig config = playerSkillComponent.OwnedSkills[slotIndex];
                btn.text = $"슬롯 {slotIndex}\n{config.name}";
            }
            else
            {
                btn.text = $"슬롯 {slotIndex}\n(비어있음)";
            }
        }

        private void OnReplaceClicked(int slotIndex)
        {
            Debug.Log($"<color=cyan>[SkillReplacePopup]</color> {slotIndex}번 슬롯의 스킬을 버리고 새 스킬({pendingNewSkillId})로 교체합니다.");
            playerSkillComponent.TryEquipSkillServerRpc(pendingNewSkillId, slotIndex);
            
            GameStatics.UIManager.ClosePopup(this);
        }

        private void OnCancelClicked()
        {
            Debug.Log($"<color=cyan>[SkillReplacePopup]</color> 새 스킬({pendingNewSkillId}) 장착을 포기했습니다.");
            GameStatics.UIManager.ClosePopup(this);
        }
    }
}
