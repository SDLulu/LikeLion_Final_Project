using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Fusion;
using Unity.Mathematics;
using UnityEngine;

public class ObjectPoolingManager : MonoBehaviour, INetworkObjectProvider
{
    //The key is the PREFAB it self and the VALUE (the list) are the actual network gameobjects that had been spawned
    //For example bullet PREFAB is the key 
    //And the bullet spawned object ("bullet (clone))" is part of the list itself (the value)
    private Dictionary<INetworkPrefabSource, List<NetworkObject>> prefabsThatHadBeenInstantiated = new();

    private void Start()
    {
        if (GlobalManagers.Instance != null)
        {
            GlobalManagers.Instance.ObjectPoolingManager = this;
        }
    }

    //Called once runner.spawn is called
    public NetworkObjectAcquireResult AcquirePrefabInstance(NetworkRunner runner, in NetworkPrefabAcquireContext context, out NetworkObject result)
    {
        NetworkObject networkObject = null;
        NetworkPrefabId prefabID = context.PrefabId;
        INetworkPrefabSource prefabSource = NetworkProjectConfig.Global.PrefabTable.GetSource(prefabID);
        prefabsThatHadBeenInstantiated.TryGetValue(prefabSource, out var networkObjects);

        bool foundMatch = false;
        if (networkObjects?.Count > 0)
        {
            foreach (var item in networkObjects)
            {
                if (item != null && item.gameObject.activeSelf == false)
                {
                    //todo object pooling aka recycle 
                    networkObject = item;
    
                    foundMatch = true;
                    break;
                }
            }
        }

        //Stays false when a complete new data that is not in our dic OR
        //When the function is getting called too fast and no object is ready to be recycled
        if (foundMatch == false)
        {
            networkObject = CreateObjectInstance(prefabSource);
        }

        result = networkObject;
        return NetworkObjectAcquireResult.Success;
    }

    private NetworkObject CreateObjectInstance(INetworkPrefabSource prefab)
    {
        prefab.Acquire(synchronous: true);
        
        // Check if it's already completed
        if (!prefab.IsCompleted)
        {
            Debug.LogError($"Prefab {prefab.Description} not immediately available");
            // You might want to handle this case differently depending on your needs
        }

            
        var obj = Instantiate(prefab.WaitForResult(), Vector3.zero, Quaternion.identity);

        //If it contains, we shall add it as a child to our prefab instance
        if (prefabsThatHadBeenInstantiated.TryGetValue(prefab, out var instanceData))
        {
            instanceData.Add(obj);
        }
        //If it's NOT in our list, we shall create data from scratch and add it 
        else
        {
            var list = new List<NetworkObject> { obj };
            prefabsThatHadBeenInstantiated.Add(prefab, list);
        }

        return obj; 
    }
    
    //Called once runner.despawn is called
    public void ReleaseInstance(NetworkRunner runner, in NetworkObjectReleaseContext context)
    {
        context.Object.gameObject.SetActive(false);
    }

    public NetworkPrefabId GetPrefabId(NetworkRunner runner, NetworkObjectGuid prefabGuid)
    {
        return runner.Prefabs.GetId(prefabGuid);
    }

    public void RemoveNetworkObjectFromDic(NetworkObject obj)
    {
        if (prefabsThatHadBeenInstantiated.Count > 0)
        {
            foreach (var item in prefabsThatHadBeenInstantiated)
            {
                foreach (var networkObject in item.Value.Where(networkObject => networkObject == obj))
                {
                    item.Value.Remove(networkObject);
                    break;
                }
            }
        }
    }
}















