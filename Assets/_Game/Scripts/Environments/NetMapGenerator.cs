using UnityEngine;
using System.Collections.Generic;
using Unity.Netcode;
using ProjectAI.Core;
using UnityEngine.Assertions;

namespace ProjectAI.Environments
{
    /// <summary>
    /// 시드(Seed)를 기반으로 결정론적인 맵을 생성하는 매니저 컴포넌트입니다.
    /// 멀티플레이 환경에서 각 클라이언트가 동일한 시드를 공유받아 동일한 맵 형태를 로컬에서 스폰합니다.
    /// </summary>
    public class NetMapGenerator : NetworkBehaviour
    {
        [Header("Map Settings")]
        
        [Tooltip("최대 몇 개의 청크를 이어 붙일 것인지 설정합니다.")]
        public int MaxChunks = 15;

        // 서버에서 생성한 시드값. 접속하는 모든 클라이언트에게 자동 동기화됨.
        private NetworkVariable<int> mapSeed = new NetworkVariable<int>(
            0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        private System.Random randomGen;
        private List<MapChunk> spawnedChunks = new List<MapChunk>();
        private List<SOpenConnector> openConnectors = new List<SOpenConnector>();
        private int lastGeneratedSeed = 0;

        /// <summary>
        /// 아직 다른 청크와 연결되지 않은(열려있는) 커넥터의 상태를 추적하는 구조체입니다.
        /// </summary>
        private struct SOpenConnector
        {
            public MapChunk ParentChunk;
            public ChunkConnector Connector;
            public Vector3 WorldPosition;
        }

        /// <summary>
        /// 생성기가 매칭 후보를 고를 때 프리팹과 해당 커넥터를 짝지어 저장하는 구조체입니다.
        /// </summary>
        private struct SPrefabMatch
        {
            public MapChunk Prefab;
            public ChunkConnector Connector;
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            if (enabled == false)
            {
                Debug.LogWarning("[NetMapGenerator] 컴포넌트가 비활성화 상태이므로 맵 생성 로직을 실행하지 않습니다.");
                return;
            }

            if (GameStatics.IsServerAuthorized)
            {
                // 서버: 랜덤 시드 생성 후 맵 생성 트리거
                mapSeed.Value = UnityEngine.Random.Range(1, 999999);
                GenerateMap(mapSeed.Value);
            }
            else
            {
                // 클라이언트: 기존 접속자일 경우 이미 값이 들어있다면 즉시 맵 생성
                if (mapSeed.Value != 0)
                {
                    GenerateMap(mapSeed.Value);
                }
            }

            // 시드값이 나중에 바뀌거나, 지연 접속(Late Joiner)인 경우 이벤트 트리거로 맵 생성
            mapSeed.OnValueChanged += OnMapSeedChanged;
        }

        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();
            mapSeed.OnValueChanged -= OnMapSeedChanged;
        }

        private void OnMapSeedChanged(int oldValue, int newValue)
        {
            if (newValue == 0)
            {
                return;
            }

            GenerateMap(newValue);
        }

