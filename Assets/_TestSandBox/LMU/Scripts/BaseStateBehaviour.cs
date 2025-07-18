using Fusion.Addons.FSM;
using LMCore;
using UnityEngine;

public class BaseStateBehaviour : StateBehaviour
{
    protected UI_Controller UIController {get; set;}
    protected Fader Fader {get; set;}
    protected PlayerManager PlayerM {get; set;}
    protected CutSceneController CutSceneC {get; set;}
}
