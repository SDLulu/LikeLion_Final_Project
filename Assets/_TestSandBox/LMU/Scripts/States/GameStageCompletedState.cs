using System.Collections.Generic;
using DG.Tweening;
using Fusion;
using Fusion.Addons.FSM;
using LMCore;
using UnityEngine;

public class GameStageCompletedState : BaseStateBehaviour
{
    [Header("설정")]
    [SerializeField, Range(10.0f, 15.0f)] private float minWaitingTime = 15.0f;
    [SerializeField] private float cutDuration = 2.0f;

    // 서버 - 플레이어 컷신완료 대기
    private Dictionary<PlayerRef, AwaitableCompletionSource> _waitingTCS;
    private TickTimer minWaitingTimer = TickTimer.None;

    protected override void OnEnterState()
    {
        if (Runner.IsServer)
        {
            var players = PlayerM.GetPlayers();

            _waitingTCS = new();
            foreach (var player in players)
            {
                PlayerRef @ref = player.Value.Object.InputAuthority;
                _waitingTCS[@ref] = new AwaitableCompletionSource();
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

    protected override void OnExitState()
    {
        minWaitingTimer = TickTimer.None;
        _waitingTCS?.Clear();
        _waitingTCS = null;
        Debug.Log("대기 상태 종료");
        RPC_FadeInUI();
    }

    /// <summary>
    /// 모든 플레이어의 컷신 완료여부 확인
    /// </summary>
    public bool IsAllCompleted()
    {
        if (_waitingTCS == null)
            return false;

        // 중간에 플레이어가 나간경우 완료처리
        foreach (var tcs in _waitingTCS)
        {
            PlayerRef @ref = tcs.Key;
             if (PlayerM.IsValidPlayer(@ref) == false)
            {
                if (_waitingTCS.TryGetValue(@ref, out var t))
                    t.TrySetResult();
            }
        }

        foreach (var tcs in _waitingTCS)
        {
            if (tcs.Value.Awaitable.IsCompleted == false)
                return false;
        }
        return true;
    }


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


    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public async void RPC_StartFadeOut()
    {
        try
        {
            UIController.ActiveGameUI(false);

            // 검은 화면 페이드 및 CutScene 화면 준비
            await Fader.FadeOutExpandAsync(Color.black, 1.0f, GetLocalPlayerWorldPos());
            _ = UIController.UIGame.FadeOutPlayerSlotsAsync();
            CutSceneC.FocusCutSceneCamera();
            CutSceneC.ActiveCutSceneResult(true);

            // CutScene 진행
            // Note - 혹시라도 살아있는 플레이어가 없는 경우에 대한 예외처리를 하지않음.
            await Fader.FadeInExpandAsync(Color.black, 1.0f, CutSceneC.GetStartPoint());
            await CutSceneC.PlayCutScene(PlayerM.GetAlivePlayers(), cutDuration);

            // 검은 화면 페이드
            await Fader.FadeOutExpandAsync(Color.black, 1.0f, CutSceneC.GetEndPoint());
            CutSceneC.DefocusCutSceneCamera();
            CutSceneC.ActiveCutSceneResult(false);
            UIController.DeactiveAllLobbyUI();
            await Awaitable.NextFrameAsync();
            RPC_PlayerCutSceneCompleted(Runner.LocalPlayer);
        }
        catch (System.Exception e)
        {
            Debug.LogError("GameStageCompletedState 오류");
            Debug.LogError(e.Message);

            Debug.Log("강제로 완료처리를 진행합니다.");
            RPC_PlayerCutSceneCompleted(Runner.LocalPlayer);
        }
    }


    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public async void RPC_FadeInUI()
    {
        UIController.ActiveGameUI(true);
        _ = UIController.UIGame.FadeInPlayerSlotsAsync();
        await Fader.FadeInExpandAsync(Color.black, 1.0f, GetLocalPlayerWorldPos());
    }

    /// <summary>
    /// 클라이언트에서 서버에게 컷씬 완료 알림
    /// </summary>
    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_PlayerCutSceneCompleted(PlayerRef player)
    {
        if (Runner.IsServer)
        {
            if (_waitingTCS == null)
            {
                Debug.LogError("RPC_PlayerCutSceneCompleted 호출 오류 - _waitingTCS가 null입니다.");
                return;
            }

            if (_waitingTCS.TryGetValue(player, out var tcs))
            {
                tcs.TrySetResult();
            }
            else
            {
                Debug.LogError($"플레이어 {player}의 컷씬 완료 알림을 찾을 수 없습니다.");
            }
        }
    }
}