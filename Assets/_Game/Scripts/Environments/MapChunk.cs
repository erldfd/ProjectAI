using UnityEngine;
using System.Collections.Generic;
using System;
using ProjectAI.Core;
using ProjectAI.GameModes;

namespace ProjectAI.Environments
{
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

        private bool isVisited = false;

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
        }

        private void OnTriggerEnter2D(Collider2D collision)
        {
            if (isVisited || !GameStatics.IsServerAuthorized)
            {
                return;
            }

            if (!collision.CompareTag(ObjectTags.PLAYER))
            {
                return;
            }

            isVisited = true;
            ActivateAllSpawners();
        }

        private void ActivateAllSpawners()
        {
            UnityEngine.Assertions.Assert.IsTrue(GameStatics.IsServerAuthorized, "[MapChunk] ActivateAllSpawners는 서버(호스트)에서만 호출되어야 합니다.");

            MonsterSpawner[] spawners = GetComponentsInChildren<MonsterSpawner>();
            foreach (MonsterSpawner spawner in spawners)
            {
                spawner.ActivateSpawner();
            }
        }
    }
}
