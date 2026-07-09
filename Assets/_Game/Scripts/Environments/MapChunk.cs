using UnityEngine;
using System.Collections.Generic;
using System;
using ProjectAI.Core;
using ProjectAI.GameModes;
using Unity.Netcode;

namespace ProjectAI.Environments
{
    public enum EChunkState
    {
        Unvisited,
        Active,
        Cleared
    }
    /// <summary>
    /// 청크를 구성하는 각각의 사각형 충돌 영역(바운더리) 데이터를 담는 클래스입니다.
    /// 여러 개를 조합하여 ㄱ자, T자 등 비정형 모양을 만들 수 있습니다.
    /// </summary>
    [Serializable]
    public class ChunkBound
    {
        [Tooltip("청크 중심을 기준으로 한 바운더리의 로컬 위치(오프셋)")]
        public Vector2 LocalCenter = Vector2.zero;
        
        [Tooltip("바운더리의 크기(너비, 높이)")]
        public Vector2 Size = new Vector2(10f, 10f);
    }

    /// <summary>
    /// 청크가 다른 청크와 결합할 수 있는 연결구(소켓) 정보를 담는 데이터 클래스입니다.
    /// </summary>
    [Serializable]
    public class ChunkConnector
    {
        [Tooltip("청크 중심을 기준으로 한 커넥터의 로컬 위치 (씬 에디터에서 드래그 조절 가능)")]
        public Vector2 LocalPosition = Vector2.zero;
        
        [Tooltip("이 커넥터 고유의 식별 태그 (예: '출구', '입구', '보스방')")]
        public string MyTag = "Default";
        
        [Tooltip("이 커넥터에 연결될 수 있도록 허용하는 대상 태그들의 목록")]
        public List<string> AcceptableTags = new List<string>() { "Default" };
    }

    /// <summary>
    /// 동적 맵 생성을 위한 단위 청크입니다.
    /// 이 스크립트가 붙은 프리팹은 맵 조각이 되며, 커스텀 에디터를 통해 씬 뷰에서 시각적으로 조작할 수 있습니다.
    /// </summary>
    public class MapChunk : MonoBehaviour
    {
        [Header("Chunk Settings")]
        [Tooltip("이 청크가 차지하는 물리적 사각형 영역들의 목록 (씬 뷰에 초록색 박스로 표시됨)")]
        public List<ChunkBound> BoundsList = new List<ChunkBound>();
        
        [Tooltip("에디터에서 커넥터를 드래그할 때 자석처럼 스냅(Snap)될 간격입니다.")]
        public float EditorSnapSize = 0.5f;
        
        [Header("Connections")]
        [Tooltip("다른 청크와 결합할 수 있는 연결구(문)의 목록")]
        public List<ChunkConnector> Connectors = new List<ChunkConnector>();

        [Header("Room Lock Settings")]
        [Tooltip("방에 진입했을 때 입구를 막고, 클리어 시 파괴되는 봉쇄용 오브젝트들 (에디터 인스펙터 수동 할당)")]
        public List<GameObject> RoomBarriers = new List<GameObject>();

        [Header("Reward Settings")]
        [Tooltip("방 클리어 시 방 중앙에 스폰될 임시 재화(기억의 파편) 프리팹")]
        public NetworkObject MemoryFragmentPrefab;

        public EChunkState State { get; private set; } = EChunkState.Unvisited;

        /// <summary> 서버에서 방 클리어 시 발생 (NetMapGenerator가 구독) </summary>
        public event Action<MapChunk> OnRoomClearedServer;

        private int activeSpawners = 0;
        private int aliveMonsters = 0;

        private void Awake()
        {
            // 에디터에서 설정한 BoundsList를 바탕으로 실제 물리 트리거(BoxCollider2D)를 자동 생성합니다.
            foreach (ChunkBound bound in BoundsList)
            {
                BoxCollider2D col = gameObject.AddComponent<BoxCollider2D>();
                col.isTrigger = true;
                col.offset = bound.LocalCenter;
                col.size = bound.Size;
            }

            foreach (GameObject barrier in RoomBarriers)
            {
                if (barrier != null)
                {
                    barrier.SetActive(false);
                }
            }
        }

