using System.Collections.Generic;
using UnityEngine;

public partial class PMK_TileRogic : MonoBehaviour
{
    /// <summary>
    /// 전체 맵 삭제
    /// </summary>
    public void DestroyAllMap()
    {
        mainTilemap.ClearAllTiles();

        foreach (Transform child in parentTrans)
        {
            if (child.GetComponent<PMK_TileRogic>() != null)
                continue;

            Destroy(child.gameObject);
        }

        SaveMapPos();
    }

    private void LateUpdate()
    {
        if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            DestroyAllMap();
        }
    }


#if UNITY_EDITOR
    [ContextMenu("LMU - 맵 랜덤 생성 테스트")]
    public void Test_RandomMapGeneration()
    {
        Debug.Log("=== LMU 맵 랜덤 생성 테스트 시작 ===");
        
        // 1. 초기화
        InitializeForEditorTest();
        
        // 2. 맵 랜덤 생성 실행
        ResetMap();
        
        // 3. 정리
        CleanupAfterEditorTest();
        
        Debug.Log("=== LMU 맵 랜덤 생성 테스트 완료 ===");
    }

    [ContextMenu("LMU - 맵 재배치 테스트")]
    public void Test_MapRearrangement()
    {
        Debug.Log("=== LMU 맵 재배치 테스트 시작 ===");
        
        // 1. 초기화
        InitializeForEditorTest();
        
        // 2. 맵 재배치 실행
        Create_EmptyMap();
        
        // 3. 정리
        CleanupAfterEditorTest();
        
        Debug.Log("=== LMU 맵 재배치 테스트 완료 ===");
    }

    [ContextMenu("LMU - 맵 파괴 및 메모리 정리")]
    public void Test_DestroyMapAndCleanup()
    {
        Debug.Log("=== LMU 맵 파괴 및 메모리 정리 시작 ===");
        
        // 1. 맵 파괴
        DestroyMap();
        
        // 2. 메모리 정리
        CleanupMemory();
        
        Debug.Log("=== LMU 맵 파괴 및 메모리 정리 완료 ===");
    }

    /// <summary>
    /// 맵 파괴
    /// </summary>
    private void DestroyMap()
    {
        Debug.Log("맵 파괴 중...");
        
        // 1. 타일맵의 모든 타일 제거
        if (mainTilemap != null)
        {
            mainTilemap.ClearAllTiles();
            Debug.Log("✓ 타일맵 클리어 완료");
        }
        
        // 2. 부모 트랜스폼의 모든 자식 오브젝트 파괴
        if (parentTrans != null)
        {
            List<Transform> childrenToDestroy = new List<Transform>();
            
            foreach (Transform child in parentTrans)
            {
                if (child.GetComponent<PMK_TileRogic>() != null)
                    continue;
                    
                childrenToDestroy.Add(child);
            }
            
            foreach (Transform child in childrenToDestroy)
            {
                DestroyImmediate(child.gameObject);
            }
            
            Debug.Log($"✓ {childrenToDestroy.Count}개의 자식 오브젝트 파괴 완료");
        }
        
        // 3. 맵 위치 배열 초기화
        if (mapXY != null)
        {
            mapXY = null;
            Debug.Log("✓ 맵 위치 배열 초기화 완료");
        }
        
        if (useMapXY != null)
        {
            useMapXY = null;
            Debug.Log("✓ 맵 사용 상태 배열 초기화 완료");
        }
        
        Debug.Log("맵 파괴 완료");
    }

    /// <summary>
    /// 메모리 정리
    /// </summary>
    private void CleanupMemory()
    {
        Debug.Log("메모리 정리 중...");
        
        // 1. mapPrefabDict 정리
        if (mapPrefabDict != null)
        {
            mapPrefabDict.Clear();
            mapPrefabDict = null;
            Debug.Log("✓ mapPrefabDict 정리 완료");
        }
        
        // 2. LR_Choose 리스트 정리
        if (LR_Choose != null)
        {
            LR_Choose.Clear();
            Debug.Log("✓ LR_Choose 리스트 정리 완료");
        }
        
        // 3. 타일맵 새로고침
        if (mainTilemap != null)
        {
            mainTilemap.RefreshAllTiles();
            Debug.Log("✓ 타일맵 새로고침 완료");
        }
        
        // 4. 물리 시스템 동기화
        Physics2D.SyncTransforms();
        Debug.Log("✓ 물리 시스템 동기화 완료");
        
        // 5. 가비지 컬렉션 강제 실행
        System.GC.Collect();
        System.GC.WaitForPendingFinalizers();
        System.GC.Collect();
        Debug.Log("✓ 가비지 컬렉션 실행 완료");
        
        // 6. 씬 뷰 업데이트
        UnityEditor.SceneView.RepaintAll();
        Debug.Log("✓ 씬 뷰 업데이트 완료");
        
        Debug.Log("메모리 정리 완료");
    }

    /// <summary>
    /// 에디터 타임 테스트를 위한 초기화
    /// </summary>
    private void InitializeForEditorTest()
    {
        Debug.Log("LMU 초기화 중...");
        
        // 1. mapPrefabDict 초기화
        mapPrefabDict = new Dictionary<string, GameObject[]>
        {
            { "C", Clear_Map_Prefab },
            { "LR", LR_Exit_Map_Prefab },
            { "D", D_Exit_Map_Prefab },
            { "W", W_Exit_Map_Prefab },
            { "WD", WD_Exit_Map_Prefab },
            { "S", Special_Map_Prefab }
        };
        
        // 2. 기존 맵 정리
        if (mainTilemap != null)
        {
            mainTilemap.ClearAllTiles();
        }
        
        // 3. 기존 오브젝트들 정리
        if (parentTrans != null)
        {
            foreach (Transform child in parentTrans)
            {
                if (child.GetComponent<PMK_TileRogic>() != null)
                    continue;
                    
                DestroyImmediate(child.gameObject);
            }
        }
        
        // 4. 맵 위치 시스템 초기화
        SaveMapPos();
        
        Debug.Log("LMU 초기화 완료");
    }

    /// <summary>
    /// 에디터 타임 테스트 후 정리
    /// </summary>
    private void CleanupAfterEditorTest()
    {
        Debug.Log("LMU 정리 중...");
        
        // 1. mapPrefabDict 정리
        if (mapPrefabDict != null)
        {
            mapPrefabDict.Clear();
            mapPrefabDict = null;
        }
        
        // 2. 타일맵 새로고침
        if (mainTilemap != null)
        {
            mainTilemap.RefreshAllTiles();
        }
        
        // 3. 물리 시스템 동기화
        Physics2D.SyncTransforms();
        
        // 4. 씬 뷰 업데이트
        UnityEditor.SceneView.RepaintAll();
        
        Debug.Log("LMU 정리 완료");
    }
#endif
}
