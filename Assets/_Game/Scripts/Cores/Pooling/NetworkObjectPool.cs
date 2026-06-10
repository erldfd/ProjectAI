using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using ProjectAI.Core.Skills;

namespace ProjectAI.Core.Pooling
{
    /// <summary>
    /// NGO(Netcode for GameObjects)와 연동하여 오브젝트 풀링을 수행하는 클래스입니다.
    /// </summary>
    public class NetworkObjectPool : MonoBehaviour
    {
        /// <summary>
        /// 프리팹별 풀 데이터를 관리하는 구조체입니다.
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

        private void OnEnable()
        {
            GameStatics.RegisterObjectPool(this);
            if (NetworkManager.Singleton != null)
            {
                NetworkManager.Singleton.OnServerStarted += RegisterPrefabHandlers;
                NetworkManager.Singleton.OnClientStarted += RegisterPrefabHandlers;
            }
        }

        private void Start()
        {
            RegisterPrefabHandlers();
            SetupSkillProjectiles();
        }

        private void SetupSkillProjectiles()
        {
            UnityEngine.Assertions.Assert.IsNotNull(GameStatics.SkillManager, "[NetworkObjectPool] SetupSkillProjectiles: SkillManager가 GameStatics에 등록되어 있지 않습니다!");

            for (int i = 0; i < GameStatics.SkillManager.SkillConfigs.Count; i++)
            {
                NetworkObject prefab = GameStatics.SkillManager.SkillConfigs[i].Prefab;
                if (prefab == null)
                {
                    continue;
                }

                SetupPool(prefab, 10, true);
            }
        }

        private void OnDisable()
        {
            if (NetworkManager.Singleton != null)
            {
                NetworkManager.Singleton.OnServerStarted -= RegisterPrefabHandlers;
                NetworkManager.Singleton.OnClientStarted -= RegisterPrefabHandlers;

                if (NetworkManager.Singleton.PrefabHandler != null)
                {
                    for (int i = 0; i < registeredPrefabs.Count; i++)
                    {
                        NetworkManager.Singleton.PrefabHandler.RemoveHandler(registeredPrefabs[i]);
                    }
                }
            }

            registeredPrefabs.Clear();
            pools.Clear();
            instanceToPrefabMap.Clear();
            instancePoolableMap.Clear();
            GameStatics.UnregisterObjectPool(this);
        }

        /// <summary>
        /// 특정 프리팹의 풀을 설정하고 초기 개수만큼 생성합니다.
        /// </summary>
        public void SetupPool(NetworkObject prefab, int initialSize, bool shouldExpandWhenEmpty = true)
        {
            UnityEngine.Assertions.Assert.IsNotNull(prefab, "[NetworkObjectPool] SetupPool: prefab이 null입니다!");

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
                instance.gameObject.SetActive(false);
                poolData.Queue.Enqueue(instance);
                instanceToPrefabMap.Add(instance, prefab);
            }
        }

        private void RegisterPrefabHandlers()
        {
            if (NetworkManager.Singleton == null || NetworkManager.Singleton.PrefabHandler == null)
            {
                return;
            }

            foreach (KeyValuePair<NetworkObject, PoolData> pair in pools)
            {
                NetworkObject prefab = pair.Value.Prefab;
                if (!registeredPrefabs.Contains(prefab))
                {
                    NetworkManager.Singleton.PrefabHandler.AddHandler(prefab, new PrefabPoolHandler(this, prefab));
                    registeredPrefabs.Add(prefab);
                }
            }
        }

        private NetworkObject CreateInstance(NetworkObject prefab)
        {
            // NGO 씬 스윕 시 경고(Disabled NetworkBehaviours...) 방지를 위해
            // 부모(base.transform)를 지정하지 않고 최상위 루트에 생성합니다.
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
            if (!pools.TryGetValue(prefab, out PoolData poolData))
            {
                Debug.LogError($"[NetworkObjectPool] Setup된 풀이 존재하지 않습니다: {prefab.name}");
                return null;
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
            ReturnNetworkObjectInternal(instance);
        }

        private void ReturnNetworkObjectInternal(NetworkObject instance)
        {
            if (instance == null)
            {
                return;
            }

            if (!instanceToPrefabMap.TryGetValue(instance, out NetworkObject prefab))
            {
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

            instance.transform.SetParent(base.transform);

            instance.gameObject.SetActive(false);
            poolData.Queue.Enqueue(instance);
        }
    }
}
