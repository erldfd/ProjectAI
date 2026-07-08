using System;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.Assertions;
using System.Collections.Generic;
using ProjectAI.Core.Enums;

namespace ProjectAI.SOs
{
    /// <summary>
    /// 개별 팝업 타입과 UXML 템플릿 에셋의 매핑 정보를 담는 클래스입니다.
    /// </summary>
    [Serializable]
    public class UIPopupMapping
    {
        [Tooltip("매핑할 팝업의 종류")]
        public EUIPopupType PopupType;
        
        [Tooltip("해당 팝업을 그릴 UI Toolkit의 UXML 템플릿 에셋")]
        public VisualTreeAsset UxmlTemplate;
    }

    /// <summary>
    /// EUIPopupType 열거형과 실제 UXML 템플릿 에셋을 연결해주는 데이터베이스입니다.
    /// </summary>
    [CreateAssetMenu(fileName = "UIPopupDB", menuName = "ProjectAI/UI/UIPopupDatabase")]
    public class UIPopupDatabaseSO : ScriptableObject
    {
        [SerializeField]
        private List<UIPopupMapping> mappings = new List<UIPopupMapping>();

        /// <summary>
        /// 요청한 팝업 타입에 해당하는 UXML 에셋을 반환합니다.
        /// </summary>
        /// <param name="popupType">찾고자 하는 팝업 타입</param>
        /// <returns>매핑된 UXML 에셋 (없으면 null)</returns>
        public VisualTreeAsset GetUxmlTemplate(EUIPopupType popupType)
        {
            Assert.IsNotNull(mappings, "[UIPopupDatabaseSO] mappings 리스트는 절대 null이 되어서는 안 됩니다!");

            foreach (UIPopupMapping mapping in mappings)
            {
                Assert.IsNotNull(mapping, "[UIPopupDatabaseSO] mappings 리스트 내부에 비어있는(null) 매핑 요소가 존재합니다!");

                if (mapping.PopupType == popupType)
                {
                    Assert.IsNotNull(mapping.UxmlTemplate, $"[UIPopupDatabaseSO] '{popupType}' 타입이 매핑되어 있으나 UXML 에셋이 할당되지 않았습니다.");
                    return mapping.UxmlTemplate;
                }
            }

            Debug.LogError($"[UIPopupDatabaseSO] '{popupType}' 타입에 매핑된 UXML 에셋을 찾을 수 없습니다! 인스펙터 세팅을 확인하세요.");
            return null;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            Assert.IsNotNull(mappings, "[UIPopupDatabaseSO] OnValidate: mappings 리스트는 절대 null이 되어서는 안 됩니다!");

            HashSet<EUIPopupType> seenTypes = new HashSet<EUIPopupType>();
            foreach (UIPopupMapping mapping in mappings)
            {
                Assert.IsNotNull(mapping, "[UIPopupDatabaseSO] OnValidate: mappings 리스트 내부에 비어있는(null) 매핑 요소가 존재합니다!");

                if (seenTypes.Contains(mapping.PopupType))
                {
                    Debug.LogWarning($"[UIPopupDatabaseSO] 중복된 팝업 타입 매핑이 감지되었습니다: {mapping.PopupType}. 첫 번째 항목만 유효하게 처리됩니다.");
                }
                else
                {
                    seenTypes.Add(mapping.PopupType);
                }
            }
        }
#endif
    }
}
