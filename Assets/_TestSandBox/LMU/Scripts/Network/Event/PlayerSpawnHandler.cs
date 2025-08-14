using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Fusion;
using UnityEngine;

public class PlayerSpawnHandler : MonoBehaviour
{
    [Header("디버그용")]
    [SerializeField] private GameMode localGameMode;
    [SerializeField] private PlayerManager hostPlayerManage;
    [SerializeField] private GameStates gameStates;
    [SerializeField] private ChatManager chatManager;
    [SerializeField] private AltarManager altarManager;
    

    private const string PLAYER_MANAGER_PREFAB_PATH = "Prefabs/PlayerManager";
    private const string GAME_STATES_PREFAB_PATH = "Prefabs/GameStates";
    private const string CHAT_MANAGER_PREFAB_PATH = "Prefabs/ChatManager";
    
    public GameObject PlayerManagerPrefab => Resources.Load<GameObject>(PLAYER_MANAGER_PREFAB_PATH);
    public GameObject GameStatesPrefab => Resources.Load<GameObject>(GAME_STATES_PREFAB_PATH);
    public GameObject ChatManagerPrefab => Resources.Load<GameObject>(CHAT_MANAGER_PREFAB_PATH);
    public GameObject altarManangerPrefab;

    private void OnDestroy()
    {
        hostPlayerManage = null;
        gameStates = null;
        chatManager = null;
        altarManager = null;
    }

    #region 플레이어 입장 및 퇴장

    /// <summary>
    /// 로비씬로드 대기
    /// </summary>
    public async Awaitable WaitForLobbySceneLoaded()
    {
        while (true)
        {
            await Awaitable.NextFrameAsync();
            if (LocalSceneManager.Inst.GetActiveScene().name == GlobalSetting.Inst.LobbyScenePath)
            {
                break;
            }
        }
    }
    public async void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        Debug.Log($"플레이어 {player} 입장");
        await WaitForLobbySceneLoaded();

        localGameMode = runner.GameMode;
        if (runner.IsServer && runner.GameMode == GameMode.Host && hostPlayerManage == null)
        {
            OnHostPlayerJoin(runner, player);
        }
        else if (runner.IsServer)
        {
            OnClientPlayerJoin(runner, player);
        }
    }

    /// <summary>
    /// 호스트 입장 처리
    /// </summary>
    private void OnHostPlayerJoin(NetworkRunner runner, PlayerRef player)
    {
        Vector3 playerSpawnPos = GlobalSetting.Inst.GetRandomLobbySpawnPos();

        SpawnManagers(runner, player);
        var id = NetworkPrefabId.FromRaw(NetObjProvider.PLAYER);
        var spawnPlayer = runner.Spawn(id, playerSpawnPos, Quaternion.identity, player);
        runner.SetPlayerObject(player, spawnPlayer);
        hostPlayerManage.AddPlayer(player);
    }


    public void SpawnManagers(NetworkRunner runner, PlayerRef player)
    {
        // 기존 방식으로 스폰 (하위 호환성 유지)
        SpawnManagersLegacy(runner, player);
        
        // LobbySpawnManager를 통한 추가 스폰
        SpawnManagersFromLobbyManager(runner, player);
    }
    
    /// <summary>
    /// 기존 방식으로 매니저들을 스폰 (하위 호환성 유지)
    /// </summary>
    private void SpawnManagersLegacy(NetworkRunner runner, PlayerRef player)
    {
        var gameManagerObj = runner.Spawn(PlayerManagerPrefab, Vector3.zero, Quaternion.identity, player);
        hostPlayerManage = gameManagerObj.GetComponent<PlayerManager>();

        var chatManagerObj = runner.Spawn(ChatManagerPrefab, Vector3.zero, Quaternion.identity, player);
        chatManager = chatManagerObj.GetComponent<ChatManager>();

        var gameStatesObj = runner.Spawn(GameStatesPrefab, Vector3.zero, Quaternion.identity, player);
        gameStates = gameStatesObj.GetComponent<GameStates>();

        var altarManagerObj = runner.Spawn(altarManangerPrefab, Vector3.zero, Quaternion.identity);
        altarManager = altarManagerObj.GetComponent<AltarManager>();
    }
    
    /// <summary>
    /// LobbySpawnManager에서 설정된 정보를 참고하여 프리팹들을 스폰
    /// </summary>
    private void SpawnManagersFromLobbyManager(NetworkRunner runner, PlayerRef player)
    {
        var lobbySpawnManager = FindObjectOfType<LobbySpawnManager>();
        if (lobbySpawnManager == null) return;
        
        foreach (var spawnData in lobbySpawnManager.SpawnDataList)
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

    /// <summary>
    /// 클라이언트 입장처리
    /// </summary>
    private void OnClientPlayerJoin(NetworkRunner runner, PlayerRef player)
    {
        Vector3 playerSpawnPos = GlobalSetting.Inst.GetRandomLobbySpawnPos();

        var id = NetworkPrefabId.FromRaw(NetObjProvider.PLAYER);
        var spawnedPlayer = runner.Spawn(id, playerSpawnPos, Quaternion.identity, player);
        runner.SetPlayerObject(player, spawnedPlayer);
        hostPlayerManage.AddPlayer(player);
    }


    /// <summary>
    /// 게임이 진행 중인지 확인
    /// </summary>
    private bool IsGameInProgress()
    {
        return gameStates != null &&
               gameStates.StateMachine != null &&
               gameStates.StateMachine.ActiveState != null;
    }

    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
    {
        Debug.Log($"플레이어 {player} 퇴장");

        if (runner.IsServer)
        {
            OnEntityLeftAsync(runner, player);
        }
    }

    private void OnEntityLeftAsync(NetworkRunner runner, PlayerRef player)
    {
        if (runner.IsServer == false)
            return;

        var playerObj = runner.GetPlayerObject(player);

        if (PlayerManager.HasInstance)
            PlayerManager.Inst.TryRemovePlayer(player);

        if (playerObj == null)
        {
            Debug.LogError($"플레이어 {player}의 오브젝트가 존재하지 않습니다.");
            return;
        }

        runner.Despawn(playerObj);
    }
    #endregion

}
