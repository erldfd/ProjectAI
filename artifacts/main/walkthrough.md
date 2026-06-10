# 오브젝트 풀링 및 스포너 매니저 1, 2단계 완료

## 1. 작업 개요
* 4인 멀티플레이 환경에서 가비지 컬렉션(GC) 병목을 유발할 수 있는 오브젝트들을 최적화하기 위해 NGO(Netcode for GameObjects) 연동 오브젝트 풀링 시스템 구축 및 플레이어 기본 마법탄 연동 완료.

## 2. 주요 구현 사항

### 1단계: 코어 풀링 인프라 구축
* **[IPoolable.cs](file:///c:/UnityProjects/ProjectAI/Assets/_Game/Scripts/Cores/Pooling/IPoolable.cs) (신규)**:
  * 풀링 객체의 수명 주기(`OnSpawn`, `OnDespawn`) 관리 인터페이스 정의.
* **[NetworkObjectPool.cs](file:///c:/UnityProjects/ProjectAI/Assets/_Game/Scripts/Cores/Pooling/NetworkObjectPool.cs) (신규)**:
  * NGO의 `INetworkPrefabInstanceHandler`를 구현하여 프리팹별 풀 캐시 및 할당/반환 제어.
  * 프리팹 핸들러 등록 타이밍 충돌을 예방하는 지연 등록(`RegisterPrefabHandlers`) 설계.
  * `GetComponent<IPoolable>()` 오버헤드를 막기 위해 인스턴스 생성 시 인터페이스 캐싱(`instancePoolableMap`) 구현.
  * `OnDisable` 시 컬렉션 `Clear` 및 강제 파괴 시 딕셔너리 원소 `Remove` 처리로 메모리 누수 방지.
  * `GlobalObjectIdHash` 대신 프리팹 `NetworkObject` 레퍼런스 자체를 키로 이용하도록 딕셔너리 구조 변경하여 해시 충돌 및 식별 불안정성 차단.
  * `Destroy(instance.gameObject)` 형태로 불필요한 네임스페이스 수식어를 제거하여 호출 코드 간소화.
  * `ShouldExpandWhenEmpty`로 동적 확장 옵션 변수명 변경.
  * 코딩 표준을 적용하여 멤버 변수에서 `m_` 접두사를 전부 지우고 기존 코드 형식에 맞는 카멜 케이스(`pools`, `instanceToPrefabMap`, `instancePoolableMap`, `registeredPrefabs`)로 리팩토링.
  * 외부(서버)에서 풀 인스턴스를 빌려올 수 있는 `GetNetworkObject` public API 추가.
* **[GameStatics.cs](file:///c:/UnityProjects/ProjectAI/Assets/_Game/Scripts/Cores/GameStatics.cs) (수정)**:
  * 싱글톤 형태 대신 `GameStatics.ObjectPool` 프로퍼티를 통해 전역에서 오브젝트 풀에 안전하게 접근하도록 등록 기능 구현.
  * 중복 등록 감지 및 경고 로그 추가.
  * `UnregisterObjectPool` 메서드를 얼리리턴 패턴으로 전환하여 코딩 컨벤션 준수.

### 2단계: 기존 마법탄 투사체 풀링 적용 및 안정성 강화
* **[NetProjectile.cs](file:///c:/UnityProjects/ProjectAI/Assets/_Game/Scripts/Projectiles/NetProjectile.cs) (수정)**:
  * `IPoolable` 인터페이스 상속 및 구현.
  * `OnDespawn()` 시 물리 속도(`linearVelocity`, `angularVelocity` = 0) 리셋 처리 적용으로 재사용 궤적 오류 차단. (얼리리턴 패턴의 널 체크 가드 적용)
  * `DestroyProjectile()` 내 서버 스폰 해제 방식을 `Despawn(false)`으로 수정하여 소멸하지 않고 풀로 복귀되도록 조정.
* **[BasicAttackLogic.cs](file:///c:/UnityProjects/ProjectAI/Assets/_Game/Scripts/Cores/Skills/Abilities/BasicAttackLogic.cs) (수정)**:
  * `Initialize` 시점에 프리팹의 `NetworkObject`를 찾아 풀 셋업(`SetupPool`)을 선제 수행하여 런타임 랙 예방. (얼리리턴 패턴 적용)
  * `Execute` 시점에 `Object.Instantiate`를 호출하는 대신 `GameStatics.ObjectPool.GetNetworkObject`를 사용하여 인스턴스를 확보한 후 `Spawn()`하도록 구조 변경.
  * 풀링 초기화 시점과 스킬 초기화 시점의 레이스 컨디션을 막기 위해 `Execute` 내부에 지연 셋업(`SetupPool`) 및 2중 null 가드 보호 설계 추가.
* **물리 리지드바디 참조 구조 개편 (리팩토링)**:
  * `NetEntity`에 임시 추가했던 Rigidbody2D 참조 프로퍼티를 제거하고, 실제 물리 이동의 주체인 `ANetMovement`로 프로퍼티(`Rb`) 및 캐싱을 이관하여 컴포넌트 간 단일 책임 원칙(SRP) 강화.
  * `NetPlayerMovement.cs` 및 `NetServerMovement.cs` 의 자체 로컬 `rb` 필드를 제거하고 부모 클래스의 `base.Rb` 참조로 일원화.
* **서버-클라이언트 풀 셋업 동기화 및 오탐 로그 방지 (크로스 가드)**:
  * `SkillManager.cs` 초기화 직후 `ObjectPool`이 켜져 있을 때 모든 스킬 투사체 프리팹 풀을 사전 빌드. (Awake 지연 등록 가드 시 에러 로그는 생략하여 오탐 방지)
  * `NetworkObjectPool.cs` 기동(`Start`) 시점에 `SkillManager`가 존재하면 모든 스킬 투사체 프리팹 풀을 사전 빌드.
  * `SkillManager.Start()` 메서드를 추가하여 모든 씬 기동이 완결된 `Start` 시점에 `ObjectPool` 누락 여부를 최종 검증하여 씬 배치 오류 시 `Debug.LogError`를 정식으로 출력.
  * 두 매니저의 생명주기(Awake/Start) 호출 순서 제약을 완벽히 해소하고, 씬 내 배치 오류를 강하게 감지하도록 보강 완료.

## 3. 검증 결과
* **리뷰어 에이전트 검증**: 지연 초기화 상호 연동 및 Start 시점 검사 이관에 따른 오탐 로그 제거 타당성 최종 합격.
* **테스터 에이전트 검증**: 컴파일 성공 여부 및 Start 단계로의 널 검사 이관 정합성, 컨벤션(얼리리턴, 중괄호 개행 등) 준수율 100% 확인 완료.
* **플래너 에이전트 검증**: 타이밀 조율 최적화 및 2단계 완성도 검토 통과, 3단계(몬스터 스폰 구축) 이행 승인 완료.
