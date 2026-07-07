using UnityEngine.Assertions;
using System;
using UnityEngine;
using Unity.Netcode;
using ProjectAI.Core;
using ProjectAI.Core.Pooling;
using ProjectAI.Core.Interfaces;

namespace ProjectAI.GameModes
{
    /// <summary>
    /// 무작위 범위 내 몬스터 스폰을 담당하는 서버 전용 스포너 컴포넌트입니다.
    /// </summary>
    public class MonsterSpawner : MonoBehaviour
    {
        [Header("Spawn Settings")]
        [SerializeField]
        [Tooltip("스폰할 몬스터 슬롯 타입")]
        private ESpawnMonsterType spawnType;

        [SerializeField]
        [Tooltip("스폰 주기 (초)")]
        private float spawnInterval = 3f;

        [SerializeField]
        [Tooltip("스폰 반경")]
        private float spawnRadius = 5f;

        [Header("Count Restrictions")]
        [SerializeField]
        [Tooltip("최대 스폰 횟수 (음수: 무제한, 0: 스폰 안 함, 1 이상: 지정 횟수)")]
        private int maxSpawnCount = -1;

        private int currentSpawnCount = 0;
        private float spawnTimer;
        private bool isActivated = false;
        private ISpawnCondition[] conditions = Array.Empty<ISpawnCondition>();

        private void Awake()
        {
            conditions = GetComponents<ISpawnCondition>();
        }

        /// <summary>
        /// 청크 트리거 등에 의해 스폰 로직을 활성화합니다.
        /// </summary>
        public void ActivateSpawner()
        {
            Assert.IsTrue(GameStatics.IsServerAuthorized, "[MonsterSpawner] ActivateSpawner는 서버에서만 작동해야 합니다.");

            if (!GameStatics.IsServerAuthorized || isActivated)
            {
                return;
            }

            isActivated = true;
        }

        private void Update()
        {
            if (!GameStatics.IsServerAuthorized || GameStatics.NetworkManager == null || !isActivated)
            {
                return;
            }

            if (maxSpawnCount == 0 || (maxSpawnCount > 0 && currentSpawnCount >= maxSpawnCount))
            {
                return;
            }

            // 조건들 중 하나라도 만족하지 않으면 타이머 대기
            for (int i = 0; i < conditions.Length; i++)
            {
                if (conditions[i] != null && !conditions[i].CheckCondition())
                {
                    return;
                }
            }

            spawnTimer += Time.deltaTime;
            if (spawnTimer >= spawnInterval)
            {
                spawnTimer = 0f;
                Vector2 randomPoint = UnityEngine.Random.insideUnitCircle * spawnRadius;
                Vector3 spawnPosition = (Vector3)((Vector2)transform.position + randomPoint);
                SpawnMonster(spawnPosition, Quaternion.identity);
            }
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            Gizmos.color = new Color(1f, 0.5f, 0f, 0.7f); // 주황색
            Gizmos.DrawWireSphere(transform.position, spawnRadius);
        }
#endif

        private void SpawnMonster(Vector3 position, Quaternion rotation)
        {
            Assert.IsTrue(GameStatics.IsServerAuthorized, "[MonsterSpawner] 몬스터 스폰 로직은 서버에서만 실행되어야 합니다.");
            
            if (!GameStatics.IsServerAuthorized)
            {
                return;
            }
            
            Assert.IsNotNull(GameStatics.ObjectPool, "[MonsterSpawner] GameStatics.ObjectPool이 등록되지 않았습니다.");
            if (GameStatics.ObjectPool == null)
            {
                return;
            }
            
            Assert.IsNotNull(GameStatics.CurrentMode, "[MonsterSpawner] 스폰 실패: CurrentMode가 null입니다.");
            if (GameStatics.CurrentMode == null)
            {
                return;
            }

            if (!(GameStatics.CurrentMode is NetDungeonGameMode dungeonGameMode))
            {
                Debug.LogWarning("[MonsterSpawner] 스폰 실패: 현재 게임 모드가 던전 게임 모드(NetDungeonGameMode)가 아닙니다. 몬스터 스포너는 던전 씬에서만 작동합니다.");
                return;
            }

            SpawnTableSO currentTable = dungeonGameMode.CurrentSpawnTable;
            if (currentTable == null)
            {
                Debug.LogWarning("[MonsterSpawner] 스폰 실패: 현재 던전 테마에 매핑된 SpawnTableSO가 없습니다.");
                return;
            }

            NetworkObject prefabToSpawn = currentTable.GetRandomPrefab(spawnType);
            if (prefabToSpawn == null)
            {
                Debug.LogWarning($"[MonsterSpawner] 스폰 실패: SpawnTableSO에서 {spawnType}에 해당하는 프리팹을 찾지 못했습니다.");
                return;
            }

            NetworkObject monsterNetObj = GameStatics.ObjectPool.GetNetworkObject(prefabToSpawn, position, rotation);
            if (monsterNetObj != null)
            {
                monsterNetObj.Spawn();
                currentSpawnCount++;
            }
        }
    }
}
