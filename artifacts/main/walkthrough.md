# 스킬 및 상태 관리 모듈화 (Enum 기반)

## 작업 목표
하드코딩되었던 플레이어의 전투 로직을 범용적이고 확장이 편한 **Enum 기반 매니저 구조**로 개편하였습니다. 이를 통해 향후 다른 캐릭터나 NPC도 코드 수정 없이 새로운 스킬을 손쉽게 사용할 수 있습니다.

## 주요 변경 사항

### 1. 전역 시스템 구성
- **[SkillEnums.cs](file:///c:/UnityProjects/ProjectAI/Assets/_Game/Scripts/Cores/Skills/SkillEnums.cs)**: 모든 스킬 식별자(`ESkillType`)와 캐릭터의 상태 비트마스크(`EStateTag`)를 정의합니다.
- **[ISkillLogic.cs](file:///c:/UnityProjects/ProjectAI/Assets/_Game/Scripts/Cores/Skills/ISkillLogic.cs)**: 개별 스킬이 반드시 구현해야 하는 인터페이스를 정의합니다.
- **[SkillManager.cs](file:///c:/UnityProjects/ProjectAI/Assets/_Game/Scripts/Cores/Skills/SkillManager.cs)**: 전역 싱글톤 매니저로, 게임 시작 시 리플렉션을 통해 모든 `ISkillLogic`을 찾아 딕셔너리에 등록하고, 서버의 스킬 실행 요청을 알맞은 로직으로 라우팅합니다.

### 2. 캐릭터 컴포넌트 리팩토링
- **[NetSkillComponent.cs](file:///c:/UnityProjects/ProjectAI/Assets/_Game/Scripts/Cores/Skills/NetSkillComponent.cs)**: 
  - 특정 스킬에 종속되지 않는 범용 구조로 변경되었습니다.
  - 자신이 보유한 스킬 리스트(`OwnedSkills`)를 관리합니다.
  - NetworkVariable을 이용해 현재 상태(`ActiveStates`)를 동기화합니다.
  - 클라이언트에서 쿨타임과 상태를 선행 예측한 뒤 서버에 RPC를 요청합니다. (Host 플레이어 쿨타임 충돌 방지 로직 포함)

### 3. 기본 마법탄 발사 분리
- **[BasicAttackLogic.cs](file:///c:/UnityProjects/ProjectAI/Assets/_Game/Scripts/Cores/Skills/Abilities/BasicAttackLogic.cs)**: `ISkillLogic`을 상속받아 마법탄 스폰과 쿨타임 검증을 전담하는 순수 C# 클래스로 분리되었습니다.

### 4. 컨트롤러 연동
- **[NetPlayerController.cs](file:///c:/UnityProjects/ProjectAI/Assets/_Game/Scripts/Players/NetPlayerController.cs)**: 기존 공격 입력 시 `TryActivateSkill(ESkillType.BasicAttack)`를 호출하도록 일원화되었습니다.

## 검증 결과
- 리뷰어/테스터 에이전트 교차 검증 통과 완료.
- 클라이언트-서버 간 `NetworkManager.ServerTime.Time` 기반 정밀 쿨타임 동기화 적용.
- Host 플레이어 쿨타임 버그 패치 및 리플렉션 안전장치(예외 처리) 확보.

> [!TIP]
> 이제 새로운 스킬을 만들고 싶다면, `ESkillType`에 이름을 추가하고 `ISkillLogic`을 상속받는 C# 스크립트만 추가하면 됩니다. 매니저가 이를 자동으로 감지하여 게임에 등록합니다. 프리팹이나 쿨타임 수치는 Scene에 배치된 `SkillManager` 프리팹 인스펙터에서 리스트로 등록할 수 있습니다.
