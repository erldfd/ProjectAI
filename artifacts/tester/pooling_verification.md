# NGO 풀링 A안 구조 검증 결과 (Tester)

## 1. 컴파일 오류 검증
* `dotnet build Assembly-CSharp.csproj` 실행 결과, 우리 프로젝트 코드(`NetworkObjectPool.cs`, `NetProjectile.cs` 등)에는 컴파일 오류가 발생하지 않았습니다. (※ Unity 내부 패키지 `com.unity.ai.assistant`에서 발생하는 자체 오류는 무시 가능)

## 2. 구조적 정합성 체크 (InvalidParentException 및 이중 반환 이슈)

### ✔️ 긍정적인 부분: `SetParent(null)`
* 스폰 직전(`GetNetworkObjectInternal`)에 객체를 루트로 빼주는 로직(`instance.transform.SetParent(null)`)은 올바르게 동작합니다. `instance.Spawn()` 호출 전에 부모가 없는 상태를 보장하므로 `InvalidParentException` 발생을 완벽하게 방지합니다.

### ❌ 치명적인 결함 발견: `Despawn(false)`의 오작동 (메모리 누수 위험)
현재 `NetworkObjectPool.ReturnNetworkObject`에서 다음과 같이 구현되어 있습니다.
```csharp
if (GameStatics.IsServerAuthorized)
{
    instance.Despawn(false);
}
```
* **문제점:** NGO 구조상 `Despawn(false)`를 호출하면 게임 오브젝트 파괴 명령을 내리지 않습니다. 이로 인해 NGO 내부(`NetworkSpawnManager.OnDespawnObject`)에서 **커스텀 콜백인 `INetworkPrefabInstanceHandler.Destroy`를 아예 호출하지 않고 무시(Bypass)해버립니다.**
* **결과:** 현재 구조에서는 스폰된 객체를 반환하려고 할 때, NGO에서 디스폰 처리만 되고 오브젝트는 풀의 큐(Queue)에 들어가지 않습니다. 객체가 씬에 계속 쌓이면서 재사용되지 않는 치명적인 누수 버그가 발생합니다.

### 💡 해결책: `Despawn(true)` 사용
* 커스텀 `PrefabHandler`를 등록한 상태에서 풀링을 정상 작동시키려면 **반드시 `instance.Despawn(true)`를 호출해야 합니다.** (혹은 인자 없이 `instance.Despawn()`)
* **이중 반환 우려 해소:** 
  `Despawn(true)`를 호출하면 NGO가 자체적으로 `PrefabHandler.Destroy`를 호출하고, 여기서 `ReturnNetworkObjectInternal`로 이어져 단 한 번만 큐에 들어갑니다. (명시적으로 `ReturnNetworkObjectInternal`을 중복 호출하지 않도록 이미 `else`문으로 분리해 두었기 때문에 이중 반환은 발생하지 않습니다.)
* **InvalidParentException 우려 해소:**
  `PrefabHandler.Destroy`가 호출되는 시점은 NGO가 이미 해당 객체의 `IsSpawned = false`로 상태를 초기화한 직후입니다. 따라서 핸들러 내부(즉, `ReturnNetworkObjectInternal`)에서 `SetParent`를 호출해도 NGO 에러가 나지 않습니다.

## 결론 및 권고사항
main agent는 `NetworkObjectPool.cs`의 `ReturnNetworkObject` 메서드에서 `instance.Despawn(false);`를 `instance.Despawn(true);`로 수정해야 완벽한 A안이 완성됩니다.
