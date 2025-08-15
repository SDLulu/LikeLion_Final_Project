using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class SpawnData
{
    [Header("스폰 정보")]
    public GameObject prefab;
    public Transform spawnTransform;
}

/// <summary>
/// 로비 씬에서 프리팹과 스폰 위치를 1대1 대응시키는 간단한 스폰 관리자
/// </summary>
public class LobbySpawnManager : MonoBehaviour
{
    [Header("스폰 데이터")]
    [SerializeField] private List<SpawnData> spawnDataList = new List<SpawnData>();
    
    /// <summary>
    /// 스폰 데이터 리스트를 반환
    /// </summary>
    public List<SpawnData> SpawnDataList => spawnDataList;
    
    /// <summary>
    /// 특정 인덱스의 스폰 데이터를 반환
    /// </summary>
    public SpawnData GetSpawnData(int index)
    {
        if (index >= 0 && index < spawnDataList.Count)
        {
            return spawnDataList[index];
        }
        return null;
    }
    
    /// <summary>
    /// 스폰 데이터 개수를 반환
    /// </summary>
    public int SpawnDataCount => spawnDataList.Count;
    
    private void OnValidate()
    {
        // 에디터에서 스폰 데이터 유효성 검사
        for (int i = 0; i < spawnDataList.Count; i++)
        {
            var data = spawnDataList[i];
            if (data.prefab == null)
            {
                Debug.LogWarning($"LobbySpawnManager: 인덱스 {i}의 프리팹이 설정되지 않았습니다.");
            }
            if (data.spawnTransform == null)
            {
                Debug.LogWarning($"LobbySpawnManager: 인덱스 {i}의 스폰 Transform이 설정되지 않았습니다.");
            }
        }
    }
}
