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

        private void OnEnable()
        {
            // [Domain Reload (스크립트 핫 리로드) 방어 코드]
            // 유니티 에디터에서 플레이 중 스크립트를 수정하여 재컴파일이 발생하면, 
            // ScriptableObject의 기본 타입(bool isCached) 상태는 유니티가 백업 후 복구해주지만, 
            // 직렬화가 불가능한 Dictionary 타입(cache)은 백업되지 않고 텅 빈 상태로 날아갑니다.
            // 이로 인해 "캐시는 완료되었다고 뜨는데(isCached == true) 안에는 데이터가 없는" 치명적인 버그가 발생합니다.
            // (실제로 이 문제 때문에 스크립트 수정 직후 'Config를 찾을 수 없습니다' 에러와 함께 스킬 발동이 먹통이 되는 현상이 발생했었습니다.)
            // 이를 방지하기 위해 객체가 로드될 때마다 무조건 캐시 상태를 초기화하여 다시 로드하도록 강제합니다.
            isCached = false;
            if (cache != null)
            {
                cache.Clear();
            }
            else
            {
                cache = new Dictionary<int, BaseSkillConfig>();
            }
        }

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
