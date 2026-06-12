using UnityEngine;
using Unity.Cinemachine;
using UnityEngine.Assertions;

namespace ProjectAI.Players
{
    /// <summary>
    /// 플레이어의 카메라 연출(조준 오프셋 등)을 시네머신과 연동하여 중앙 통제하는 클래스입니다.
    /// </summary>
    public class PlayerCamera : MonoBehaviour
    {
        [SerializeField]
        private Transform targetTransform;

        private CinemachineCamera cinemachineCamera;

        #region Unity Lifecycle
        private void Update()
        {
            if (targetTransform == null)
            {
                return;
            }

            // 마우스 기반 오프셋 로직은 제거되었습니다.
            // 필요 시 향후 캐릭터가 바라보는 방향 기반 오프셋으로 교체할 수 있습니다.
            targetTransform.localPosition = Vector2.zero;
        }
        #endregion

        #region Public Methods
        public void InitCamera()
        {
            cinemachineCamera = FindAnyObjectByType<CinemachineCamera>();
            
            if (cinemachineCamera == null)
            {
                return;
            }

            Assert.IsNotNull(targetTransform, "[PlayerCamera] InitCamera: targetTransform이 할당되지 않았습니다.");

            cinemachineCamera.Follow = targetTransform;
        }
        #endregion
    }
}
