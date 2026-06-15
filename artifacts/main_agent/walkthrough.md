# 소환 스킬 구현 작업 완료 (Walkthrough)

소환 스킬 기획부터 실제 데이터 구조 변경 및 로직 구현까지 모든 과정을 완료했습니다.

## 1. 스킬 데이터 구조 변경 (마스터 SO 방식)
* 개별 스킬별로 무수히 많은 SO 파일이 생성되는 문제를 방지하기 위해 다형성 직렬화(`[SerializeReference]`)를 활용한 마스터 SO 구조를 도입했습니다.
* `ABaseSkillConfig`, `SummonSkillConfig` 등 데이터 전용 클래스들을 생성했습니다.
* `SkillDatabaseSO` 마스터 객체에서 전체 스킬 리스트를 관리하며, 인게임 런타임 성능 최적화를 위해 내부 딕셔너리 캐싱 로직을 추가했습니다.
* `SkillManager` 및 기존 스킬(`BasicAttackLogic`, `ProjectileAttackLogic`)들이 새 마스터 구조를 참조하도록 리팩토링했습니다.
* 스킬 매니저가 유니티 에디터 인스펙터 참조 대신 런타임 중 `Resources.Load`로 에셋(`SkillDatabaseSO.asset`)을 자동 로드하도록 개선했습니다.

## 2. 소환 스킬 로직 구현
* `ISkillLogic`을 구현하는 **`SummonSkillLogic`**을 추가했습니다.
* 스킬 실행 시 서버 오너십을 기반으로 소환수 프리팹을 `NetworkObjectPool`에서 꺼내어 스폰(`SpawnWithOwnership`)하도록 구현했습니다.
* 스폰 후 소환수 프리팹에 부착된 `NetMonsterBrain`을 탐색하여, 주인을 시전자로 등록(`Owner = caster.transform`)합니다.
* 마스터 SO에서 설정한 '유지 거리'를 소환수 전용 대기 상태인 `SummonFollowState`에 즉시 주입해줍니다.

## 3. 소환수 만료 타이머
* 소환수 프리팹에 부착되어 수명을 체크하는 **`NetSummonDespawnTimer`** 컴포넌트를 추가했습니다.
* 마스터 SO에서 설정된 유지시간이 만료되면, 서버에서 안전하게 풀 매니저로 오브젝트를 반환(`ReturnNetworkObject`)합니다.
* NGO 제약에 따라 스폰 후 런타임 중 `NetworkBehaviour` 컴포넌트 동적 추가를 방지하기 위해, 소환수 프리팹에 컴포넌트 누락 시 에러 로그를 띄우도록 방어 로직을 작성했습니다.

## 다음 작업 안내
유니티 에디터로 이동하여 다음 항목들을 세팅해 주시면 즉시 테스트가 가능합니다.
1. 소환수로 사용할 프리팹 최상단에 `NetSummonDespawnTimer` 컴포넌트 부착.
2. 해당 프리팹의 `NetMonsterBrain` 내 상태 머신에 `SummonFollowState` 추가 여부 확인.
3. `Resources/SOs/SkillDatabaseSO` 에셋에서 `SummonSkillConfig` 항목을 추가하고 세부 능력치(유지 시간 10초, 유지 거리 2 등) 및 프리팹 할당.
