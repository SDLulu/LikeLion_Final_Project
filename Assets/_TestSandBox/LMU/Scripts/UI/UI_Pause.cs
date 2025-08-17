using UnityEngine;
using UnityEngine.UI;

public class UI_Pause : MonoBehaviour
{
    [Header("인스펙터 참조")]
    [SerializeField] private Button _resumeButton;
    [SerializeField] private Button _mainMenuButton;

    private void Awake()
    {
        _resumeButton.onClick.AddListener(OnClickResumeButton);
        _mainMenuButton.onClick.AddListener(OnClickMainMenuButton);
    }

    private void OnDestroy()
    {
        _resumeButton.onClick.RemoveListener(OnClickResumeButton);
        _mainMenuButton.onClick.RemoveListener(OnClickMainMenuButton);
    }

    private void OnClickResumeButton()
    {
        UIEventSystem.Inst.TriggerPauseUIActive(false);
    }

    private void OnClickMainMenuButton()    
    {
        _ = LobbyManager.Inst.LeaveGame();
    }
}
