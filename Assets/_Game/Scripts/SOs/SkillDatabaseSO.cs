using System.Collections.Generic;
using UnityEngine;
using ProjectAI.Core.Skills;

namespace ProjectAI.SOs
{
    /// <summary>
    /// 시스템의 모든 스킬 설정 데이터를 리스트로 통합 관리하는 마스터 ScriptableObject입니다.
    /// </summary>
    [CreateAssetMenu(fileName = "SkillDatabase", menuName = "ProjectAI/SOs/SkillDatabase")]
    public class SkillDatabaseSO : ScriptableObject
    {
        public List<BaseSkillConfig> SkillConfigs = new List<BaseSkillConfig>();

        private Dictionary<int, BaseSkillConfig> cache = new Dictionary<int, BaseSkillConfig>();
        private bool isCached = false;

        public void InitializeCache()
        {
            if (isCached)
            {
                return;
            }

            cache.Clear();
            foreach (BaseSkillConfig config in SkillConfigs)
            {
                if (config != null && !cache.ContainsKey(config.SkillId))
                {
                    cache.Add(config.SkillId, config);
                }
            }

            isCached = true;
        }
        
        public BaseSkillConfig GetConfig(int skillId)
        {
            InitializeCache();
            if (cache.TryGetValue(skillId, out BaseSkillConfig config))
            {
                return config;
            }

            return null;
        }
    }
}
