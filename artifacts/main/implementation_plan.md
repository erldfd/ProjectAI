# [Enum 기반 경량화 스킬 & 상태 관리 시스템 도입]

무거운 ScriptableObject(데이터 주도) 방식 대신, 코드 기반(Enum & Manager)으로 빠르고 직관적으로 통제할 수 있는 실용적인 모듈화 설계입니다. NGO 환경에서 Enum은 직렬화가 매우 가벼우므로 네트워크 통신에도 최적화된 좋은 접근입니다.

> [!IMPORTANT]
> **User Review Required**
> 제안하신 방식(Enum + SkillManager + State Enum)을 바탕으로 구체적인 구조를 잡았습니다. 이 설계대로 코드를 작성할지 확인 부탁드립니다.

## Proposed Changes

### 1. 전역 Enum 정의
**[NEW] `Assets/_Game/Scripts/Cores/Skills/SkillEnums.cs`**
- `ESkillType`: 스킬 식별자 (예: `BasicAttack`, `Dash`, `Fireball`)
- `EStateTag`: 캐릭터의 상태 (예: `None`, `Casting`, `Silenced`, `Stunned`, `Invincible`)
  - 상태는 여러 개가 중첩될 수 있으므로 `[Flags]` 속성을 부여하여 비트마스크(Bitmask)로 활용하거나 `NetworkList`로 관리합니다.

### 2. 스킬 로직 인터페이스 및 매니저
**[NEW] `Assets/_Game/Scripts/Cores/Skills/ISkillLogic.cs`**
- `void Execute(NetSkillComponent caster);`
- `bool CanExecute(NetSkillComponent caster);` (쿨타임, 상태 체크 등)

**[NEW] `Assets/_Game/Scripts/Cores/Skills/SkillManager.cs`**
- 싱글톤 패턴으로 구현.
- 게임 시작 시 모든 `ESkillType`에 매칭되는 `ISkillLogic` 구현체들을 Dictionary에 미리 할당해 둡니다.
- `Execute(ESkillType type, NetSkillComponent caster)` 호출 시 해당 로직을 실행합니다.

### 3. NetSkillComponent 구조 개편
**[MODIFY] `Assets/_Game/Scripts/Cores/Skills/NetSkillComponent.cs`**
- **보유 스킬**: `List<ESkillType> ownedSkills` (자신이 쓸 수 있는 스킬 목록)
- **상태 관리**: `NetworkVariable<int> ActiveStates` (비트마스크를 이용해 현재 상태 동기화)
- **작동 흐름**:
  1. 클라이언트 컨트롤러가 `TryActivateSkill(ESkillType.BasicAttack)` 호출.
  2. `ownedSkills`에 있는지, 현재 상태가 스킬 사용을 막고 있지 않은지(`Silenced` 등) 클라이언트 단에서 1차 검사.
  3. `ServerRpcActivateSkill(ESkillType.BasicAttack)` 호출.
  4. 서버에서 2차 검증 후 `SkillManager.Instance.Execute(skill, this)` 호출.

### 4. 스킬 구현체 분리
**[NEW] `Assets/_Game/Scripts/Cores/Skills/Abilities/BasicAttackLogic.cs`**
- `ISkillLogic`을 구현하며, 매니저에 의해 실행될 때 실질적인 투사체(마법탄) 생성 코드를 담당합니다.

## Validation Plan
1. 상태 부여 테스트: 스킬 로직 안에서 컴포넌트의 상태를 `EStateTag.Casting`으로 변경해보고, 다른 스킬이 막히는지 확인.
2. 매니저 연동: `SkillManager`가 올바르게 로직을 라우팅하여 마법탄이 정상적으로 발사되는지 확인.
