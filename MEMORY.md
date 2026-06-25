# MEMORY.md

* **물리 탐색**: 레거시 `NonAlloc` 대신 최신 `Physics2D.OverlapCircle(pos, radius, filter, array)` 사용 (가비지 방지)
* **타겟팅**: 탐색 루프 시 자기 자신(`gameObject == gameObject`) 제외 처리 필수
* **제어문 규칙**: 1줄짜리 `if`문 절대 금지. 무조건 중괄호 `{}` 사용 및 줄바꿈 적용
* **포맷팅**: 로직 블록(for, if 등) 종료(중괄호 닫기 `}` 직후) 후 새 변수 할당 전 반드시 빈 줄 추가. 단, 중괄호 열기(`{`) 직후에는 빈 줄을 넣지 않는다.
* **서버 권한 체크**: 싱글플레이/오프라인 확장성을 고려하여 `NetworkBehaviour.IsServer` 사용을 지양하고, 반드시 `GameStatics.IsServerAuthorized`를 사용할 것
