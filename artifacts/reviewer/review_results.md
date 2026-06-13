# 사망 처리 로직 코드 리뷰 결과

## 1. `Assets\_Game\Scripts\Players\NetPlayerController.cs`
* **[Blocker]** `var` 키워드 사용 위반 (수정 완료)
  * `HandleDeath` 메서드 내 `colliders`, `col` 변수에 `var` 사용됨. 명시적 타입(`Collider2D[]`, `Collider2D`)으로 변경 요망.
* **[Major]** 비정상적인 사망 이벤트 동기화 (Owner 편향) (수정 완료)
  * `OnNetworkSpawn`에서 `IsOwner` 체크 **이후**에 `healthComponent.OnDeath += HandleDeath` 구독.
  * **수정안:** `OnDeath` 구독/해제 로직을 `IsOwner` 체크 이전으로 옮겨 모든 클라이언트가 구독하게 함. 단, `HandleDeath` 내부의 `inputReader.DisableInput()` 호출은 `if (IsOwner)`로 보호 요망.
* **[Major]** `OnNetworkDespawn`의 비대칭 해제 (수정 완료)
  * `OnNetworkSpawn`과 달리 `IsOwner` 체크 후 `return` 로직 누락됨. Non-Owner 클라이언트에서 불필요한/오작동 가능한 `DisableInput()` 발생 위험.

## 2. `Assets\_Game\Scripts\Characters\NetMonsterController.cs`
* **[Blocker]** `var` 키워드 사용 위반 (수정 완료)
  * `HandleDeath` 메서드 내 `colliders`, `col`, `rb` 변수에 `var` 사용됨.
* **[Minor]** Early Return 패턴 적용 (수정 완료)
  * `DespawnRoutine` 내 `if (NetworkObject != null && NetworkObject.IsSpawned)` 블록을 Early Return 형태로 리팩토링 권장.
* **[Question]** 클라이언트 측 물리 제어 (수용)
  * `HandleDeath`에서 모든 클라이언트가 `rb.simulated = false` 수행함. 향후 `NetworkRigidbody2D` 연동 시 서버/클라이언트 간 Authority 충돌 워닝 발생 여부 점검 요망. (현재 MVP에서는 유지)

## 3. `Assets\_Game\Scripts\Anims\EntityAnimator.cs`
* **[Minor]** Early Return 패턴 미적용 (수정 완료)
  * `HandleDeathTriggered`에서 `if (dieStateHash != 0)` 대신 `if (dieStateHash == 0) return;` 형태로 변경 권장.

## 4. `Assets\_Game\Scripts\Cores\Entities\EntityEvents.cs`
* 특이사항 없음. 컨벤션 준수 및 구조 양호.
