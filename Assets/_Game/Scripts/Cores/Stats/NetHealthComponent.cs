using System;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Assertions;

using ProjectAI.Core.Entities;

namespace ProjectAI.Core.Stats
{
    /// <summary>
    /// 개체의 현재 체력과 피격/사망 연출을 관리하는 단일 책임 컴포넌트입니다.
    /// 체력 증감은 반드시 서버에서만 처리되며, 연출은 ClientRpc를 통해 즉시 동기화됩니다.
    /// </summary>
    public class NetHealthComponent : NetworkBehaviour, IDamageable
    {
        /// <summary>
        /// 현재 체력 (서버에서만 수정 가능, 클라이언트는 읽기만 가능)
        /// (주의: 최대 체력(MaxHealth)은 버프/장비 시스템과의 연동을 위해 NetStatComponent에서 중앙 관리하므로 그쪽을 참조해야 합니다)
        /// </summary>
        public NetworkVariable<int> CurrentHealth = new NetworkVariable<int>(
            100,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

        /// <summary> 체력 수치 변경 이벤트 (UI 갱신용) </summary>
        public event Action<int> OnHealthChanged;

        /// <summary> 1회성 피격 이벤트 (파티클, 사운드 등 연출 트리거). 파라미터는 (데미지, 남은 체력) </summary>
        public event Action<int, int> OnHit;

        /// <summary> 사망 이벤트 </summary>
        public event Action OnDeath;

        /// <summary>
        /// 이 컴포넌트를 소유하고 있는 루트 엔티티 참조
        /// </summary>
        public NetEntity OwnerEntity { get; private set; }

        public void SetOwner(NetEntity owner)
        {
            OwnerEntity = owner;
        }


        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            CurrentHealth.OnValueChanged += HandleHealthChanged;

            // [Review Fix] 클라이언트 체력 UI 초기화 명시적 호출
            OnHealthChanged?.Invoke(CurrentHealth.Value);

            // [Review Fix] NGO Late Joiner(지연 접속자) 사망 상태 동기화
            if (CurrentHealth.Value <= 0)
            {
                OnDeath?.Invoke();
            }
        }

        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();
            CurrentHealth.OnValueChanged -= HandleHealthChanged;
        }

        /// <summary>
        /// NetStatComponent 등 상위 컨트롤러에서 초기 체력을 세팅할 때 호출합니다.
        /// </summary>
        public void InitializeHealth(int maxHealth)
        {
            Assert.IsTrue(GameStatics.IsServerAuthorized, "[NetHealthComponent] InitializeHealth는 서버에서만 호출되어야 합니다.");
            
            if (!GameStatics.IsServerAuthorized)
            {
                return;
            }
            
            CurrentHealth.Value = maxHealth;
        }

        private void HandleHealthChanged(int previousValue, int newValue)
        {
            OnHealthChanged?.Invoke(newValue);
        }

        /// <summary>
        /// IDamageable 구현체. 서버 파이프라인(GameStatics)에서만 호출되어야 합니다.
        /// </summary>
        public void TakeDamage(int damage)
        {
            Assert.IsTrue(GameStatics.IsServerAuthorized, "[NetHealthComponent] TakeDamage는 서버에서만 호출되어야 합니다.");
            
            if (!GameStatics.IsServerAuthorized)
            {
                return;
            }

            if (damage <= 0)
            {
                Debug.LogWarning($"[NetHealthComponent] 데미지가 음수이거나 0입니다: {damage}");
                return; // 데미지가 음수이거나 0이면 무시 (회복 방지)
            }

            if (CurrentHealth.Value <= 0)
            {
                Debug.LogWarning("[NetHealthComponent] 이미 사망한 개체에 데미지를 입히려 합니다.");
                return; // 이미 사망한 개체
            }

            // 체력 차감 로직
            int remainingHealth = Mathf.Max(0, CurrentHealth.Value - damage);
            CurrentHealth.Value = remainingHealth;

            // 독립적인 피격/사망 트리거 발송 (레이스 컨디션 방지를 위해 remainingHealth 동봉)
            if (remainingHealth <= 0)
            {
                DieClientRpc();
            }
            else
            {
                HitClientRpc(damage, remainingHealth);
            }
        }

        [Rpc(SendTo.ClientsAndHost, Delivery = RpcDelivery.Reliable)]
        private void HitClientRpc(int damage, int remainingHealth)
        {
            OnHit?.Invoke(damage, remainingHealth);
        }

        [Rpc(SendTo.ClientsAndHost, Delivery = RpcDelivery.Reliable)]
        private void DieClientRpc()
        {
            OnDeath?.Invoke();
            // 참고: 객체 파괴(Despawn) 및 랙돌 연출 등은 NetHealthComponent에서 직접 하지 않고,
            // OnDeath 이벤트를 구독하는 외부 컨트롤러(NetPlayerController, NetMonsterController)에 위임합니다.
        }
    }
}
