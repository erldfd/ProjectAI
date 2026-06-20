using UnityEngine.Assertions;
using UnityEngine;
using ProjectAI.Network;

namespace ProjectAI.Core
{
    /// <summary>
    /// 게임 전체의 생명주기를 관리하고 씬 전환 간에 파괴되지 않는 전역 매니저입니다.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        [Header("Global Settings")]
        [Tooltip("벨트스크롤 Y축 원근 왜곡 수치 (높을수록 Y축 거리가 멀게 계산됨)")]
        [SerializeField]
        private float beltScrollDepthScale = 2.5f;

        public float BeltScrollDepthScale => beltScrollDepthScale;

        public MultiplayerServiceManager MultiplayerService { get; private set; }

        private void Awake()
        {
            // 이미 씬에 활성화된 GameManager가 있다면 중복 생성을 막고 스스로 파괴합니다.
            if (GameStatics.GameManager != null && GameStatics.GameManager != this)
            {
                Debug.LogWarning("[GameManager] 중복된 GameManager 발견. 이전 객체를 유지하고 새로 로드된 객체를 파괴합니다.");
                // Destroy는 프레임 끝에 실행되므로, 그 찰나의 순간에도 로직이 돌지 않도록 즉시 꺼버립니다.
                gameObject.SetActive(false);
                UnityEngine.Object.Destroy(gameObject);
                return;
            }

            // 최초 생성 시 GameStatics Gateway의 안전한 메서드를 통해 자신을 등록합니다.
            GameStatics.RegisterManager(this);
            
            MultiplayerService = GetComponent<MultiplayerServiceManager>();
            Assert.IsNotNull(MultiplayerService, "[GameManager] MultiplayerServiceManager 컴포넌트가 부착되어 있지 않습니다. 필수 컴포넌트입니다.");
            
            // 씬을 전환해도 이 객체(및 부착된 네트워크 매니저)가 삭제되지 않도록 보호합니다.
            DontDestroyOnLoad(gameObject);
            
            Debug.Log("[GameManager] 전역 매니저가 성공적으로 초기화되었습니다.");
        }
    }
}
