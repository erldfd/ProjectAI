# 무브먼트 아키텍처 개선 및 AI 로직 단순화 계획

## 🎯 문제 원인 분석 (유저 통찰력 일치)
정확한 통찰력입니다. 현재 AI 로직이 지나치게 복잡해지고 밀어내기 시 멀리 날아가는 근본적인 원인은 **`NetServerMovement`의 아키텍처 한계**에 있습니다.

현재 `SetDirection` 함수는 방향 벡터를 무조건 `.normalized` (길이 1)로 강제 변환한 뒤 **최고 속도(`baseSpeed`)**를 곱해서 이동시킵니다.
따라서 AI가 아주 살짝만 밀어내고 싶어도, 일단 밀어내기 방향이 설정되면 무조건 **최고 속도로 전력 질주**하게 됩니다. 이 상태에서 인터벌을 1초로 늘려버리면 1초 내내 최고 속도로 뛰어가니 경계를 한참 벗어나 저 멀리 날아가 버리는 것입니다.

또한 "목적지로 가라"는 명령이 없기 때문에, AI 상태 스크립트가 매 틱마다 거리를 재고, 0.1 이하인지 확인하고, 멈추고(`Vector2.zero`) 하는 잡다한 연산을 모두 떠안고 있었습니다.

## 🛠 제안하는 구조 개선 방안

이 문제를 근본적으로 해결하고 AI 로직을 극도로 단순화하기 위해 코어 아키텍처를 개선하고자 합니다.

### 1. `NetServerMovement.cs` 유동적 속도 허용
*   **[MODIFY]** `NetServerMovement.cs`
    *   `SetDirection` 내부의 `.normalized` 처리를 `Vector2.ClampMagnitude(direction, 1f)`로 변경합니다.
    *   이렇게 하면 벡터 길이가 1 이상일 때는 기존처럼 최고 속도를 내지만, 길이가 0.5라면 **절반의 속도**로 걷는 것이 가능해집니다. (살짝 밀어내기 가능)

### 2. `NetMonsterBrain.cs`에 도착 자동화 헬퍼 추가
*   **[MODIFY]** `NetMonsterBrain.cs`
    *   `MoveTowards(Vector2 targetLocation, float stopDistance, float slowDownRadius)` 함수를 추가합니다.
    *   목적지에 가까워질수록 벡터 길이를 1에서 0으로 부드럽게 줄여주어(Steering Arrive) **자연스러운 감속 및 자동 정지**를 뇌(Brain) 단에서 처리합니다.

### 3. `SummonFollowState.cs` 복잡한 로직 대거 삭제
*   **[MODIFY]** `SummonFollowState.cs`
    *   거리 계산, `hasArrivedRoamTarget` 등의 지저분한 상태 변수들을 모두 삭제합니다.
    *   단순히 `Brain.MoveTowards(roamTargetPosition, ...)` 한 줄로 목적지 이동을 지시합니다.
    *   밀어내기 힘(`separationForce`) 역시 길이를 아주 작게 주어, 최고 속도가 아닌 느린 속도로 살짝만 밀려나도록 수정합니다.

## ❓ 뷰어(User) 검토 요망
> [!IMPORTANT]
> 이 변경은 몬스터 이동의 근간이 되는 `NetServerMovement`와 `NetMonsterBrain`을 건드리는 핵심 아키텍처 수정입니다.
> 유저분께서 지적하신 "목적지 이동 함수 부재"를 완벽히 해소할 수 있는 구조적 리팩토링인데, 이 방향으로 진행해도 될지 검토 후 승인 부탁드립니다.
