using UnityEngine.Assertions;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using ProjectAI.Core.Skills;
using ProjectAI.Core;

namespace ProjectAI.Core.Pooling
{
    /// <summary>
    /// NGO(Netcode for GameObjects)와 연동하여 오브젝트 풀링을 수행하는 클래스입니다.
    /// </summary>
    public class NetworkObjectPool : MonoBehaviour
    {
        /// <summary>
        /// 프리팹별 풀 데이터를 관리하는 클래스입니다.
        /// </summary>
        private class PoolData
        {
            public Queue<NetworkObject> Queue = new Queue<NetworkObject>();
            public NetworkObject Prefab;
            public int InitialSize;
            public bool ShouldExpandWhenEmpty;
        }

        /// <summary>
        /// NGO의 프리팹 인스턴스 핸들러를 래핑하여 특정 프리팹의 풀링을 처리합니다.
        /// </summary>
        private class PrefabPoolHandler : INetworkPrefabInstanceHandler
        {
            private readonly NetworkObjectPool pool;
            private readonly NetworkObject prefab;

            public PrefabPoolHandler(NetworkObjectPool pool, NetworkObject prefab)
            {
                this.pool = pool;
                this.prefab = prefab;
            }

            public NetworkObject Instantiate(ulong ownerClientId, Vector3 position, Quaternion rotation)
            {
                return pool.GetNetworkObjectInternal(prefab, position, rotation);
            }

            public void Destroy(NetworkObject networkObject)
            {
                pool.ReturnNetworkObjectInternal(networkObject);
            }
        }

        private readonly Dictionary<NetworkObject, PoolData> pools = new Dictionary<NetworkObject, PoolData>();
        private readonly Dictionary<NetworkObject, NetworkObject> instanceToPrefabMap = new Dictionary<NetworkObject, NetworkObject>();
        private readonly Dictionary<NetworkObject, IPoolable> instancePoolableMap = new Dictionary<NetworkObject, IPoolable>();
        private readonly List<NetworkObject> registeredPrefabs = new List<NetworkObject>();

        [Header("Debug")]
        [Tooltip("체크 시 하이어라키에서 숨겨진 대기 중인 풀 객체들이 다시 나타납니다.")]
        public bool ShouldShowHiddenPoolObjects;

        private bool isInitialized = false;

#if UNITY_EDITOR
        private void OnValidate()
        {
            HideFlags flag = ShouldShowHiddenPoolObjects ? HideFlags.None : HideFlags.HideInHierarchy;
            foreach (PoolData pool in pools.Values)
            {
                foreach (NetworkObject obj in pool.Queue)
                {
                    if (obj != null && obj.gameObject != null)
                    {
                        obj.gameObject.hideFlags = flag;
                    }
                }
            }
        }
#endif

        private void OnEnable()
        {
            GameStatics.RegisterObjectPool(this);
            if (GameStatics.NetworkManager != null)
            {
                GameStatics.NetworkManager.OnServerStarted += InitializePools;
                GameStatics.NetworkManager.OnClientStarted += InitializePools;

                // 이미 서버나 클라이언트가 가동된 이후에 OnEnable이 불린 경우 즉시 초기화
                if (GameStatics.NetworkManager.IsServer || GameStatics.NetworkManager.IsClient)
                {
                    InitializePools();
                }
            }
        }

        private void InitializePools()
        {
            if (isInitialized)
            {
                return;
            }

            isInitialized = true;
            SetupSkillPrefabs();
            RegisterPrefabHandlers();
        }

        private void SetupSkillPrefabs()
        {
            Assert.IsNotNull(GameStatics.SkillManager, "[NetworkObjectPool] SetupSkillPrefabs: SkillManager가 GameStatics에 등록되어 있지 않습니다!");

            for (int i = 0; i < GameStatics.SkillManager.SkillConfigs.Count; i++)
            {
                NetworkObject prefab = GameStatics.SkillManager.SkillConfigs[i].Prefab;
                if (prefab == null)
                {
                    continue;
                }

                SetupPool(prefab, 1, true);
            }
        }

