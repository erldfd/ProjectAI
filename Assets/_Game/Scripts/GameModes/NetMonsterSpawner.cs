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
    public class NetMonsterSpawner : NetworkBehaviour
    {
        [Header("Spawn Settings")]
        [SerializeField]
        [Tooltip("스폰할 몬스터 프리팹")]
        private NetworkObject monsterPrefab;

        [SerializeField]
        [Tooltip("스폰 주기 (초)")]
        private float spawnInterval = 3f;

        [SerializeField]
        [Tooltip("스폰 반경")]
        private float spawnRadius = 5f;

        [SerializeField]
        [Tooltip("오브젝트 풀 초기 워밍업 크기")]
        private int initialPoolSize = 10;

        [Header("Count Restrictions")]
        [SerializeField]
        [Tooltip("최대 스폰 횟수 (음수: 무제한, 0: 스폰 안 함, 1 이상: 지정 횟수)")]
        private int maxSpawnCount = -1;

        private int currentSpawnCount = 0;
        private float spawnTimer;
        private ISpawnCondition[] conditions = Array.Empty<ISpawnCondition>();

        private void Awake()
        {
            conditions = GetComponents<ISpawnCondition>();
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            Assert.IsNotNull(monsterPrefab, "[NetMonsterSpawner] 몬스터 프리팹이 누락되었습니다.");

            // 서버/클라이언트 공통: 풀링 핸들러 사전 등록 필수
            if (GameStatics.ObjectPool != null)
            {
                GameStatics.ObjectPool.SetupPool(monsterPrefab, initialPoolSize, true);
            }

            if (!GameStatics.IsServerAuthorized)
            {
                return;
            }

            Assert.IsNotNull(GameStatics.ObjectPool, "[NetMonsterSpawner] GameStatics.ObjectPool이 등록되지 않았습니다.");
        }

        private void Update()
        {
            if (!GameStatics.IsServerAuthorized || !IsSpawned)
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

        private void SpawnMonster(Vector3 position, Quaternion rotation)
        {
            Assert.IsTrue(GameStatics.IsServerAuthorized, "[NetMonsterSpawner] 몬스터 스폰 로직은 서버에서만 실행되어야 합니다.");
            
            if (!GameStatics.IsServerAuthorized)
            {
                return;
            }
            
            if (GameStatics.ObjectPool == null || monsterPrefab == null)
            {
                return;
            }

            Vector2 randomPoint = UnityEngine.Random.insideUnitCircle * spawnRadius;
            Vector2 spawnPosition = (Vector2)transform.position + randomPoint;

            NetworkObject monsterNetObj = GameStatics.ObjectPool.GetNetworkObject(monsterPrefab, spawnPosition, Quaternion.identity);
            if (monsterNetObj != null)
            {
                monsterNetObj.Spawn();
                currentSpawnCount++;
            }
        }
    }
}
