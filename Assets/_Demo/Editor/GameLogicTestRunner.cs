using UnityEngine;
using UnityEditor;
using ProjectAI.Core.Stats;
using ProjectAI.Core.Entities;
using ProjectAI.Characters;
using ProjectAI.Core.Skills.Abilities;
using ProjectAI.Core.Skills;
using ProjectAI.SOs;
using System.Reflection;

namespace ProjectAI.Tests
{
    public class GameLogicTestRunner
    {
        [MenuItem("Tests/Run Game Logic Tests")]
        public static void RunTests()
        {
            Debug.Log("==================== TESTS STARTED ====================");
            
            bool test1Passed = RunLateJoinerOrderTest();
            bool test2Passed = RunBasicAttackHitTest();
            
            Debug.Log($"[Test Results] LateJoinerOrderTest: {(test1Passed ? "PASSED" : "FAILED")}");
            Debug.Log($"[Test Results] BasicAttackHitTest: {(test2Passed ? "PASSED" : "FAILED")}");
            
            Debug.Log("==================== TESTS FINISHED ====================");
            
            // CI/CD 환경용 (BatchMode)
            if (Application.isBatchMode)
            {
                EditorApplication.Exit((test1Passed && test2Passed) ? 0 : 1);
            }
        }

        private static bool RunLateJoinerOrderTest()
        {
            Debug.Log("[TEST] RunLateJoinerOrderTest 시작");
            GameObject go = new GameObject("TestCharacter_LateJoiner");
            var entityEvents = go.AddComponent<EntityEvents>();
            var healthComp = go.AddComponent<NetHealthComponent>();
            var statComp = go.AddComponent<NetStatComponent>();
            var character = go.AddComponent<NetCharacter>();
            
            // 의존성 주입 (Awake 시뮬레이션)
            var statHealthField = typeof(NetStatComponent).GetField("healthComponent", BindingFlags.NonPublic | BindingFlags.Instance);
            if (statHealthField != null) statHealthField.SetValue(statComp, healthComp);
            
            var statEventsField = typeof(NetStatComponent).GetField("entityEvents", BindingFlags.NonPublic | BindingFlags.Instance);
            if (statEventsField != null) statEventsField.SetValue(statComp, entityEvents);
            
            healthComp.CurrentHealth.Value = 0; // 초기 체력 0
            
            bool deathTriggered = false;
            entityEvents.OnDeathTriggered += () => { 
                deathTriggered = true; 
                Debug.Log("[TEST] 사망 이벤트가 OnDeathTriggered로 전달됨!");
            };
            
            // 시나리오: NetHealthComponent가 먼저 OnNetworkSpawn 됨
            Debug.Log("[TEST] NetHealthComponent OnNetworkSpawn 호출 (사망 상태 동기화 시도)");
            healthComp.OnNetworkSpawn();
            
            // 이후 다른 컴포넌트들 OnNetworkSpawn
            Debug.Log("[TEST] NetStatComponent, NetCharacter OnNetworkSpawn 호출 (이벤트 구독 시도)");
            statComp.OnNetworkSpawn();
            character.OnNetworkSpawn();
            
            Object.DestroyImmediate(go);
            
            // 검증 로직
            if (!deathTriggered)
            {
                Debug.LogError("[TEST FAILED] 지연 접속자의 사망 이벤트 구독이 누락되었습니다! (CurrentHealth <= 0 이지만 OnDeathTriggered가 호출되지 않음)");
                return false; // 실패해야 정상 시나리오(버그 재현 성공)지만, 검증이므로 일단 실패 처리
            }
            
            Debug.Log("[TEST PASSED] 사망 이벤트가 정상적으로 처리되었습니다.");
            return true;
        }