        private void OnDisable()
        {
            if (GameStatics.NetworkManager != null)
            {
                GameStatics.NetworkManager.OnServerStarted -= InitializePools;
                GameStatics.NetworkManager.OnClientStarted -= InitializePools;

                if (GameStatics.NetworkManager.PrefabHandler != null)
                {
                    for (int i = 0; i < registeredPrefabs.Count; i++)
                    {
                        GameStatics.NetworkManager.PrefabHandler.RemoveHandler(registeredPrefabs[i]);
                    }
                }
            }

            registeredPrefabs.Clear();
            
            foreach (PoolData pool in pools.Values)
            {
                foreach (NetworkObject obj in pool.Queue)
                {
                    if (obj != null && obj.gameObject != null)
                    {
                        Destroy(obj.gameObject);
                    }
                }
            }

            pools.Clear();
            instanceToPrefabMap.Clear();
            instancePoolableMap.Clear();
            isInitialized = false;
            GameStatics.UnregisterObjectPool(this);
        }

        /// <summary>
        /// 특정 프리팹의 풀을 설정하고 초기 개수만큼 생성합니다.
        /// </summary>
        public void SetupPool(NetworkObject prefab, int initialSize, bool shouldExpandWhenEmpty = true)
        {
            Assert.IsNotNull(prefab, "[NetworkObjectPool] SetupPool: prefab이 null입니다!");

            if (pools.ContainsKey(prefab))
            {
                return;
            }

            PoolData poolData = new PoolData
            {
                Prefab = prefab,
                InitialSize = initialSize,
                ShouldExpandWhenEmpty = shouldExpandWhenEmpty
            };

            pools.Add(prefab, poolData);

            RegisterPrefabHandlers();

            for (int i = 0; i < initialSize; i++)
            {
                NetworkObject instance = CreateInstance(prefab);
                instance.gameObject.hideFlags = ShouldShowHiddenPoolObjects ? HideFlags.None : HideFlags.HideInHierarchy;
                instance.gameObject.SetActive(false);
                poolData.Queue.Enqueue(instance);
                instanceToPrefabMap.Add(instance, prefab);
            }
        }

        private void RegisterPrefabHandlers()
        {
            if (GameStatics.NetworkManager == null || GameStatics.NetworkManager.PrefabHandler == null)
            {
                return;
            }

            foreach (KeyValuePair<NetworkObject, PoolData> pair in pools)
            {
                NetworkObject prefab = pair.Value.Prefab;
                if (!registeredPrefabs.Contains(prefab))
                {
                    GameStatics.NetworkManager.PrefabHandler.AddHandler(prefab, new PrefabPoolHandler(this, prefab));
                    registeredPrefabs.Add(prefab);
                }
            }
        }

        private NetworkObject CreateInstance(NetworkObject prefab)
        {
            // 루트에 생성하되 HideFlags를 통해 에디터 하이어라키에서 숨깁니다.
            NetworkObject instance = UnityEngine.Object.Instantiate(prefab);
            IPoolable poolable = instance.GetComponent<IPoolable>();

            if (poolable != null)
            {
                instancePoolableMap.Add(instance, poolable);
            }

            return instance;
        }

        /// <summary>
        /// 풀에서 준비된 오브젝트 인스턴스를 가져옵니다. (주로 서버 스폰 시 호출)
        /// </summary>
        public NetworkObject GetNetworkObject(NetworkObject prefab, Vector3 position, Quaternion rotation)
        {
            return GetNetworkObjectInternal(prefab, position, rotation);
        }

