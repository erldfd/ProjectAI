using UnityEngine;
using Unity.Cinemachine;

namespace PortalBroke.Player
{
    /// <summary>
    /// 플레이어의 카메라 연출(조준 오프셋 등)을 시네머신과 연동하여 중앙 통제하는 클래스입니다.
    /// </summary>
    public class PlayerCamera : MonoBehaviour
    {
        [Header("References")]
        [SerializeField]
        private PlayerInputReader inputReader;

        [SerializeField]
        private Transform targetTransform;

        [Header("Aim Offset Settings")]
        [SerializeField]
        private float maxOffsetDistance = 3f;
        
        [SerializeField]
        private float offsetMultiplier = 0.5f;

        private CinemachineCamera cinemachineCamera;

        #region Unity Lifecycle
        private void Update()
        {
            if (inputReader == null || targetTransform == null)
            {
                Debug.Log($"input Reader : {inputReader == null}, targetTransfrom : {targetTransform == null}");
                return;
            }

            Vector2 mousePos = inputReader.MouseWorldPosition;
            Vector2 myPos = transform.position;
            Vector2 direction = mousePos - myPos;

            Vector2 offset = Vector2.ClampMagnitude(direction * offsetMultiplier, maxOffsetDistance);
            targetTransform.localPosition = offset;
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

            if (targetTransform == null)
            {
                return;
            }

            cinemachineCamera.Follow = targetTransform;
        }
        #endregion
    }
}
