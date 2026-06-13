#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

namespace ProjectAI.Core.Attributes.Editor
{
    /// <summary>
    /// TagSelectorAttribute가 붙은 string 필드를 인스펙터에서 태그 드롭다운으로 그려줍니다.
    /// </summary>
    [CustomPropertyDrawer(typeof(TagSelectorAttribute))]
    public class TagSelectorPropertyDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (property.propertyType == SerializedPropertyType.String)
            {
                EditorGUI.BeginProperty(position, label, property);
                property.stringValue = EditorGUI.TagField(position, label, property.stringValue);
                EditorGUI.EndProperty();
            }
            else
            {
                EditorGUI.PropertyField(position, property, label);
            }
        }
    }
}
#endif
