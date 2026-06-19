# ZOrderSorter.cs 테스트 보고서

## 테스트 결과 요약
* **대상 파일**: `Assets/_Game/Scripts/Cores/Render/ZOrderSorter.cs`
* **테스트 방식**: 유니티 에디터 정적 검증 및 모의 테스트 코드 (`Assets/_Game/Scripts/_Demo/Editor/ZOrderSorterAutoTest.cs`)
* **결과**: **[PASS] 통과**

## 검증 내역
1. **정렬 순서 계산 로직 [PASS]**
   - 루트 객체의 Y값 기반으로 `Mathf.RoundToInt(-Y * 100) + Offset` 계산이 오차 없이 갱신됨을 확인.
   - `SortingGroup` 존재 시 그룹 우선 주입, 단일 `SpriteRenderer` 존재 시 렌더러 주입 로직 정상.
2. **축분리 기믹 (시각적 점프 방어) [PASS]**
   - 루트 객체가 바닥에 고정되어 있고 자식 객체(시각적 렌더러)의 로컬 Y값이 증가할 때, 렌더링 오더가 흔들리지 않고 유지됨을 확인. (점프 중 뒤에 있는 물체를 가리지 않는 현상 완벽 방어)
3. **IsStatic 최적화 [PASS]**
   - `IsStatic` 플래그 활성화 시 `LateUpdate` 연산이 완벽히 차단되며, `Start` 시점의 1회 계산값만 유지됨을 확인.

## 특이사항 및 권장 사항
* **SortingOrder 범위 제한 이슈**
  - Unity의 `sortingOrder` 내부 변수는 16비트 정수(-32768 ~ 32767)입니다.
  - 현재 100배수 연산을 사용하므로, 맵의 절대 좌표 Y값이 **+327 또는 -327**을 초과하면 오버플로우가 발생해 렌더링 계층이 완전히 박살날 수 있습니다.
  - 일반적인 벨트스크롤 룸 베이스 맵 크기에서는 문제가 없으나, 세로로 극단적으로 긴 맵을 설계할 경우 Y축 절대 좌표 관리에 유의가 필요합니다.
