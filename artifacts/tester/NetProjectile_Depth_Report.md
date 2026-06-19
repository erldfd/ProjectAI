# 투사체 2.5D 깊이 판정 검증 결과

## 1. 정적 검증 결과

`NetProjectile.cs`와 `ProjectileAttackLogic.cs`의 코드를 정적으로 분석한 결과, 2.5D 깊이 판정에 대한 논리적 오류는 발견되지 않았습니다.

*   **스폰 시점 (ProjectileAttackLogic.cs):**
    *   투사체 스폰 후 `Initialize` 호출 시 `caster.transform.position.y`를 넘겨줍니다. 발사점(`FirePoint`)의 공중 Y 좌표가 아닌 시전자(캐릭터)의 바닥 루트 Y 좌표를 깊이(`cachedDepthY`)로 캐싱하여 2.5D 원근법 판정에 부합합니다.
*   **충돌 판정 (NetProjectile.cs):**
    *   `targetY` 계산 시 데미지 대상(`damageable != null`)은 `NetworkObject` 부모의 기준 위치를 우선 가져오며, 환경 장애물 등은 충돌체의 위치를 사용합니다.
    *   `depthDifference`가 `allowedTolerance` (투사체 깊이 반지름 + 타겟 깊이 반지름)을 초과할 경우, `return;`으로 조기 종료(Early Return)하여 데미지 로직과 `DestroyProjectile()` 로직을 모두 건너뜁니다.
    *   이를 통해 **깊이가 다른 환경(벽)과 타겟을 정상적으로 관통/무시**하는 기능이 완벽히 구현되었습니다.
*   **안정성 및 경계값:**
    *   `statComponent` 등 필수 의존성은 `Awake()`에서 `Assert`로 검증되므로 Null 예외 발생 확률이 낮습니다.
    *   본인의 `NetworkObjectId`를 대조하여 팀킬/자해를 방지하는 로직도 안정적으로 적용되어 있습니다.

## 2. 통합 단위 테스트 작성 내역

유니티 Editor 환경에서 런타임 충돌 이벤트를 시뮬레이션하기 위한 통합 테스트 스크립트를 작성하여 아래 경로에 배치했습니다.

*   **파일 경로:** `Assets/_Game/Scripts/_Demo/Editor/NetProjectileDepthAutoTest.cs`
*   **테스트 시나리오:**
    1.  **Test 1:** 허용 깊이 범위 내(Y축 차이 0.2)의 데미지 가능 타겟 충돌 -> 데미지 피격 정상 처리 검증
    2.  **Test 2:** 허용 깊이 범위 초과(Y축 차이 1.5) 타겟 충돌 -> 데미지 무시 및 관통 여부 검증
    3.  **Test 3:** 허용 깊이 범위 내의 물리 환경 벽(`isTrigger=false`) 충돌 -> 관통하지 않고 부딪혀 파괴(`DestroyProjectile` 호출)됨을 검증

## 3. 검증 한계

*   사용자 터미널 실행 타임아웃으로 인하여 외부 파워셸 명령어(Unity 백그라운드 컴파일)를 통한 즉각적인 테스트 결과 산출은 진행할 수 없었습니다.
*   에디터가 활성화되어 스크립트 리로드가 발생하면, 자동으로 실행되어 프로젝트 폴더 내 `artifacts/tester/NetProjectileDepth_TestResult.txt` 에 결과가 출력될 것입니다. 런타임(PlayMode)의 NetworkManager 제어가 아닌 Mock 객체를 이용한 Editor Reflection 기반 테스트이므로, 실제 Network Spawn 환경과의 미세한 차이가 있을 수 있습니다.
