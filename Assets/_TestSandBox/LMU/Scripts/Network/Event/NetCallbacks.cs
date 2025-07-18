using System;
using System.Collections.Generic;
using Fusion;
using Fusion.Sockets;
using UnityEngine;

public class NetCallbacks : MonoBehaviour
{
    [Header("디버그용")]
    [SerializeField] private GameMode localGameMode;
    [SerializeField] private PlayerManager hostPlayerManage;
    [SerializeField] private GameStates gameStates;
    public PlayerRef LocalPlayer {get; private set;}

    private byte[] connectionToken;
    private void Awake()
    {
        connectionToken = ConnectionTokens.NewToken();
        LobbyManager.Inst.OnExitButtonClicked -= OnPlayerLeftAction;
        LobbyManager.Inst.OnExitButtonClicked += OnPlayerLeftAction;
    }

    private void OnDestroy()
    {
        if (LobbyManager.HasInstance)
            LobbyManager.Inst.RemoveAction();
        hostPlayerManage = null;
    }

    /// <summary>
    /// 게임 시작
    /// </summary>
    private async Awaitable StartGameAsync(NetworkRunner runner, 
                                            GameMode mode, 
                                            string roomName,
                                            SceneRef sceneRef,
                                            byte[] connectionToken = default,
                                            Action<NetworkRunner> migrationAction = default,
                                            HostMigrationToken token = default)
    {
        var startGameArgs = new StartGameArgs()
        {
            GameMode = mode,
            SessionName = roomName,
            PlayerCount = 4,
            SceneManager = LevelManager.Inst,
            ObjectProvider = NetObjProvider.Inst,
            ConnectionToken = connectionToken,
            HostMigrationToken = token,
            HostMigrationResume = migrationAction,
            Scene = sceneRef,
        };

        await runner.StartGame(startGameArgs);
        await Awaitable.NextFrameAsync();
    }
    
    /// <summary>
    /// 로비 입장
    /// </summary>
    public async Awaitable JoinOrCreateLobby(GameMode mode = GameMode.AutoHostOrClient,
                                            string roomName = "TestRoom",
                                            Action OnEnterLobby = default)
    {
        var netRunner = LobbyManager.Inst.NetRunner;

        if (netRunner == null)
        {
            Debug.LogError("네트워크 러너가 존재하지 않습니다.");
            return;
        }

        netRunner.AddCallbacks(NetworkEventSystem.Inst);
        netRunner.ProvideInput = true;

        var sceneRef = SceneRef.FromIndex(UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex);
        await StartGameAsync(netRunner, mode, roomName, sceneRef, this.connectionToken);
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

    public void OnPlayerLeftAction()
    {
        if (this == null || this.Equals(null))
        {
            Debug.LogError(nameof(OnPlayerLeftAction));
            GameObject.Destroy(this);
            return;
        }
        
        var runner = LobbyManager.Inst.NetRunner;
        if (runner == null || runner.IsRunning == false)
        {
            Debug.LogError("NetworkRunner가 실행 중이지 않습니다.");
            UI_Controller.Inst.ActiveTitleUI();
            return;
        }

        runner.Shutdown();
        UI_Controller.Inst.ActiveTitleUI();
    }
}


