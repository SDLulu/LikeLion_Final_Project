using UnityEngine;
using UnityEngine.UI;
using LMCore;
using Fusion;

public class UI_Title : MonoBehaviour
{
    [Header("인스펙터 참조")]
    [SerializeField] private Button _soloPlayBtn;
    [SerializeField] private Button _onlinePlayBtn;
    [SerializeField] private Button _leaderboardBtn;
    [SerializeField] private Button _userProfileBtn;
    [SerializeField] private Button _settingBtn;
    [SerializeField] private Button _exitBtn;
    [SerializeField] private RectTransform _preventPanel;
    [SerializeField] private UI_OtherPanelHolder _otherPanelHolder;

    [Header("패널 트랜지션")]
    [SerializeField] private UI_PanelEdgeTransition _titleTransition;
    [SerializeField] private UI_PanelEdgeTransition _onlineTransition;
    [SerializeField] private UI_PanelEdgeTransition _nicknameTransition;

    [Header("타이틀공용 백 버튼")]
    [SerializeField] private UI_BackButton _backButton;

    private void Awake()
    {
        _soloPlayBtn.onClick.AddListener(OnClickSoloPlayBtn);
        _onlinePlayBtn.onClick.AddListener(OnClickOnlinePlayBtn);
        _settingBtn.onClick.AddListener(OnClickSettingBtn);
        _exitBtn.onClick.AddListener(OnClickExitBtn);
        _leaderboardBtn.onClick.AddListener(OnClickLeaderboardBtn);
        _userProfileBtn.onClick.AddListener(OnClickUserProfileBtn);
        _backButton.gameObject.SetActive(false);
        _titleTransition.gameObject.SetActive(false);
        _onlineTransition.gameObject.SetActive(false);
        _nicknameTransition.gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
        _soloPlayBtn.onClick.RemoveAllListeners();
        _onlinePlayBtn.onClick.RemoveAllListeners();
        _leaderboardBtn.onClick.RemoveAllListeners();
        _userProfileBtn.onClick.RemoveAllListeners();
        _settingBtn.onClick.RemoveAllListeners();
        _exitBtn.onClick.RemoveAllListeners();
    }

    public void ActiveBackButton(bool active)
    {
        _backButton.gameObject.SetActive(active);
    }

    public void ShowTitle()
    {
        if (gameObject.activeSelf == false)
            gameObject.SetActive(true);

        UI_HoverText.IsHoverBlocked = true;
        _nicknameTransition.Show();
        _titleTransition.Show(() => UI_HoverText.IsHoverBlocked = false);
    }

    public void HideTitle()
    {
        UI_HoverText.IsHoverBlocked = true;
        _nicknameTransition.Hide();
        _titleTransition.Hide(() =>
        {
            UI_HoverText.IsHoverBlocked = false;
            gameObject.SetActive(false);
        });
    }

    private async void OnClickSoloPlayBtn()
    {
        UIGlobalSetting?.ActiveUI(false);
        _preventPanel.gameObject.SetActive(true);
        await LobbyManager.Inst.JoinOrCreateLobby(
            isSoloPlay: true,
            mode: GameMode.AutoHostOrClient,
            roomName: "TestRoom",
            OnEnterLobby: () =>
            {
                LobbyUI_Manager.Inst.ActiveLobbyOnLineUI();
                _preventPanel.gameObject.SetActive(false);
            },
            OnCancel: () =>
            {
                LobbyUI_Manager.Inst.ActiveTitleUI();
                _preventPanel.gameObject.SetActive(false);
            }
        );
    }

    private UI_GlobalSetting _uiGlobalSetting = null;
    public UI_GlobalSetting UIGlobalSetting => _uiGlobalSetting ??= FindAnyObjectByType<UI_GlobalSetting>();
    public void OnClickEnterOnlineBackBtn()
    {
        UI_HoverText.IsHoverBlocked = true;
        _backButton.Hide(OnComplete);

        void OnComplete()
        {
            _onlineTransition.Hide(() =>
            {
                _nicknameTransition.Show();
                _titleTransition.Show(() => UI_HoverText.IsHoverBlocked = false);
                UIGlobalSetting?.ActiveUI(true);
            });
        }
    }

    private void OnClickOnlinePlayBtn()
    {
        UI_HoverText.IsHoverBlocked = true;
        _nicknameTransition.Hide();
        _titleTransition.Hide(() =>
        {
            UIGlobalSetting?.ActiveUI(false);
            _onlineTransition.Show(() =>
            {
                _backButton.gameObject.SetActive(true);
                _backButton.Show(() => UI_HoverText.IsHoverBlocked = false);
            });
        });
    }

    private void OnClickSettingBtn()
    {
        _otherPanelHolder.ShowSettingPanel();
    }

    private void OnClickLeaderboardBtn()
    {
        _otherPanelHolder.ShowLeaderboardPanel();
    }

    private void OnClickUserProfileBtn()
    {
        _otherPanelHolder.ShowUserProfilePanel();
    }

    private async void OnClickExitBtn()
    {
        await Fader.Inst.FadeOutAsync(seconds: 0.5f);

        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }


}