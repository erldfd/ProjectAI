# 코드 리뷰 결과

최근 진행된 스킬 시스템 및 투사체 최적화 관련 수정 사항에 대한 리뷰. 요구사항 반영 및 분리(Two-State Pattern) 구현 양호함. 몇 가지 위반 사항과 개선점 도출됨.

* [Blocker] 코딩 컨벤션 위반 (한 줄 if문 사용)
  - 대상: `NetProjectileMovement.cs` (78, 94 라인)
  - 원인: `if (!GameStatics.IsServerAuthorized) return;`
  - 수정 지시: "if문은 한 줄에 적지 않고 항상 중괄호를 사용한다" 컨벤션 위반. 중괄호 포함하여 다중 라인으로 수정 요망.

* [Major] 클라이언트 예측 쿨타임 실패 시 롤백 누락
  - 대상: `NetSkillComponent.cs`의 `TryActivateSkill`
  - 원인: 클라이언트가 선제적으로 `SetLocalActivationTime`을 갱신하나, 서버 측 검증(`CanExecute` 등)에서 실패하여 취소될 경우 클라이언트의 쿨타임만 도는 문제 발생 가능.
  - 수정 지시: 서버에서 스킬 발동 실패 혹은 거부 시, 클라이언트의 쿨타임을 원복시키거나 동기화해주는 처리 추가 권장.

* [Minor] 클라이언트 물리 연산 최적화
  - 대상: `NetProjectile.cs` 및 투사체 프리팹
  - 원인: 클라이언트는 `OnTriggerEnter2D` 내부 로직만 스킵할 뿐 물리 충돌 감지 연산 자체는 계속 수행함.
  - 수정 지시: 클라이언트에서는 물리 이벤트를 감지할 필요가 없으므로, 스폰 시 클라이언트 권한일 경우 Collider를 비활성화하는 방식 도입 고려. (단, Rigidbody2D 속도 제어를 사용하므로 simulated=false는 불가)

* [Question] 즉각적 클라이언트 애니메이션 피드백 여부
  - 대상: 스킬 애니메이션 동기화 로직
  - 원인: 현재는 서버에서 스킬 발동 확정 후 `BroadcastPlayAnimationClientRpc`를 통해 클라이언트에 재생 지시함.
  - 질의: 클라이언트가 버튼을 눌렀을 때 핑 지연 없이 즉각적으로 예측 애니메이션을 재생해야 하는지, 아니면 현재처럼 서버 권한 확인 후 재생하는 방식이 의도된 기획인지 확인 필요.
