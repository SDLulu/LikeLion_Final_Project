using System;
using Fusion;
using LMCore;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LobbyManager : BaseManager<LobbyManager>
{
    [Header("디버그용")]
    [SerializeField] private GameMode localGameMode;
    [SerializeField] private string localRoomName;

    public GameMode LocalGameMode => localGameMode;
    public string LocalRoomName => localRoomName;
    public PlayerRef LocalPlayer { get; private set; }

    public Func<Awaitable> OnEnterLobbyAction { get; set; }
    private byte[] connectionToken;
    protected override void Awake()
    {
        connectionToken = ConnectionTokens.NewToken();
        localGameMode = default;
        localRoomName = default;
    }

    private void OnDestroy()
    {
        connectionToken = default;
        localGameMode = default;
        localRoomName = default;
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
    private async Awaitable<StartGameResult> StartGameAsync(NetworkRunner runner,
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

        var ret = await runner.StartGame(startGameArgs);
        await Awaitable.NextFrameAsync();
        return ret;
    }

    [field: SerializeField] public bool IsSoloPlay {get; private set;}
    /// <summary>
    /// 로비 입장
    /// </summary>
    public async Awaitable JoinOrCreateLobby(bool isSoloPlay = false, GameMode mode = GameMode.AutoHostOrClient,
                                            string roomName = "TestRoom",
                                            Action OnEnterLobby = default)
    {
        try
        {
            IsSoloPlay = isSoloPlay;
            
            await Fader.Inst.ShowLoadingAsync();
            if (NetRunner == null)
            {
                Debug.LogError("네트워크 러너가 존재하지 않습니다.");
                return;
            }

            NetRunner.AddCallbacks(NetworkEventSystem.Inst);
            NetRunner.ProvideInput = true;
            
            // 게임시작결과에 따른 처리
            var startGameResult = await StartGameAsync(NetRunner, mode, roomName, this.connectionToken);
            await Fader.Inst.HideLoadingAsync();
            if (startGameResult.Ok)
            {
                await Fader.Inst.WideFadeOutAsync();
                await LocalSceneManager.Inst.LoadSceneAsync("DevLobby", LoadSceneMode.Additive, true);
                OnEnterLobby?.Invoke();
            }
            else
            {
                Debug.LogError($"게임 시작 실패: {startGameResult.ShutdownReason}");
                return;
            }


            localGameMode = mode;
            localRoomName = roomName;

            Debug.Log($"방에 입장함 {roomName}");
            await Awaitable.NextFrameAsync();
            await Fader.Inst.WideFadeInAsync();
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning("JoinOrCreateLobby - 로비입장중 오류");
            Debug.LogError(ex);
            await LeaveGame();
        }
    }

    /// <summary>
    /// 게임 종료 / 로비이동
    /// </summary>
    public async Awaitable LeaveGame()
    {
        try
        {
            await Fader.Inst.WideFadeOutAsync(1.5f);

            var runner = LobbyManager.Inst.NetRunner;
            if (runner == null || runner.IsRunning == false)
            {
                Debug.LogError("NetworkRunner가 실행 중이지 않습니다.");
                LobbyUI_Manager.Inst.ActiveTitleUI();
                await Fader.Inst.WideFadeInAsync(1.5f);
                return;
            }

            await runner.Shutdown(true);

            // 타이틀씬을 제외한 모든 씬을 UnLoad
            var scenes = LocalSceneManager.Inst.GetAllLoadedScenes();
            foreach (var scene in scenes)
            {
                if (scene.name == "DevLobby")
                {
                    _ = LocalSceneManager.Inst.UnloadSceneAsync(scene.name);
                }
                else if (scene.name == "DevGame")
                {
                    _ = LocalSceneManager.Inst.UnloadSceneAsync(scene.name);
                }
            }
            LobbyUI_Manager.Inst.ActiveTitleUI();

            localGameMode = default;
            localRoomName = default;
            await Fader.Inst.WideFadeInAsync(1.5f);
        }
        catch (Exception e)
        {
            Debug.LogWarning("LeaveGame - 게임종료중 오류");
            Debug.LogError(e);
        }
    }
}
