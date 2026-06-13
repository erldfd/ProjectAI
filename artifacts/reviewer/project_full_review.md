# 프로젝트 전체 코드 리뷰 보고서

전체 49개 C# 스크립트 탐색 및 분석 완료.
NGO 기반 호스트-클라이언트(리슨 서버) 환경 및 코딩 컨벤션 기준 평가.

## * [Blocker] 반드시 수정
* 현재 시스템 상 치명적인 컴파일/런타임 에러나 구조적 결함은 발견되지 않음.

## * [Major] 수정 권장
* **비동기 함수 네이밍 컨벤션 위반**
  * `async` 키워드 사용 시 접미사 `Async`를 붙여야 하는 규칙 누락.
  * 대상 1: `_Demo/Scripts/MultiplayerDemoUI.cs` 내 `StartHostProcess`, `StartClientProcess`
  * 대상 2: `_Game/Scripts/Networks/MultiplayerServiceManager.cs` 내 `StartHost`, `StartClient`
  * 조치: 함수명에 `Async` 접미사 추가 요망.
* **Early Return 로그 누락**
  * 단발성 트리거 함수에서 Early Return 시 로그 작성 규칙 미준수.
  * 대상 1: `_Game/Scripts/Cores/NetInteractor.cs` - `TryInteract()` 진입 시 소유권(IsOwner) 없음 및 상호작용 대상 부재 시.
  * 대상 2: `_Game/Scripts/Cores/Skills/Abilities/BasicAttackLogic.cs` - `CanExecute()` 쿨타임 부족 시.
  * 대상 3: `_Game/Scripts/Cores/Skills/NetSkillComponent.cs` - `TryActivateSkill()` 내 미보유 스킬 시도 시.
  * 조치: 디버깅 용이성을 위해 각 Early Return 지점에 `Debug.Log` 추가 권장. (Update문 제외)

## * [Minor] 선택 사항
* **중괄호 이후 공백 규칙 점검**
  * 전반적으로 잘 지켜지고 있으나, 일부 함수에서 중괄호 닫힘 후 바로 다음 코드가 이어지는 구간 일괄 점검 요망.
* **성능 및 안전성 보완**
  * 대상 1: `_Game/Scripts/Movements/NetPlayerMovement.cs`
  * 내용: 클라이언트 예측의 `ReSimulate` 및 수동 속도 가산 시 벽 뚫림 방지를 위한 `Raycast/BoxCast` 물리 검사 로직(TODO) 빠른 적용 권장.
  * 대상 2: `_Game/Scripts/Cores/GameStatics.cs`
  * 내용: `ApplyDamage` 내 `GetComponentInChildren<IDamageable>()` 사용 시 오브젝트 하위 뎁스가 깊을 경우 오버헤드 우려. 부모 뎁스의 `TryGetComponent` 우선 검사 구조로 개선 권장.

## * [Question] 확인 필요
* **정적 변수를 활용한 씬 전환 데이터 관리**
  * 대상: `_Game/Scripts/Cores/SceneTransitionData.cs`
  * 내용: 씬 이동 데이터를 `static`으로 공유 중. 현재 단일 세션에서는 문제없으나, 향후 Additive 다중 씬이나 다중 방(Room) 구조 도입 시 값 덮어쓰기 문제(Race Condition)가 발생하지 않을지 기획/구조적 확장성 확인 요망.
* **호스트 전용 상호작용 판정 기준**
  * 대상: `_Game/Scripts/Gameplays/InteractionConditions/RequireHostCondition.cs`
  * 내용: `OwnerClientId == NetworkManager.ServerClientId` 로 판정. 리슨 서버 구조에서는 정확히 호스트를 필터링하지만, 향후 데디케이티드 서버(Dedicated Server) 도입 계획이 있다면 클라이언트 방장 권한 개념 분리 고려 필요.
