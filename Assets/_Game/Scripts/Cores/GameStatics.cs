using UnityEngine.Assertions;
using Unity.Netcode;
using UnityEngine;
using ProjectAI.GameModes;
using ProjectAI.Network;
using ProjectAI.Core.Stats;
using ProjectAI.Core.Skills;
using ProjectAI.Core.Pooling;
using ProjectAI.Environments;
using ProjectAI.SOs;
using ProjectAI.UIs.Cores;
using ProjectAI.Core.Inputs;
using System.Collections.Generic;

namespace ProjectAI.Core
{
    /// <summary>
    /// 게임 전반에서 사용하는 정적 매니저 및 헬퍼 기능을 제공하는 클래스입니다.
    /// </summary>
    public static class GameStatics
    {
        // ----------------------------------------------------
        // 1. Static Variables & Consts
        // ----------------------------------------------------
        private static ChunkDatabaseSO _mapChunkDB;
        private static SpawnTableDatabaseSO _spawnTableDB;

        // O(1) 룩업을 위한 전역 데미지 인터페이스 레지스트리
        private static readonly Dictionary<int, IDamageable> damageableRegistry = new Dictionary<int, IDamageable>();


        // ----------------------------------------------------
        // 2. Properties
        // ----------------------------------------------------
        /// <summary>
        /// Resources/MapChunkDB.asset 을 지연 로딩하여 캐싱합니다.
        /// </summary>
        public static ChunkDatabaseSO MapChunkDB
        {
            get
            {
                if (_mapChunkDB == null)
                {
                    string resourceName = GameManager != null ? GameManager.MapChunkDatabaseResourceName : "MapChunkDB";
                    _mapChunkDB = Resources.Load<ChunkDatabaseSO>(resourceName);
                    Assert.IsNotNull(_mapChunkDB, $"[GameStatics] Resources 폴더 내에 '{resourceName}' SO 파일을 찾을 수 없습니다! 반드시 생성해 주세요.");
                }

                return _mapChunkDB;
            }
        }

        /// <summary>
        /// Resources/SOs/SpawnTableDatabase.asset 을 지연 로딩하여 캐싱합니다.
        /// </summary>
        public static SpawnTableDatabaseSO SpawnTableDB
        {
            get
            {
                if (_spawnTableDB == null)
                {
                    _spawnTableDB = Resources.Load<SpawnTableDatabaseSO>("SOs/SpawnTableDatabaseSO");
                    Assert.IsNotNull(_spawnTableDB, "[GameStatics] Resources/SOs 폴더 내에 'SpawnTableDatabaseSO' 파일을 찾을 수 없습니다!");
                }

                return _spawnTableDB;
            }
        }

        public static GameManager GameManager { get; private set; }
        
        /// <summary>
        /// 전역 UIManager에 접근합니다. UI 팝업(스택)을 띄우거나 닫을 때 사용합니다.
        /// </summary>
        public static UIManager UIManager
        {
            get
            {
                Assert.IsNotNull(GameManager, "[GameStatics] GameManager가 아직 생성되지 않아 UIManager에 접근할 수 없습니다.");
                return GameManager.UIManager;
            }
        }

        /// <summary>
        /// 전역 입력(ESC 팝업 닫기 등) 이벤트에 접근합니다.
        /// </summary>
        public static GlobalInputReader GlobalInput
        {
            get
            {
                Assert.IsNotNull(GameManager, "[GameStatics] GameManager가 아직 생성되지 않아 GlobalInput에 접근할 수 없습니다.");
                return GameManager.GlobalInputReader;
            }
        }

        public static ANetGameModeBase CurrentMode { get; private set; }
        public static SkillManager SkillManager { get; private set; }
        public static NetworkObjectPool ObjectPool { get; private set; }

        public static NetworkManager NetworkManager => NetworkManager.Singleton;

        public static MultiplayerServiceManager MultiplayerManager => GameManager != null ? GameManager.MultiplayerService : null;

        /// <summary>
        /// 현재 오프라인 상태이거나(로그인 전), 접속 중인 경우 서버 권한이 있는지 확인합니다.
        /// </summary>
        public static bool IsServerAuthorized
        {
            get
            {
                if (NetworkManager == null || !NetworkManager.IsListening)
                {
                    return true;
                }
                
                return NetworkManager.IsServer;
            }
        }

        /// <summary>
        /// 현재 게임의 벨트스크롤 Y축 깊이 왜곡률을 가져옵니다. 0 나누기 예외를 막기 위해 최소 0.001을 보장합니다.
        /// </summary>
        public static float DepthScale
        {
            get
            {
                float scale = GameManager != null ? GameManager.BeltScrollDepthScale : 2.5f;
                return Mathf.Max(0.001f, scale);
            }
        }

        /// <summary>
        /// 벨트스크롤 시각 왜곡에 맞추어 실제 상하 이동 물리 속도를 느리게 보정하는 비율입니다.
        /// </summary>
        public static float MovementDepthRatio => 1.0f / DepthScale;


        // ----------------------------------------------------
        // 3. Methods
        // ----------------------------------------------------

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            _mapChunkDB = null;
            _spawnTableDB = null;
            damageableRegistry.Clear();
            GameManager = null;
            CurrentMode = null;
            SkillManager = null;
            ObjectPool = null;
        }

