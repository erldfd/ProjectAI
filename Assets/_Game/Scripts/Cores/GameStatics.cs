using UnityEngine.Assertions;
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
        /// 전역 데미지 파이프라인입니다.
        /// 방어력 차감, 크리티컬 등 복잡한 데미지 계산 공식이 추가될 경우 여기서 중앙 통제합니다.
        /// </summary>
        /// <param name="target">피격을 받을 대상 오브젝트</param>
        /// <param name="baseDamage">기본 타격 데미지</param>
        public static void ApplyDamage(GameObject target, int baseDamage)
        {
            Assert.IsNotNull(target, "[GameStatics] ApplyDamage: target 오브젝트가 null입니다!");

            Assert.IsTrue(IsServerAuthorized, "[GameStatics] ApplyDamage는 서버(또는 오프라인)에서만 호출되어야 합니다.");
            
            if (!IsServerAuthorized)
            {
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

        #region Belt-Scroll Depth Distortion Helpers
        
        /// <summary>
        /// 현재 게임의 벨트스크롤 Y축 깊이 왜곡률을 가져옵니다.
        /// </summary>
        public static float DepthScale => GameManager != null ? GameManager.BeltScrollDepthScale : 2.5f;

        /// <summary>
        /// 벨트스크롤 시각 왜곡에 맞추어 실제 상하 이동 물리 속도를 느리게 보정하는 비율입니다.
        /// </summary>
        public static float MovementDepthRatio => 1.0f / DepthScale;

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
