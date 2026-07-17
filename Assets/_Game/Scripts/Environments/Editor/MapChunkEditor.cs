#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace ProjectAI.Environments.Editor
{
    /// <summary>
    /// MapChunk를 에디터 씬 뷰에서 시각적으로 조작할 수 있게 해주는 커스텀 에디터 스크립트입니다.
    /// </summary>
    [CustomEditor(typeof(MapChunk))]
    public class MapChunkEditor : UnityEditor.Editor
    {
        private GUIStyle spawnLabelStyle;
        private GUIStyle connectorLabelStyle;

        private void OnSceneGUI()
        {
            MapChunk chunk = (MapChunk)target;
            if (chunk == null)
            {
                return;
            }

            Transform t = chunk.transform;

            // 1. 바운더리(Bounds) 영역들을 씬 뷰에 약간 진한 반투명 초록색 박스로 모두 표시
            Color originalColor = Handles.color;
            try
            {
                Handles.color = new Color(0f, 1f, 0f, 0.35f);

                for (int i = 0; i < chunk.BoundsList.Count; i++)
                {
                    ChunkBound bound = chunk.BoundsList[i];
                    if (bound == null)
                    {
                        continue;
                    }

                    Vector3[] rect = GetBoundsRect(t, (Vector3)bound.LocalCenter, bound.Size);
                    Handles.DrawSolidRectangleWithOutline(rect, new Color(0f, 1f, 0f, 0.35f), Color.green);
                }
            }
            finally
            {
                Handles.color = originalColor; // 원래 색상 복원
            }

            // 2. 기억의 파편 스폰 오프셋 시각화 (시안색 텍스트 및 핸들)
            Vector3 spawnPos = t.TransformPoint((Vector3)chunk.MemoryFragmentSpawnOffset);
            Handles.color = Color.cyan;
            Handles.DrawWireDisc(spawnPos, Vector3.forward, 0.5f);
            
            if (spawnLabelStyle == null)
            {
                spawnLabelStyle = new GUIStyle();
                spawnLabelStyle.normal.textColor = Color.cyan;
                spawnLabelStyle.alignment = TextAnchor.MiddleCenter;
                spawnLabelStyle.fontSize = 12;
                spawnLabelStyle.fontStyle = FontStyle.Bold;
            }

            Handles.Label(spawnPos + new Vector3(0, 0.7f, 0), "Memory Fragment Spawn", spawnLabelStyle);

            EditorGUI.BeginChangeCheck();
            Vector3 newSpawnWorldPos = Handles.PositionHandle(spawnPos, Quaternion.identity);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(chunk, "Move Memory Fragment Spawn");
                chunk.MemoryFragmentSpawnOffset = (Vector2)t.InverseTransformPoint(newSpawnWorldPos);
            }

            // 3. 커넥터(연결구) 핸들 표시 및 상호작용
            for (int i = 0; i < chunk.Connectors.Count; i++)
            {
                ChunkConnector connector = chunk.Connectors[i];
                if (connector == null)
                {
                    continue;
                }

                // 로컬 좌표를 월드 좌표로 변환하여 에디터 화면에 띄움
                Vector3 worldPos = t.TransformPoint(connector.LocalPosition);

                // 연결구 태그 표시 (노란색 텍스트)
                if (connectorLabelStyle == null)
                {
                    connectorLabelStyle = new GUIStyle(EditorStyles.label);
                    connectorLabelStyle.normal.textColor = Color.yellow;
                    connectorLabelStyle.fontStyle = FontStyle.Bold;
                }

                Handles.Label(worldPos + Vector3.up * 0.5f, $"커넥터: {connector.MyTag}", connectorLabelStyle);

                // 씬 뷰에서 마우스로 붙잡고 이동할 수 있는 이동 축(Position Handle) 제공
                EditorGUI.BeginChangeCheck();
                Vector3 newWorldPos = Handles.PositionHandle(worldPos, Quaternion.identity);

                // 마우스 드래그로 위치가 변경되었다면
                if (EditorGUI.EndChangeCheck())
                {
                    // Undo(Ctrl+Z) 기록 남기기
                    Undo.RecordObject(chunk, "Move Chunk Connector");
                    
                    // 마우스가 이동한 새 월드 좌표를 다시 로컬 좌표로 변환
                    Vector3 newLocalPos = t.InverseTransformPoint(newWorldPos);
                    
                    // [편의성 개선] 타일맵 그리드 단위에 자석처럼 딱딱 붙도록 스냅(Snap) 처리
                    float snapSize = chunk.EditorSnapSize;
                    if (snapSize > 0f)
                    {
                        newLocalPos.x = Mathf.Round(newLocalPos.x / snapSize) * snapSize;
                        newLocalPos.y = Mathf.Round(newLocalPos.y / snapSize) * snapSize;
                    }
                    
                    newLocalPos.z = 0f; // 2D 환경이므로 Z축은 무조건 0으로 고정

                    connector.LocalPosition = newLocalPos;
                    
                    EditorUtility.SetDirty(chunk);
                    PrefabUtility.RecordPrefabInstancePropertyModifications(chunk);
                }
            }
        }

        /// <summary>
        /// 바운더리 사각형의 4개 꼭짓점 좌표를 반환하는 헬퍼 함수
        /// </summary>
        private Vector3[] GetBoundsRect(Transform t, Vector3 localCenter, Vector2 size)
        {
            float halfX = size.x * 0.5f;
            float halfY = size.y * 0.5f;
            Vector3[] corners = new Vector3[4];
            corners[0] = t.TransformPoint(localCenter + new Vector3(-halfX, -halfY, 0f));
            corners[1] = t.TransformPoint(localCenter + new Vector3(-halfX,  halfY, 0f));
            corners[2] = t.TransformPoint(localCenter + new Vector3( halfX,  halfY, 0f));
            corners[3] = t.TransformPoint(localCenter + new Vector3( halfX, -halfY, 0f));
            return corners;
        }
    }
}
#endif
