using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

namespace ProjectAI.Core
{
    /// <summary>
    /// 시스템 간의 의존성을 분리하기 위한 전역 이벤트 매니저입니다.
    /// 제네릭(struct) 기반 및 List 풀링 설계로 GC(Garbage Collector) 할당이 발생하지 않습니다.
    /// 파괴된 유니티 오브젝트(좀비 리스너)에 대한 자동 청소 기능이 포함되어 있습니다.
    /// </summary>
    public static class EventManager
    {
        private static readonly Dictionary<Type, List<Delegate>> delegates = new Dictionary<Type, List<Delegate>>();

        /// <summary>
        /// 특정 타입의 이벤트를 구독합니다.
        /// </summary>
        public static void AddListener<T>(Action<T> listener) where T : struct
        {
            if (!delegates.TryGetValue(typeof(T), out List<Delegate> list))
            {
                list = new List<Delegate>(4);
                delegates[typeof(T)] = list;
            }

            if (!list.Contains(listener))
            {
                list.Add(listener);
            }
        }

        /// <summary>
        /// 특정 타입의 이벤트 구독을 해제합니다.
        /// </summary>
        public static void RemoveListener<T>(Action<T> listener) where T : struct
        {
            if (delegates.TryGetValue(typeof(T), out List<Delegate> list))
            {
                list.Remove(listener);
            }
        }

        /// <summary>
        /// 이벤트를 발생시킵니다.
        /// 내부적으로 역순 순회를 통해 파괴된 유니티 오브젝트(좀비 리스너)를 GC 할당 없이 안전하게 제거합니다.
        /// </summary>
        public static void TriggerEvent<T>(T e) where T : struct
        {
            if (delegates.TryGetValue(typeof(T), out List<Delegate> list))
            {
                for (int i = list.Count - 1; i >= 0; i--)
                {
                    Delegate handler = list[i];
                    if (handler.Target is UnityEngine.Object obj && obj == null)
                    {
#if UNITY_EDITOR
                        Debug.LogWarning("[EventManager] 파괴된 리스너 자동 제거됨.");
#endif
                        list.RemoveAt(i);
                    }
                    else
                    {
                        Action<T> action = handler as Action<T>;
                        Assert.IsNotNull(action, "[EventManager] 등록된 델리게이트 타입 불일치!");
                        action.Invoke(e);
                    }
                }
            }
        }
    }
}
