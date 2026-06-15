# 스킬 시스템 Data-Driven 아키텍처 개편

## 목표
`ESkillType` 1:1 매핑 구조를 탈피하고, `BaseSkillConfig` SO 에셋에 고유 ID를 부여하여 플레이어가 원하는 스킬 데이터를 직접 소유하고 실행(RPC)하는 진정한 데이터 기반 아키텍처로 개편합니다.

## User Review Required
> [!IMPORTANT]
> - 스킬 식별자 추가: `BaseSkillConfig`에 고유 ID(정수형)가 추가됩니다. 앞으로 새로운 스킬 SO를 생성할 때마다 서로 다른 ID 값을 인스펙터에서 기입해야 합니다.
> - 애니메이션 맵핑: 기존에는 `ESkillType` 단위로 애니메이션을 매핑했지만, 이제 스킬별(`BaseSkillConfig`)로 다를 수 있습니다. 이를 지원하기 위해 `NetSkillComponent`의 애니메이션 맵핑 리스트를 `SkillId` 기준으로 변경할 예정입니다. (동의하시나요?)

## Proposed Changes

### [MODIFY] [BaseSkillConfig.cs](file:///c:/UnityProjects/ProjectAI/Assets/_Game/Scripts/SOs/BaseSkillConfig.cs)
- 네트워크 통신 시 스킬을 식별하기 위한 `public int SkillId;` 변수를 추가합니다.

### [MODIFY] [SkillDatabaseSO.cs](file:///c:/UnityProjects/ProjectAI/Assets/_Game/Scripts/SOs/SkillDatabaseSO.cs)
- `Dictionary<ESkillType, BaseSkillConfig>` 캐싱 방식을 `Dictionary<int, BaseSkillConfig>` (SkillId 기반)으로 변경합니다.

### [MODIFY] [ISkillLogic.cs](file:///c:/UnityProjects/ProjectAI/Assets/_Game/Scripts/Cores/Skills/ISkillLogic.cs)
- 기존에는 `Initialize` 시점에 Config를 한 번만 로드하여 클래스 멤버로 들고 있었으나 (상태 유지형), 여러 Config가 하나의 로직 인스턴스를 공유해야 하므로 인자에 `BaseSkillConfig config`를 추가하여 상태를 들고 있지 않게(Stateless) 변경합니다.
  - `bool CanExecute(NetCharacter caster, BaseSkillConfig config);`
  - `void Execute(NetCharacter caster, BaseSkillConfig config);` 등

### [MODIFY] 스킬 로직 구현체들
- [BasicAttackLogic.cs](file:///c:/UnityProjects/ProjectAI/Assets/_Game/Scripts/Cores/Skills/Abilities/BasicAttackLogic.cs)
- [ProjectileAttackLogic.cs](file:///c:/UnityProjects/ProjectAI/Assets/_Game/Scripts/Cores/Skills/Abilities/ProjectileAttackLogic.cs)
- [SummonSkillLogic.cs](file:///c:/UnityProjects/ProjectAI/Assets/_Game/Scripts/Cores/Skills/Abilities/SummonSkillLogic.cs)
- 내부 변수로 캐싱하던 `cooldown`, `prefab`, `duration` 등을 제거하고, 호출 시 전달받는 `config`에서 즉시 데이터를 읽어오도록 리팩토링합니다.

### [MODIFY] [SkillManager.cs](file:///c:/UnityProjects/ProjectAI/Assets/_Game/Scripts/Cores/Skills/SkillManager.cs)
- `ExecuteSkill`, `ActionSkill`, `EndSkill`의 매개변수를 `ESkillType`에서 `int skillId`로 변경합니다.
- 내부적으로 전달받은 `skillId`로 `SkillDatabaseSO`에서 Config를 찾은 뒤, `config.SkillType`에 해당하는 로직을 꺼내와 실행합니다.

### [MODIFY] [NetSkillComponent.cs](file:///c:/UnityProjects/ProjectAI/Assets/_Game/Scripts/Cores/Skills/NetSkillComponent.cs)
- 인스펙터 노출 변수를 `List<BaseSkillConfig> OwnedSkills` 로 변경하여 기획자가 SO를 직접 드래그 앤 드롭으로 할당할 수 있게 합니다.
- 쿨타임 캐싱 딕셔너리의 키를 `ESkillType`에서 `int`(SkillId)로 변경합니다.
- `TryActivateSkill`과 RPC 함수들 역시 `skillId`를 기반으로 통신하도록 수정합니다.

## Verification Plan
1. 빌드 성공 확인
2. 씬에 배치된 플레이어 프리팹의 `NetSkillComponent`에 새로 만든 스킬 SO 할당 및 ID 지정.
3. 게임 실행 후 정상적으로 평타/소환 스킬이 발동하는지 확인.
