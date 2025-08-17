using System;
using System.Collections.Generic;
using System.Linq;
using Fusion;
using Fusion.Addons.FSM;
using LMCore;
using UnityEngine;

public class GameStageCompletedState : BaseStateBehaviour
{
    public override E_StateName StateName => E_StateName.CompletedState;

    [Networked] public bool IsCutSceneActive { get; set; } = false;

    [Header("설정")]
    [SerializeField, Range(10.0f, 15.0f)] private float _minWaitingTime = 15.0f;
    [SerializeField] private float _cutDuration = 2.0f;

    // 서버 - 백그라운드 작업진행 - 컷신 재생과 맵 로딩을 병렬실행 - 0: 컷신, 1: 맵 로딩
    private Dictionary<PlayerRef, List<Tuple<int, AwaitableCompletionSource>>> _bgTaskTCS;
    private TickTimer _minWaitingTimer = TickTimer.None;
    public int StageDataIndex { get; private set; } = -1;
    private bool _isStateActive = false;
    private bool _isCompleted = false;
    public override void Spawned()
    {
        if (Runner.IsServer)
        {
            // 첫번째 스테이지 로드는 WaitingState에서 진행하고, 이후 스테이지는 이 클래스에서 진행
            StageDataIndex = DataManager.Inst.StageData.First().Key + 1;
        }
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        _isStateActive = false;
        _bgTaskTCS?.Clear();
        _bgTaskTCS = null;
        
        // 클라이언트 TCS 정리
        if (_clientFadeOutTCS != null)
        {
            _clientFadeOutTCS.TrySetResult();
            _clientFadeOutTCS = null;
        }
        
        base.Despawned(runner, hasState);
    }

    protected override void OnEnterState()
    {
        _isStateActive = true;
        _isCompleted = false;
        if (Runner.IsServer)
        {
            GameStates.RPC_FadeOutBGM(this.Runner, false);
            PlayerM.SetPlayerPositions(new Vector3(-100.0f, -100.0f, 0.0f));
            var players = PlayerM.GetPlayers();
            InitTCS(players);
            _minWaitingTimer = TickTimer.CreateFromSeconds(Runner, _minWaitingTime);
            RPC_StartFadeOut();
        }
    }

    protected override void OnFixedUpdate()
    {
        if (Runner.IsServer && _isCompleted == false)
        {
            if (IsAllCompleted())
            {
                _isCompleted = true;
                Debug.Log("모든 플레이어가 컷신을 완료했습니다.");
                RPC_PostCutScene();
                Machine.ForceActivateState(Machine.GetState<GameStagePlayingState>());
            }
            else if (_minWaitingTimer.Expired(Runner))
            {
                _isCompleted = true;
                Debug.Log($"최대 대기시간 {_minWaitingTime}초가 초과되었습니다.");
                RPC_PostCutScene();
                Machine.ForceActivateState(Machine.GetState<GameStagePlayingState>());
            }
        }
    }

    /// <summary>
    /// Note - 주의: 화면은 어두운 상태로 유지 (FadeIn 호출 안함)
    /// </summary>
    protected override void OnExitState()
    {
        _isStateActive = false;
        _minWaitingTimer = TickTimer.None;
        _bgTaskTCS?.Clear();
        _bgTaskTCS = null;
        
        // 클라이언트 TCS 정리
        if (_clientFadeOutTCS != null)
        {
            _clientFadeOutTCS.TrySetResult();
            _clientFadeOutTCS = null;
        }
        
        StageDataIndex++;
        Debug.Log("다음 스테이지 인덱스 : " + StageDataIndex);
        base.OnExitState();
    }

    public void InitTCS(NetworkDictionary<PlayerRef, NetworkObject> players)
    {
        _bgTaskTCS = new();
        foreach (var player in players)
        {
            PlayerRef @ref = player.Key;
            _bgTaskTCS[@ref] = new List<Tuple<int, AwaitableCompletionSource>>()
                {
                    Tuple.Create(0, new AwaitableCompletionSource()),
                    Tuple.Create(1, new AwaitableCompletionSource())
                };
        }
    }


