using UnityEngine;

namespace ProjectAI.Core
{
    /// <summary>
    /// 게임 시작 시 또는 에디터에서 씬을 재생할 때 전역 매니저 프리팹을 자동으로 주입하는 클래스입니다.
    /// 어느 씬에서든 플레이 버튼을 눌러도 테스트가 가능하게 해주는 1등 공신입니다.
    /// </summary>
    public static class AppBootstrapper
    {
        private const string GAME_MANAGER_PATH = "Prefabs/GameManager";

        // 씬이 로드되기 직전(BeforeSceneLoad)에 무조건 1회 실행됩니다.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        public static void Bootstrap()
        {
            Debug.Log("[AppBootstrapper] 핵심 시스템 자동 주입 확인 중...");

            // 이미 GameManager가 메모리에 존재한다면(가령 첫 씬부터 시작했거나, 씬에 수동 배치된 경우) 무시합니다.
            if (Object.FindAnyObjectByType<GameManager>() != null)
            {
                Debug.Log("[AppBootstrapper] GameManager가 이미 존재합니다. 주입을 건너뜁니다.");
                return;
            }

            // Resources 폴더에서 GameManager 프리팹을 불러옵니다.
            GameObject prefab = Resources.Load<GameObject>(GAME_MANAGER_PATH);
            if (prefab == null)
            {
                Debug.LogError("[AppBootstrapper] Resources/GameManager.prefab을 찾을 수 없습니다! 프리팹을 확인해 주세요.");
                return;
            }

            // 프리팹을 동적으로 씬에 생성합니다. (생성되면서 GameManager.Awake()가 호출되어 DontDestroyOnLoad 처리됨)
            GameObject instance = Object.Instantiate(prefab);
            instance.name = "[GameManager]";
            
            Debug.Log("[AppBootstrapper] GameManager 동적 주입 완료!");
        }
    }
}
