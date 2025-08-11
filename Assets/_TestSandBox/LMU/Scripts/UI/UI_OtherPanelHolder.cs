using UnityEngine;
using UnityEngine.UI;

public class UI_OtherPanelHolder : MonoBehaviour
{
    [Header("인스펙터 참조")]
    [SerializeField] private UI_PanelEdgeTransition _settingPanel;
    [SerializeField] private UI_PanelEdgeTransition _leaderboardPanel;
    [SerializeField] private UI_PanelEdgeTransition _userProfilePanel;
    [SerializeField] private Button _backBtn;

    [Header("디버그용")]
    [SerializeField] private UI_PanelEdgeTransition _curPanel;

    private void Awake()
    {
        _backBtn.onClick.AddListener(OnClickBackBtn);
        _backBtn.gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
        _backBtn.onClick.RemoveAllListeners();
    }

    private void OnClickBackBtn()
    {
        _curPanel?.Hide(() =>
        {
            _curPanel = null;
            _backBtn.gameObject.SetActive(false);
        });
    }

    public void ShowSettingPanel()
    {
        _curPanel = _settingPanel;
        _backBtn.gameObject.SetActive(true);
        _settingPanel.Show();
    }

    public void ShowLeaderboardPanel()
    {
        _curPanel = _leaderboardPanel;
        _backBtn.gameObject.SetActive(true);
        _leaderboardPanel.Show();
    }

    public void ShowUserProfilePanel()
    {
        _curPanel = _userProfilePanel;
        _backBtn.gameObject.SetActive(true);
        _userProfilePanel.Show();
    }
}
