using System;
using System.Collections.Generic;
using Fusion;
using UnityEngine;

public class LobbyState : BaseStateBehaviour, IPlayerJoined
{
    public override E_StateName StateName => E_StateName.LobbyState;

    [Header("설정")]
    [SerializeField] private bool _isIntervalSoftReset = true;
    [SerializeField] private float _playerSoftResetInterval = 5.0f;

    [Header("디버그용")]
    [SerializeField] private int _minPlayersToStart = 2;
    [SerializeField] private bool _isInGame = false;
    [SerializeField] private bool _isGameSceneLoading = false;
    [SerializeField] private bool _isGameSceneLoaded = false;

    public int MinPlayersToStart => _minPlayersToStart = (LobbyManager.Inst.IsSoloPlay ? 1 : 2);
    public bool IsInGame => _isInGame;
    public bool IsGameSceneLoading => _isGameSceneLoading;
    public bool IsGameSceneLoaded => _isGameSceneLoaded;
    private Dictionary<PlayerRef, AwaitableCompletionSource> _fadingTCS = new();
    private TickTimer _playerSoftResetTimer;
    private static bool _isFirst = true;

    // 로비씬에 플레이어가 참가할때
    public void PlayerJoined(PlayerRef player)
    {
        if (Runner.IsServer)
        {
            PlayerM.RPC_MoveToLobbyScene_Target(player, GlobalSetting.Inst.LobbySpawnPos);
        }
    }

    protected override async void OnEnterState()
    {
        if (Runner.IsServer)
        {
            PlayerM.RPC_MoveToLobbyScene();
            PlayerM.SetPlayerPositions(GlobalSetting.Inst.LobbySpawnPos);

            _playerSoftResetTimer = TickTimer.None;
            _playerSoftResetInterval = 5.0f;
            if (_isFirst == false)
            {
                await Awaitable.WaitForSecondsAsync(3.0f);
                RPC_FadeInUI();
            }
            _isFirst = false;
        }

    }

    protected override void OnFixedUpdate()
    {
        if (Runner.IsServer == false)
            return;

        CheckStartGame();

        // 일정주기마다 플레이어 스탯정보 초기화
        if (_isIntervalSoftReset && _playerSoftResetTimer.ExpiredOrNotRunning(Runner))
        {
            PlayerM.SoftResetAllPlayers();
            _playerSoftResetTimer = TickTimer.CreateFromSeconds(Runner, _playerSoftResetInterval);
        }
    }

    private async void CheckStartGame()
    {
        // 조건을 만족하면 게임 시작 - 씬변경후 게임의 상태를 PlayingState로 변경
        bool startCondition = Runner.IsServer &&
                                PlayerM.GetPlayers().Count >= MinPlayersToStart &&
                                _isGameSceneLoading == false &&
                                _isInGame == false;

        if (startCondition == false)
            return;

        var result = await TryStartGameAsync(isStart: AreAllPlayersReady());
        if (result)
        {
            GameStates.Inst.DelayForceActiveState<GameStageWaitingState>();
        }
    }

    protected override void OnExitState()
    {
        _isInGame = false;
        _isGameSceneLoading = false;
        _isGameSceneLoaded = false;

        _fadingTCS.Clear();

        if (Runner.IsServer)
        {
            // 모든 플레이어의 준비 상태를 초기화
            var players = PlayerM.GetPlayerDatas();
            foreach (var p in players)
            {
                var playerData = p.Value;
                playerData.RPC_RequestToggleReady(false);
            }
        }
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        _fadingTCS.Clear();
    }


    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public async void RPC_FadeOutUI()
    {
        await Fader.FadeOutAsync(Color.black, 1.0f);
        RPC_FadeOutCompleted(Runner.LocalPlayer);
    }


    /// <summary>
    /// Note - 중간에 플레이어가 나가는 경우에 대한 예외처리를 하지않음
    /// </summary>
    public async Awaitable<bool> WaitForAllPlayerFading()
    {
        foreach (var player in _fadingTCS)
        {
            await player.Value.Awaitable;
        }
        return true;
    }

    /// <summary>
    /// 클라이언트에서 서버에게 FadeOut 완료 알림
    /// </summary>
    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_FadeOutCompleted(PlayerRef player)
    {
        _fadingTCS[player].SetResult();
    }


    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_FadeInUI()
    {
        UIEventSystem.Inst.TriggerGameUIActive(false);
        LobbyUI_Manager.Inst.ActiveLobbyOnLineUI();
        LobbyUI_Manager.Inst.UITitle.gameObject.SetActive(false);
        _ = Fader.FadeInAsync(Color.black, 1.0f);
    }

    #region 게임 시작
    /// <summary>
    /// 최소인원수 이상이면서 준비여부를 확인하는 함수 
    /// </summary>
    public bool AreAllPlayersReady()
    {
        int requiredPlayers = MinPlayersToStart;
        if (PlayerM.GetPlayers().Count < requiredPlayers)
            return false;

        foreach (var kvp in PlayerM.GetPlayers())
        {
            NetworkObject netObj = kvp.Value;
            if (netObj == null)
                continue;

            PlayerData pData = netObj.GetComponent<PlayerData>();
            if (pData == null)
                continue;

            if (pData.IsReady == false)
                return false;
        }

        return true;
    }


    public async Awaitable<bool> TryStartGameAsync(bool isStart = true)
    {
        // 준비 되지 않은경우
        if (isStart == false)
        {
            //Debug.Log("아직 준비되지 않은 플레이어가 있습니다.");
            return false;
        }

        // 클라이언트인 경우
        if (Runner.GameMode == GameMode.Client)
        {
            Debug.Log("클라이언트 접속완료");
            _isGameSceneLoading = false;
            _isInGame = true;
            _isGameSceneLoaded = true;
            PlayerM.RPC_MoveToGameScene();
            return false;
        }

        // 서버인 경우
        try
        {
            _isGameSceneLoading = true;
            _isGameSceneLoaded = false;
            _isInGame = false;

            _fadingTCS.Clear();
            foreach (var player in PlayerM.GetPlayers())
            {
                var playerRef = player.Key;
                _fadingTCS.Add(playerRef, new());
            }

            Debug.Log("모든 플레이어가 준비되었습니다!");

            // FadeOut 신호를 보내고 완료될때까지 서버는 대기
            // 씬로드 완료후 FadeIn은 GameStates(GameStagePlayingState) 에서 관리
            RPC_FadeOutUI();
            await WaitForAllPlayerFading();

            // 이미 로드되어 있으면 재로드 없이 이동만 수행
            var gameScenePath = GlobalSetting.Inst.GameScenePath;
            if (LocalSceneManager.Inst.IsSceneLoaded(gameScenePath) == false)
            {
                await LevelManager.LoadSceneAsync(
                    gameScenePath,
                    UnityEngine.SceneManagement.LoadSceneMode.Additive,
                    onLoadComplete: () =>
                    {
                        LobbyManager.Inst.UpdateSessionInfo(isInGame: true);
                    });
            }
            else
            {
                LobbyManager.Inst.UpdateSessionInfo(isInGame: true);
            }

            // 씬 로드 여부와 관계 없이 플레이어는 게임씬으로 이동
            PlayerM.RPC_MoveToGameScene();
            _isGameSceneLoading = false;
            _isGameSceneLoaded = true;
            _isInGame = true;
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"게임 시작 중 오류 발생: {e.Message}");
            return false;
        }
    }
    #endregion
}
