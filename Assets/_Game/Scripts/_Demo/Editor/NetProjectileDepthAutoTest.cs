using UnityEngine;
using UnityEditor;
using System.IO;
using System.Reflection;
using System.Collections.Generic;
using Unity.Netcode;
using ProjectAI.Projectiles;
using ProjectAI.Core.Entities;
using ProjectAI.Core.Stats;
using ProjectAI.Movements;
using ProjectAI.Core;

namespace ProjectAI.Demo.Editor
{
    // 테스트용 모의 Movement 클래스
    public class MockMovement : ANetMovement
    {
        public override Vector2 Velocity => Vector2.zero;
    }

    // 테스트용 모의 Damageable 클래스
    public class MockDamageable : MonoBehaviour, IDamageable
    {
        public NetEntity OwnerEntity { get; set; } = null;
        public float DepthRadius { get; set; } = 0.5f;
        public int LastTakenDamage { get; private set; } = 0;
        public bool WasDamaged { get; private set; } = false;

        public void TakeDamage(int amount)
        {
            WasDamaged = true;
            LastTakenDamage = amount;
        }

        public void ResetMock()
        {
            WasDamaged = false;
            LastTakenDamage = 0;
        }
    }

    [InitializeOnLoad]
    public class NetProjectileDepthAutoTest
    {
        private static List<string> capturedLogs = new List<string>();

        static NetProjectileDepthAutoTest()
        {
            // Unity 로드 완료 후 실행
            EditorApplication.delayCall += RunTests;
        }

        private static void LogCallback(string condition, string stackTrace, LogType type)
        {
            if (type == LogType.Log || type == LogType.Warning || type == LogType.Error)
            {
                capturedLogs.Add(condition);
            }
        }

        static void RunTests()
        {
            string resultPath = "artifacts/tester/NetProjectileDepth_TestResult.txt";
            Directory.CreateDirectory("artifacts/tester");

            using (var writer = new StreamWriter(resultPath, false))
            {
                writer.WriteLine("--- NetProjectile Depth Unity Editor Tests ---");

                Application.logMessageReceived += LogCallback;

                try
                {
                    Test_Damageable_WithinDepth(writer);
                    Test_Damageable_OutOfDepth(writer);
                    Test_Environment_WithinDepth(writer);
                    Test_Environment_OutOfDepth(writer);
                }
                catch (System.Exception e)
                {
                    writer.WriteLine("Test Suite Error: " + e.Message + "\n" + e.StackTrace);
                }
                finally
                {
                    Application.logMessageReceived -= LogCallback;
                }

                writer.WriteLine("--- End of Tests ---");
            }
        }

        static GameObject CreateProjectileContext(out NetProjectile projectile)
        {
            GameObject projGo = new GameObject("TestProjectile");
            var events = projGo.AddComponent<EntityEvents>();
            var stat = projGo.AddComponent<NetStatComponent>();
            stat.DepthRadius = 0.5f;
            stat.AttackPower.Value = 10;
            
            var move = projGo.AddComponent<MockMovement>();
            var rb = projGo.AddComponent<Rigidbody2D>();
            
            projectile = projGo.AddComponent<NetProjectile>();
            
            // Awake를 수동으로 호출하여 참조 세팅
            CallAwake(events);
            CallAwake(stat);
            CallAwake(move);
            CallAwake(projectile);

            return projGo;
        }

