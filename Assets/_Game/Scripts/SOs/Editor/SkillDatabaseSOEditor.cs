using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

namespace ProjectAI.SOs.EditorScripts
{
    /// <summary>
    /// SkillDatabaseSO 인스펙터 커스텀 에디터 스크립트입니다.
    /// 프로젝트 내의 활성화된 스킬 데이터를 일괄 불러오고 중복을 제거하는 기능을 제공합니다.
    /// </summary>
    [CustomEditor(typeof(SkillDatabaseSO))]
    public class SkillDatabaseSOEditor : UnityEditor.Editor
    {
        private const string ACTIVE_SKILLS_PATH = "Assets/_Game/SOs/SkillDatas/Actives";

        public override void OnInspectorGUI()
        {
            SkillDatabaseSO database = (SkillDatabaseSO)target;
            if (database == null)
            {
                return;
            }

            // 1. 중복 감지 및 경고 UI
            bool hasDuplicates = false;
            if (database.SkillConfigs != null)
            {
                System.Collections.Generic.IEnumerable<BaseSkillConfig> validSkills = database.SkillConfigs.Where(s => s != null);
                int distinctCount = validSkills.Distinct().Count();
                int totalCount = validSkills.Count();
                
                if (distinctCount < totalCount)
                {
                    hasDuplicates = true;
                }
            }

            if (hasDuplicates)
            {
                EditorGUILayout.HelpBox("⚠️ 경고: 리스트에 중복된 스킬 데이터(SO)가 감지되었습니다!", MessageType.Error);
                if (GUILayout.Button("중복 스킬 자동 제거하기", GUILayout.Height(30)))
                {
                    Undo.RecordObject(database, "Remove Duplicate Skills");
                    database.SkillConfigs = database.SkillConfigs.Where(s => s != null).Distinct().ToList();
                    EditorUtility.SetDirty(database);
                }
                GUILayout.Space(10);
            }

            // 2. Active 폴더의 스킬 일괄 불러오기 버튼
            if (GUILayout.Button($"'{ACTIVE_SKILLS_PATH}' 폴더 내\n전체 스킬 자동 불러오기", GUILayout.Height(40)))
            {
                LoadAllSkillsFromActivePath(database);
            }

            GUILayout.Space(15);

            // 기본 리스트 인스펙터
            base.OnInspectorGUI();
        }

        private void LoadAllSkillsFromActivePath(SkillDatabaseSO database)
        {
            if (!AssetDatabase.IsValidFolder(ACTIVE_SKILLS_PATH))
            {
                Debug.LogWarning($"[SkillDatabaseSOEditor] '{ACTIVE_SKILLS_PATH}' 폴더를 찾을 수 없습니다. 폴더가 존재하는지 확인하세요.");
                return;
            }

            // 해당 경로에 있는 BaseSkillConfig 타입의 에셋을 모두 찾습니다.
            string[] guids = AssetDatabase.FindAssets($"t:{nameof(BaseSkillConfig)}", new string[] { ACTIVE_SKILLS_PATH });
            List<BaseSkillConfig> loadedSkills = new List<BaseSkillConfig>();

            foreach (string guid in guids)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                BaseSkillConfig config = AssetDatabase.LoadAssetAtPath<BaseSkillConfig>(assetPath);
                if (config != null)
                {
                    loadedSkills.Add(config);
                }
            }

            // 중복 없이 불러온 리스트로 완전히 교체합니다.
            Undo.RecordObject(database, "Load All Active Skills");
            database.SkillConfigs = loadedSkills;
            
            // 변경 사항을 저장합니다.
            EditorUtility.SetDirty(database);
            AssetDatabase.SaveAssets();

            Debug.Log($"[SkillDatabaseSOEditor] 총 {loadedSkills.Count}개의 스킬을 성공적으로 불러왔습니다!");
        }
    }
}
