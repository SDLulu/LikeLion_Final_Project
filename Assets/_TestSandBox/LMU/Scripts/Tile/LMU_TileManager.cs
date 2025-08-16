using System.Collections.Generic;
using Fusion;
using UnityEditor;
using UnityEngine;

public partial class PMK_TileRogic : NetworkBehaviour
{
    public void RunTestMode()
    {
        if (IsStageTestNetwork && Runner.IsServer && HasStateAuthority)
        {
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
                   ResetBoosMap();
                   Create_Map("B", bossStage, 0, 0);
                   bossStage++;
                   return;
                }
                else if (stageInfo.IsBossStage == 0)
                {
                   Debug.Log("일반 스테이지 로드");
                   RPC_ResetMap();
                }
            };
            return;
        }
    }
}