        private void OnTriggerEnter2D(Collider2D collision)
        {
            if (State != EChunkState.Unvisited)
            {
                return;
            }

            if (!collision.CompareTag(ObjectTags.PLAYER))
            {
                return;
            }

            State = EChunkState.Active;
            
            // 결계 활성화 (클라이언트와 서버 모두 로컬에서 켜짐)
            foreach (GameObject barrier in RoomBarriers)
            {
                if (barrier != null)
                {
                    barrier.SetActive(true);
                }
            }

            if (GameStatics.IsServerAuthorized)
            {
                ActivateAllSpawners();
                CheckRoomClear(); // 혹시 몬스터가 0마리 스폰되는 방일 경우 대비
            }
        }

        private void ActivateAllSpawners()
        {
            UnityEngine.Assertions.Assert.IsTrue(GameStatics.IsServerAuthorized, "[MapChunk] ActivateAllSpawners는 서버(호스트)에서만 호출되어야 합니다.");

            MonsterSpawner[] spawners = GetComponentsInChildren<MonsterSpawner>();
            activeSpawners = spawners.Length;

            foreach (MonsterSpawner spawner in spawners)
            {
                spawner.OnMonsterSpawned += HandleMonsterSpawned;
                spawner.OnSpawningFinished += HandleSpawningFinished;
                spawner.ActivateSpawner();
            }
        }

        private void HandleMonsterSpawned(NetworkObject monsterNetObj)
        {
            // 몬스터 체력 컴포넌트를 찾아 사망(OnDeath) 이벤트 구독
            ProjectAI.Core.Stats.NetHealthComponent healthComp = monsterNetObj.GetComponentInChildren<ProjectAI.Core.Stats.NetHealthComponent>();
            if (healthComp != null)
            {
                aliveMonsters++;
                healthComp.OnDeath += HandleMonsterDeath;
            }
        }

        private void HandleSpawningFinished()
        {
            activeSpawners--;
            CheckRoomClear();
        }

        private void HandleMonsterDeath(ProjectAI.Core.Stats.NetHealthComponent deadHealth)
        {
            aliveMonsters--;
            CheckRoomClear();
            deadHealth.OnDeath -= HandleMonsterDeath;
        }

        private void CheckRoomClear()
        {
            if (State != EChunkState.Active || !GameStatics.IsServerAuthorized)
            {
                return;
            }

            if (activeSpawners <= 0 && aliveMonsters <= 0)
            {
                // 생성기(NetMapGenerator)에게 내가 클리어되었음을 알림
                OnRoomClearedServer?.Invoke(this);
                
                ClearRoomLocally();
                
                // 기억의 파편 스폰 로직 추가 (서버 권한 한정)
                if (MemoryFragmentPrefab != null)
                {
                    NetworkObject fragment = Instantiate(MemoryFragmentPrefab, transform.position, Quaternion.identity);
                    fragment.Spawn();
                }
                else
                {
                    Debug.LogWarning($"[MapChunk] 방 클리어! 하지만 MemoryFragmentPrefab이 할당되지 않았습니다. (Chunk: {gameObject.name})");
                }
            }
        }

        /// <summary>
        /// 방을 클리어 상태로 만들고 결계를 풉니다. (클라이언트 동기화를 위해 public 오픈)
        /// </summary>
        public void ClearRoomLocally()
        {
            if (State == EChunkState.Cleared)
            {
                return;
            }

            State = EChunkState.Cleared;
            
            // 결계 해제 (방 개방)
            foreach (GameObject barrier in RoomBarriers)
            {
                if (barrier != null)
                {
                    barrier.SetActive(false);
                }
            }
            
            Debug.Log($"[MapChunk] 방 클리어! (Chunk: {gameObject.name})");
        }
    }
}
