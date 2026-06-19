# 네트워크 이동 보정 및 보안 로직 테스트 보고서

## 테스트 대상
* `Assets/_Game/Scripts/Movements/ANetMovement.cs`
* `Assets/_Game/Scripts/Movements/NetPlayerMovement.cs`
* `Assets/_Game/Scripts/Movements/NetServerMovement.cs`

## 검증 내역
1. **상하 깊이 이동속도 보정 [PASS]**
   - Y축 물리 이동에만 `depthSpeedRatio` (기본 0.6) 가 정상적으로 곱해짐.
   - `NetAnimVelocity` 변수에는 보정 전 원본 벡터가 주입되어, 2.5D 화면상 깊이 이동 시 속도가 느려져도 캐릭터의 다리(애니메이션)는 가로 이동과 똑같이 빠르게 걷는 의도된 연출 확인.
2. **이속 갱신 즉각 반영 (반응성) [PASS]**
   - 이동 중에 버프/디버프 획득 시 새로운 키보드 조작 없이 즉각적으로 새로운 애니메이션 속도가 재생됨을 확인.
3. **스피드핵 방어 패킷 검증 [PASS]**
   - 클라이언트에서 조작된 비정상 크기의 `inputVector` 패킷 전송 시, 서버 측의 `ApplyPhysics`에서 크기 1로 강제 정규화(`Normalize`) 처리됨. 
   - 이로 인해 해커 클라이언트 본인 화면에서만 심각한 고무줄 현상(Rubber-banding)이 발생하며 정상 클라이언트나 서버에는 영향을 주지 않음.
4. **클라이언트 예측 보간 안정성 [PASS]**
   - Y축 보정 및 서버 강제 동기화 시, 재시뮬레이션 루프의 1프레임 가산 및 충돌 체크(벽 뚫림 방지)가 완벽하게 기능함.

## 최종 결과
결함 없음. 기획 요구사항 및 리뷰어 피드백 모두 완벽히 충족.
