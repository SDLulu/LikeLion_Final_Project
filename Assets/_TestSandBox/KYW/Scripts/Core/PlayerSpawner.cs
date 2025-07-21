using UnityEngine;

// 🎮 플레이어 스폰 전용 클래스
// 네트워크 기능 없이 단순히 플레이어를 스폰하는 역할만 담당
public class PlayerSpawner : MonoBehaviour
{
    [Header("👤 플레이어 설정")]
    [SerializeField] private GameObject playerPrefab; // 스폰할 플레이어 프리팹
    [SerializeField] private Vector3 spawnPosition = Vector3.zero; // 스폰 위치
    [SerializeField] private bool autoSpawnOnStart = true; // 시작 시 자동 스폰
    

    
    [Header("⏰ 타이밍")]
    [SerializeField] private float spawnDelay = 0.1f; // 스폰 대기 시간
    
    private GameObject spawnedPlayer;
    private bool hasSpawned = false;

    private void Start()
    {
        if (autoSpawnOnStart && !hasSpawned)
        {
            StartCoroutine(SpawnPlayerWithDelay());
        }
    }
    
    private System.Collections.IEnumerator SpawnPlayerWithDelay()
    {
        hasSpawned = true;
        
        Debug.Log($"🎮 [PlayerSpawner] {spawnDelay}초 후 플레이어 스폰 시작...");
        yield return new WaitForSeconds(spawnDelay);
        
        SpawnPlayer();
    }
    
    // 플레이어 스폰
    public void SpawnPlayer()
    {
        if (playerPrefab == null)
        {
            Debug.LogError("🎮 [PlayerSpawner] 플레이어 프리팹이 설정되지 않았습니다!");
            return;
        }
        
        // 기존 플레이어가 있으면 제거
        if (spawnedPlayer != null)
        {
            DestroyImmediate(spawnedPlayer);
        }
        
        // 새 플레이어 스폰
        spawnedPlayer = Instantiate(playerPrefab, spawnPosition, Quaternion.identity);
        
        Debug.Log($"🎮 [PlayerSpawner] 플레이어 스폰 완료! 위치: {spawnedPlayer.transform.position}");
    }
    

    
    // 현재 스폰된 플레이어 반환
    public GameObject GetSpawnedPlayer()
    {
        return spawnedPlayer;
    }
    
    // 플레이어 제거
    public void DestroyPlayer()
    {
        if (spawnedPlayer != null)
        {
            DestroyImmediate(spawnedPlayer);
            spawnedPlayer = null;
            hasSpawned = false;
            Debug.Log("🎮 [PlayerSpawner] 플레이어 제거 완료");
        }
    }
    
    // 스폰 위치 변경
    public void SetSpawnPosition(Vector3 newPosition)
    {
        spawnPosition = newPosition;
        Debug.Log($"🎮 [PlayerSpawner] 스폰 위치 변경: {spawnPosition}");
    }
} 