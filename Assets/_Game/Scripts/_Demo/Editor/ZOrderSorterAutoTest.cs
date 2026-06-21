using UnityEngine;
using UnityEditor;
using System.IO;
using System.Reflection;
using UnityEngine.Rendering;
using ProjectAI.Render;

namespace ProjectAI.Demo.Editor
{
    [InitializeOnLoad]
    public class ZOrderSorterAutoTest
    {
        static ZOrderSorterAutoTest()
        {
            // Delay call to ensure Unity is ready and we don't block assembly reload
            EditorApplication.delayCall += RunTests;
        }

        static void RunTests()
        {
            string resultPath = "artifacts/tester/ZOrderSorter_TestResult.txt";
            Directory.CreateDirectory("artifacts/tester");
            using (StreamWriter writer = new StreamWriter(resultPath, false))
            {
                writer.WriteLine("--- ZOrderSorter Test Results ---");

                try {
                    Test1_SortingOrderCalculation(writer);
                    Test2_ChildYChangeIndependent(writer);
                    Test3_IsStaticOptimization(writer);
                }
                catch (System.Exception e) {
                    writer.WriteLine("Test Suite Error: " + e.Message + "\n" + e.StackTrace);
                }

                writer.WriteLine("--- End of Tests ---");
            }
        }

        static void Test1_SortingOrderCalculation(StreamWriter writer)
        {
            writer.WriteLine("\n[Test 1] Y Change -> SortingOrder Update (Offset included)");
            GameObject go = new GameObject("Test1_Root");
            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            ZOrderSorter sorter = go.AddComponent<ZOrderSorter>();
            sorter.SortingOffset = 50;
            
            go.transform.position = new Vector3(0, -5.2f, 0);
            sorter.UpdateSortingOrder(); 
            
            // Expected: -(-5.2) * 100 + 50 = 520 + 50 = 570
            int expected = Mathf.RoundToInt(5.2f * 100f) + 50;
            bool pass = sr.sortingOrder == expected;
            writer.WriteLine($"Expected: {expected}, Actual: {sr.sortingOrder} => {(pass ? "PASS" : "FAIL")}");
            
            GameObject.DestroyImmediate(go);
        }

        static void Test2_ChildYChangeIndependent(StreamWriter writer)
        {
            writer.WriteLine("\n[Test 2] Child Local Y Change (Jump) -> SortingOrder remains stable");
            GameObject root = new GameObject("Test2_Root");
            root.transform.position = new Vector3(0, -2.0f, 0); 

            GameObject child = new GameObject("Visual");
            child.transform.SetParent(root.transform);
            child.transform.localPosition = Vector3.zero;

            SpriteRenderer sr = child.AddComponent<SpriteRenderer>();
            ZOrderSorter sorter = root.AddComponent<ZOrderSorter>(); 
            
            sorter.UpdateSortingOrder();
            int initialOrder = sr.sortingOrder;
            
            // Simulating jump (Child Y changes, Root Y stays same)
            child.transform.localPosition = new Vector3(0, 5.0f, 0);
            sorter.UpdateSortingOrder();
            
            bool pass = sr.sortingOrder == initialOrder;
            writer.WriteLine($"Initial Order: {initialOrder}, After Jump Order: {sr.sortingOrder}");
            writer.WriteLine($"Root Y: {root.transform.position.y}, Child World Y: {child.transform.position.y}");
            writer.WriteLine($"Is Stable? => {(pass ? "PASS" : "FAIL")}");
            
            GameObject.DestroyImmediate(root);
        }

        static void Test3_IsStaticOptimization(StreamWriter writer)
        {
            writer.WriteLine("\n[Test 3] IsStatic = true -> LateUpdate doesn't change order");
            GameObject go = new GameObject("Test3_Root");
            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            ZOrderSorter sorter = go.AddComponent<ZOrderSorter>();
            
            // 1. Initial calculate
            go.transform.position = new Vector3(0, -1.0f, 0);
            sorter.UpdateSortingOrder();
            int initialOrder = sr.sortingOrder;

            // 2. Set IsStatic = true and change position
            sorter.IsStatic = true;
            go.transform.position = new Vector3(0, -5.0f, 0); 

            // 3. Call LateUpdate via reflection
            MethodInfo lateUpdateMethod = typeof(ZOrderSorter).GetMethod("LateUpdate", BindingFlags.NonPublic | BindingFlags.Instance);
            lateUpdateMethod.Invoke(sorter, null);

            int afterStaticUpdateOrder = sr.sortingOrder;
            
            // 4. Set IsStatic = false and call LateUpdate
            sorter.IsStatic = false;
            lateUpdateMethod.Invoke(sorter, null);
            int afterDynamicUpdateOrder = sr.sortingOrder;

            bool pass1 = (initialOrder == 100 && afterStaticUpdateOrder == 100);
            bool pass2 = (afterDynamicUpdateOrder == 500);

            writer.WriteLine($"Static Order (should remain 100): {afterStaticUpdateOrder} => {(pass1 ? "PASS" : "FAIL")}");
            writer.WriteLine($"Dynamic Order (should update to 500): {afterDynamicUpdateOrder} => {(pass2 ? "PASS" : "FAIL")}");
            
            GameObject.DestroyImmediate(go);
        }
    }
}
