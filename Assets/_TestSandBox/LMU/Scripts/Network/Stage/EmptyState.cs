using System;
using System.Linq;
using Fusion;
using Fusion.Addons.FSM;
using UnityEngine;

public class EmptyState : BaseStateBehaviour
{
    public override E_StateName StateName => E_StateName.EmptyState;

    
    private TickTimer _fadeOutTimer = TickTimer.None;
    private float _fadeOutTime = 1.5f;

    protected override  void OnEnterState()
    {
        if (Runner.IsServer)
        {
            GameStates.RPC_FadeOutUI(this.Runner, 1.0f);
            _fadeOutTimer = TickTimer.CreateFromSeconds(Runner, _fadeOutTime);
        }
    }

    private bool _isEmptyState = false;
    protected override async void OnFixedUpdate()
    {
        if (Runner.IsServer && _fadeOutTimer.ExpiredOrNotRunning(Runner) &&  _isEmptyState == false)
        {
            _isEmptyState = true;
            var lastPos = GameObject.FindGameObjectsWithTag("LastPos");
            PlayerM.SetPlayerPositions(lastPos[0].transform.position);
            
            await Awaitable.WaitForSecondsAsync(0.5f);
            BGMManager.Inst.PlayBGM("Title");
            await Awaitable.WaitForSecondsAsync(1.0f);
            GameStates.RPC_FadeInUI(this.Runner, 1.0f);
        }
    }

    protected override void OnExitState()
    {
    }
} 