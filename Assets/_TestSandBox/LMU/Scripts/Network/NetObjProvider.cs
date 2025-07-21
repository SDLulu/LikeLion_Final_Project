using Fusion;
using LMCore;
using UnityEngine;

/// <summary>
/// 프리팹 Baker 클래스
/// </summary>
public class NetObjProvider : NetworkObjectProviderDefault
{
    public const int PLAYER = 100000;
    private static NetworkObjectBaker _baker;
    private static NetworkObjectBaker Baker => _baker ??= new NetworkObjectBaker();
    public static NetObjProvider Inst => BaseManager<NetObjProvider>.Inst;
    
    public override NetworkObjectAcquireResult AcquirePrefabInstance(NetworkRunner runner, in NetworkPrefabAcquireContext context, out NetworkObject result)
    {
        if (context.PrefabId.RawValue == PLAYER)
        {
            var prefab = GlobalSetting.Inst.PlayerPrefab;
            var obj = GameObject.Instantiate(prefab)
                    .AddComponent<PlayerStageController>().gameObject
                    .AddComponent<PlayerData>().gameObject;
            
            Baker.Bake(obj);
            
            if (context.DontDestroyOnLoad)
                runner.MakeDontDestroyOnLoad(obj);
            else
                runner.MoveToRunnerScene(obj);
            
            result = obj.GetComponent<NetworkObject>();
            return NetworkObjectAcquireResult.Success;
        }
        
        return base.AcquirePrefabInstance(runner, context, out result);
    }
} 