        static void CallAwake(MonoBehaviour mb)
        {
            MethodInfo awakeMethod = mb.GetType().GetMethod("Awake", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            if (awakeMethod != null)
            {
                awakeMethod.Invoke(mb, null);
            }
        }

        static void CallOnTriggerEnter2D(NetProjectile proj, Collider2D col)
        {
            MethodInfo onTriggerMethod = typeof(NetProjectile).GetMethod("OnTriggerEnter2D", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            if (onTriggerMethod != null)
            {
                onTriggerMethod.Invoke(proj, new object[] { col });
            }
        }

        static void Test_Damageable_WithinDepth(StreamWriter writer)
        {
            writer.WriteLine("\n[Test 1] Damageable Object - Within Depth Tolerance (Should take damage)");
            capturedLogs.Clear();

            GameObject projGo = CreateProjectileContext(out NetProjectile projectile);
            
            // 캐싱된 깊이를 0으로 설정
            projectile.Initialize(Vector2.right, 999, 0f);

            // Target 생성
            GameObject targetGo = new GameObject("DamageTarget");
            targetGo.transform.position = new Vector3(5, 0.2f, 0); // Y차이 = 0.2 (허용 범위: 0.5 + 0.5 = 1.0)
            
            var damageable = targetGo.AddComponent<MockDamageable>();
            damageable.DepthRadius = 0.5f;
            
            var col = targetGo.AddComponent<BoxCollider2D>();
            col.isTrigger = false;

            // Trigger Enter 
            CallOnTriggerEnter2D(projectile, col);

            bool passed = damageable.WasDamaged && damageable.LastTakenDamage == 10;
            writer.WriteLine($"Expected: Damage Taken, Actual: {(damageable.WasDamaged ? "Taken" : "Missed")} => {(passed ? "PASS" : "FAIL")}");

            GameObject.DestroyImmediate(projGo);
            GameObject.DestroyImmediate(targetGo);
        }

        static void Test_Damageable_OutOfDepth(StreamWriter writer)
        {
            writer.WriteLine("\n[Test 2] Damageable Object - Out of Depth Tolerance (Should ignore/pierce)");
            capturedLogs.Clear();

            GameObject projGo = CreateProjectileContext(out NetProjectile projectile);
            
            // 캐싱된 깊이를 0으로 설정
            projectile.Initialize(Vector2.right, 999, 0f);

            // Target 생성
            GameObject targetGo = new GameObject("DamageTarget_OutOfDepth");
            // Y차이 = 1.5 (허용 범위: 0.5 + 0.5 = 1.0) 초과
            targetGo.transform.position = new Vector3(5, 1.5f, 0); 
            
            var damageable = targetGo.AddComponent<MockDamageable>();
            damageable.DepthRadius = 0.5f;
            
            var col = targetGo.AddComponent<BoxCollider2D>();
            col.isTrigger = false;

            // Trigger Enter 
            CallOnTriggerEnter2D(projectile, col);

            bool ignoredLogFound = capturedLogs.Exists(log => log.Contains("깊이(Y축) 차이가 너무 커서 관통(무시)됨"));
            bool passed = !damageable.WasDamaged && ignoredLogFound;
            
            writer.WriteLine($"Expected: No Damage & Ignore Log, Actual Damage: {damageable.WasDamaged}, LogFound: {ignoredLogFound} => {(passed ? "PASS" : "FAIL")}");

            GameObject.DestroyImmediate(projGo);
            GameObject.DestroyImmediate(targetGo);
        }

        static void Test_Environment_WithinDepth(StreamWriter writer)
        {
            writer.WriteLine("\n[Test 3] Environment Wall - Within Depth Tolerance (Should be destroyed)");
            capturedLogs.Clear();

            GameObject projGo = CreateProjectileContext(out NetProjectile projectile);
            
            // 캐싱된 깊이를 0으로 설정
            projectile.Initialize(Vector2.right, 999, 0f);

            // Wall 생성 (IDamageable 없음, isTrigger = false)
            GameObject wallGo = new GameObject("Wall");
            wallGo.transform.position = new Vector3(5, 0.2f, 0); // Y차이 0.2
            
            var col = wallGo.AddComponent<BoxCollider2D>();
            col.isTrigger = false;

            // Trigger Enter 
            CallOnTriggerEnter2D(projectile, col);

            bool destroyLogFound = capturedLogs.Exists(log => log.Contains("물리 장애물(벽)에 부딪혀 파괴됨"));
            bool passed = destroyLogFound;
            
            writer.WriteLine($"Expected: Destroy Log, LogFound: {destroyLogFound} => {(passed ? "PASS" : "FAIL")}");

            GameObject.DestroyImmediate(projGo);
            GameObject.DestroyImmediate(wallGo);
        }

        static void Test_Environment_OutOfDepth(StreamWriter writer)
        {
            writer.WriteLine("\n[Test 4] Environment Wall - Out of Depth Tolerance (Should ignore/pierce)");
            capturedLogs.Clear();

            GameObject projGo = CreateProjectileContext(out NetProjectile projectile);
            
            // 캐싱된 깊이를 0으로 설정
            projectile.Initialize(Vector2.right, 999, 0f);

            // Wall 생성 (IDamageable 없음, isTrigger = false)
            GameObject wallGo = new GameObject("Wall_OutOfDepth");
            wallGo.transform.position = new Vector3(5, 1.5f, 0); // Y차이 1.5 (허용 범위: 0.5 + 0.5 = 1.0)
            
            var col = wallGo.AddComponent<BoxCollider2D>();
            col.isTrigger = false;

            // Trigger Enter 
            CallOnTriggerEnter2D(projectile, col);

            bool ignoredLogFound = capturedLogs.Exists(log => log.Contains("깊이(Y축) 차이가 너무 커서 관통(무시)됨"));
            bool passed = ignoredLogFound;
            
            writer.WriteLine($"Expected: Ignore Log, LogFound: {ignoredLogFound} => {(passed ? "PASS" : "FAIL")}");

            GameObject.DestroyImmediate(projGo);
            GameObject.DestroyImmediate(wallGo);
        }
    }
}
