using System;
using System.Collections.Generic;
using Fusion;
using Fusion.Sockets;
using UnityEngine;

public class NetRunner : MonoBehaviour, INetworkRunnerCallbacks
{
    [SerializeField] private GameObject gameStatesPrefab;
    [SerializeField] private GameObject playerMPrefab;

    [Header("디버그용")]
    [SerializeField] private GameMode localGameMode;
    [SerializeField] private PlayerManager hostPlayerManage;
    [SerializeField] private GameStates gameStates;

    private void OnDestroy()
    {
        hostPlayerManage = null;
        OnSceneLoadDoneAction = null;
    }


    public PlayerRef LocalPlayer {get; private set;}

    
    /// <summary>
    /// 로비 입장
    /// </summary>
    public async Awaitable JoinOrCreateLobby(GameMode mode = GameMode.AutoHostOrClient,
                                        string roomName = "TestRoom",
                                        Action OnEnterLobby = default)
    {
        var netRunner = GetComponent<NetworkRunner>();

        if (netRunner == null)
        {
            Debug.LogError("네트워크 러너가 존재하지 않습니다.");
            return;
        }

        netRunner.AddCallbacks(this);
        netRunner.ProvideInput = true;

       var startGameArgs = new StartGameArgs()
       {
           GameMode = mode,                   
           SessionName = roomName,       
           PlayerCount = 4,      
           SceneManager = LevelManager.Inst,
       };

        var startGameTask = netRunner.StartGame(startGameArgs);
        await startGameTask;
        OnEnterLobby?.Invoke();
       
       Debug.Log($"방에 입장함 {roomName}");
       await Awaitable.NextFrameAsync();
    }

    /// <summary>
    /// 네트워크 접속해제
    /// </summary>
    public void Shutdown(Action OnShutdown = default)
    {
        var netRunner = GetComponent<NetworkRunner>();
        netRunner.Shutdown();
        OnShutdown?.Invoke();
    }


    /// <summary>
    /// 플레이어 입장 및 생성
    /// </summary>
    public async void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        Debug.Log($"플레이어 {player} 입장!");
        if (runner.IsServer)
        {
            LocalPlayer = player;
            localGameMode = runner.GameMode;
            
            // Late Join 처리: 게임이 이미 시작되었는지 확인
            bool isGameInProgress = gameStates != null && 
                                   gameStates.StateMachine != null && 
                                   gameStates.StateMachine.ActiveState != null &&
                                   !(gameStates.StateMachine.ActiveState is LobbyState);
            
            // 호스트인경우, 객체 생성후 네트워크 등록까지 대기
            if (runner.GameMode == GameMode.Host && hostPlayerManage == null)
            {
                var playerPrefab = GlobalSetting.Inst.PlayerPrefab;
                await runner.SpawnAsync(playerPrefab, Vector3.zero, Quaternion.identity, player,
                onCompleted: (NetworkSpawnOp obj) =>
                {
                    var gameManagerObj = runner.Spawn(playerMPrefab, Vector3.zero, Quaternion.identity, player);
                    hostPlayerManage = gameManagerObj.GetComponent<PlayerManager>();

                    var gameStatesObj = runner.Spawn(gameStatesPrefab, Vector3.zero, Quaternion.identity, player);
                    gameStates = gameStatesObj.GetComponent<GameStates>();
                });
            }
            // 클라이언트인 경우
            else
            {
                var playerPrefab = GlobalSetting.Inst.PlayerPrefab;
                var spawnedPlayer = runner.Spawn(playerPrefab, Vector3.zero, Quaternion.identity, player);
                
                // Late Join 처리: 게임이 진행 중이면 현재 게임 상태에 맞춰 플레이어 동기화
                 if (isGameInProgress)
                 {
                     Debug.Log($"Late Join 감지: 플레이어 {player}가 게임 진행 중에 입장했습니다.");
                     
                     // 게임 진행 중이면 현재 게임 씬으로 이동
                     _ = HandleLateJoinPlayer(runner, player, spawnedPlayer);
                 }
            }
        }
    }

    /// <summary>
    /// Late Join 플레이어 처리
    /// </summary>
    private async Awaitable HandleLateJoinPlayer(NetworkRunner runner, PlayerRef player, NetworkObject spawnedPlayer)
    {
        // 1. 현재 게임 상태 확인
        var currentState = gameStates.StateMachine.ActiveState;
        
        // 2. 게임 씬이 로드되었는지 확인하고 플레이어를 해당 씬으로 이동
        if (currentState is GameStageWaitingState || 
            currentState is GameStagePlayingState ||
            currentState is GameStageTransitionState)
        {
            // 게임 씬으로 플레이어 이동
            var gameSceneObj = GameObject.Find("GameScene");
            if (gameSceneObj != null)
            {
                runner.MoveGameObjectToSameScene(spawnedPlayer.gameObject, gameSceneObj);
                
                // 플레이어 위치 설정
                var teleporter = spawnedPlayer.GetComponent<PlayerStageController>();
                if (teleporter != null)
                {
                    teleporter.SetPosition(new Vector2(0, 0));
                }
            }
        }
        
        // 3. 플레이어 상태를 게임 진행 상태에 맞춰 설정
         var playerData = spawnedPlayer.GetComponent<PlayerData>();
         if (playerData != null)
         {
             // Late Join 플레이어는 자동으로 준비 상태로 설정 (서버에서만 가능)
             playerData.SetReadyState(true);
             
             // 게임이 이미 시작되었으면 플레이어 상태를 활성화
             if (currentState is GameStagePlayingState)
             {
                 // 플레이어를 게임 플레이 상태로 설정
                 Debug.Log($"Late Join 플레이어 {player}를 게임 플레이 상태로 설정합니다.");
             }
         }
        
        Debug.Log($"Late Join 처리 완료: 플레이어 {player}");
    }

    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
    {
    }

    public void OnConnectedToServer(NetworkRunner runner)
    {
    }

    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason)
    {
    }

    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token)
    {
    }

    public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data)
    {
    }

    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason)
    {
    }

    public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken)
    {
    }

    public void OnInput(NetworkRunner runner, NetworkInput input)
    {
    }

    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input)
    {
    }

    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player)
    {
    }

    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player)
    {
    }


    public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress)
    {
    }

    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data)
    {
    }

    

    public Action<string> OnSceneLoadDoneAction;
    public void OnSceneLoadDone(NetworkRunner runner)
    {
        var sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        OnSceneLoadDoneAction?.Invoke(sceneName);
    }

    public void OnSceneLoadStart(NetworkRunner runner)
    {
    }

    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList)
    {
    }

    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason)
    {
    }

    public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message)
    {
    }
}
