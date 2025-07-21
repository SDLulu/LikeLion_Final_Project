using Fusion.Addons.FSM;
using LMCore;
using UnityEngine;

public enum E_StateName
{
    GameStageWaitingState,
    GameStageTransitionState,
    GameStagePlayingState,
    GameStageFailedState,
    GameStageCompletedState,
}

public abstract class BaseStateBehaviour : StateBehaviour
{
    protected UI_Controller UIController {get; set;}
    protected Fader Fader {get; set;}
    protected PlayerManager PlayerM {get; set;}
    protected CutSceneController CutSceneC {get; set;}
    public abstract E_StateName StateName {get;}
}
