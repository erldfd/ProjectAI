# 포탈 상호작용 시스템 완성과 멀티플레이어 네트워크 리팩토링 (2026년 05월 30일)

## 📌 오늘 구현한 핵심 시스템 요약

### 1. 객체 지향 포탈 시스템 기반 구축
*   **전략 패턴(Strategy Pattern) 적용:** `IInteractionCondition` 인터페이스를 도입하여 상호작용 조건을 유연하게 조립할 수 있게 설계했습니다.
*   **컴포넌트 분리:** 방장 권한을 검사하는 `RequireHostCondition`을 별도의 컴포넌트로 분리했습니다.
*   **추상화 뼈대 설계:** 공통 상호작용 검증 로직을 담당하는 `APortalInteractable` 부모 클래스를 작성했습니다.

### 2. 씬 전환 및 텔레포트 기능 완성
*   **`SceneTransitionPortal`:** Enum(`SceneType`)을 사용하여 안전하게 다른 씬(로비 ↔ 던전)으로 파티를 강제 이동시키는 포탈을 완성했습니다.
*   **목표 스폰 위치 지정 시스템:**
    *   씬 이동 임시 메타데이터를 전담하는 `SceneTransitionData` 정적 클래스 생성.
    *   포탈을 탈 때 특정 ID나 쌩 좌표(Raw Coordinates)를 지정하면, 씬 로드 후 `ANetGameModeBase`가 해당 위치의 `PlayerStart`를 찾아 파티원들을 정확히 배달하는 로직을 구현했습니다.
*   **`LocationTeleportPortal`:** 동일 씬 내에서 개인 혹은 파티 전체를 특정 오브젝트 위치로 순간이동시키는 포탈을 추가했습니다.

### 3. 플레이어 입력 및 카메라 시스템 수정
*   **입력 시스템 확장:** `PlayerInputReader`에 상호작용(E키) 액션을 추가하고, 향후 꾹 누르기(Hold) 구현을 대비해 `canceled` 이벤트까지 바인딩을 마쳤습니다.
*   **마우스 좌표 버그 픽스:** 캐릭터 이동 시 조준점이 어긋나는 문제와 씬 전환 시 카메라 참조가 끊기는 문제를 프로퍼티 지연 초기화(Lazy Initialization)를 통해 한 번에 해결했습니다.
*   **카메라 미아 버그 픽스:** `NetPlayerController`에서 NGO 전용 씬 동기화 완료 콜백(`OnLoadEventCompleted`)을 사용하여, 씬이 바뀔 때마다 카메라가 정확히 플레이어를 다시 추적하도록 수정했습니다.

### 4. 🚨 치명적 네트워크 붕괴 복구 및 최적화 (Server-Authoritative)
*   **상호작용 서버 권한 위임:** 클라이언트가 씬 이동을 직접 호출하려다 에러가 나는 문제를 해결하기 위해, `NetInteractor`의 구조를 전면 리팩토링했습니다. 클라이언트는 대상의 ID만 넘기고, 실제 로직은 최신 NGO 문법인 `[Rpc(SendTo.Server)]`를 통해 서버(Host)에서 안전하게 실행되도록 구조를 개선했습니다.
*   **물리 루프 한계 최적화:** `GetComponentInParent` 로직을 하이어라키 구조에 맞게 롤백하고, 무거운 `Vector2.Distance` 대신 `sqrMagnitude`를 사용하는 등 조기 리턴(Early Return) 패턴을 적용하여 상호작용 물리 체크 연산 비용을 최소화했습니다.

---

## 🎯 내일(Next) 진행할 작업
*   로비(방) 생성 및 방 참가(Join Room) 시스템 구현 (매치메이킹 / 릴레이 등)
