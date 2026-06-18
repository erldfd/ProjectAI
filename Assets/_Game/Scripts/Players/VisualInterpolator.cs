using UnityEngine;

namespace ProjectAI.Players
{
    /// <summary>
    /// 시각적 객체(스프라이트 등)를 부모(물리 객체)와 분리하여, 
    /// 부모가 멀티플레이 통신으로 인해 강제로 순간이동(Snap)하더라도 시각적으로는 부드럽게 따라가도록(Lerp) 보간해주는 클래스입니다.
    /// 
    /// <2.5D 축분리(점프) 사용 시 주의사항>
    /// 이 스크립트는 매 프레임 `localPosition`을 (0,0,0)으로 강제하므로, 점프(로컬 Y값 변경)와 충돌합니다.
    /// 따라서 캐릭터가 점프를 사용한다면 반드시 다음과 같은 3단 계층 구조를 사용해야 합니다.
    /// 1. Root (물리 본체) : 이동 로직 및 NetworkTransform
    /// 2. LerpNode (보간 전용 자식) : VisualInterpolator 부착 (이 객체가 부드럽게 따라감)
    /// 3. Visuals (점프/렌더링 자식) : LerpNode의 자식. 점프 시 이 객체의 로컬 Y값만 위아래로 움직임.
    /// 
    /// *참고: 몬스터나 투사체처럼 "점프(시각적 Y축 상승)" 기믹이 아예 없는 객체라면 
    /// LerpNode 없이 2단 구조(Root -> Visuals)로 Visuals 객체에 직접 부착해도 무방합니다.
    /// 
    /// <NGO 멀티플레이 연동 시 주의사항 (리뷰어 피드백)>
    /// 1. 이중 보간 방지: Root의 `NetworkTransform` 보간 기능이 켜져 있으면, 이 스크립트와 겹쳐 시각적 지연이 두 배로 발생합니다. `NetworkTransform`의 보간을 끄고(Snap) 사용해야 합니다.
    /// 2. 텔레포트 슬라이딩 방지: 리스폰이나 강제 텔레포트 시, 화면을 가로질러 슬라이딩하는 현상을 막으려면 명시적으로 `TeleportToParent()`를 호출해야 합니다.
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
            UnityEngine.Assertions.Assert.IsNotNull(transform.parent, "VisualInterpolator는 부모 객체가 있어야 정상 동작합니다.");
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
