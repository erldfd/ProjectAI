using ProjectAI.Characters;
using ProjectAI.Core;
using ProjectAI.Core.Enums;
using ProjectAI.SOs;
using ProjectAI.UIs.Cores;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UIElements;

namespace ProjectAI.UIs
{
    /// <summary>
    /// 코어 정화 완료 시 화면 우측에 뜨는 히오스 스타일의 보상 선택 팝업 UI를 관리합니다.
    /// AUIPopup을 상속받아 UIManager에 의해 관리되며, IsOverlay = true로 스택 간섭을 방지합니다.
    /// </summary>
    public class CorePurificationPopup : AUIPopup
    {
        public override bool IsOverlay => true;

        private VisualElement container;
        private Button btnSummon;
        private Button btnSummonUpgrade;
        private Button btnPlayerUpgrade;

        private DungeonRewardTableSO rewardTable;
        private NetPlayerCharacter localPlayer;

        private int currentSummonIdx = -1;
        private int currentSummonUpgradeIdx = -1;
        private int currentPlayerUpgradeIdx = -1;

        protected override void OnInitialize()
        {
            // RootElement는 UIManager가 생성해서 Initialize를 통해 주입해 줍니다.
            Assert.IsNotNull(base.RootElement, "[CorePurificationPopup] RootElement가 null입니다.");

            container = base.RootElement.Q<VisualElement>("reward-container");
            Assert.IsNotNull(container, "[CorePurificationPopup] reward-container 요소를 찾을 수 없습니다.");

            btnSummon = base.RootElement.Q<Button>("btn-summon");
            btnSummonUpgrade = base.RootElement.Q<Button>("btn-summon-upgrade");
            btnPlayerUpgrade = base.RootElement.Q<Button>("btn-player-upgrade");

            Assert.IsNotNull(btnSummon, "[CorePurificationPopup] btn-summon 요소를 찾을 수 없습니다.");
            Assert.IsNotNull(btnSummonUpgrade, "[CorePurificationPopup] btn-summon-upgrade 요소를 찾을 수 없습니다.");
            Assert.IsNotNull(btnPlayerUpgrade, "[CorePurificationPopup] btn-player-upgrade 요소를 찾을 수 없습니다.");

            btnSummon.RegisterCallback<ClickEvent>(OnSummonClicked);
            btnSummonUpgrade.RegisterCallback<ClickEvent>(OnSummonUpgradeClicked);
            btnPlayerUpgrade.RegisterCallback<ClickEvent>(OnPlayerUpgradeClicked);
        }

        protected override void OnShow()
        {
        }

        protected override void OnHide()
        {
            localPlayer = null;
        }

        /// <summary>
        /// 팝업을 열면서 필요한 데이터를 주입받습니다.
        /// </summary>
        public void SetupPopup(int summonIdx, int summonUpgradeIdx, int playerUpgradeIdx, NetPlayerCharacter player)
        {
            rewardTable = GameStatics.CurrentRewardTable;
            Assert.IsNotNull(rewardTable, "[CorePurificationPopup] SetupPopup: 전역 RewardTable을 찾을 수 없습니다.");

            localPlayer = player;
            currentSummonIdx = summonIdx;
            currentSummonUpgradeIdx = summonUpgradeIdx;
            currentPlayerUpgradeIdx = playerUpgradeIdx;

            container.style.display = DisplayStyle.Flex;

            SetupButton(btnSummon, summonIdx, ERewardType.Summon);
            SetupButton(btnSummonUpgrade, summonUpgradeIdx, ERewardType.SummonUpgrade);
            SetupButton(btnPlayerUpgrade, playerUpgradeIdx, ERewardType.PlayerUpgrade);
        }

        private void SetupButton(Button btn, int idx, ERewardType type)
        {
            if (idx < 0)
            {
                btn.style.display = DisplayStyle.None;
                return;
            }

            btn.style.display = DisplayStyle.Flex;
            
            string rewardName = "Unknown";
            string description = "No description.";
            
            if (rewardTable.TryGetRewardData(type, idx, out SRewardItemData data))
            {
                rewardName = data.RewardName;
                description = data.Description;
            }
            else
            {
                btn.style.display = DisplayStyle.None;
                return;
            }

            Label titleLabel = btn.Q<Label>("reward-title");
            Assert.IsNotNull(titleLabel, $"[CorePurificationPopup] {type} 버튼 내 reward-title을 찾을 수 없습니다.");
            titleLabel.text = rewardName;

            Label descLabel = btn.Q<Label>("reward-desc");
            Assert.IsNotNull(descLabel, $"[CorePurificationPopup] {type} 버튼 내 reward-desc를 찾을 수 없습니다.");
            descLabel.text = description;
            
            Label typeLabel = btn.Q<Label>("reward-type");
            Assert.IsNotNull(typeLabel, $"[CorePurificationPopup] {type} 버튼 내 reward-type을 찾을 수 없습니다.");
            typeLabel.text = GetTypeName(type);

            VisualElement iconElement = btn.Q<VisualElement>("reward-icon");
            Assert.IsNotNull(iconElement, $"[CorePurificationPopup] {type} 버튼 내 reward-icon을 찾을 수 없습니다.");
            
            if (data.Icon != null)
            {
                iconElement.style.backgroundImage = new StyleBackground(data.Icon);
                iconElement.style.display = DisplayStyle.Flex;
            }
            else
            {
                iconElement.style.backgroundImage = StyleKeyword.Null;
                iconElement.style.display = DisplayStyle.None;
            }
        }

        private string GetTypeName(ERewardType type)
        {
            return type switch
            {
                ERewardType.Summon => "소환수 계약",
                ERewardType.SummonUpgrade => "소환수 강화",
                ERewardType.PlayerUpgrade => "코어 강화",
                _ => "보상"
            };
        }

        private void OnRewardSelected(ERewardType type, int idx)
        {
            if (idx < 0)
            {
                Debug.LogWarning("[CorePurificationPopup] 유효하지 않은 인덱스입니다.");
                return;
            }

            Assert.IsNotNull(rewardTable, "[CorePurificationPopup] 런타임 중 RewardTable 참조가 유실되었습니다.");

            if (!rewardTable.TryGetRewardData(type, idx, out _))
            {
                Debug.LogWarning($"[CorePurificationPopup] DB 범위를 벗어난 비정상적인 인덱스입니다. 악의적 클릭 차단. (Index: {idx})");
                return;
            }
            
            Assert.IsNotNull(localPlayer, "[CorePurificationPopup] 런타임 중 로컬 플레이어 참조가 유실되었습니다.");

            if (!localPlayer.IsSpawned || !localPlayer.IsOwner)
            {
                Debug.LogWarning("[CorePurificationPopup] 유효한(스폰된 및 오너십을 가진) 로컬 플레이어가 없어 보상 선택을 서버로 보낼 수 없습니다.");
                return;
            }

            localPlayer.SubmitRewardChoiceRpc(type, idx);
            
            GameStatics.UIManager.ClosePopup(this);
        }

        private void OnSummonClicked(ClickEvent evt) => OnRewardSelected(ERewardType.Summon, currentSummonIdx);
        private void OnSummonUpgradeClicked(ClickEvent evt) => OnRewardSelected(ERewardType.SummonUpgrade, currentSummonUpgradeIdx);
        private void OnPlayerUpgradeClicked(ClickEvent evt) => OnRewardSelected(ERewardType.PlayerUpgrade, currentPlayerUpgradeIdx);
    }
}
