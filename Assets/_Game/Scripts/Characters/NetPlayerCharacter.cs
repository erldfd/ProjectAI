using ProjectAI.Characters.Summons;
using ProjectAI.Core;
using ProjectAI.Movements;
using ProjectAI.SOs;
using ProjectAI.UIs;
using ProjectAI.UIs.Popups;
using ProjectAI.Core.Enums;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Assertions;
using ProjectAI.Core.Skills;
using ProjectAI.Core.Stats;

namespace ProjectAI.Characters
{
    /// <summary>
    /// 플레이어가 조종하는 캐릭터 전용 로직(직접 이동 명령, 상호작용 등)을 담는 퍼사드 클래스입니다.
    /// NetCharacter의 공통 기능(스킬, 상태 등)을 상속받습니다.
    /// </summary>
    public class NetPlayerCharacter : NetCharacter
    {
        private NetInteractor interactor;
        private int pendingRewardCount = 0; // 보상 누적 대기 횟수 (서버 전용)



        public NetSummonController SummonController { get; private set; }

        protected override void Awake()
        {
            base.Awake();
            interactor = GetComponentInChildren<NetInteractor>();
            SummonController = GetComponentInChildren<NetSummonController>();
            
            Assert.IsNotNull(interactor, "[NetPlayerCharacter] NetInteractor를 찾을 수 없습니다.");
            Assert.IsNotNull(SummonController, "[NetPlayerCharacter] NetSummonController를 찾을 수 없습니다. 플레이어 프리팹에 추가해 주세요.");
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            
            if (IsOwner)
            {
                EventManager.TriggerEvent(new SLocalPlayerSpawnedEvent { PlayerObject = NetworkObject });
            }
        }

        /// <summary>
        /// 외부(컨트롤러)에서 캐릭터의 이동을 지시하는 퍼사드 메서드입니다.
        /// </summary>
        public void Move(Vector2 direction)
        {
            if (base.Movement is not NetPlayerMovement playerMovement)
            {
                Debug.LogWarning($"[NetPlayerCharacter] Move 실패: Movement가 NetPlayerMovement가 아닙니다. (ID: {NetworkObjectId})");
                return;
            }

            playerMovement.SetMoveInput(direction);
        }

        /// <summary>
        /// 외부(컨트롤러)에서 캐릭터의 상호작용을 지시하는 퍼사드 메서드입니다.
        /// </summary>
        public void TryInteract()
        {
            interactor.TryInteract();
        }

        /// <summary>
        /// 새로운 보상 페이즈가 시작될 때 서버에서 호출하여 보상 대기 횟수를 증가시킵니다.
        /// </summary>
        public void IncrementPendingRewardCount()
        {
            Assert.IsTrue(GameStatics.IsServerAuthorized, "[NetPlayerCharacter] IncrementPendingRewardCount는 서버에서만 호출되어야 합니다.");
            
            if (!GameStatics.IsServerAuthorized)
            {
                Debug.LogWarning("[NetPlayerCharacter] IncrementPendingRewardCount 호출이 거부되었습니다. (서버 권한 없음)");
                return;
            }

            pendingRewardCount++;
        }