        private NetworkObject GetNetworkObjectInternal(NetworkObject prefab, Vector3 position, Quaternion rotation)
        {
            Assert.IsNotNull(prefab, "[NetworkObjectPool] GetNetworkObjectInternal: prefab이 null입니다!");

            if (!pools.TryGetValue(prefab, out PoolData poolData))
            {
                Debug.Log($"[NetworkObjectPool] Setup된 풀이 존재하지 않아 자동으로 초기화합니다 (ExpandWhenEmpty=true, InitialSize=1): {prefab.name}");
                SetupPool(prefab, 1, true);
                pools.TryGetValue(prefab, out poolData);
            }

            NetworkObject instance = null;

            if (poolData.Queue.Count > 0)
            {
                instance = poolData.Queue.Dequeue();
            }
            else
            {
                if (!poolData.ShouldExpandWhenEmpty)
                {
                    Debug.LogWarning($"[NetworkObjectPool] 풀이 고갈되었습니다: {prefab.name}. 강제로 임시 인스턴스를 확장합니다.");
                }

                instance = CreateInstance(prefab);
                instanceToPrefabMap.Add(instance, prefab);
            }

            if (instance != null)
            {
                instance.gameObject.hideFlags = HideFlags.None;

                Transform instTransform = instance.transform;
                instTransform.position = position;
                instTransform.rotation = rotation;
                instance.gameObject.SetActive(true);

                if (instancePoolableMap.TryGetValue(instance, out IPoolable poolable))
                {
                    poolable.OnSpawn();
                }
            }

            return instance;
        }

        /// <summary>
        /// 서버 또는 호스트 환경에서 명시적으로 오브젝트를 풀에 반환할 때 호출합니다.
        /// </summary>
        public void ReturnNetworkObject(NetworkObject instance)
        {
            Assert.IsNotNull(instance, "[NetworkObjectPool] ReturnNetworkObject: 반환하려는 instance가 null입니다!");

            if (instance.IsSpawned)
            {
                // 스폰된 상태라면 서버만 Despawn을 호출할 수 있습니다. 
                // 클라이언트는 서버의 Despawn 메시지를 받아 NGO 콜백으로 자동 처리됩니다.
                if (GameStatics.IsServerAuthorized)
                {
                    instance.Despawn(true);
                }
                else
                {
                    Debug.LogWarning($"[NetworkObjectPool] 클라이언트가 스폰된 객체의 반환을 시도했습니다: {instance.name}. 서버의 Despawn 메시지를 대기합니다.");
                }
            }
            else
            {
                ReturnNetworkObjectInternal(instance);
            }
        }

        private void ReturnNetworkObjectInternal(NetworkObject instance)
        {
            if (instance == null)
            {
                return;
            }

            if (!instanceToPrefabMap.TryGetValue(instance, out NetworkObject prefab))
            {
                Debug.LogWarning($"[NetworkObjectPool] 풀에 등록되지 않은 객체 반환 시도: {instance.name}. 강제 파괴합니다.");
                if (instance.gameObject != null)
                {
                    Destroy(instance.gameObject);
                }

                return;
            }

            if (!pools.TryGetValue(prefab, out PoolData poolData))
            {
                instanceToPrefabMap.Remove(instance);
                instancePoolableMap.Remove(instance);
                Debug.LogWarning($"[NetworkObjectPool] 삭제되었거나 유효하지 않은 풀로의 반환 시도: {prefab.name}. 강제 파괴합니다.");
                if (instance.gameObject != null)
                {
                    Destroy(instance.gameObject);
                }

                return;
            }

            if (instancePoolableMap.TryGetValue(instance, out IPoolable poolable))
            {
                poolable.OnDespawn();
            }

            // 폴더 대신 하이어라키 숨김 방식을 사용하여 NGO 제약을 회피합니다.
            instance.gameObject.hideFlags = ShouldShowHiddenPoolObjects ? HideFlags.None : HideFlags.HideInHierarchy;
            instance.gameObject.SetActive(false);
            poolData.Queue.Enqueue(instance);
        }
    }
}
