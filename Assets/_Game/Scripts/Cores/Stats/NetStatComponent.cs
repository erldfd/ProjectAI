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

        [SerializeField]
        private int baseSummonAttackPower = 0;

        [SerializeField]
        private int baseSummonMaxHealth = 0;

        /// <summary>
        /// 이 스탯 컴포넌트가 통제하는 체력 컴포넌트 참조
        /// </summary>
        public NetHealthComponent HealthComponent { get; private set; }
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
        /// 소환수 공통 추가 공격력 보너스 수치
        /// </summary>
        public NetworkVariable<int> SummonAttackPower = new NetworkVariable<int>(
            0,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

        /// <summary>
        /// 소환수 공통 추가 체력 보너스 수치
        /// </summary>
        public NetworkVariable<int> SummonMaxHealth = new NetworkVariable<int>(
            0,
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
            
            if (HealthComponent != null)
            {
                HealthComponent.SetOwner(owner);
            }
        }

        private void Awake()
        {
            HealthComponent = GetComponentInChildren<NetHealthComponent>();

            entityEvents = GetComponentInParent<EntityEvents>();
            Assert.IsNotNull(entityEvents, "[NetStatComponent] entityEvents가 null입니다.");

            if (HealthComponent != null)
            {
                HealthComponent.OnHit += HandleHit;
                HealthComponent.OnDeath += HandleDeath;
            }
        }

        public override void OnDestroy()
        {
            base.OnDestroy();
            if (HealthComponent != null)
            {
                HealthComponent.OnHit -= HandleHit;
                HealthComponent.OnDeath -= HandleDeath;
            }
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            MoveSpeedModifier.OnValueChanged += HandleMoveSpeedModifierChanged;
            
            if (GameStatics.IsServerAuthorized)
            {
                RecalculateStats();

                if (HealthComponent != null)
                {
                    HealthComponent.InitializeHealth(MaxHealth.Value);
                }
            }

            entityEvents.InvokeMoveSpeedModifierChanged(MoveSpeedModifier.Value);
        }

        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();
            MoveSpeedModifier.OnValueChanged -= HandleMoveSpeedModifierChanged;

            if (GameStatics.IsServerAuthorized)
            {
                ClearAllModifiers();
            }
        }

        /// <summary>
        /// 모든 런타임 스탯 변경자를 일괄 제거하고 스탯을 초기화합니다. (오브젝트 풀 재사용 시 필수) (서버 전용)
        /// </summary>
        public void ClearAllModifiers()
        {
            Assert.IsTrue(GameStatics.IsServerAuthorized, "[NetStatComponent] ClearAllModifiers는 서버에서만 호출되어야 합니다.");

            if (!GameStatics.IsServerAuthorized)
            {
                Debug.LogWarning("[NetStatComponent] ClearAllModifiers: 서버 권한이 없어 거부되었습니다.");
                return;
            }

            if (statModifiers.Count > 0)
            {
                statModifiers.Clear();
                RecalculateStats();
                Debug.Log($"<color=cyan>[NetStatComponent]</color> {gameObject.name}의 모든 스탯 Modifier 초기화 완료 (풀 반환/재사용)");
            }
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
            float finalSummonAttack = baseSummonAttackPower;
            float finalSummonHealth = baseSummonMaxHealth;

            for (int i = 0; i < statModifiers.Count; i++)
            {
                StatModifier mod = statModifiers[i];
                if (mod == null)
                {
                    Debug.LogWarning($"<color=cyan>[NetStatComponent]</color> {gameObject.name}의 statModifiers[{i}]가 null입니다.");
                    continue;
                }

                Debug.Log($"<color=cyan>[NetStatComponent]</color> {gameObject.name} 스탯 변경자 적용 -> {mod.StatType} +{mod.Value} (출처: {mod.Source})");

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
                    case EStatType.SummonAttackPower:
                        finalSummonAttack += mod.Value;
                        break;
                    case EStatType.SummonMaxHealth:
                        finalSummonHealth += mod.Value;
                        break;
                }
            }

            MaxHealth.Value = Mathf.Max(1, Mathf.RoundToInt(finalHealth));
            AttackPower.Value = Mathf.Max(0, Mathf.RoundToInt(finalAttack));
            MoveSpeedModifier.Value = Mathf.Max(0.1f, finalMoveSpeed);
            SummonAttackPower.Value = Mathf.Max(0, Mathf.RoundToInt(finalSummonAttack));
            SummonMaxHealth.Value = Mathf.Max(0, Mathf.RoundToInt(finalSummonHealth));

            Debug.Log($"<color=cyan>[NetStatComponent]</color> {gameObject.name} 스탯 재계산 완료 -> MaxHealth: {MaxHealth.Value}, AttackPower: {AttackPower.Value}, MoveSpeedModifier: {MoveSpeedModifier.Value}, SummonAttackPower: {SummonAttackPower.Value}, SummonMaxHealth: {SummonMaxHealth.Value}");
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