        private static bool RunBasicAttackHitTest()
        {
            Debug.Log("[TEST] RunBasicAttackHitTest 시작");
            // BasicAttackLogic 생성 및 환경 구성
            GameObject casterGo = new GameObject("Caster");
            var casterCharacter = casterGo.AddComponent<NetCharacter>();
            var casterStat = casterGo.AddComponent<NetStatComponent>();
            var casterSkill = casterGo.AddComponent<NetSkillComponent>();
            
            GameObject targetGo = new GameObject("Target");
            var targetHealth = targetGo.AddComponent<NetHealthComponent>();
            var targetStat = targetGo.AddComponent<NetStatComponent>();
            var targetEntity = targetGo.AddComponent<NetEntity>();
            
            // Entity 연결
            targetHealth.SetOwner(targetEntity);
            var targetStatOwnerField = typeof(NetStatComponent).GetProperty("OwnerEntity");
            if (targetStatOwnerField != null && targetStatOwnerField.CanWrite)
                targetStatOwnerField.SetValue(targetStat, targetEntity);
            else if(targetStatOwnerField == null)
            {
               var field = typeof(NetStatComponent).GetField("<OwnerEntity>k__BackingField", BindingFlags.NonPublic | BindingFlags.Instance);
               if(field != null) field.SetValue(targetStat, targetEntity);
            }
            
            var targetEntityStatProp = typeof(NetEntity).GetProperty("StatComponent");
            if (targetEntityStatProp != null && targetEntityStatProp.CanWrite) targetEntityStatProp.SetValue(targetEntity, targetStat);
            else
            {
                var field = typeof(NetEntity).GetField("<StatComponent>k__BackingField", BindingFlags.NonPublic | BindingFlags.Instance);
                if(field != null) field.SetValue(targetEntity, targetStat);
            }
            
            // 스탯 세팅
            casterStat.DepthRadius = 0.5f;
            targetStat.DepthRadius = 0.5f;
            
            // 타겟 Y 오프셋을 두어 뎁스(Z축 개념) 테스트
            casterGo.transform.position = new Vector3(0, 0, 0);
            targetGo.transform.position = new Vector3(0, 1.5f, 0); // 1.5f 차이 -> 허용치 1.0f(0.5+0.5) 초과로 빗나감 예상
            
            // Hitbox 구성
            var casterHitbox = casterGo.AddComponent<BoxCollider2D>();
            casterHitbox.isTrigger = true;
            casterHitbox.size = new Vector2(5, 5); // 충분히 크게
            
            var targetCollider = targetGo.AddComponent<BoxCollider2D>();
            targetCollider.isTrigger = true;
            targetCollider.size = new Vector2(1, 1);
            
            // Reflection으로 MeleeHitbox 세팅
            var meleeHitboxProp = typeof(NetSkillComponent).GetProperty("MeleeHitbox");
            if (meleeHitboxProp != null && meleeHitboxProp.CanWrite)
            {
                meleeHitboxProp.SetValue(casterSkill, casterHitbox);
            }
            else
            {
                var hitboxField = typeof(NetSkillComponent).GetField("<MeleeHitbox>k__BackingField", BindingFlags.NonPublic | BindingFlags.Instance);
                if (hitboxField != null) hitboxField.SetValue(casterSkill, casterHitbox);
            }
            
            var statProp = typeof(NetCharacter).GetProperty("StatComponent");
            if (statProp != null && statProp.CanWrite) statProp.SetValue(casterCharacter, casterStat);
            else
            {
                var statField = typeof(NetCharacter).GetField("<StatComponent>k__BackingField", BindingFlags.NonPublic | BindingFlags.Instance);
                if (statField != null) statField.SetValue(casterCharacter, casterStat);
            }

            var skillProp = typeof(NetCharacter).GetProperty("SkillComponent");
            if (skillProp != null && skillProp.CanWrite) skillProp.SetValue(casterCharacter, casterSkill);
            else
            {
                var skillField = typeof(NetCharacter).GetField("<SkillComponent>k__BackingField", BindingFlags.NonPublic | BindingFlags.Instance);
                if (skillField != null) skillField.SetValue(casterCharacter, casterSkill);
            }
            
            // Physics 갱신
            Physics2D.SyncTransforms();
            
            // 체력 이벤트 모니터링
            bool isHit = false;
            targetHealth.OnHit += (dmg, remain) => { isHit = true; Debug.Log($"[TEST] Target Hit! Damage: {dmg}"); };
            targetHealth.TakeDamage(0); // 런타임 오류 방지용 초기화 겸 체크
            
            var logic = new BasicAttackLogic();
            var config = ScriptableObject.CreateInstance<BaseSkillConfig>();
            config.SkillId = 1;
            
            Debug.Log("[TEST] 깊이 차이가 허용치(1.0f)를 초과(1.5f)하는 타겟 공격 시도 (빗나감 예상)");
            logic.Action(casterCharacter, config);
            
            bool passed = true;
            if (isHit)
            {
                Debug.LogError("[TEST FAILED] 빗나가야 할 타겟이 피격되었습니다!");
                passed = false;
            }
            else
            {
                Debug.Log("[TEST] 예상대로 빗나갔습니다. 이제 위치를 일치시키고 다시 공격합니다.");
                
                targetGo.transform.position = new Vector3(0, 0.5f, 0); // 0.5f 차이 -> 허용치(1.0f) 내이므로 맞아야 함
                Physics2D.SyncTransforms();
                
                logic.Action(casterCharacter, config);
                
                if (!isHit)
                {
                    Debug.LogError("[TEST FAILED] 맞아야 할 타겟이 피격되지 않았습니다!");
                    passed = false;
                }
                else
                {
                    Debug.Log("[TEST] 타겟 피격 확인 완료!");
                }
            }
            
            Object.DestroyImmediate(casterGo);
            Object.DestroyImmediate(targetGo);
            Object.DestroyImmediate(config);
            
            return passed;
        }
    }
}
