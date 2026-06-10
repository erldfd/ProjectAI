# 잿빛 늑대 소환 시스템 구현 (풀링, 소환 연동, AI)

플래너 에이전트의 Week 2 기획에 따라 플레이어의 잿빛 늑대(소환수) 소환 기능 및 관련 타겟팅 AI를 구현합니다.

## User Review Required

> [!IMPORTANT]
> - 소환수(잿빛 늑대)는 플레이어 당 최대 1마리로 제한됩니다. L키로 소환합니다.
> - K키 입력 시 소환수가 최우선으로 타겟팅할 적을 지정하는 "집중 명령"이 구현됩니다.
> - 본 기획은 범위가 넓으므로, 우선 **작업 1 & 작업 2 (풀링 탑재 및 L키 서버 스폰)** 만 먼저 진행할 계획입니다. 승인하시겠습니까?

## Proposed Changes

### 작업 1: 잿빛 늑대 프리팹 풀링 탑재 및 초기화 로직 구현
- **대상 스크립트 신규/수정**: `Assets/_Game/Scripts/Characters/Summons/NetWolfSummon.cs` (가칭)
- **내용**:
  - `IPoolable` 인터페이스 상속 및 `OnDespawn` 구현.
  - 소환 해제/사망 시 HP, 이동속도, 물리(Rigidbody) 리셋 로직 구현으로 재소환 시 상태 초기화 보장.

### 작업 2: 플레이어 소환 스킬(L키) 및 서버 스폰 연동
- **대상 스크립트 수정**: `PlayerInputReader.cs`, `NetPlayerController.cs`, `BasicAttackLogic.cs`(또는 신규 소환스킬 클래스)
- **내용**:
  - L키 액션 맵핑 및 입력 구독 연동.
  - `GameStatics.ObjectPool.GetNetworkObject`를 활용해 서버 권한(`ServerRpc`)으로 늑대 스폰 호출.
  - 플레이어당 1마리 제한을 위한 식별(OwnerClientId 매핑) 및 중복 방지 제어.

### 작업 3: 소환수 AI 및 집중 명령(K키) 구현 (추후 진행)
- **내용**: 주변 적 탐색 AI 및 K키 타겟 우선 지정(집중 명령) 시스템은 기반 스폰 기능이 완성된 후 2차로 구현합니다.

## Verification Plan

### Manual Verification
- Host/Client 접속 후 L키를 눌러 잿빛 늑대가 동기화되어 정상 스폰되는지 확인.
- 늑대를 강제 디스폰(사망) 후 L키로 재소환했을 때 스탯과 물리가 온전히 초기화된 상태로 나오는지 확인.
