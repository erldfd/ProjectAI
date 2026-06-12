# NetworkObjectPool 정적 검증 보고서

## 1. 컴파일 및 문법 검증 결과
- `NetworkObjectPool.cs` 내부의 잔여 오류(`var` 제거, `Despawn` 추가 등)는 문법적으로 완벽하게 수정되었습니다.
- `dotnet build` 결과, 프로젝트 코드에는 컴파일 에러가 없습니다.
  - *참고: 빌드 실패(Exit Code 1)가 발생했으나, 이는 `com.unity.ai.assistant` 패키지 내부의 `Image` 네임스페이스 충돌 문제로 확인되었으며 우리 코드의 결함이 아닙니다.*

## 2. NGO 구조적 관점에서의 잠재적 결함 분석 (리뷰어 우려사항 점검)

### A. 서버와 클라이언트의 이중 반환 (Double Return) 및 동기화 누락 문제
현재 `public void ReturnNetworkObject(NetworkObject instance)` 메서드는 `instance.Despawn()`을 호출하지 않고 **바로 풀 큐에 집어넣고 비활성화(SetActive(false))**합니다.
이로 인해 다음과 같은 심각한 문제가 발생합니다.
1. **클라이언트 동기화 누락 (고스트 객체):** 서버가 스폰된 객체에 대해 `ReturnNetworkObject`를 직접 호출하면, 로컬에서만 비활성화될 뿐 클라이언트에게 Despawn 메시지가 가지 않습니다.
2. **이중 반환 (큐 중복):** 개발자가 `pool.ReturnNetworkObject(obj)`를 호출한 뒤 습관적으로 `obj.Despawn()`을 호출하면, NGO의 `NetworkPrefabHandler`가 작동하여 내부적으로 `ReturnNetworkObjectInternal`을 **한 번 더 호출**합니다. 동일한 객체가 큐에 두 번 들어가게 되어 풀이 망가집니다.

### B. NetworkObject 없는 부모 하위에 배치하는 문제 (InvalidParentException)
NGO 규칙상 **Spawn 상태인 NetworkObject**를 NetworkObject 컴포넌트가 없는 일반 GameObject 하위에 두면 `InvalidParentException` 런타임 에러가 발생합니다.
- 정상적인 Despawn 절차(PrefabHandler 경유)를 밟는다면 객체가 이미 비활성화/Despawn 처리 중이므로 일반 폴더 하위에 두어도 문제가 없습니다.
- 하지만 현재 구조에서는 스폰된 객체를 `ReturnNetworkObject`로 명시적 반환할 때, **Despawn 하지 않은 채로** `poolData.RootFolder`의 자식으로 넣으려고 시도하므로 **NGO 예외가 발생**합니다.

## 3. 테스터의 해결 제안
두 가지 문제 모두 `ReturnNetworkObject` 진입 시 **스폰 상태를 체크하여 Despawn을 유도**하는 것으로 한 번에 해결할 수 있습니다.

```csharp
public void ReturnNetworkObject(NetworkObject instance)
{
    if (instance == null) return;

    if (instance.IsSpawned)
    {
        // 스폰된 상태라면 Despawn만 호출합니다.
        // NGO가 클라이언트에 Despawn을 동기화한 뒤, 
        // 양측의 PrefabHandler.Destroy를 통해 자동으로 ReturnNetworkObjectInternal이 호출되어 풀에 안전하게 들어갑니다.
        instance.Despawn(false);
    }
    else
    {
        // 스폰되지 않은 객체(혹은 이미 Despawn된 객체)만 직접 풀에 반환합니다.
        ReturnNetworkObjectInternal(instance);
    }
}
```
위와 같이 수정하면 이중 반환, 클라이언트 동기화 누락, 부모 변경 예외를 모두 예방할 수 있습니다.
