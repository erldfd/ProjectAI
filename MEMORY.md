# MEMORY.md

* **물리 탐색**: 레거시 `NonAlloc` 대신 최신 `Physics2D.OverlapCircle(pos, radius, filter, array)` 사용 (가비지 방지)
* **타겟팅**: 탐색 루프 시 자기 자신(`gameObject == gameObject`) 제외 처리 필수
* **제어문 규칙**: 1줄짜리 `if`문 절대 금지. 무조건 중괄호 `{}` 사용 및 줄바꿈 적용
* **포맷팅**: 로직 블록(for, if 등) 종료 후 새 변수 할당 전 반드시 빈 줄 추가
