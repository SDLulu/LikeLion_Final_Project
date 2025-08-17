using System.Collections.Generic;
using Fusion;
using UnityEditor;
using UnityEngine;

public partial class PMK_TileRogic : NetworkBehaviour
{
    public bool isTestMode = false;

    /// <summary>
    /// 테스트 모드를 위한 스테이지 로드 함수 구독
    /// </summary>
    /// <returns>네트워크 씬로드 구독 성공 여부를 반환</returns>
    public bool RunTestMode()
    {
        if (IsStageTestNetwork && Runner.IsServer && HasStateAuthority)
        {
            isTestMode = true;
            Debug.Log("구독수행됨");
            NetworkEventSystem.Inst.OnStageLoadDoneEvent += (stageInfo) =>
            {
                Debug.Log($"스테이지 정보: {stageInfo.CurrentStage} | {stageInfo.CurrentStageName} | {stageInfo.IsBossStage}");

                LoadMapPrefabsAutomatically(stageInfo.CurrentStage);
                SaveMapPos();
                if (mapPrefabDict == null)
                {
                    mapPrefabDict = new Dictionary<string, GameObject[]>();

                    foreach (var set in mapPrefabSets)
                    {
                        if (!mapPrefabDict.ContainsKey(set.mapType))
                            mapPrefabDict.Add(set.mapType, set.prefabs);
                    }
                }

                //스테이지 로더 추가 할 곳
                if (stageInfo.IsBossStage == 1)
                {
                   Debug.Log("보스 스테이지 로드");
                    RPC_ResetBoosMap();
                   // 보스 맵 인덱스를 안전하게 순환
                   if (mapPrefabDict != null && mapPrefabDict.TryGetValue("B", out var bossPrefabs) && bossPrefabs != null && bossPrefabs.Length > 0)
                   {
                       int safeIndex = bossStage % bossPrefabs.Length;
                       Create_Map("B", safeIndex, 0, 0);
                       bossStage++;
                   }
                   else
                   {
                       Debug.LogError("보스 맵 프리팹을 찾을 수 없거나 비어있습니다. Resources/Maps/*Stage 아래의 'B' 타입 네이밍을 확인하세요.");
                   }
                   return;
                }
                else if (stageInfo.IsBossStage == 0)
                {
                   Debug.Log("일반 스테이지 로드");
                   RPC_ResetMap();
                }
            };
            return true;
        }
        return false;
    }
}
