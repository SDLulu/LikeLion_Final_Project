using System;
using System.Collections.Generic;
using System.Linq;
using Fusion;
using Fusion.Addons.FSM;
using UnityEngine;

public class GameStageWaitingState : BaseStateBehaviour
{
    public override E_StateName StateName => E_StateName.WaitingState;
    
    [Header("디버그용")]
    private bool _isMapLoadCompleted = false;

    protected override async void OnEnterState()
    {
        if (Runner.IsServer)
        {
            try
            {
                await LoadFirstMapAsync();
                _isMapLoadCompleted = true;
            }
            catch (Exception e)
            {
                Debug.LogError("GameStageWaitingState 맵 로딩 중 오류 발생:");
                Debug.LogError(e.Message);
                _isMapLoadCompleted = true;
            }
        }
    }

    protected override void OnFixedUpdate()
    {
        if (Runner.IsServer && _isMapLoadCompleted)
        {
            Debug.Log("맵 로딩이 완료되어 GameStagePlayingState로 전환합니다.");
            Machine.ForceActivateState(Machine.GetState<GameStagePlayingState>());
        }    
    }

    /// <summary>
    /// Note - 주의: 화면은 어두운 상태로 유지 (FadeIn 호출 안함)
    /// </summary>
    protected override void OnExitState()
    {
        _isMapLoadCompleted = false;
        base.OnExitState();
    }

    private async Awaitable LoadFirstMapAsync()
    {
        if (Runner.IsServer)
        {
            await Awaitable.NextFrameAsync();
            await Awaitable.WaitForSecondsAsync(1.5f);
            var firstStageData = DataManager.Inst.StageData.First().Value;
            NetEvent.TriggerStageLoadDoneEvent(firstStageData);
            Debug.Log("첫 번째 스테이지 로딩이 완료되었습니다.");
        }
    }
} 