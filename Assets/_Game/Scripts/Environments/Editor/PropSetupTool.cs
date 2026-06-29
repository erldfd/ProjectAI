#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using ProjectAI.Render;

namespace ProjectAI.Environments.Editor
{
    /// <summary>
    /// 수많은 프롭(Prop) 프리팹에 일일이 컴포넌트를 달아주는 수고를 덜기 위한 에디터 자동화 툴입니다.
    /// </summary>
    public class PropSetupTool
    {
        [MenuItem("ProjectAI/Tools/Auto Setup Props ZOrder")]
        public static void SetupZOrderOnSelected()
        {
            GameObject[] selectedObjects = Selection.gameObjects;
            int count = 0;

            for (int i = 0; i < selectedObjects.Length; i++)
            {
                GameObject go = selectedObjects[i];
                string assetPath = AssetDatabase.GetAssetPath(go);
                
                // 에셋이 아니거나 프리팹 확장자가 아니라면 무시
                if (string.IsNullOrEmpty(assetPath) || !assetPath.EndsWith(".prefab", System.StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                GameObject prefabRoot = PrefabUtility.LoadPrefabContents(assetPath);
                if (prefabRoot == null)
                {
                    continue;
                }
                
                try
                {
                    ZOrderSorter sorter = prefabRoot.GetComponent<ZOrderSorter>();
                    if (sorter == null)
                    {
                        sorter = prefabRoot.AddComponent<ZOrderSorter>();
                        sorter.IsStatic = true; // 프롭이므로 이동하지 않는 Static으로 기본 설정
                        count++;
                    }

                    // 변경된 내용을 프리팹에 덮어씌움
                    PrefabUtility.SaveAsPrefabAsset(prefabRoot, assetPath);
                }
                finally
                {
                    // 예외가 발생하더라도 반드시 메모리 해제 보장
                    PrefabUtility.UnloadPrefabContents(prefabRoot);
                }
            }

            Debug.Log($"[PropSetupTool] 작업 완료! 총 {count}개의 프리팹에 ZOrderSorter(Static)를 자동으로 부착했습니다.");
        }
    }
}
#endif
