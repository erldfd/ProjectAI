#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using ProjectAI.Core.Entities;

namespace ProjectAI.Editor
{
    /// <summary>
    /// AnimStateSelector 속성이 달린 string 필드를 인스펙터에 드롭다운으로 표시해 주는 PropertyDrawer입니다.
    /// 대상 오브젝트의 Animator를 분석하여 애니메이션 상태 목록을 추출합니다.
    /// </summary>
    [CustomPropertyDrawer(typeof(AnimStateSelectorAttribute))]
    public class AnimStateSelectorDrawer : PropertyDrawer
    {
        // 컨트롤러 단위 캐싱으로 매 프레임 OnGUI 호출 시 GC 할당을 방지합니다.
        private static readonly Dictionary<AnimatorController, string[]> stateCache = new Dictionary<AnimatorController, string[]>();

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (property.propertyType != SerializedPropertyType.String)
            {
                EditorGUI.LabelField(position, label.text, "Use [AnimStateSelector] with string.");
                return;
            }

            // 현재 직렬화 중인 컴포넌트(Target)가 속한 오브젝트 트리에서 Animator를 탐색합니다.
            Animator animator = null;
            if (property.serializedObject.targetObject is Component comp)
            {
                animator = comp.GetComponentInChildren<Animator>(true);
                if (animator == null)
                {
                    animator = comp.GetComponentInParent<Animator>(true);
                }
            }
            else if (property.serializedObject.targetObject is GameObject go)
            {
                animator = go.GetComponentInChildren<Animator>(true);
            }

            // 애니메이터나 컨트롤러를 찾지 못한 경우 일반 string 필드로 표시 (Fallback)
            if (animator == null || animator.runtimeAnimatorController == null)
            {
                EditorGUI.PropertyField(position, property, label);
                return;
            }

            RuntimeAnimatorController runtimeController = animator.runtimeAnimatorController;
            AnimatorController editorController = null;
            
            if (runtimeController is AnimatorOverrideController overrideController)
            {
                editorController = overrideController.runtimeAnimatorController as AnimatorController;
            }
            else
            {
                editorController = runtimeController as AnimatorController;
            }

            if (editorController == null)
            {
                EditorGUI.PropertyField(position, property, label);
                return;
            }

            // 컨트롤러 캐시 파싱
            if (!stateCache.TryGetValue(editorController, out string[] stateNamesArray))
            {
                List<string> stateNames = new List<string>();
                stateNames.Add("None");

                foreach (AnimatorControllerLayer layer in editorController.layers)
                {
                    ExtractStatesFromStateMachine(layer.stateMachine, stateNames);
                }

                HashSet<string> uniqueNames = new HashSet<string>(stateNames);
                uniqueNames.Remove("None"); // 정렬 전 None 제거

                stateNames = new List<string>(uniqueNames);
                stateNames.Sort(); // 알파벳 정렬

                stateNames.Insert(0, "None"); // 무조건 0번 인덱스에 None 삽입

                stateNamesArray = stateNames.ToArray();
                stateCache[editorController] = stateNamesArray;
            }

            // 현재 속성(property)에 저장된 값이 목록의 몇 번째 인덱스인지 찾음
            int selectedIndex = System.Array.IndexOf(stateNamesArray, property.stringValue);
            if (selectedIndex < 0)
            {
                selectedIndex = 0; // 목록에 없거나 비어있으면 None으로 설정
            }

            // 인스펙터에 Popup(드롭다운 콤보박스) 그리기
            EditorGUI.BeginChangeCheck();
            selectedIndex = EditorGUI.Popup(position, label.text, selectedIndex, stateNamesArray);
            if (EditorGUI.EndChangeCheck())
            {
                if (selectedIndex == 0) // "None"
                {
                    property.stringValue = "";
                }
                else
                {
                    property.stringValue = stateNamesArray[selectedIndex];
                }
                property.serializedObject.ApplyModifiedProperties();
            }
        }

        private void ExtractStatesFromStateMachine(AnimatorStateMachine stateMachine, List<string> stateNames)
        {
            if (stateMachine == null)
            {
                return;
            }

            foreach (ChildAnimatorState stateNode in stateMachine.states)
            {
                stateNames.Add(stateNode.state.name);
            }

            foreach (ChildAnimatorStateMachine subStateMachineNode in stateMachine.stateMachines)
            {
                ExtractStatesFromStateMachine(subStateMachineNode.stateMachine, stateNames);
            }
        }
    }
}
#endif
