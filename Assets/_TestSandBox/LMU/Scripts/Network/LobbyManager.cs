using System;
using Fusion;
using LMCore;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LobbyManager : BaseManager<LobbyManager>
{
    [Header("디버그용")]
    [SerializeField] private GameMode localGameMode;
    [SerializeField] private PlayerManager hostPlayerManage;
    [SerializeField] private GameStates gameStates;
    public PlayerRef LocalPlayer { get; private set; }

    private byte[] connectionToken;
    private void Awake()
    {
        connectionToken = ConnectionTokens.NewToken();
    }

    private void OnDestroy()
    {
        hostPlayerManage = null;
    }


    [SerializeField] private NetworkRunner netRunner;
    public NetworkRunner NetRunner
    {
        get
        {
            if (netRunner == null)
            {
                netRunner = FindAnyObjectByType<NetworkRunner>();
            }

            if (netRunner == null)
            {
                var obj = Resources.Load<GameObject>("Prefabs/LobbyBatchModule/@NetworkRunner");
                netRunner = Instantiate(obj).GetComponent<NetworkRunner>();
            }

            return netRunner;
        }
    }

    /// <summary>
    /// 외부에서 강제로 Runner 설정
    /// </summary>
    public NetworkRunner SetForcingRunner(string runnerID, INetworkRunnerCallbacks callbacks)
    {
        var obj = Resources.Load<GameObject>("Prefabs/LobbyBatchModule/@NetworkRunner");
        var runner = Instantiate(obj).GetComponent<NetworkRunner>();
        runner.name += $"_{runnerID}";
        runner.AddCallbacks(callbacks);
        runner.ProvideInput = true;
        netRunner = runner;
        return runner;
    }

    /// <summary>
    /// 게임 시작
    /// </summary>
    private async Awaitable StartGameAsync(NetworkRunner runner,
                                            GameMode mode,
                                            string roomName,
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
        try
        {
            await Fader.Inst.FadeOutAsync(Color.black, 1.0f);
            if (NetRunner == null)
            {
                Debug.LogError("네트워크 러너가 존재하지 않습니다.");
                return;
            }

            NetRunner.AddCallbacks(NetworkEventSystem.Inst);
            NetRunner.ProvideInput = true;
            await LocalSceneManager.Inst.LoadSceneAsync("DevLobby", LoadSceneMode.Additive, true);
            var startGameAwait = StartGameAsync(NetRunner, mode, roomName, this.connectionToken);
            OnEnterLobby?.Invoke();

            await startGameAwait;
            Debug.Log($"방에 입장함 {roomName}");
            await Awaitable.NextFrameAsync();
            await Fader.Inst.FadeInAsync(Color.black, 1.0f);
        }
        catch (System.Exception ex)
        {
            Debug.LogError(ex);
        }
    }

    /// <summary>
    /// 게임 종료 / 로비이동
    /// </summary>
    public async Awaitable LeaveGame()
    {
        try
        {
            await Fader.Inst.FadeOutAsync();

            var runner = LobbyManager.Inst.NetRunner;
            if (runner == null || runner.IsRunning == false)
            {
                Debug.LogError("NetworkRunner가 실행 중이지 않습니다.");
                UI_Controller.Inst.ActiveTitleUI();
                return;
            }

            await runner.Shutdown(true);

            // 타이틀씬을 제외한 모든 씬을 UnLoad
            var scenes = LocalSceneManager.Inst.GetAllLoadedScenes();
            Awaitable waitScene1 = default;
            Awaitable waitScene2 = default;
            foreach (var scene in scenes)
            {
                if (scene.name == "DevLobby")
                {
                    waitScene1 = LocalSceneManager.Inst.UnloadSceneAsync(scene.name);
                }
                else if (scene.name == "DevGame")
                {
                    waitScene2 = LocalSceneManager.Inst.UnloadSceneAsync(scene.name);
                }
            }
            UI_Controller.Inst.ActiveTitleUI();

            await waitScene1;
            await waitScene2;
            await Fader.Inst.FadeInAsync();
        }
        catch (Exception e)
        {
            Debug.LogError(e);
        }
    }
}
