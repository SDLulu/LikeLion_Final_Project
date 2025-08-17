using UnityEngine;

public class TitleScene : MonoBehaviour
{
    private static bool _inited = false;
    private async void Awake()
    {
        if (_inited)
            return;
        _inited = true;

        UI_Setting.ApplyResolution();
        await BackEndWorkFlow.Inst.LoginGuest();
        LobbyUI_Manager.Inst.ActiveTitlePanel(true);
    }

    private void Start()
    {
        UI_Setting.ApplyBGMVolume();
        BGMManager.Inst.PlayBGM("Title");
    }
}
