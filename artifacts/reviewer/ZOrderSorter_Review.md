# ZOrderSorter.cs 코드 리뷰 결과

## 리뷰 요약
* **대상 파일**: `Assets/_Game/Scripts/Cores/Render/ZOrderSorter.cs`
* **주요 평가**: 생명주기(`LateUpdate` 사용) 및 값이 다를 때만 렌더링 오더 갱신(`!= order`)하는 핵심 로직은 적절하게 구현됨.

## 발견된 이슈 및 해결 사항
1. **[Major] 코딩 컨벤션 위반 (if문 중괄호 누락)**
   - **문제**: 1줄짜리 if문에도 무조건 중괄호 `{}`를 사용해야 한다는 컨벤션 누락.
   - **해결**: 모든 if 블록에 중괄호 명시적 추가 완료.
2. **[Major] 코딩 컨벤션 위반 (Assert 누락)**
   - **문제**: 필수 컴포넌트 체크 시 null 대신 Assert를 활용해야 한다는 규칙 누락.
   - **해결**: `Awake()` 내에 `Assert.IsTrue`를 추가하여 `SortingGroup`과 `SpriteRenderer` 동시 누락을 엄격히 방지.
3. **[Minor] 성능 최적화 (이전 Y값 캐싱)**
   - **문제**: `IsStatic`이 false일 때 매 프레임 `Mathf.RoundToInt` 등 CPU 연산 낭비.
   - **해결**: `lastYPosition`을 캐싱하여, 이전 프레임과 Y 좌표가 `0.001f` 이상 차이 날 때만 연산을 수행하도록 최적화 적용 완료.
