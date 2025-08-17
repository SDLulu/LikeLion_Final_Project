# PMK 네트워크 오브젝트 소환 문제점 분석 및 해결방안

## 📋 **문제 개요**
멀티플레이어 2D 플랫포머 게임에서 화살함정, 문, 플랫폼 등의 오브젝트가 제대로 동기화되지 않는 문제가 발생하고 있습니다.

## 🔍 **현재 코드 분석**

### **1. 맵 프리팹 소환 방식 (혼재)**
```csharp
// PMK_TileRogic.cs - DelayCreate_Map 메서드
if (prefab.GetComponent<NetworkObject>() != null && HasStateAuthority)
{
    // NetworkObject가 있으면 Runner.Spawn() 사용
    Runner.Spawn(prefab, spawnPosition, child.rotation, null, (runner, obj) =>
    {
        obj.transform.SetParent(parentTrans);
        obj.name = prefab.name;
    });
}
else if (prefab.GetComponent<NetworkObject>() == null)
{
    // NetworkObject가 없으면 Instantiate() 사용
    GameObject obj = Instantiate(prefab, spawnPosition, child.rotation, parentTrans);
    obj.name = prefab.name;
}
```

**문제점**: 맵 프리팹에 `NetworkObject` 포함 여부에 따라 소환 방식이 달라짐

### **2. 현재 프리팹 설정 현황**

#### **정상 동작하는 것들**
- `trap[]`: NetworkObject 배열로 선언되어 `Runner.Spawn()` 사용
- `LaunchTrapPrefab[]`: NetworkObject 배열로 선언되어 `Runner.Spawn()` 사용

#### **문제가 있는 것들**
- `stage1EnemySpawn[]`, `stage2EnemySpawn[]`, `stage3EnemySpawn[]`: GameObject 배열
- `objSpawn[]`: GameObject 배열
- 맵 프리팹 내부의 문, 플랫폼 등: NetworkObject 포함 여부 불명확

### **3. 코드 주석의 모순**
```csharp
// 맵 프리팹에는 네트워크 오브젝트가 포함되어있지 않습니다.
```
하지만 실제로는 NetworkObject 포함 여부를 확인하여 소환 방식을 결정하고 있음

## ⚠️ **발견된 문제점들**

### **1. 일관성 없는 소환 방식**
- 같은 맵 프리팹 내에서도 오브젝트마다 소환 방식이 다름
- NetworkObject가 있으면 Runner.Spawn(), 없으면 Instantiate()

### **2. 네트워크 동기화 누락**
- GameObject 배열로 선언된 오브젝트들은 네트워크 동기화 안됨
- 클라이언트 간 상태 불일치 발생 가능

### **3. 프리팹 설정 불일치**
- 맵 프리팹에 NetworkObject 포함 여부가 일관되지 않음
- 개발자마다 다른 방식으로 프리팹 설정 가능성

## 🛠️ **해결방안**

### **1. 즉시 적용 가능한 수정사항**

#### **A. GameObject 배열을 NetworkObject 배열로 변경**
```csharp
// 기존
[SerializeField] private GameObject[] stage1EnemySpawn;
[SerializeField] private GameObject[] stage2EnemySpawn;
[SerializeField] private GameObject[] stage3EnemySpawn;
[SerializeField] private GameObject[] objSpawn;

// 수정 후
[SerializeField] private NetworkObject[] stage1EnemySpawn;
[SerializeField] private NetworkObject[] stage2EnemySpawn;
[SerializeField] private NetworkObject[] stage3EnemySpawn;
[SerializeField] private NetworkObject[] objSpawn;
```

#### **B. 소환 방식 통일**
```csharp
// 모든 오브젝트를 Runner.Spawn()으로 통일
if (HasStateAuthority)
{
    Runner.Spawn(prefab, spawnPosition, child.rotation, null, (runner, obj) =>
    {
        obj.transform.SetParent(parentTrans);
        obj.name = prefab.name;
    });
}
```

### **2. 중장기 개선사항**

#### **A. 프리팹 구조 표준화**
- 모든 맵 프리팹에 NetworkObject 포함
- 문, 플랫폼 등을 별도 NetworkObject 프리팹으로 분리
- 프리팹 네이밍 규칙 통일

#### **B. 소환 매니저 시스템 구축**
```csharp
public class NetworkSpawnManager : NetworkBehaviour
{
    public static NetworkSpawnManager Instance { get; private set; }
    
    [Header("프리팹 설정")]
    [SerializeField] private NetworkObject[] enemyPrefabs;
    [SerializeField] private NetworkObject[] objectPrefabs;
    [SerializeField] private NetworkObject[] trapPrefabs;
    
    public void SpawnNetworkObject(NetworkObject prefab, Vector3 position, Transform parent = null)
    {
        if (!HasStateAuthority) return;
        
        Runner.Spawn(prefab, position, Quaternion.identity, null, (runner, obj) =>
        {
            if (parent != null)
                obj.transform.SetParent(parent);
        });
    }
}
```

#### **C. 프리팹 검증 시스템**
```csharp
#if UNITY_EDITOR
private void OnValidate()
{
    // NetworkObject가 없는 프리팹 경고
    ValidateNetworkPrefabs();
}

private void ValidateNetworkPrefabs()
{
    var allPrefabArrays = new object[] 
    { 
        stage1EnemySpawn, stage2EnemySpawn, stage3EnemySpawn, 
        objSpawn, trap, LaunchTrapPrefab 
    };
    
    foreach (var array in allPrefabArrays)
    {
        if (array is NetworkObject[] netArray)
        {
            foreach (var prefab in netArray)
            {
                if (prefab == null) continue;
                if (prefab.GetComponent<NetworkObject>() == null)
                {
                    Debug.LogError($"프리팹 {prefab.name}에 NetworkObject가 없습니다!");
                }
            }
        }
    }
}
#endif
```

## 📝 **수정 우선순위**

### **High Priority (즉시 수정)**
1. GameObject 배열을 NetworkObject 배열로 변경
2. 소환 방식 통일 (Runner.Spawn() 사용)

### **Medium Priority (1-2주 내)**
1. 맵 프리팹에 NetworkObject 추가
2. 프리팹 검증 시스템 구축

### **Low Priority (1개월 내)**
1. 소환 매니저 시스템 구축
2. 프리팹 구조 완전 표준화

## 🔗 **관련 파일들**
- `Assets/_TestSandBox/PMK/Scripts/Tile/PMK_TileRogic.cs`
- `Assets/_TestSandBox/PMK/Scripts/Tile/PMK_TileRPC_Manager.cs`
- `Assets/_TestSandBox/PMK/Scripts/Tile/PMK_TileZoneSpawner.cs`

## 📚 **참고 자료**
- Photon Fusion 공식 문서: NetworkObject 생성 및 관리
- Unity 멀티플레이어 게임 개발 가이드
- 프로젝트 네트워킹 패턴 가이드

---
*작성일: 2024년*
*작성자: AI Assistant*
*문서 버전: 1.0*
