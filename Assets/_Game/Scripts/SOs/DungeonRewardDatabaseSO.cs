using System;
using System.Collections.Generic;
using UnityEngine;
using ProjectAI.GameModes;

namespace ProjectAI.SOs
{
    /// <summary>
    /// 던전 테마별 보상 테이블(DungeonRewardTableSO)을 매핑하는 글로벌 매니저 데이터베이스입니다.
    /// </summary>
    [CreateAssetMenu(fileName = "DungeonRewardDatabase", menuName = "ProjectAI/SOs/DungeonRewardDatabase")]
    public class DungeonRewardDatabaseSO : ScriptableObject
    {
        [Serializable]
        public class ThemeRewardMapping
        {
            public EDungeonTheme Theme;
            public DungeonRewardTableSO RewardTable;
        }

        [Tooltip("던전 테마와 해당 테마의 보상 테이블을 매핑합니다.")]
        public List<ThemeRewardMapping> Mappings = new List<ThemeRewardMapping>();

        private Dictionary<EDungeonTheme, DungeonRewardTableSO> cache;
        private bool isInitialized = false;

        private void OnEnable()
        {
            isInitialized = false;
            if (cache != null)
            {
                cache.Clear();
            }
            else
            {
                cache = new Dictionary<EDungeonTheme, DungeonRewardTableSO>();
            }
        }

        private void InitializeCache()
        {
            if (isInitialized)
            {
                return;
            }

            isInitialized = true;

            if (cache == null)
            {
                cache = new Dictionary<EDungeonTheme, DungeonRewardTableSO>();
            }
            
            if (Mappings == null)
            {
                Debug.LogWarning($"[DungeonRewardDatabaseSO] {this.name}의 Mappings 리스트가 null입니다. 인스펙터를 확인하세요.");
                return;
            }

            foreach (ThemeRewardMapping mapping in Mappings)
            {
                if (mapping == null || mapping.RewardTable == null)
                {
                    Debug.LogWarning($"[DungeonRewardDatabaseSO] {this.name}에 RewardTable이 할당되지 않은 매핑이 존재합니다. 인스펙터를 확인하세요.");
                    continue;
                }

                if (!cache.ContainsKey(mapping.Theme))
                {
                    cache.Add(mapping.Theme, mapping.RewardTable);
                }
                else
                {
                    Debug.LogWarning($"[DungeonRewardDatabaseSO] {this.name}에 중복된 테마({mapping.Theme}) 매핑이 존재합니다! 첫 번째 매핑만 적용됩니다.");
                }
            }
        }

        /// <summary>
        /// 주어진 테마에 맞는 보상 테이블을 반환합니다.
        /// </summary>
        public DungeonRewardTableSO GetTable(EDungeonTheme theme)
        {
            InitializeCache();

            if (cache.TryGetValue(theme, out DungeonRewardTableSO table))
            {
                return table;
            }

            Debug.LogWarning($"[DungeonRewardDatabaseSO] {theme} 테마에 매핑된 보상 테이블이 없습니다.");
            return null;
        }
    }
}
