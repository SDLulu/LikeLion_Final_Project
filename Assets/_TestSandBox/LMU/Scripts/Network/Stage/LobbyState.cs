using System;
using System.Collections.Generic;
using Fusion;
using UnityEngine;

public class LobbyState : BaseStateBehaviour
{
    public override E_StateName StateName => E_StateName.LobbyState;

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

    protected override void OnEnterState()
    {
        Debug.Log("LobbyState 진입");
        PlayerM.RPC_MoveToLobbyScene();
        LobbyUI_Manager.Inst.ActiveLobbyOnLineUI();
        LobbyUI_Manager.Inst.UITitle.gameObject.SetActive(false);

        Vector3 targetPos = GlobalSetting.Inst.LobbySpawnPos;
        var players = PlayerM.GetPlayers();
        foreach (var player in players)
        {
            var stageController = player.Value.GetComponent<PlayerStageController>();
            stageController.SetPosition(targetPos);
        }
    }

    protected override async void OnFixedUpdate()
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
            GameStates.Inst.DelayForceActiveState<GameStageWaitingState>();
    }

    protected override void OnExitState()
    {
        Debug.Log("LobbyState 퇴장");
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
        UIEventSystem.Inst.TriggerGameUIActive(true);
        LobbyUI_Manager.Inst.DeactiveAllLobbyUI();
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

            var gameScenePath = GlobalSetting.Inst.GameScenePath;
            await LevelManager.LoadSceneAsync(
                gameScenePath,
                UnityEngine.SceneManagement.LoadSceneMode.Additive,
                onLoadComplete: () =>
                {
                    Debug.Log("게임 씬 로드 완료");
                    _isGameSceneLoading = false;
                    _isInGame = true;
                    _isGameSceneLoaded = true;

                    // 서버가 세션을 '게임 중'으로 표시하여 랜덤 매치 대상에서 제외
                    var sessionProperties = new Dictionary<string, SessionProperty>();
                    sessionProperties["InGame"] = true;
                    Runner.SessionInfo.UpdateCustomProperties(sessionProperties);
                });

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
