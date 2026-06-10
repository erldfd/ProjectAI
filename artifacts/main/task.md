# 현재 진행 상황

- [x] 1단계: 오브젝트 풀링 아키텍처 기본 인터페이스 구성 (IPoolable, NetworkObjectPool, GameStatics)
- [x] 2단계: 플레이어 기본 마법탄 투사체 풀링 연동 및 리지드바디 캐싱/컴포넌트 의존성 최적화 완료
- [x] 3단계: 몬스터 스폰 시스템 (NetMonsterSpawner) 및 몬스터 풀링 연동
  - [x] `Assets/_Game/Scripts/GameModes/NetMonsterSpawner.cs` 생성
  - [x] `OnNetworkSpawn()` 시점 몬스터 풀 사전 워밍업 구현
  - [x] `IsServer` 가드 기반 타이머 루프 구현
  - [x] 무작위 2D 좌표 산출 및 `GameStatics.ObjectPool.GetNetworkObject` 스폰 구현
  - [x] 테스터 및 리뷰어 검증 (1단계 NetworkObjectPool 방어 코드 보완 포함) 완료