        public static void RegisterDamageable(GameObject rootObj, IDamageable damageable)
        {
            if (rootObj != null && damageable != null)
            {
                damageableRegistry[rootObj.GetInstanceID()] = damageable;
            }
        }

        public static void UnregisterDamageable(GameObject rootObj)
        {
            if (rootObj != null)
            {
                damageableRegistry.Remove(rootObj.GetInstanceID());
            }
        }

        public static void UnregisterDamageable(int instanceId)
        {
            damageableRegistry.Remove(instanceId);
        }

        public static bool TryGetDamageable(GameObject rootObj, out IDamageable damageable)
        {
            if (rootObj == null)
            {
                damageable = null;
                return false;
            }

            return damageableRegistry.TryGetValue(rootObj.GetInstanceID(), out damageable);
        }

        /// <summary>
        /// 전역 데미지 파이프라인입니다.
        /// 방어력 차감, 크리티컬 등 복잡한 데미지 계산 공식이 추가될 경우 여기서 중앙 통제합니다.
        /// </summary>
        /// <param name="target">피격을 받을 대상 오브젝트 (Root GameObject 권장)</param>
        /// <param name="baseDamage">기본 타격 데미지</param>
        public static void ApplyDamage(GameObject target, int baseDamage)
        {
            Assert.IsNotNull(target, "[GameStatics] ApplyDamage: target 오브젝트가 null입니다!");

            // 기존 GetComponent를 O(1) 딕셔너리 룩업으로 최적화
            if (TryGetDamageable(target, out IDamageable damageable))
            {
                ApplyDamage(damageable, baseDamage);
            }
        }

        /// <summary>
        /// IDamageable을 직접 전달받는 최적화된 데미지 적용 오버로드입니다.
        /// </summary>
        public static void ApplyDamage(IDamageable damageable, int baseDamage)
        {
            Assert.IsTrue(IsServerAuthorized, "[GameStatics] ApplyDamage는 서버(또는 오프라인)에서만 호출되어야 합니다.");
            
            if (!IsServerAuthorized || damageable == null)
            {
                return;
            }

            int finalDamage = baseDamage;

            // TODO: 방어력, 상태이상 공식 등 추가 (예: target.GetComponent<NetStatComponent>())
            // int armor = ...
            // finalDamage = Mathf.Max(1, baseDamage - armor);

            damageable.TakeDamage(finalDamage);
        }

        public static void RegisterManager(GameManager manager)
        {
            if (GameManager != null)
            {
                Debug.LogError("[GameStatics] 누군가 이미 존재하는 GameManager를 덮어쓰려고 시도했습니다!");
                return;
            }

            GameManager = manager;
        }

        public static void UnregisterManager(GameManager manager)
        {
            if (GameManager == manager)
            {
                GameManager = null;
            }
        }

        public static void RegisterGameMode(ANetGameModeBase mode)
        {
            CurrentMode = mode;
        }

        public static void UnregisterGameMode(ANetGameModeBase mode)
        {
            if (CurrentMode == mode)
            {
                CurrentMode = null;
            }
        }

        public static void RegisterSkillManager(SkillManager manager)
        {
            SkillManager = manager;
        }

        public static void UnregisterSkillManager(SkillManager manager)
        {
            if (SkillManager == manager)
            {
                SkillManager = null;
            }
        }

        public static void RegisterObjectPool(NetworkObjectPool pool)
        {
            if (ObjectPool != null)
            {
                Debug.LogError("[GameStatics] 누군가 이미 존재하는 NetworkObjectPool을 덮어쓰려고 시도했습니다!");
                return;
            }

            ObjectPool = pool;
        }


        public static void UnregisterObjectPool(NetworkObjectPool pool)
        {
            if (ObjectPool != pool)
            {
                return;
            }

            ObjectPool = null;
        }

        #region Belt-Scroll Depth Distortion Helpers
        
        /// <summary>
        /// Y축 거리에 원근 왜곡률(DepthScale)을 곱한 논리적 벡터를 반환합니다.
        /// 벨트스크롤 환경에서 상하 거리를 시각적 느낌에 맞게 보정할 때 사용합니다.
        /// </summary>
        public static Vector2 GetPerspectiveVector(Vector2 diff)
        {
            return new Vector2(diff.x, diff.y * DepthScale);
        }

        /// <summary>
        /// 원근 왜곡이 적용된 거리의 제곱(sqrMagnitude)을 반환합니다. 
        /// 연산 속도가 빠르므로 거리 비교 시 주로 사용합니다.
        /// </summary>
        public static float GetPerspectiveSqrMagnitude(Vector2 diff)
        {
            return GetPerspectiveVector(diff).sqrMagnitude;
        }

        /// <summary>
        /// 원근 왜곡이 적용된 절대 거리(magnitude)를 반환합니다.
        /// 정밀한 거리 수치가 필요할 때 사용합니다.
        /// </summary>
        public static float GetPerspectiveMagnitude(Vector2 diff)
        {
            return GetPerspectiveVector(diff).magnitude;
        }

        #endregion
    }

}
