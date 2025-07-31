using System.Collections.Generic;
using Fusion;
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
                Debug.Log($"스테이지 정보: {stageInfo.CurrentStage} | {stageInfo.CurrentStageName}");
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
                RPC_ResetMap();
            };
            return;
        }
    }
}
