# 오브젝트 풀링 및 스폰 매니저 구현

## User Review Required

> [!IMPORTANT]
> **스폰 로직 분리 제안**
> 1. **통합 관리 (NetworkObjectPool)**: 메모리를 관리하는 풀 시스템은 단일 매니저로 묶어 NGO의 `INetworkPrefabInstanceHandler`를 통해 모든 NetworkObject 통합 관리.
> 2. **분리 관리 (Spawner)**: 몬스터는 `MonsterSpawner`가 담당하고, 투사체는 스킬 컴포넌트(`NetSkillComponent` 또는 `ISkillLogic`)가 직접 풀에서 꺼내어 사용.

## Open Questions
- 피드백 반영 완료 (무작위 스폰 우선, 풀 사이즈 동적 확장 기본 활성화, GameStatics 등록).

## Proposed Changes

### Object Pooling (Core)
#### [NEW] Assets/_Game/Scripts/Cores/Pooling/NetworkObjectPool.cs
- `INetworkPrefabInstanceHandler` 구현 풀 매니저.
- `GameStatics` 전역 접근 등록.

#### [NEW] Assets/_Game/Scripts/Cores/Pooling/IPoolable.cs
- 초기화 보장 인터페이스.

### Projectile Updates
#### [MODIFY] Assets/_Game/Scripts/Projectiles/NetProjectile.cs
- `IPoolable` 적용. `Despawn()` 처리.

#### [MODIFY] Assets/_Game/Scripts/Cores/Skills/Abilities/BasicAttackLogic.cs
- `Instantiate` -> `NetworkObjectPool.GetNetworkObject` 로직 교체.

### Spawners (Gameplay)
#### [NEW] Assets/_Game/Scripts/GameModes/MonsterSpawner.cs
- 타이머 기반 무작위 위치 스폰 로직 구현.
