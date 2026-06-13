# 🧪 정적 검증 결과 보고서

## 📌 개요
* 대상 파일: `NetEntity.cs`, `NetProjectileMovement.cs`
* 검증 방식: 정적 코드 분석
* 기준: 코딩 컨벤션, NGO 생명주기, Null/예외 발생 가능성

## ✅ 정적 검증 결과
* **컴파일 오류 가능성:** 없음
* **코딩 컨벤션:** 
  * `NetEntity.cs`: 규칙 준수 확인 (중괄호, Early Return 적용 등)
  * `NetProjectileMovement.cs`: 규칙 준수 확인 (서버 검증용 Assert와 if문 혼용 적용, 변조 대비 완비)
* **네트워크 콜백 & 생명주기:** 
  * `NetProjectileMovement`의 `OnNetworkSpawn` 및 `OnNetworkDespawn`에서 `NetworkVariable` 이벤트 구독/해제 정상 구현 확인.

## ⚠️ 의심되는 문제
1. **`NetProjectileMovement.cs` - `OnEnable` 시 Null 가능성**
   * `OnEnable()`에서 `base._entityEvents.OnMoveSpeedModifierChanged` 이벤트를 구독하고 있습니다.
   * 부모 클래스 `ANetMovement`의 초기화(보통 `Awake`)가 해당 스크립트의 `OnEnable`보다 늦어지는 경우, 또는 프리팹 초기화 순서 문제로 `_entityEvents`가 null 상태일 때 `NullReferenceException`이 발생할 위험이 있습니다.
2. **`NetEntity.cs` - 애니메이터 `CullCompletely` 관련**
   * 클라이언트 측 화면 밖으로 벗어날 시 `AnimatorCullingMode.CullCompletely`로 인해 애니메이션이 완전히 정지됩니다.
   * 단순 시각적 용도라면 최적화 측면에서 올바르나, 클라이언트의 특정 로직이 `Animation Event` 콜백에 의존한다면 이벤트 누락 버그가 유발될 수 있습니다.

## 🤖 자동 테스트 가능 여부
* **테스트 환경:** PlayMode 기반 통합 네트워크 테스트 필요 (Host-Client 구조).
* **자동화 가능 여부:** 가능 (NGO NetworkManager 셋업 및 ClientRpc 동기화 확인).
* **실행 보류:** 사용자의 명시적인 PlayMode 및 빌드 실행 허가가 없어 정적 검증으로 갈음함.

## 🚧 검증 한계
* **정적 분석의 한계:** 런타임 환경에서 `GetComponent`가 참조하는 객체의 실제 생성 타이밍이나, NGO 특성상 발생하는 간헐적 지연 동기화에 따른 엣지 케이스는 정적 분석만으로는 확인 불가.
