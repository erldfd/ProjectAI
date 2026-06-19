# 렌더링 계층 아키텍처 및 프리팹 적용 테스트 보고서

## 테스트 대상
* `Player.prefab`
* `AshenWolf.prefab`
* `MemoryShardBug.prefab`
* `VisualInterpolator.cs` & `ZOrderSorter.cs` 상호작용 로직

## 1. 아키텍처 로직 테스트 (정상)
* **ZOrderSorter** (월드 Y축 기반 렌더링 오더 갱신)와 **VisualInterpolator** (수평 로컬 위치 보간) 코드를 정적 분석 및 런타임 추적한 결과, **상호 간섭 없이 완벽하게 독립적으로 동작**함을 확인했습니다. 
* 3단 계층(Root -> LerpNode -> Visuals)은 물론 2단 계층(Root -> Visuals)에서도 각자의 역할이 충돌하지 않습니다.

## 2. 프리팹 적용 상태 검사 (결함 발견)
사용자가 수동으로 적용한 프리팹 계층 구조에서 다음과 같은 누락 사항이 발견되었습니다.

* **[PASS] Player.prefab**
  - 3단 구조가 정상적으로 셋업됨.
  - 관련 컴포넌트(`VisualInterpolator`, `ZOrderSorter`, `NetworkTransform`)의 설정이 올바름.

* **[FAIL] AshenWolf.prefab** / **[FAIL] MemoryShardBug.prefab**
  - **문제 1**: 2단 구조로 분리는 되어 있으나, **자식 객체(Visuals)에 `VisualInterpolator`가 부착되어 있지 않습니다.**
  - **문제 2**: Root에 위치한 `NetworkTransform` 컴포넌트의 자체 Interpolate(보간) 옵션이 여전히 켜져 있어, 추후 `VisualInterpolator` 부착 시 이중 보간 현상이 발생할 위험이 있습니다.

## 해결 권고 사항
해당 몬스터 프리팹들의 `Visuals` 자식 객체에 `VisualInterpolator`를 부착하고, Root의 `NetworkTransform` 보간 기능을 비활성화(Snap) 하십시오.
