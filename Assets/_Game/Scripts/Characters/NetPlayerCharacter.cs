using ProjectAI.Characters.Summons;
using ProjectAI.Core;
using ProjectAI.Movements;
using ProjectAI.SOs;
using ProjectAI.UIs;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Assertions;

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

        [Rpc(SendTo.Server)]
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
                    Assert.IsNotNull(data.SummonPrefab, "[NetPlayerCharacter] 보상 데이터에 SummonPrefab이 할당되지 않았습니다.");
                    SummonController.ReplaceSummon(data.SummonPrefab, 60f); // 60초 임시 소환 시간
                    Debug.Log($"<color=cyan>[NetPlayerCharacter]</color> 소환수 계약 적용: {data.RewardName}");
                    break;

                case ERewardType.SummonUpgrade:
                    // TODO: 소환수 스탯 강화 로직 (추후 스탯 시스템과 연동)
                    Debug.Log($"<color=cyan>[NetPlayerCharacter]</color> 소환수 강화 적용: {data.RewardName} (Value: {data.UpgradeValue})");
                    break;

                case ERewardType.PlayerUpgrade:
                    // TODO: 플레이어 코어 강화 로직 (추후 스탯 시스템과 연동)
                    Debug.Log($"<color=cyan>[NetPlayerCharacter]</color> 코어 강화 적용: {data.RewardName} (Value: {data.UpgradeValue})");
                    break;
            }
        }
    }
}
