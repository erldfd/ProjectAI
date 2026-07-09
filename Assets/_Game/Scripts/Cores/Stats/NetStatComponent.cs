using UnityEngine.Assertions;
using Unity.Netcode;
using UnityEngine;
using ProjectAI.Core.Entities;

namespace ProjectAI.Core.Stats
{
    /// <summary>
    /// 캐릭터의 영구적/가변적 주요 스탯(최대 체력, 공격력, 이동 속도 등)을 통제합니다.
    /// </summary>
    public class NetStatComponent : NetworkBehaviour
    {
        private NetHealthComponent healthComponent;
        private EntityEvents entityEvents;

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
        /// 이동 속도 배율 (기본 1.0)
        /// 버프/디버프에 따라 변동되며, 이동 컴포넌트(Movement)가 이 배율을 곱하여 최종 속도를 결정합니다.
        /// </summary>
        public NetworkVariable<float> MoveSpeedModifier = new NetworkVariable<float>(
            1f,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );


        /// <summary>
        /// 이 스탯 컴포넌트를 소유하고 있는 루트 엔티티 참조
        /// </summary>
        public NetEntity OwnerEntity { get; private set; }

        public void SetOwner(NetEntity owner)
        {
            OwnerEntity = owner;
            
            // HealthComponent가 존재한다면, 스탯이 Health를 통제하는 계층형(탑다운) 구조이므로 Owner를 하달함
            if (healthComponent != null)
            {
                healthComponent.SetOwner(owner);
            }
        }

        private void Awake()
        {
            healthComponent = GetComponentInChildren<NetHealthComponent>();

            entityEvents = GetComponentInParent<EntityEvents>();
            Assert.IsNotNull(entityEvents, "NetStatComponent는 EntityEvents가 필요합니다.");

            // NGO 초기화 불확실성 방지를 위해 로컬 이벤트는 Awake에서 미리 구독
            if (healthComponent != null)
            {
                healthComponent.OnHit += HandleHit;
                healthComponent.OnDeath += HandleDeath;
            }
        }

        public override void OnDestroy()
        {
            base.OnDestroy();
            if (healthComponent != null)
            {
                healthComponent.OnHit -= HandleHit;
                healthComponent.OnDeath -= HandleDeath;
            }
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            MoveSpeedModifier.OnValueChanged += HandleMoveSpeedModifierChanged;
            
            if (healthComponent != null && GameStatics.IsServerAuthorized)
            {
                healthComponent.InitializeHealth(MaxHealth.Value);
            }

            entityEvents.InvokeMoveSpeedModifierChanged(MoveSpeedModifier.Value);
        }

        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();
            MoveSpeedModifier.OnValueChanged -= HandleMoveSpeedModifierChanged;
        }

        private void HandleHit(int damage, int remainingHealth)
        {
            entityEvents.InvokeHitTriggered(damage, remainingHealth);
        }

        private void HandleDeath(NetHealthComponent deadHealth)
        {
            entityEvents.InvokeDeathTriggered();
        }

        private void HandleMoveSpeedModifierChanged(float previousValue, float newValue)
        {
            entityEvents.InvokeMoveSpeedModifierChanged(newValue);
        }

        // TODO: 향후 레벨업, 아이템 장착, 버프에 따른 스탯 변동 로직(수정자) 추가 예정
    }
}
