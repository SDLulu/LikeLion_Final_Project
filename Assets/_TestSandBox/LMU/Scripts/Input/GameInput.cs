using LMCore;
using UnityEngine;
using UnityEngine.InputSystem;

public class GameInput : MonoBehaviour, GameInputAction.IGameActions
{
    private GameInputAction _input;
    private void Awake()
    {
        _input = InputGetter.Init();
        _input.Game.SetCallbacks(this);
        _input.Game.Enable();
    }

    private void OnDestroy()
    {
        _input.Game.RemoveCallbacks(this);
        _input.Game.Disable();
        InputGetter.Clear();
    }

    public bool IsValid()
    {
        // 현재 씬확인
        var sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        if (sceneName != "DevGame")
        {
            return false;
        }

        // 페이딩 확인
        if (Fader.Inst.IsFading)
            return false;

        // 현재 게임 상태확인
        var stateName = GameStates.Inst.GetActiveStateName();
        if (stateName != E_StateName.PlayingState)
        {
            return false;
        }

        return true;
    }

    public void OnESC(InputAction.CallbackContext context)
    {
        if (IsValid() == false)
            return;

        if (context.started)
        {
            UIEventSystem.Inst.TriggerPauseUIToggle();
        }
    }

    public void OnVoice(InputAction.CallbackContext context)
    {
    }
}
