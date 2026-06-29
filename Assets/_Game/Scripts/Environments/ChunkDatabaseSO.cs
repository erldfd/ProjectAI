using UnityEngine;
using System.Collections.Generic;

namespace ProjectAI.Environments
{
    /// <summary>
    /// 동적 맵 생성을 위한 청크 프리팹 데이터베이스 에셋입니다.
    /// 시작 청크와 사용 가능한 청크 목록을 보관합니다.
    /// </summary>
    [CreateAssetMenu(fileName = "NewChunkDatabase", menuName = "ProjectAI/Environments/Chunk Database")]
    public class ChunkDatabaseSO : ScriptableObject
    {
        [Tooltip("맵의 첫 시작 지점으로 쓰일 고정된 청크 프리팹")]
        public MapChunk StartChunkPrefab;

        [Tooltip("생성기가 꼬리를 물며 스폰할 수 있는 모든 맵 청크 프리팹 목록")]
        public List<MapChunk> AvailableChunks = new List<MapChunk>();
    }
}
