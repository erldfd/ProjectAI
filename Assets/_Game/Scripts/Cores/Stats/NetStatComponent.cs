using Unity.Netcode;
using UnityEngine;
using ProjectAI.Characters;

namespace ProjectAI.Core.Stats
{
    /// <summary>
    /// 캐릭터의 영구적/가변적 주요 스탯(최대 체력, 공격력, 이동 속도 등)을 통제합니다.
    /// </summary>
    [RequireComponent(typeof(NetHealthComponent))]
    public class NetStatComponent : NetworkBehaviour
    {
        [SerializeField]
        private NetHealthComponent healthComponent;

        /// <summary>
        /// 최대 체력
        /// (주의: 추후 장비/버프 등으로 최대 체력이 변할 때 다른 스탯들과 함께 일괄 통제하기 위해, NetHealthComponent가 아닌 이곳에 분리되어 있습니다)
        /// </summary>
        public NetworkVariable<int> MaxHealth = new NetworkVariable<int>(
            100,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

        /// <summary>
        /// 기본 공격력
        /// </summary>
        public NetworkVariable<int> AttackPower = new NetworkVariable<int>(
            10,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

        /// <summary>
        /// 이동 속도
        /// </summary>
        public NetworkVariable<float> MoveSpeed = new NetworkVariable<float>(
            5f,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

        private void Awake()
        {
            if (healthComponent == null)
            {
                healthComponent = GetComponent<NetHealthComponent>();
            }

            UnityEngine.Assertions.Assert.IsNotNull(healthComponent, "NetStatComponent는 NetHealthComponent가 필요합니다.");
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            if (IsServer)
            {
                // 스폰 완료 시, 오너로서 하위 체력 컴포넌트를 하향식으로 초기화
                healthComponent.InitializeHealth(MaxHealth.Value);
            }
        }

        // TODO: 향후 레벨업, 아이템 장착, 버프에 따른 스탯 변동 로직(수정자) 추가 예정
    }
}
