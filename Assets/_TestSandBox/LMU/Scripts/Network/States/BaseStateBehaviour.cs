using Fusion.Addons.FSM;
using LMCore;
using UnityEngine;

public enum E_StateName
{
    EmptyState,
    GameStageWaitingState,
    GameStageTransitionState,
    GameStagePlayingState,
    GameStageFailedState,
    GameStageCompletedState,
}

public abstract class BaseStateBehaviour : StateBehaviour
{
    protected LobbyUI_Manager UIController {get; set;}
    protected Fader Fader {get; set;}
    protected PlayerManager PlayerM {get; set;}
    protected CutSceneController CutSceneC {get; set;}
    protected NetworkEventSystem NetEvent {get; set;}
    public abstract E_StateName StateName {get;}
}
