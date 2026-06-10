# 오브젝트 풀링 및 스포너 매니저 개발

- `[x]` **1단계: 코어 풀링 시스템 구축**
  - `[x]` `IPoolable.cs` 인터페이스 생성
  - `[x]` `NetworkObjectPool.cs` 생성 (NGO INetworkPrefabInstanceHandler 연동)
  - `[x]` `GameStatics.cs`에 접근자 추가

- `[x]` **2단계: 기존 투사체에 풀링 적용 및 안정성 강화**
  - `[x]` `NetProjectile.cs`에 `IPoolable` 적용 및 `Despawn(false)` 교체
  - `[x]` `BasicAttackLogic.cs` 풀링 시스템 스폰 연동 및 얼리리턴/지연 셋업 적용
  - `[x]` 리지드바디(`Rb`) 캐싱 및 속도 제어 권한을 `ANetMovement` 최상위 클래스로 통합 이관
  - `[x]` `SkillManager`와 `NetworkObjectPool` 간의 상호 크로스 풀 셋업 연동(Awake/Start 시점 순서 제약 제거)

- `[ ]` **3단계: 몬스터 스폰 시스템 구축**
  - `[ ]` `MonsterSpawner.cs` 무작위 위치 스폰 로직 추가
