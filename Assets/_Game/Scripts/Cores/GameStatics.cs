using Unity.Netcode;
using UnityEngine;
using ProjectAI.GameModes;
using ProjectAI.Network;
using ProjectAI.Core.Stats;
using ProjectAI.Core.Skills;
using ProjectAI.Core.Pooling;


namespace ProjectAI.Core
{
    /// <summary>
    /// 게임 전반에서 사용하는 정적 매니저 및 헬퍼 기능을 제공하는 클래스입니다.
    /// </summary>
    public static class GameStatics
    {

        public static GameManager GameManager { get; private set; }
        public static ANetGameModeBase CurrentMode { get; private set; }
        public static SkillManager SkillManager { get; private set; }
        public static NetworkObjectPool ObjectPool { get; private set; }


        public static NetworkManager NetworkManager => NetworkManager.Singleton;

        public static MultiplayerServiceManager MultiplayerManager => GameManager != null ? GameManager.MultiplayerService : null;

        /// <summary>
        /// 전역 데미지 파이프라인입니다.
        /// 방어력 차감, 크리티컬 등 복잡한 데미지 계산 공식이 추가될 경우 여기서 중앙 통제합니다.
        /// </summary>
        /// <param name="target">피격을 받을 대상 오브젝트</param>
        /// <param name="baseDamage">기본 타격 데미지</param>
        public static void ApplyDamage(GameObject target, int baseDamage)
        {
            UnityEngine.Assertions.Assert.IsNotNull(target, "[GameStatics] ApplyDamage: target 오브젝트가 null입니다!");

            if (NetworkManager != null && !NetworkManager.IsServer)
            {
                Debug.LogWarning("[GameStatics] ApplyDamage는 서버에서만 호출되어야 합니다.");
                return;
            }

            IDamageable damageable = target.GetComponentInChildren<IDamageable>();

            if (damageable == null)
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
    }

}
