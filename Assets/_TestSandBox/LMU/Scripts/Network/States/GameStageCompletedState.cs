using System;
using System.Collections.Generic;
using System.Linq;
using Fusion;
using Fusion.Addons.FSM;
using LMCore;
using UnityEngine;

public class GameStageCompletedState : BaseStateBehaviour
{
    public override E_StateName StateName => E_StateName.GameStageCompletedState;

    [Header("설정")]
    [SerializeField, Range(10.0f, 15.0f)] private float minWaitingTime = 15.0f;
    [SerializeField] private float cutDuration = 2.0f;

    // 서버 - 백그라운드 작업진행 - 컷신 재생과 맵 로딩을 병렬실행 - 0: 컷신, 1: 맵 로딩
    private Dictionary<PlayerRef, List<Tuple<int, AwaitableCompletionSource>>> _bgTaskTCS;
    private TickTimer minWaitingTimer = TickTimer.None;
    private int _stageDataIndex = -1;
    public override void Spawned()
    {
        if (Runner.IsServer)
        {
            _stageDataIndex = DataManager.Inst.StageData.First().Key;
        }
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        _bgTaskTCS?.Clear();
        _bgTaskTCS = null;
        base.Despawned(runner, hasState);
    }

    protected override async void OnEnterState()
    {
        if (Runner.IsServer)
        {
            await Awaitable.NextFrameAsync();
            var players = PlayerM.GetPlayers();

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


            minWaitingTimer = TickTimer.CreateFromSeconds(Runner, minWaitingTime);
            RPC_StartFadeOut();
        }
    }

    protected override void OnFixedUpdate()
    {
        if (Runner.IsServer && IsAllCompleted())
        {
            Debug.Log("모든 플레이어가 컷신을 완료했습니다.");
            Machine.ForceActivateState(Machine.GetState<GameStagePlayingState>());
        }
        else if (Runner.IsServer && minWaitingTimer.Expired(Runner))
        {
            Debug.Log($"최대 대기시간 {minWaitingTime}초가 초과되었습니다.");
            Machine.ForceActivateState(Machine.GetState<GameStagePlayingState>());
        }
    }

    /// <summary>
    /// Note - 주의: 화면은 어두운 상태로 유지 (FadeIn 호출 안함)
    /// </summary>
    protected override void OnExitState()
    {
        minWaitingTimer = TickTimer.None;
        _bgTaskTCS?.Clear();
        _bgTaskTCS = null;
        _stageDataIndex++;
        base.OnExitState();
    }


    /// <summary>
    /// 모든 플레이어의 백그라운드 작업 완료여부 확인
    /// </summary>
    public bool IsAllCompleted()
    {
        if (_bgTaskTCS == null)
            return false;

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
                return false;
        }

        RPC_PostCutScene();
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
            playerWorldPos = player.GetPosition();
        }
        return playerWorldPos;
    }


    /// <summary>
    /// Note
    /// 기본적으로 Task또는 UniTask의 WhenAll과 같은 함수를 Awaitable에서 제공하지않아서
    /// AwaitableCompletionSource과 Action을 사용해 우회적으로 WhenAll의 기능을 구현
    /// </summary>
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public async void RPC_StartFadeOut()
    {
        try
        {
            UIEventSystem.Inst.TriggerGameUIActive(false);

            // 검은 화면 페이드 및 CutScene 화면 준비
            await Fader.FadeOutExpandAsync(Color.black, 1.0f, GetLocalPlayerWorldPos());
            _ = UIEventSystem.Inst.TriggerPlayerSlotsFadeOutAsync();
            CutSceneC.FocusCutSceneCamera();
            CutSceneC.ActiveCutSceneResult(true);

            _ = PlayCutSceneAsync(() => 
            {
                RPC_PlayerBackgroundCompleted(Runner.LocalPlayer, 0);
            });

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
    /// 컷신 재생 처리
    /// </summary>
    private async Awaitable PlayCutSceneAsync(Action onCompleted)
    {
        try 
        {
            // Note - 혹시라도 살아있는 플레이어가 없는 경우에 대한 예외처리를 하지않음.
            await Fader.FadeInExpandAsync(Color.black, 1.0f, CutSceneC.GetStartPoint());
            PlayerSlotUIManager.Inst.gameObject.SetActive(false);
            await CutSceneC.PlayCutScene(PlayerM.GetPlayerDatas().Count, cutDuration);
            await Fader.FadeOutExpandAsync(Color.black, 1.0f, CutSceneC.GetEndPoint());
            PlayerSlotUIManager.Inst.gameObject.SetActive(true);
            onCompleted?.Invoke();
        }
        catch (System.Exception e)
        {
            onCompleted?.Invoke();
            Debug.LogError("PlayCutSceneAsync 오류");
            Debug.LogError(e.Message);
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
                await Awaitable.WaitForSecondsAsync(2.0f);
                var nextStageData = DataManager.Inst.GetStageData(_stageDataIndex);
                NetEvent.TriggerStageLoadDoneEvent(nextStageData);

                var startPos = new Vector2(15.0f, 15.0f);
                foreach (var player in PlayerM.Players)
                {
                    var playerC = player.Value.GetComponent<PlayerStageController>();
                    playerC.SetPosition(startPos);
                }
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
        finally
        {
            _stageDataIndex++;
            Debug.Log("다음 스테이지 인덱스 : " + _stageDataIndex);
        }
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public async void RPC_FadeInUI()
    {
        UIEventSystem.Inst.TriggerGameUIActive(true);
        await UIEventSystem.Inst.TriggerPlayerSlotsFadeInAsync();
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
            if (_bgTaskTCS == null)
            {
                Debug.LogError("RPC_PlayerCutSceneCompleted 호출 오류 - _waitingTCS가 null입니다.");
                return;
            }

            if (_bgTaskTCS.TryGetValue(player, out var tcs))
            {
                tcs.ForEach(t => 
                {
                    if (t.Item1 == taskIndex)
                        t.Item2.TrySetResult();
                });
            }
            else
            {
                Debug.LogError($"플레이어 {player}의 컷씬 완료 알림을 찾을 수 없습니다.");
            }
        }
    }
}