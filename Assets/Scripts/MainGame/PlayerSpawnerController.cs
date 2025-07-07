using System.Collections.Generic;
using Fusion;
using UnityEngine;
using Random = UnityEngine.Random;

public class PlayerSpawnerController : NetworkBehaviour, IPlayerJoined, IPlayerLeft
{
    [SerializeField] private NetworkPrefabRef playerNetworkPrefab = NetworkPrefabRef.Empty;
    [SerializeField] private Transform[] spawnPoints;
    private Dictionary<PlayerRef, NetworkObject> currentSpawnedPlayers = new Dictionary<PlayerRef, NetworkObject>();
    
    private void Awake()
    {
        if (GlobalManagers.Instance != null)
        {
            GlobalManagers.Instance.PlayerSpawnerController = this;
        }
    }
    
    public void AddToEntry(PlayerRef player, NetworkObject obj)
    {
        currentSpawnedPlayers.TryAdd(player, obj);
    }

    //nondeterministic
    public Vector2 GetRandomSpawnPoint()
    {
        var index = Random.Range(0, spawnPoints.Length - 1);
        return spawnPoints[index].position;
    }

    private void SpawnPlayer(PlayerRef playerRef)
    {
        if (Runner.IsServer)
        {
            var index = playerRef.AsIndex % spawnPoints.Length;
            var spawnPoint = spawnPoints[index].transform.position;
            var playerObject = Runner.Spawn(playerNetworkPrefab, spawnPoint, Quaternion.identity, playerRef);
            
            Runner.SetPlayerObject(playerRef, playerObject);
        }
    }

    private void DespawnPlayer(PlayerRef playerRef)
    {
        if (Runner.IsServer)
        {
            if (currentSpawnedPlayers.TryGetValue(playerRef, out var playerNetworkObject))
            {
                Runner.Despawn(playerNetworkObject);
            }
            
            //Reset player object
            Runner.SetPlayerObject(playerRef, null);
        }
    }
    
    public void PlayerJoined(PlayerRef player)
    {
        SpawnPlayer(player);
    }

    public void PlayerLeft(PlayerRef player)
    {
        DespawnPlayer(player);
    }
}