        private void GenerateMap(int seed)
        {
            if (lastGeneratedSeed == seed)
            {
                Debug.Log($"[NetMapGenerator] 이미 생성된 맵과 동일한 시드({seed})이므로 스킵합니다.");
                return;
            }

            lastGeneratedSeed = seed;

            ClearMap();

            Assert.IsNotNull(GameStatics.MapChunkDB, "[NetMapGenerator] MapChunkDB가 로드되지 않았습니다!");
            Assert.IsNotNull(GameStatics.MapChunkDB.StartChunkPrefab, "[NetMapGenerator] StartChunkPrefab이 할당되지 않았습니다!");

            // 모든 클라이언트가 이 시드를 기준으로 동일한 난수를 뽑아냅니다 (결정론적 동기화).
            randomGen = new System.Random(seed);
            Debug.Log($"[MapGenerator] 시드({seed}) 기반 동적 맵 생성 시작! (MaxChunks: {MaxChunks})");

            // 1. 시작 청크 스폰
            MapChunk startChunk = Instantiate(GameStatics.MapChunkDB.StartChunkPrefab, transform.position, Quaternion.identity, transform);
            startChunk.name = "StartChunk";
            spawnedChunks.Add(startChunk);

            AddOpenConnectors(startChunk, null);

            int currentChunkCount = 1;

            // 2. 꼬리물기 스폰 반복 루프
            while (openConnectors.Count > 0 && currentChunkCount < MaxChunks)
            {
                // 연결 대기 중인 빈 문(커넥터) 중 하나를 무작위로 선택
                int connectorIndex = randomGen.Next(0, openConnectors.Count);
                SOpenConnector targetOpenConnector = openConnectors[connectorIndex];
                
                // 선택한 문은 이제 막히게 되므로 목록에서 제거
                openConnectors.RemoveAt(connectorIndex);

                // 이 커넥터가 수용할 수 있고, 상대방 커넥터도 이를 수용할 수 있는(상호 동의) 모든 조합을 탐색
                List<SPrefabMatch> candidates = new List<SPrefabMatch>();
                for (int i = 0; i < GameStatics.MapChunkDB.AvailableChunks.Count; i++)
                {
                    MapChunk chunk = GameStatics.MapChunkDB.AvailableChunks[i];
                    if (chunk == null)
                    {
                        continue;
                    }

                    for (int j = 0; j < chunk.Connectors.Count; j++)
                    {
                        ChunkConnector candidateConnector = chunk.Connectors[j];
                        if (candidateConnector == null)
                        {
                            continue;
                        }

                        bool isSourceAcceptingCandidate = targetOpenConnector.Connector.AcceptableTags != null && targetOpenConnector.Connector.AcceptableTags.Contains(candidateConnector.MyTag);
                        bool isCandidateAcceptingSource = candidateConnector.AcceptableTags != null && candidateConnector.AcceptableTags.Contains(targetOpenConnector.Connector.MyTag);

                        if (!isSourceAcceptingCandidate || !isCandidateAcceptingSource)
                        {
                            continue;
                        }

                        SPrefabMatch match = new SPrefabMatch();
                        match.Prefab = chunk;
                        match.Connector = candidateConnector;
                        candidates.Add(match);
                    }
                }

                if (candidates.Count == 0)
                {
                    Debug.LogWarning($"[NetMapGenerator] 태그 '{targetOpenConnector.Connector.MyTag}'와 상호 연결이 가능한 청크를 찾지 못해 막다른 길이 됩니다.");
                    continue;
                }

                // 무작위로 섞기
                int n = candidates.Count;
                while (n > 1)
                {
                    n--;

                    int k = randomGen.Next(n + 1);
                    SPrefabMatch value = candidates[k];
                    candidates[k] = candidates[n];
                    candidates[n] = value;
                }

                bool isSpawned = false;

                for (int i = 0; i < candidates.Count; i++)
                {
                    MapChunk prefabToSpawn = candidates[i].Prefab;
                    ChunkConnector prefabConnector = candidates[i].Connector;

                    // 핵심 로직: 새 청크가 놓여야 할 정확한 월드 위치 계산
                    Vector3 newChunkPos = targetOpenConnector.WorldPosition - (Vector3)prefabConnector.LocalPosition;

                    // 물리적 오버랩(충돌) 검사
                    if (!CheckOverlap(prefabToSpawn, newChunkPos))
                    {
                        // 겹치지 않으면 스폰 진행
                        Assert.IsNull(prefabToSpawn.GetComponentInChildren<NetworkObject>(true), 
                            $"[NetMapGenerator] 에러: '{prefabToSpawn.name}' 프리팹에 NetworkObject가 포함되어 있습니다. 결정론적 로컬 스폰 방식에서는 맵 청크에 NetworkObject를 포함할 수 없습니다.");

                        MapChunk newChunk = Instantiate(prefabToSpawn, newChunkPos, Quaternion.identity, transform);
                        newChunk.name = $"{prefabToSpawn.name}_{currentChunkCount}";
                        spawnedChunks.Add(newChunk);
                        currentChunkCount++;

                        AddOpenConnectors(newChunk, prefabConnector);
                        isSpawned = true;
                        break; // 성공적으로 스폰했으므로 후보군 탐색 종료
                    }
                }

                if (!isSpawned)
                {
                    Debug.LogWarning($"[NetMapGenerator] 태그 '{targetOpenConnector.Connector.MyTag}' 주변의 공간이 너무 비좁아 막다른 길이 됩니다.");
                }
            }

            Debug.Log($"[MapGenerator] 맵 생성 완료! 스폰된 청크 총 개수: {spawnedChunks.Count}");
        }

        private void AddOpenConnectors(MapChunk chunk, ChunkConnector ignoreConnector)
        {
            for (int i = 0; i < chunk.Connectors.Count; i++)
            {
                ChunkConnector c = chunk.Connectors[i];
                if (c == null)
                {
                    continue;
                }

                if (ignoreConnector != null && c.LocalPosition == ignoreConnector.LocalPosition && c.MyTag == ignoreConnector.MyTag)
                {
                    continue;
                }

                SOpenConnector openC = new SOpenConnector();
                openC.ParentChunk = chunk;
                openC.Connector = c;
                openC.WorldPosition = chunk.transform.TransformPoint(c.LocalPosition);
                
                openConnectors.Add(openC);
            }
        }

        private bool CheckOverlap(MapChunk prefabToSpawn, Vector3 newChunkPos)
        {
            float offset = 0.1f;

            for (int i = 0; i < prefabToSpawn.BoundsList.Count; i++)
            {
                ChunkBound newBound = prefabToSpawn.BoundsList[i];
                if (newBound == null)
                {
                    continue;
                }

                Vector3 newCenter = newChunkPos + (Vector3)newBound.LocalCenter;
                Vector2 newSize = newBound.Size;
                Rect newRect = new Rect(newCenter.x - newSize.x * 0.5f + offset, newCenter.y - newSize.y * 0.5f + offset, newSize.x - offset * 2f, newSize.y - offset * 2f);

                for (int j = 0; j < spawnedChunks.Count; j++)
                {
                    MapChunk existingChunk = spawnedChunks[j];
                    if (existingChunk == null)
                    {
                        continue;
                    }

                    Vector3 existingPos = existingChunk.transform.position;

                    for (int k = 0; k < existingChunk.BoundsList.Count; k++)
                    {
                        ChunkBound existingBound = existingChunk.BoundsList[k];
                        if (existingBound == null)
                        {
                            continue;
                        }

                        Vector3 center = existingPos + (Vector3)existingBound.LocalCenter;
                        Vector2 size = existingBound.Size;
                        Rect existingRect = new Rect(center.x - size.x * 0.5f + offset, center.y - size.y * 0.5f + offset, size.x - offset * 2f, size.y - offset * 2f);

                        if (newRect.Overlaps(existingRect))
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }



        private void ClearMap()
        {
            for (int i = 0; i < spawnedChunks.Count; i++)
            {
                if (spawnedChunks[i] == null)
                {
                    continue;
                }

                Destroy(spawnedChunks[i].gameObject);
            }

            spawnedChunks.Clear();
            openConnectors.Clear();
        }
    }
}
