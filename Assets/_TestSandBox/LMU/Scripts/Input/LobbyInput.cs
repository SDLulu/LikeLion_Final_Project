using UnityEngine;
using UnityEngine.InputSystem;

public static class InputGetter
{
    public static GameInputAction Input;
    public static GameInputAction Init()
    {
        if (Input == null)
            Input = new();
        return Input;
    }

    public static void Clear()
    {
        if (Input != null)
            Input.Dispose();
        Input = null;
    }
}

public class LobbyInput : MonoBehaviour, GameInputAction.ILobbyActions
{
    private GameInputAction _input;

    private void Awake()
    {
        _input = InputGetter.Init();
        _input.Lobby.SetCallbacks(this);
        _input.Lobby.Enable();
    }

    private void OnDestroy()
    {
        _input.Lobby.RemoveCallbacks(this);
        _input.Lobby.Disable();
        InputGetter.Clear();
    }

    public void OnCancel(InputAction.CallbackContext context)
    {
    }

    public void OnClick(InputAction.CallbackContext context)
    {
    }

    public void OnMiddleClick(InputAction.CallbackContext context)
    {
    }

    public void OnNavigate(InputAction.CallbackContext context)
    {
    }

    public void OnPoint(InputAction.CallbackContext context)
    {
    }

    public void OnRightClick(InputAction.CallbackContext context)
    {
    }

    public void OnScrollWheel(InputAction.CallbackContext context)
    {
    }

    public void OnSubmit(InputAction.CallbackContext context)
    {
    }

    public void OnTrackedDeviceOrientation(InputAction.CallbackContext context)
    {
    }

    public void OnTrackedDevicePosition(InputAction.CallbackContext context)
    {
    }
}