    /// <summary>
    /// 모든 플레이어의 백그라운드 작업 완료여부 확인 
    /// </summary>
    public bool IsAllCompleted()
    {
        if (_bgTaskTCS == null)
        {
            return false;
        }

        // 중간에 플레이어가 나간경우 완료처리
        foreach (var tcs in _bgTaskTCS)
        {
            PlayerRef @ref = tcs.Key;
            if (PlayerM.IsValidPlayer(@ref) == false)
            {
                if (_bgTaskTCS.TryGetValue(@ref, out var t))
                {
                    t.ForEach(t => t.Item2.TrySetResult());
                }
            }
        }

        foreach (var tcs in _bgTaskTCS)
        {
            if (tcs.Value.Any(t => t.Item2.Awaitable.IsCompleted == false))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// 컷씬 종료 후처리
    /// </summary>
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_PostCutScene()
    {
        CutSceneC.DefocusCutSceneCamera();
        CutSceneC.ActiveCutSceneResult(false);
        UIController.DeactiveAllLobbyUI();
        NetEvent.TriggerCutSceneActiveEvent(false);
        UIEventSystem.Inst.TriggerCutSceneActive(false);
    }


    /// <summary>
    /// 로컬 플레이어의 월드 좌표 반환
    /// </summary>
    public Vector2 GetLocalPlayerWorldPos()
    {
        Vector2 playerWorldPos = Vector2.zero;
        if (Runner.TryGetPlayerObject(Runner.LocalPlayer, out var playerObj))
        {
            var player = playerObj.GetComponent<PlayerStageController>();
            if (player != null)
            {
                playerWorldPos = player.GetPosition();
            }
        }

        var uiGame = FindAnyObjectByType<UI_Game>();
        if (uiGame != null)
        {
            var canvas = uiGame.GetComponent<Canvas>();
            if (canvas != null)
            {
                FaderUtil.GetUIPosition(canvas, playerWorldPos);
            }
        }

        return playerWorldPos;
    }


    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public async void RPC_StartFadeOut()
    {
        try
        {
            // 검은 화면 페이드 및 CutScene 화면 준비
            await Awaitable.NextFrameAsync();
            await Fader.FadeOutExpandAsync(Color.black, 1.0f, GetLocalPlayerWorldPos());

            CutSceneC.FocusCutSceneCamera();
            CutSceneC.ActiveCutSceneResult(true);
            UIEventSystem.Inst.TriggerCutSceneActive(true);

            // 서버에서만 실제 컷신 로직을 처리
            if (Runner.IsServer)
            {
                _ = PlayServerCutSceneAsync(() =>
                {
                    // 서버가 입력 완료하면 모든 클라이언트에게 FadeOut 신호
                    RPC_NotifyClientsToFadeOut();
                    RPC_PlayerBackgroundCompleted(Runner.LocalPlayer, 0);
                });
            }
            else
            {
                // 클라이언트는 컷신 + UI 표시하고 서버 신호 대기
                _ = PlayClientCutSceneAndWaitAsync(() =>
                {
                    RPC_PlayerBackgroundCompleted(Runner.LocalPlayer, 0);
                });
            }

            _ = LoadNextMapAsync(() =>
            {
                RPC_PlayerBackgroundCompleted(Runner.LocalPlayer, 1);
            });
            await Awaitable.NextFrameAsync();
        }
        catch (System.Exception e)
        {
            Debug.LogError("GameStageCompletedState 오류");
            Debug.LogError(e.Message);

            Debug.Log("강제로 완료처리를 진행합니다.");
            RPC_PlayerBackgroundCompleted(Runner.LocalPlayer, 0);
            RPC_PlayerBackgroundCompleted(Runner.LocalPlayer, 1);
        }
    }

    /// <summary>
    /// 서버에서만 실행되는 컷신 재생 처리 (입력 대기 포함)
    /// </summary>
    private async Awaitable PlayServerCutSceneAsync(Action onCompleted)
    {
        try
        {
            // Note - 혹시라도 살아있는 플레이어가 없는 경우에 대한 예외처리를 하지않음.
            CutSceneC.UpdateStageUI();
            CutSceneC.UpdateScoreUI(PlayerM.GetPlayerRefs()[0]);
            await Fader.FadeInExpandAsync(Color.black, 1.0f, CutSceneC.GetStartPos());
            await CutSceneC.PlayCutScene(PlayerM.GetAlivePlayers().Count, _cutDuration);
            await CutSceneC.WaitForNext(PlayerM.GetPlayerRefs());
            await Fader.FadeOutExpandAsync(Color.black, 1.0f, CutSceneC.GetEndPos());
            onCompleted?.Invoke();
        }
        catch (System.Exception e)
        {
            onCompleted?.Invoke();
            Debug.LogError("PlayCutSceneAsync 오류");
            Debug.LogError(e.Message);
        }
    }

    private AwaitableCompletionSource _clientFadeOutTCS;

    /// <summary>
    /// 클라이언트에서 실행되는 컷신 + UI 표시 후 서버 신호 대기
    /// </summary>
    private async Awaitable PlayClientCutSceneAndWaitAsync(Action onCompleted)
    {
        try
        {
            await Fader.FadeInExpandAsync(Color.black, 1.0f, CutSceneC.GetStartPos());
            await CutSceneC.PlayCutScene(PlayerM.GetAlivePlayers().Count, _cutDuration);
            
            // 클라이언트는 UI를 보여주고 서버의 신호를 대기
            _clientFadeOutTCS = new AwaitableCompletionSource();
            await _clientFadeOutTCS.Awaitable;
            
            // 서버 신호가 오면 FadeOut 진행
            await Fader.FadeOutExpandAsync(Color.black, 1.0f, CutSceneC.GetEndPos());
            onCompleted?.Invoke();
        }
        catch (System.Exception e)
        {
            onCompleted?.Invoke();
            Debug.LogError("PlayClientCutSceneAndWaitAsync 오류");
            Debug.LogError(e.Message);
        }
    }

    /// <summary>
    /// 서버에서 클라이언트들에게 FadeOut 시작 신호
    /// </summary>
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_NotifyClientsToFadeOut()
    {
        if (Runner.IsClient && _clientFadeOutTCS != null)
        {
            _clientFadeOutTCS.TrySetResult();
            _clientFadeOutTCS = null;
        }
    }

    /// <summary>
    /// 맵 로드 실행 - 서버에서만 실행 클라이언트는 바로 완료처리
    /// </summary>
    public async Awaitable LoadNextMapAsync(Action onComplete = default)
    {
        try
        {
            if (Runner.IsServer)
            {
                await Awaitable.NextFrameAsync();
                await Awaitable.WaitForSecondsAsync(1.5f);
                var nextStageData = DataManager.Inst.GetStageData(StageDataIndex);
                NetEvent.TriggerStageLoadDoneEvent(nextStageData);
                onComplete?.Invoke();
                Debug.Log("다음 스테이지 로딩이 완료되었습니다.");
            }
            else
            {
                onComplete?.Invoke();
            }
        }
        catch (System.Exception e)
        {
            onComplete?.Invoke();
            Debug.LogError("LoadNextMapAsync 오류");
            Debug.LogError(e.Message);
        }
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public async void RPC_FadeInUI()
    {
        NetEvent.TriggerCutSceneActiveEvent(false);
        await Fader.FadeInExpandAsync(Color.black, 1.0f, GetLocalPlayerWorldPos());
    }

    /// <summary>
    /// 클라이언트에서 서버에게 백그라운드 작업 완료 알림
    /// </summary>
    /// <param name="taskIndex"> 작업 인덱스 : 0: 컷신, 1: 맵 로딩 </param>
    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_PlayerBackgroundCompleted(PlayerRef player, byte taskIndex)
    {
        if (Runner.IsServer)
        {
            // State가 비활성화된 경우 무시
            if (_isStateActive == false)
            {
                Debug.LogWarning($"State가 비활성화되어 RPC_PlayerBackgroundCompleted 호출을 무시합니다. Player: {player}, TaskIndex: {taskIndex}");
                return;
            }

            if (_bgTaskTCS == null)
            {
                Debug.LogError("RPC_PlayerCutSceneCompleted 호출 오류 - _bgTaskTCS가 null입니다.");
                return;
            }

            if (_bgTaskTCS.TryGetValue(player, out var tcs))
            {
                tcs.ForEach(t =>
                {
                    if (t.Item1 == taskIndex)
                    {
                        t.Item2.TrySetResult();
                    }
                });
            }
            else
            {
                Debug.LogError($"플레이어 {player}의 컷씬 완료 알림을 찾을 수 없습니다.");
            }
        }
    }
}