using UnityEngine;

// 🎁 랜덤 스폰 풀 - 여러 아이템에서 공통으로 사용할 프리팹 풀
[CreateAssetMenu(fileName = "RandomSpawnPool", menuName = "Items/Random Spawn Pool")]
public class RandomSpawnPool : ScriptableObject
{
    [System.Serializable]
    public class SpawnCategory
    {
        [Header("카테고리 설정")]
        public string categoryName = "기본 카테고리";
        public GameObject[] spawnPrefabs;
        
        [Header("확률 설정")]
        [Range(0f, 100f)]
        public float spawnChance = 25f; // 0-100% 확률
        
        // 프리팹이 할당되어 있는지 확인
        public bool HasPrefabs => spawnPrefabs != null && spawnPrefabs.Length > 0;
        
        // 랜덤 프리팹 가져오기
        public GameObject GetRandomPrefab()
        {
            if (!HasPrefabs) return null;
            return spawnPrefabs[UnityEngine.Random.Range(0, spawnPrefabs.Length)];
        }
    }
    
    [Header("🎁 카테고리별 스폰 설정")]
    [SerializeField] private SpawnCategory[] spawnCategories;
    
    // 프리팹이 할당되어 있는지 확인
    public bool HasPrefabs
    {
        get
        {
            if (spawnCategories == null || spawnCategories.Length == 0) return false;
            
            foreach (var category in spawnCategories)
            {
                if (category.HasPrefabs) return true;
            }
            return false;
        }
    }
    
    // 카테고리별로 랜덤 프리팹 가져오기 (확률 기반)
    public GameObject GetRandomPrefab()
    {
        if (!HasPrefabs) return null;
        
        // 확률 기반으로 카테고리 선택
        SpawnCategory selectedCategory = SelectCategoryByChance();
        if (selectedCategory == null) return null;
        
        return selectedCategory.GetRandomPrefab();
    }
    
    // 확률 기반으로 카테고리 선택
    private SpawnCategory SelectCategoryByChance()
    {
        if (spawnCategories == null || spawnCategories.Length == 0) return null;
        
        // 전체 확률 합계 계산
        float totalChance = 0f;
        foreach (var category in spawnCategories)
        {
            if (category.HasPrefabs)
            {
                totalChance += category.spawnChance;
            }
        }
        
        if (totalChance <= 0f) return null;
        
        // 확률 합이 100%가 아닐 경우 경고 (디버그용)
        if (Mathf.Abs(totalChance - 100f) > 0.1f)
        {
            Debug.LogWarning($"[RandomSpawnPool] 확률 합계가 100%가 아닙니다: {totalChance:F1}%");
        }
        
        // 랜덤 값 생성
        float randomValue = UnityEngine.Random.Range(0f, totalChance);
        float currentChance = 0f;
        
        // 확률에 따라 카테고리 선택
        foreach (var category in spawnCategories)
        {
            if (!category.HasPrefabs) continue;
            
            currentChance += category.spawnChance;
            if (randomValue <= currentChance)
            {
                return category;
            }
        }
        
        // 마지막 카테고리 반환 (부동소수점 오차 방지)
        return spawnCategories[spawnCategories.Length - 1];
    }
    
    // 확률 정보 디버그 출력
    [ContextMenu("Show Probability Info")]
    private void ShowProbabilityInfo()
    {
        if (spawnCategories == null || spawnCategories.Length == 0)
        {
            Debug.Log("[RandomSpawnPool] 카테고리가 설정되지 않았습니다.");
            return;
        }
        
        float totalChance = 0f;
        Debug.Log("[RandomSpawnPool] 확률 정보:");
        
        foreach (var category in spawnCategories)
        {
            if (category.HasPrefabs)
            {
                totalChance += category.spawnChance;
                Debug.Log($"- {category.categoryName}: {category.spawnChance:F1}% (프리팹 {category.spawnPrefabs.Length}개)");
            }
            else
            {
                Debug.Log($"- {category.categoryName}: {category.spawnChance:F1}% (프리팹 없음)");
            }
        }
        
        Debug.Log($"총 확률: {totalChance:F1}%");
        
        if (Mathf.Abs(totalChance - 100f) > 0.1f)
        {
            Debug.LogWarning($"확률 합계가 100%가 아닙니다! (차이: {Mathf.Abs(totalChance - 100f):F1}%)");
        }
    }
    
    // 특정 카테고리명으로 프리팹 가져오기
    public GameObject GetRandomPrefabByCategory(string categoryName)
    {
        if (spawnCategories == null || spawnCategories.Length == 0) return null;
        
        foreach (var category in spawnCategories)
        {
            if (category.categoryName == categoryName && category.HasPrefabs)
            {
                return category.GetRandomPrefab();
            }
        }
        
        return null;
    }
    
    // 에디터에서 테스트용 메서드
    [ContextMenu("Test Random Spawn")]
    private void TestRandomSpawn()
    {
        Debug.Log($"랜덤 프리팹: {GetRandomPrefab()?.name ?? "없음"}");
    }
}
