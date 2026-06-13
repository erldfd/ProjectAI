using ProjectAI.Core.Entities;
using ProjectAI.Core.Skills;
using UnityEngine;
using UnityEngine.Assertions;
using System.Collections;
using ProjectAI.Core.Stats;

namespace ProjectAI.Characters
{
    /// <summary>
    /// 플레이어나 NPC 등 생명체 캐릭터의 핵심 로직을 연결하는 허브 컴포넌트입니다.
    /// </summary>
    public class NetCharacter : NetEntity
    {
        public NetSkillComponent SkillComponent { get; private set; }
        public NetStatComponent StatComponent { get; private set; }

        [Header("Death Settings")]
        [SerializeField] 
        private float despawnDelay = 1.5f;
        
        private WaitForSeconds despawnWait;

        protected override void Awake()
        {
            base.Awake();
            despawnWait = new WaitForSeconds(despawnDelay);
            
            SkillComponent = GetComponentInChildren<NetSkillComponent>();
            Assert.IsNotNull(SkillComponent, "[NetCharacter] NetSkillComponent를 찾을 수 없습니다.");

            StatComponent = GetComponentInChildren<NetStatComponent>();
        }

        /// <summary>
        /// 외부(컨트롤러 등)에서 캐릭터에게 스킬 사용을 지시하는 퍼사드 메서드입니다.
        /// </summary>
        public void TryActivateSkill(ESkillType skillType)
        {
            // 캐릭터 내부망(EntityEvents)을 통해 각 컴포넌트들에게 지시를 내립니다.
            base.Events.InvokeSkillTriggered(skillType);
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            base.Events.OnDeathTriggered += HandleDeathTriggered;

            // 오브젝트 풀에서 재소환 시 사망 시 비활성화했던 물리 상태 원상 복구
            Collider2D[] colliders = GetComponentsInChildren<Collider2D>();
            foreach (Collider2D col in colliders)
            {
                col.enabled = true;
            }

            Rigidbody2D rb = GetComponentInChildren<Rigidbody2D>();
            if (rb != null)
            {
                rb.simulated = true;
            }
        }

        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();
            base.Events.OnDeathTriggered -= HandleDeathTriggered;
        }

        protected virtual void HandleDeathTriggered()
        {
            // 1. 물리 충돌체 비활성화
            Collider2D[] colliders = GetComponentsInChildren<Collider2D>();
            foreach (Collider2D col in colliders)
            {
                col.enabled = false;
            }

            Rigidbody2D rb = GetComponentInChildren<Rigidbody2D>();
            if (rb != null)
            {
                rb.linearVelocity = Vector2.zero;
                rb.simulated = false;
            }

            // 2. 서버 권한일 경우 디스폰
            if (!IsServer)
            {
                Debug.Log($"[NetCharacter] 디스폰은 서버 권한입니다. (ID: {NetworkObjectId})");
                return;
            }

            StartCoroutine(DespawnRoutine());
        }

        private IEnumerator DespawnRoutine()
        {
            yield return despawnWait;
            
            if (NetworkObject == null || !NetworkObject.IsSpawned)
            {
                Debug.LogWarning("[NetCharacter] 이미 디스폰되었거나 파괴된 개체이므로 무시합니다.");
                yield break;
            }

            NetworkObject.Despawn(true);
        }
    }
}
