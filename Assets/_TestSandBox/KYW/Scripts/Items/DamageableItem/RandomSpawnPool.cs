using UnityEngine;

// 🎁 랜덤 스폰 풀 - 여러 아이템에서 공통으로 사용할 프리팹 풀
[CreateAssetMenu(fileName = "RandomSpawnPool", menuName = "Items/Random Spawn Pool")]
public class RandomSpawnPool : ScriptableObject
{
    [Header("🎁 스폰할 프리팹들")]
    [SerializeField] private GameObject[] spawnPrefabs;
    
    // 프리팹이 할당되어 있는지 확인
    public bool HasPrefabs => spawnPrefabs != null && spawnPrefabs.Length > 0;
    
    // 랜덤 프리팹 가져오기
    public GameObject GetRandomPrefab()
    {
        if (!HasPrefabs) return null;
        return spawnPrefabs[Random.Range(0, spawnPrefabs.Length)];
    }
}
