using ProjectAI.Characters;

namespace ProjectAI.Core
{

    // EventManager를 통해 전달될 전역 이벤트 구조체들을 정의합니다.
    
    /// <summary>
    /// 서버가 로컬 클라이언트에게 정화 보상 팝업창(3택 1)을 표시하라고 지시할 때 발생하는 이벤트.
    /// UI 시스템(UIManager/UIEventListener)에서 수신하여 팝업을 엽니다.
    /// </summary>
    public struct SShowRewardPopupEvent
    {
        /// <summary>
        /// 첫 번째 버튼에 할당될 새로운 소환수 계약 보상의 인덱스입니다.
        /// (인덱스가 음수일 경우 버튼이 비활성화됩니다.)
        /// </summary>
        public int SummonRewardIndex;

        /// <summary>
        /// 두 번째 버튼에 할당될 기존 소환수 강화 보상의 인덱스입니다.
        /// </summary>
        public int SummonUpgradeRewardIndex;

        /// <summary>
        /// 세 번째 버튼에 할당될 로컬 플레이어 코어 강화 보상의 인덱스입니다.
        /// </summary>
        public int PlayerUpgradeRewardIndex;

        /// <summary>
        /// 보상 선택 완료 후 RPC로 응답(SubmitRewardChoiceRpc)을 보내기 위한 플레이어 캐릭터 참조입니다.
        /// </summary>
        public NetPlayerCharacter LocalPlayer;

        /// <summary>
        /// 정화 보상 팝업 이벤트를 생성합니다.
        /// </summary>
        /// <param name="summonRewardIndex">첫 번째 버튼에 할당될 새로운 소환수 계약 보상의 인덱스 (음수면 비활성화)</param>
        /// <param name="summonUpgradeRewardIndex">두 번째 버튼에 할당될 기존 소환수 강화 보상의 인덱스</param>
        /// <param name="playerUpgradeRewardIndex">세 번째 버튼에 할당될 로컬 플레이어 코어 강화 보상의 인덱스</param>
        /// <param name="localPlayer">선택 완료 후 RPC 응답을 보낼 로컬 플레이어 객체 참조</param>
        public SShowRewardPopupEvent(int summonRewardIndex, int summonUpgradeRewardIndex, int playerUpgradeRewardIndex, NetPlayerCharacter localPlayer)
        {
            SummonRewardIndex = summonRewardIndex;
            SummonUpgradeRewardIndex = summonUpgradeRewardIndex;
            PlayerUpgradeRewardIndex = playerUpgradeRewardIndex;
            LocalPlayer = localPlayer;
        }
    }
}
