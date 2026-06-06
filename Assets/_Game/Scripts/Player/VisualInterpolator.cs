using UnityEngine;

namespace ProjectAI.Player
{
    /// <summary>
    /// 시각적 객체(스프라이트 등)를 부모(물리 객체)와 분리하여, 
    /// 부모가 강제로 순간이동(Snap)하더라도 시각적으로는 부드럽게 따라가도록(Lerp) 보간해주는 클래스입니다.
    /// </summary>
    public class VisualInterpolator : MonoBehaviour
    {
        [Tooltip("보간 속도. 값이 클수록 부모의 위치로 빠르게 복귀합니다.")]
        [SerializeField]
        private float returnSpeed = 15f;

        private Vector3 previousParentPosition;
        private Transform cachedParent;

        private void Start()
        {
            if (transform.parent == null)
            {
                Debug.LogWarning("VisualInterpolator는 부모 객체가 있어야 정상 동작합니다.", this);
            }
        }

        private void OnEnable()
        {
            if (transform.parent != null)
            {
                // 객체가 비활성화되었다가 켜질 때 옛날 위치에서 날아오는 현상을 방지하기 위해 즉시 부모에게 붙임
                cachedParent = transform.parent;
                TeleportToParent();
            }
        }

        private void LateUpdate()
        {
            if (transform.parent == null)
            {
                cachedParent = null;
                return;
            }

            // 부모가 새로 지정되거나 변경되었을 때 (Instantiate 직후 SetParent 등)
            // 기존 위치와 새 부모 위치 간의 거대한 오차로 인해 우주로 날아가는 버그를 방지함
            // 단, 부드러운 보간은 유지하기 위해 TeleportToParent() 대신 이전 부모 위치(기준점)만 갱신함
            if (cachedParent != transform.parent)
            {
                cachedParent = transform.parent;
                previousParentPosition = transform.parent.position;
            }

            // 1. 부모의 이번 프레임 이동량 계산
            Vector3 currentParentPosition = transform.parent.position;
            Vector3 parentDelta = currentParentPosition - previousParentPosition;

            // 2. 부모가 이동한 만큼 자식의 월드 위치를 반대로 빼서(상쇄) 시각 객체가 제자리에 머물게 함
            // (주의: 부모가 회전할 경우 궤도가 틀어질 수 있으므로, 부모 PhysicsBody는 회전하지 않는다고 가정함)
            transform.position -= parentDelta;

            // 3. 하지만 최종 목표는 부모의 한가운데(localPosition = 0,0,0)이므로 서서히 Lerp 시켜서 따라붙음
            // 테스터 피드백 반영: 프레임 레이트 종속성 제거를 위해 Mathf.Exp 공식 사용
            transform.localPosition = Vector3.Lerp(transform.localPosition, Vector3.zero, 1f - Mathf.Exp(-returnSpeed * Time.deltaTime));

            // 4. 다음 프레임을 위해 부모 위치 갱신 (캐싱된 값 재사용으로 성능 최적화)
            previousParentPosition = currentParentPosition;
        }
        
        /// <summary>
        /// 텔레포트나 리스폰 등, 보간 없이 즉시 시각 객체도 순간이동해야 할 때 호출합니다.
        /// </summary>
        public void TeleportToParent()
        {
            transform.localPosition = Vector3.zero;
            if (transform.parent != null)
            {
                previousParentPosition = transform.parent.position;
            }
        }
    }
}
