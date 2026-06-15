using UnityEngine;
using UnityEditor;
using UnityEngine.Assertions;
using ProjectAI.Core.Skills;

namespace ProjectAI.SOs.EditorScripts
{
    /// <summary>
    /// BaseSkillConfig 인스펙터 커스텀 에디터 스크립트입니다.
    /// 기획자가 애니메이션 상태 이름을 쉽게 복사하여 활용할 수 있도록 복사 버튼을 제공합니다.
    /// </summary>
    [CustomEditor(typeof(BaseSkillConfig), true)]
    public class BaseSkillConfigEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            BaseSkillConfig config = (BaseSkillConfig)target;
            if (config == null)
            {
                return;
            }

            GUILayout.Space(10);
            
            bool previousGuiState = GUI.enabled;
            GUI.enabled = config.AnimState != EAnimState.None;

            if (GUILayout.Button("애니메이션 이름 클립보드 복사", GUILayout.Height(30)))
            {
                string stateName = config.AnimState.ToString();
                EditorGUIUtility.systemCopyBuffer = stateName;
                Debug.Log($"[BaseSkillConfig] '{stateName}' 복사 완료! 애니메이터 창의 State 이름 칸에 붙여넣기(Ctrl+V) 하세요.");
            }

            GUI.enabled = previousGuiState;
        }
    }
}
