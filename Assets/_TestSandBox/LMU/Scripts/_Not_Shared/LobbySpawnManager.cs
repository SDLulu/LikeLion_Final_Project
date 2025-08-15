using System.Collections.Generic;
using Fusion;
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
        List<int> invalidIndices = new List<int>();
        
        for (int i = 0; i < spawnDataList.Count; i++)
        {
            var data = spawnDataList[i];
            bool isInvalid = false;
            
            if (data.prefab == null)
            {
                Debug.LogWarning($"LobbySpawnManager: 인덱스 {i}의 프리팹이 설정되지 않아 삭제를 진행합니다.");
                isInvalid = true;
            }
            if (data.spawnTransform == null)
            {
                Debug.LogWarning($"LobbySpawnManager: 인덱스 {i}의 스폰 Transform이 설정되지 않아 삭제를 진행합니다.");
                isInvalid = true;
            }
            
            if (isInvalid)
            {
                invalidIndices.Add(i);
            }
        }
        
        for (int i = invalidIndices.Count - 1; i >= 0; i--)
        {
            int indexToRemove = invalidIndices[i];
            spawnDataList.RemoveAt(indexToRemove);
        }
    }

    public void Spawn(NetworkRunner runner)
    {
        if (runner == null || runner.IsServer == false)
            return;

        foreach (var spawnData in SpawnDataList)
        {
            if (spawnData.prefab != null && spawnData.spawnTransform != null)
            {
                try
                {
                    runner.Spawn(spawnData.prefab,
                        spawnData.spawnTransform.position,
                        spawnData.spawnTransform.rotation);
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"LobbySpawnManager 스폰 실패: {spawnData.prefab.name}, 오류: {e.Message}");
                }
            }
        }
    }
}
