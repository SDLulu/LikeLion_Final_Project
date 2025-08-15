using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Fusion;
using UnityEngine;

public class PlayerSpawnHandler : MonoBehaviour
{
    [Header("인스펙터 참조")]
    public GameObject _altarManangerPrefab;

    [Header("디버그용")]
    [SerializeField] private GameMode localGameMode;
    [SerializeField] private PlayerManager _hostPlayerManage;
    [SerializeField] private GameStates _gameStates;
    [SerializeField] private ChatManager _chatManager;
    [SerializeField] private AltarManager _altarManager;
    [SerializeField] private LobbySpawnManager _lobbySpawnManager;

    private const string PLAYER_MANAGER_PREFAB_PATH = "Prefabs/PlayerManager";
    private const string GAME_STATES_PREFAB_PATH = "Prefabs/GameStates";
    private const string CHAT_MANAGER_PREFAB_PATH = "Prefabs/ChatManager";
    
    public GameObject PlayerManagerPrefab => Resources.Load<GameObject>(PLAYER_MANAGER_PREFAB_PATH);
    public GameObject GameStatesPrefab => Resources.Load<GameObject>(GAME_STATES_PREFAB_PATH);
    public GameObject ChatManagerPrefab => Resources.Load<GameObject>(CHAT_MANAGER_PREFAB_PATH);

    private void OnDestroy()
    {
        _hostPlayerManage = null;
        _gameStates = null;
        _chatManager = null;
        _altarManager = null;
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
        if (runner.IsServer && runner.GameMode == GameMode.Host && _hostPlayerManage == null)
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
        _hostPlayerManage.AddPlayer(player);
    }

    public void SpawnManagers(NetworkRunner runner, PlayerRef player)
    {
        var gameManagerObj = runner.Spawn(PlayerManagerPrefab, Vector3.zero, Quaternion.identity, player);
        _hostPlayerManage = gameManagerObj.GetComponent<PlayerManager>();

        var chatManagerObj = runner.Spawn(ChatManagerPrefab, Vector3.zero, Quaternion.identity, player);
        _chatManager = chatManagerObj.GetComponent<ChatManager>();

        var gameStatesObj = runner.Spawn(GameStatesPrefab, Vector3.zero, Quaternion.identity, player);
        _gameStates = gameStatesObj.GetComponent<GameStates>();

        var altarManagerObj = runner.Spawn(_altarManangerPrefab, Vector3.zero, Quaternion.identity);
        _altarManager = altarManagerObj.GetComponent<AltarManager>();

        // 로비씬에 종속적인 객체스폰
        _lobbySpawnManager = FindAnyObjectByType<LobbySpawnManager>();
        _lobbySpawnManager.Spawn(runner);
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
        _hostPlayerManage.AddPlayer(player);
    }

    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
    {
        Debug.Log($"플레이어 {player} 퇴장");

        if (runner.IsServer)
        {
            OnLeft(runner, player);
        }
    }

    private void OnLeft(NetworkRunner runner, PlayerRef player)
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
