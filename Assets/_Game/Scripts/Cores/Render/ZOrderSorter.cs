using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Assertions;

namespace ProjectAI.Render
{
    /// <summary>
    /// [2.5D 벨트스크롤 동적 정렬 시스템]
    /// 시각적 점프(로컬 Y축 상승)로 인한 원근법 렌더링 파괴를 방지하기 위해, 
    /// 무조건 부모(루트 객체)의 실제 바닥 Y좌표를 기준으로 SortingOrder를 강제하는 코어 스크립트입니다.
    /// 
    /// <사용법 (프리팹 계층 분리)>
    /// 1. Root 객체 (Logic): 
    ///    - 이 객체는 무조건 바닥(X,Y 평면)으로만 이동해야 합니다. 
    ///    - ZOrderSorter 스크립트와 이동용(벽/지형 충돌용) BoxCollider2D를 부착합니다.
    ///    - (선택) 캐릭터가 무기, 그림자 등 여러 다중 파츠로 나뉘어 있을 경우에만 SortingGroup 컴포넌트를 부착하세요. 단일 스프라이트라면 필수가 아닙니다.
    /// 2. Visuals 자식 객체 (View): 
    ///    - Root의 하위에 빈 게임 오브젝트를 생성합니다.
    ///    - SpriteRenderer, Animator, 그리고 적에게 맞는 피격용(Hitbox) PolygonCollider2D를 이곳에 부착합니다.
    /// 3. 동작 원리: 
    ///    - 캐릭터가 점프할 때는 Root가 아닌 Visuals(자식)의 로컬 Y값만 위로 올라갑니다.
    ///    - 이렇게 하면 타격 판정 박스도 시각적으로 위로 이동하여 하단 공격을 회피할 수 있으며, 
    ///    - 이 스크립트는 Root의 바닥 Y값을 계속 추적하여 정렬 순서를 유지하므로 공중에 뜬 상태에서도 뒤에 있는 물체를 가리지 않습니다.
    /// </summary>
    public class ZOrderSorter : MonoBehaviour
    {
        [Tooltip("정밀 조정을 위한 정렬 오프셋 값입니다.")]
        public int SortingOffset = 0;

        [Tooltip("이동하지 않는 고정(Static) 오브젝트일 경우 체크하세요. 매 프레임 연산하지 않아 성능이 향상됩니다.")]
        public bool IsStatic = false;

        private SortingGroup sortingGroup;
        private SpriteRenderer singleRenderer;

        // [Review Fix] 성능 최적화: 이전 Y값 캐싱
        private float lastYPosition = float.MinValue;

        private void Awake()
        {
            // 루트 객체에 SortingGroup이 있으면 그룹 전체를 정렬
            sortingGroup = GetComponent<SortingGroup>();
            
            // 없으면 자식들 중 첫 번째 SpriteRenderer를 찾아 단일 렌더러만 정렬
            if (sortingGroup == null)
            {
                singleRenderer = GetComponentInChildren<SpriteRenderer>();
            }

            // [Review Fix] 필수 컴포넌트 Assert 추가
            Assert.IsTrue(sortingGroup != null || singleRenderer != null, "[ZOrderSorter] SortingGroup 또는 SpriteRenderer가 필수적으로 필요합니다.");
        }

        private void Start()
        {
            UpdateSortingOrder();
        }

        private void LateUpdate()
        {
            if (!IsStatic)
            {
                // [Review Fix] 성능 최적화: Y값이 변경되었을 때만 정렬 순서 업데이트
                if (Mathf.Abs(transform.position.y - lastYPosition) > 0.001f)
                {
                    UpdateSortingOrder();
                }
            }
        }

        public void UpdateSortingOrder()
        {
            lastYPosition = transform.position.y;

            // 화면 아래쪽(Y값이 작을수록)에 있을수록 플레이어 쪽(화면 앞)에 그려져야 하므로 -1을 곱함
            // 주의: Unity의 sortingOrder는 16-bit 정수(-32768 ~ 32767)이므로 Y 좌표가 대략 ±327을 초과하면 오버플로우가 발생합니다.
            int order = Mathf.RoundToInt(-lastYPosition * 100f) + SortingOffset;

            if (sortingGroup != null)
            {
                // [Review Fix] if문 중괄호 추가
                if (sortingGroup.sortingOrder != order)
                {
                    sortingGroup.sortingOrder = order;
                }
            }
            else if (singleRenderer != null)
            {
                // [Review Fix] if문 중괄호 추가
                if (singleRenderer.sortingOrder != order)
                {
                    singleRenderer.sortingOrder = order;
                }
            }
        }
    }
}
