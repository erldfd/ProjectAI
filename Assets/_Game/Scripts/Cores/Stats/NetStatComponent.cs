using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Assertions;
using ProjectAI.Core.Entities;
using ProjectAI.Core.Enums;

namespace ProjectAI.Core.Stats
{
    /// <summary>
    /// 캐릭터의 영구적/가변적 주요 스탯(최대 체력, 공격력, 이동 속도 등)을 통제하고 런타임 Modifier를 계산합니다.
    /// </summary>
    public class NetStatComponent : NetworkBehaviour
    {
        [Header("Base Stats")]
        [SerializeField]
        private int baseMaxHealth = 100;

        [SerializeField]
        private int baseAttackPower = 10;

        [SerializeField]
        private float baseMoveSpeedModifier = 1f;

        private NetHealthComponent healthComponent;
        private EntityEvents entityEvents;
        private readonly List<StatModifier> statModifiers = new List<StatModifier>();

        /// <summary>
        /// 최종 동기화된 최대 체력
        /// </summary>
        public NetworkVariable<int> MaxHealth = new NetworkVariable<int>(
            100,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

        /// <summary>
        /// 최종 동기화된 기본 공격력
        /// </summary>
        public NetworkVariable<int> AttackPower = new NetworkVariable<int>(
            10,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

        /// <summary>
        /// 최종 동기화된 이동 속도 배율
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
            
            if (healthComponent != null)
            {
                healthComponent.SetOwner(owner);
            }
        }

        private void Awake()
        {
            healthComponent = GetComponentInChildren<NetHealthComponent>();

            entityEvents = GetComponentInParent<EntityEvents>();
            Assert.IsNotNull(entityEvents, "[NetStatComponent] entityEvents가 null입니다.");

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
            
            if (GameStatics.IsServerAuthorized)
            {
                RecalculateStats();

                if (healthComponent != null)
                {
                    healthComponent.InitializeHealth(MaxHealth.Value);
                }
            }

            entityEvents.InvokeMoveSpeedModifierChanged(MoveSpeedModifier.Value);
        }

        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();
            MoveSpeedModifier.OnValueChanged -= HandleMoveSpeedModifierChanged;
        }

        /// <summary>
        /// 런타임 스탯 변경자(버프/디버프)를 추가합니다. (서버 전용)
        /// </summary>
        public void AddModifier(StatModifier modifier)
        {
            Assert.IsTrue(GameStatics.IsServerAuthorized, "[NetStatComponent] AddModifier는 서버에서만 호출되어야 합니다.");
            
            if (!GameStatics.IsServerAuthorized)
            {
                return;
            }

            if (modifier == null)
            {
                return;
            }

            statModifiers.Add(modifier);
            RecalculateStats();
        }

        /// <summary>
        /// 런타임 스탯 변경자를 제거합니다. (서버 전용)
        /// </summary>
        public void RemoveModifier(StatModifier modifier)
        {
            Assert.IsTrue(GameStatics.IsServerAuthorized, "[NetStatComponent] RemoveModifier는 서버에서만 호출되어야 합니다.");

            if (!GameStatics.IsServerAuthorized)
            {
                return;
            }

            if (modifier == null)
            {
                return;
            }

            if (statModifiers.Remove(modifier))
            {
                RecalculateStats();
            }
        }

        /// <summary>
        /// 특정 출처(Source)의 스탯 변경자를 일괄 제거합니다. (서버 전용)
        /// </summary>
        /// <param name="source">[주의] 박싱(Garbage) 방지를 위해 int, enum 등 값 타입(Value Type)이 아닌 반드시 클래스(Reference Type) 객체를 전달하세요.</param>
        public void RemoveAllModifiersFromSource(object source)
        {
            Assert.IsTrue(GameStatics.IsServerAuthorized, "[NetStatComponent] RemoveAllModifiersFromSource는 서버에서만 호출되어야 합니다.");

            if (!GameStatics.IsServerAuthorized)
            {
                return;
            }

            if (source == null)
            {
                return;
            }

            bool isRemovedAny = false;
            for (int i = statModifiers.Count - 1; i >= 0; i--)
            {
                if (statModifiers[i].Source == source)
                {
                    statModifiers.RemoveAt(i);
                    isRemovedAny = true;
                }
            }

            if (isRemovedAny)
            {
                RecalculateStats();
            }
        }

        /// <summary>
        /// Base 스탯과 Modifiers를 종합하여 최종 NetworkVariable 수치를 재계산합니다.
        /// </summary>
        private void RecalculateStats()
        {
            Assert.IsTrue(GameStatics.IsServerAuthorized, "[NetStatComponent] RecalculateStats는 서버에서만 호출되어야 합니다.");

            if (!GameStatics.IsServerAuthorized)
            {
                return;
            }

            float finalHealth = baseMaxHealth;
            float finalAttack = baseAttackPower;
            float finalMoveSpeed = baseMoveSpeedModifier;

            for (int i = 0; i < statModifiers.Count; i++)
            {
                StatModifier mod = statModifiers[i];
                if (mod == null)
                {
                    continue;
                }

                switch (mod.StatType)
                {
                    case EStatType.MaxHealth:
                        finalHealth += mod.Value;
                        break;
                    case EStatType.AttackPower:
                        finalAttack += mod.Value;
                        break;
                    case EStatType.MoveSpeed:
                        finalMoveSpeed += mod.Value;
                        break;
                }
            }

            MaxHealth.Value = Mathf.Max(1, Mathf.RoundToInt(finalHealth));
            AttackPower.Value = Mathf.Max(0, Mathf.RoundToInt(finalAttack));
            MoveSpeedModifier.Value = Mathf.Max(0.1f, finalMoveSpeed);

            Debug.Log($"<color=cyan>[NetStatComponent]</color> {gameObject.name} 스탯 재계산 완료 -> MaxHealth: {MaxHealth.Value}, AttackPower: {AttackPower.Value}, MoveSpeedModifier: {MoveSpeedModifier.Value}");
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
    }
}