        [Rpc(SendTo.Owner)]
        public void ShowRewardPopupRpc(int summonRewardIndex, int summonUpgradeRewardIndex, int playerUpgradeRewardIndex)
        {
            Debug.Log($"<color=yellow>[NetPlayerCharacter]</color> 보상 UI 오픈 지시 받음 (Summon: {summonRewardIndex}, SUpgrade: {summonUpgradeRewardIndex}, PUpgrade: {playerUpgradeRewardIndex})");
            
            EventManager.TriggerEvent(new SShowRewardPopupEvent(
                summonRewardIndex,
                summonUpgradeRewardIndex,
                playerUpgradeRewardIndex,
                this
            ));
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
        public void SubmitRewardChoiceRpc(ERewardType type, int index)
        {
            Assert.IsTrue(GameStatics.IsServerAuthorized, "[NetPlayerCharacter] SubmitRewardChoiceRpc는 서버에서만 실행되어야 합니다.");
            
            // ServerRpc이므로 100% 서버에서 실행되지만, 에디터 Assert 및 Fail-fast를 위한 방어 코드를 기획자 요청에 따라 유지합니다.
            if (!GameStatics.IsServerAuthorized)
            {
                Debug.LogError("[NetPlayerCharacter] ServerRpc 내부에서 권한이 없는 비정상적 상태가 감지되었습니다.");
                return;
            }

            if (pendingRewardCount <= 0)
            {
                Debug.LogWarning($"[NetPlayerCharacter] 대기 중인 보상이 없는데 요청이 왔습니다. 악의적 접근 가능성. (ID: {NetworkObjectId})");
                return;
            }

            if (index < 0)
            {
                Debug.LogWarning($"[NetPlayerCharacter] 비정상적인 인덱스로 보상을 요청했습니다. (Index: {index})");
                return;
            }

            DungeonRewardTableSO rewardTable = GameStatics.CurrentRewardTable;
            if (rewardTable == null)
            {
                Debug.LogError("[NetPlayerCharacter] GameStatics.CurrentRewardTable이 null입니다. 로드 실패.");
                return;
            }

            if (!rewardTable.TryGetRewardData(type, index, out SRewardItemData data))
            {
                Debug.LogWarning($"[NetPlayerCharacter] DB 범위를 벗어난 비정상적인 인덱스입니다. 악의적 접근 차단. (Index: {index})");
                return;
            }

            pendingRewardCount--; // 보상 수령 처리 (대기 횟수 차감)

            ApplyReward(type, data);

            Debug.Log($"<color=yellow>[NetPlayerCharacter]</color> 유저의 보상 선택 서버 적용 완료 (Type: {type}, Index: {index})");
        }

        private void ApplyReward(ERewardType type, SRewardItemData data)
        {
            switch (type)
            {
                case ERewardType.Summon:
                    Assert.IsNotNull(data.SummonSkillConfig, "[NetPlayerCharacter] 보상 데이터에 SummonSkillConfig가 할당되지 않았습니다.");
                    UnlockSkillOwnerRpc(data.SummonSkillConfig.SkillId);
                    Debug.Log($"<color=cyan>[NetPlayerCharacter]</color> 도감 해금 지시 완료: {data.RewardName}");
                    break;

                case ERewardType.SummonUpgrade:
                    Assert.IsNotNull(base.StatComponent, "[NetPlayerCharacter] StatComponent가 null입니다.");
                    base.StatComponent.AddModifier(new StatModifier(EStatType.SummonAttackPower, data.UpgradeValue, this));
                    
                    if (SummonController != null)
                    {
                        SummonController.SyncSummonStats(base.StatComponent);
                    }

                    Debug.Log($"<color=cyan>[NetPlayerCharacter]</color> 소환수 공통 공격력 강화 적용: {data.RewardName} (+{data.UpgradeValue})");
                    break;

                case ERewardType.PlayerUpgrade:
                    Assert.IsNotNull(base.StatComponent, "[NetPlayerCharacter] StatComponent가 null입니다.");
                    base.StatComponent.AddModifier(new StatModifier(EStatType.AttackPower, data.UpgradeValue, this));
                    Debug.Log($"<color=cyan>[NetPlayerCharacter]</color> 플레이어 공격력 강화 적용: {data.RewardName} (+{data.UpgradeValue})");
                    break;
            }
        }

        [Rpc(SendTo.Owner)]
        private void UnlockSkillOwnerRpc(int skillId)
        {
            bool isNewlyUnlocked = CodexManager.UnlockSkill(skillId);
            if (isNewlyUnlocked)
            {
                Debug.Log($"<color=cyan>[NetPlayerCharacter]</color> 로컬 도감에 스킬({skillId})이 영구 해금되었습니다!");
                // TODO: 해금 축하 연출 팝업 등 UI 띄우기
            }

            Assert.IsNotNull(base.SkillComponent, "[NetPlayerCharacter] SkillComponent가 null입니다.");

            bool isAlreadyEquipped = false;
            for (int i = 0; i < base.SkillComponent.OwnedSkills.Count; i++)
            {
                if (base.SkillComponent.OwnedSkills[i] != null && base.SkillComponent.OwnedSkills[i].SkillId == skillId)
                {
                    isAlreadyEquipped = true;
                    break;
                }
            }

            if (isAlreadyEquipped)
            {
                Debug.Log($"<color=cyan>[NetPlayerCharacter]</color> 현재 장착 중인 스킬({skillId})입니다. 이번 런 한정 스펙 강화 버프(공격력 +5)로 전환됩니다.");
                RequestAddStatModifierServerRpc(EStatType.AttackPower, 5f);
                return;
            }

            int targetSlot = -1;
            for (int i = (int)ESkillSlot.Summon1; i <= (int)ESkillSlot.Summon3; i++)
            {
                if (base.SkillComponent.OwnedSkills.Count <= i || base.SkillComponent.OwnedSkills[i] == null)
                {
                    targetSlot = i;
                    break;
                }
            }

            if (targetSlot != -1)
            {
                Debug.Log($"<color=cyan>[NetPlayerCharacter]</color> 빈 슬롯({targetSlot}) 발견. 자동 장착을 요청합니다.");
                base.SkillComponent.TryEquipSkillServerRpc(skillId, targetSlot);
            }
            else
            {
                Debug.Log($"<color=cyan>[NetPlayerCharacter]</color> 모든 소환수 슬롯이 꽉 찼습니다. 스킬 교체 팝업을 엽니다.");
                SkillReplacePopup replacePopup = GameStatics.UIManager.ShowPopup<SkillReplacePopup>(EUIPopupType.SkillReplace);
                if (replacePopup != null)
                {
                    replacePopup.Setup(skillId, base.SkillComponent);
                }
            }
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
        private void RequestAddStatModifierServerRpc(EStatType statType, float value)
        {
            Assert.IsTrue(GameStatics.IsServerAuthorized, "[NetPlayerCharacter] RequestAddStatModifierServerRpc는 서버에서만 실행되어야 합니다.");

            if (!GameStatics.IsServerAuthorized)
            {
                Debug.LogWarning("[NetPlayerCharacter] RequestAddStatModifierServerRpc: 서버 권한이 없어 거부되었습니다.");
                return;
            }

            Assert.IsNotNull(base.StatComponent, "[NetPlayerCharacter] StatComponent가 null입니다.");
            StatModifier modifier = new StatModifier(statType, value, this);
            base.StatComponent.AddModifier(modifier);
            Debug.Log($"<color=cyan>[NetPlayerCharacter]</color> 서버에서 스탯 버프 적용 완료 ({statType} +{value})");
        }
    }
}
