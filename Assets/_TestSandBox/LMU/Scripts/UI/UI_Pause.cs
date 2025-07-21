using UnityEngine;
using UnityEngine.UI;

public class UI_Pause : MonoBehaviour
{
    [Header("인스펙터 참조")]
    [SerializeField] private Button _resumeButton;
    [SerializeField] private Button _adventureEndButton;
    [SerializeField] private Button _instanceRestartButton;
    [SerializeField] private Button _settingButton;
    [SerializeField] private Button _mainMenuButton;
    [SerializeField] private Button _gameEndButton;

    private void Awake()
    {
        _resumeButton.onClick.AddListener(OnClickResumeButton);
        _adventureEndButton.onClick.AddListener(OnClickAdventureEndButton);
        _instanceRestartButton.onClick.AddListener(OnClickInstanceRestartButton);
        _settingButton.onClick.AddListener(OnClickSettingButton);
        _mainMenuButton.onClick.AddListener(OnClickMainMenuButton);
        _gameEndButton.onClick.AddListener(OnClickGameEndButton);
    }

    private void OnDestroy()
    {
        _resumeButton.onClick.RemoveListener(OnClickResumeButton);
        _adventureEndButton.onClick.RemoveListener(OnClickAdventureEndButton);
        _instanceRestartButton.onClick.RemoveListener(OnClickInstanceRestartButton);
        _settingButton.onClick.RemoveListener(OnClickSettingButton);
        _mainMenuButton.onClick.RemoveListener(OnClickMainMenuButton);
        _gameEndButton.onClick.RemoveListener(OnClickGameEndButton);
    }

    private void OnClickResumeButton()
    {
        UIEventSystem.Inst.TriggerPauseUIActive(false);
    }

    private void OnClickAdventureEndButton()        
    {
        _ = LobbyManager.Inst.LeaveGame();
    }

    private void OnClickInstanceRestartButton()
    {
        _ = LobbyManager.Inst.LeaveGame();
    }

    private void OnClickSettingButton()
    {

    }

    private void OnClickMainMenuButton()    
    {
        _ = LobbyManager.Inst.LeaveGame();
    }

    private void OnClickGameEndButton()
    {
        Application.Quit();
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #endif
    }
}
