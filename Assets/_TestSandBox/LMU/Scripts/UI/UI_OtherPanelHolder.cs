using UnityEngine;
using UnityEngine.UI;

public class UI_OtherPanelHolder : MonoBehaviour
{
    [Header("인스펙터 참조")]
    [SerializeField] private UI_PanelEdgeTransition _settingPanel;
    [SerializeField] private UI_PanelEdgeTransition _leaderboardPanel;
    [SerializeField] private UI_PanelEdgeTransition _userProfilePanel;
    [SerializeField] private RectTransform _preventPanel;

    [Header("디버그용")]
    [SerializeField] private UI_PanelEdgeTransition _curPanel;

    public void ShowSettingPanel()
    {
        _curPanel = _settingPanel;
        _settingPanel.Show();
        _preventPanel.gameObject.SetActive(true);
    }

    public void ShowLeaderboardPanel()
    {
        _curPanel = _leaderboardPanel;
        _leaderboardPanel.Show();
        _preventPanel.gameObject.SetActive(true);
    }

    public void ShowUserProfilePanel()
    {
        _curPanel = _userProfilePanel;
        _userProfilePanel.Show();
        _preventPanel.gameObject.SetActive(true);
    }
}
