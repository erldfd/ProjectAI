using System.Collections.Generic;
using UnityEngine;
using ProjectAI.GameModes;

namespace ProjectAI.SOs
{
    /// <summary>
    /// 던전 테마별 스폰 테이블(SpawnTableSO)을 매핑하는 전역 데이터베이스입니다.
    /// </summary>
    [CreateAssetMenu(fileName = "SpawnTableDatabase", menuName = "ProjectAI/SOs/SpawnTableDatabase")]
    public class SpawnTableDatabaseSO : ScriptableObject
    {
        [System.Serializable]
        public class ThemeTableMapping
        {
            public EDungeonTheme Theme;
            public SpawnTableSO SpawnTable;
        }

        [Tooltip("던전 테마와 해당 테마의 몬스터 스폰 테이블을 매핑합니다.")]
        public List<ThemeTableMapping> Mappings = new List<ThemeTableMapping>();

        private Dictionary<EDungeonTheme, SpawnTableSO> cache;

        private void OnEnable()
        {
            if (cache != null)
            {
                cache.Clear();
            }
            else
            {
                cache = new Dictionary<EDungeonTheme, SpawnTableSO>();
            }
        }

        private void InitializeCache()
        {
            if (cache != null && cache.Count > 0)
            {
                return;
            }

            if (cache == null)
            {
                cache = new Dictionary<EDungeonTheme, SpawnTableSO>();
            }
            
            if (Mappings == null)
            {
                Debug.LogWarning($"[SpawnTableDatabaseSO] {this.name}의 Mappings 리스트가 null입니다. 인스펙터를 확인하세요.");
                return;
            }

            foreach (ThemeTableMapping mapping in Mappings)
            {
                if (mapping == null || mapping.SpawnTable == null)
                {
                    Debug.LogWarning($"[SpawnTableDatabaseSO] {this.name}에 SpawnTable이 할당되지 않은 매핑이 존재합니다. 인스펙터를 확인하세요.");
                    continue;
                }

                if (!cache.ContainsKey(mapping.Theme))
                {
                    cache.Add(mapping.Theme, mapping.SpawnTable);
                }
                else
                {
                    Debug.LogWarning($"[SpawnTableDatabaseSO] {this.name}에 중복된 테마({mapping.Theme}) 매핑이 존재합니다! 첫 번째 매핑만 적용됩니다.");
                }
            }
        }

        /// <summary>
        /// 주어진 테마에 맞는 스폰 테이블을 반환합니다.
        /// </summary>
        public SpawnTableSO GetTable(EDungeonTheme theme)
        {
            InitializeCache();

            if (cache.TryGetValue(theme, out SpawnTableSO table))
            {
                return table;
            }

            Debug.LogWarning($"[SpawnTableDatabaseSO] {theme} 테마에 매핑된 스폰 테이블이 없습니다.");
            return null;
        }
    }
}
