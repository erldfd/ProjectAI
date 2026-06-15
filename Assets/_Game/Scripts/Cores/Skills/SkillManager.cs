using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using Unity.Netcode;
using UnityEngine.Assertions;
using ProjectAI.Core.Pooling;
using ProjectAI.Characters;
using ProjectAI.Core;
using ProjectAI.SOs;

namespace ProjectAI.Core.Skills
{


    /// <summary>
    /// 시스템에 존재하는 모든 스킬 로직(ISkillLogic) 인스턴스를 보관하고,
    /// 요청된 Enum 타입에 맞춰 알맞은 스킬을 라우팅해주는 전역 싱글톤 매니저입니다.
    /// 서버 측에서 주도적으로 스킬 판정을 내릴 때 활용됩니다.
    /// </summary>
    public class SkillManager : MonoBehaviour
    {

        private SkillDatabaseSO skillDatabase;

        public IReadOnlyList<BaseSkillConfig> SkillConfigs => skillDatabase.SkillConfigs;

        private Dictionary<ESkillType, ISkillLogic> skillLogics = new Dictionary<ESkillType, ISkillLogic>();

        private void Awake()
        {
            GameStatics.RegisterSkillManager(this);
            
            skillDatabase = Resources.Load<SkillDatabaseSO>("SOs/SkillDatabaseSO");
            UnityEngine.Assertions.Assert.IsNotNull(skillDatabase, "[SkillManager] Awake: Resources/SOs/SkillDatabaseSO 로드 실패");

            InitializeSkills();
        }

        private void Start()
        {
            Assert.IsNotNull(GameStatics.ObjectPool, "[SkillManager] Start: ObjectPool이 GameStatics에 등록되어 있지 않습니다! 씬 배치를 확인하십시오.");
        }

        private void OnDestroy()
        {
            GameStatics.UnregisterSkillManager(this);
        }

        /// <summary>
        /// 프로젝트 내에 존재하는 모든 ISkillLogic 구현체들을 동적으로 찾아서 매니저에 등록합니다.
        /// </summary>
        private void InitializeSkills()
        {
            skillLogics.Clear();

            // 리플렉션을 사용하여 ISkillLogic을 구현한 모든 클래스를 찾아 인스턴스화
            Type type = typeof(ISkillLogic);
            IEnumerable<Type> types = null;

            try
            {
                types = AppDomain.CurrentDomain.GetAssemblies()
                    .SelectMany(s => s.GetTypes())
                    .Where(p => type.IsAssignableFrom(p) && !p.IsInterface && !p.IsAbstract);
            }
            catch (ReflectionTypeLoadException e)
            {
                Debug.LogWarning($"[SkillManager] 리플렉션 어셈블리 로드 중 예외 발생. 일부 스킬이 누락될 수 있습니다: {e.Message}");
                types = e.Types.Where(t => t != null && type.IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract);
            }

            foreach (Type t in types)
            {
                // ScriptableObject나 MonoBehaviour가 아닌 순수 C# 클래스라고 가정합니다.
                if (typeof(UnityEngine.Object).IsAssignableFrom(t))
                {
                    Debug.LogWarning($"[SkillManager] {t.Name}은 Unity Object입니다. 순수 C# 클래스(ISkillLogic) 전용 자동 등록에서는 무시될 수 있습니다. 필요시 수동 등록하세요.");
                    continue; // 만약 MonoBehaviour로 구현한다면 여기서 AddComponent 등을 처리해야 합니다. 현재는 순수 C# 클래스를 권장합니다.
                }

                ISkillLogic skillLogic = (ISkillLogic)Activator.CreateInstance(t);
                if (skillLogics.ContainsKey(skillLogic.SkillType))
                {
                    Debug.LogError($"[SkillManager] 중복된 스킬 타입이 발견되었습니다: {skillLogic.SkillType}");
                    continue;
                }

                skillLogic.Initialize(this); // 매니저 데이터 주입
                skillLogics.Add(skillLogic.SkillType, skillLogic);
                Debug.Log($"[SkillManager] 스킬 로직 등록 완료: {skillLogic.SkillType} -> {t.Name}");
            }
        }

        public BaseSkillConfig GetConfig(int skillId)
        {
            UnityEngine.Assertions.Assert.IsNotNull(skillDatabase, "[SkillManager] GetConfig: skillDatabase is null");
            return skillDatabase.GetConfig(skillId);
        }

        public bool ExecuteSkill(int skillId, NetCharacter caster)
        {
            Assert.IsTrue(GameStatics.IsServerAuthorized, "[SkillManager] ExecuteSkill은 서버에서만 호출되어야 합니다.");
            
            if (!GameStatics.IsServerAuthorized)
            {
                return false;
            }

            BaseSkillConfig config = GetConfig(skillId);
            if (config == null)
            {
                Debug.LogWarning($"[SkillManager] ExecuteSkill 실패: SkillId {skillId} 에 해당하는 Config를 찾을 수 없습니다.");
                return false;
            }

            if (!skillLogics.TryGetValue(config.SkillType, out ISkillLogic logic))
            {
                Debug.LogWarning($"[SkillManager] 알 수 없는 스킬 타입입니다: {config.SkillType}");
                return false;
            }

            if (!logic.CanExecute(caster, config))
            {
                return false; // 상태 제약 등
            }

            logic.Execute(caster, config);
            return true;
        }

        public void ActionSkill(int skillId, NetCharacter caster)
        {
            Assert.IsTrue(GameStatics.IsServerAuthorized, "[SkillManager] ActionSkill은 서버에서만 호출되어야 합니다.");
            
            if (!GameStatics.IsServerAuthorized)
            {
                return;
            }

            BaseSkillConfig config = GetConfig(skillId);
            if (config == null)
            {
                return;
            }

            if (skillLogics.TryGetValue(config.SkillType, out ISkillLogic logic))
            {
                logic.Action(caster, config);
            }
        }

        public void EndSkill(int skillId, NetCharacter caster)
        {
            Assert.IsTrue(GameStatics.IsServerAuthorized, "[SkillManager] EndSkill은 서버에서만 호출되어야 합니다.");
            
            if (!GameStatics.IsServerAuthorized)
            {
                return;
            }

            BaseSkillConfig config = GetConfig(skillId);
            if (config == null)
            {
                return;
            }

            if (skillLogics.TryGetValue(config.SkillType, out ISkillLogic logic))
            {
                logic.End(caster, config);
            }
        }
    }
}
