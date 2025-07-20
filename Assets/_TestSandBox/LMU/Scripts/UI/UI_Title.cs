using UnityEngine;
using UnityEngine.UI;

public class UI_Title : MonoBehaviour
{
    [Header("인스펙터 참조")]
    [SerializeField] private Button soloPlayBtn;
    [SerializeField] private Button onlinePlayBtn;
    [SerializeField] private Button settingBtn;
    [SerializeField] private Button exitBtn;
    [SerializeField] private RectTransform _titleButtonPanel;
    [SerializeField] private RectTransform _enterOnlinePanel;
    [SerializeField] private RectTransform _fastTestPanel;
    [SerializeField] private RectTransform _preventPanel;
    [SerializeField] private UI_CreateNickName _uiCreateNickName;

    [Header("EnterOnline 패널 뒤로가기")]
    [SerializeField] private Button _enterOnlineBackBtn;

    private void Awake()
    {
        soloPlayBtn.onClick.AddListener(OnClickSoloPlayBtn);
        onlinePlayBtn.onClick.AddListener(OnClickOnlinePlayBtn);
        settingBtn.onClick.AddListener(OnClickSettingBtn);
        exitBtn.onClick.AddListener(OnClickExitBtn);
        _enterOnlineBackBtn.onClick.AddListener(OnClickEnterOnlineBackBtn);

        _uiCreateNickName.gameObject.SetActive(true);
        _fastTestPanel.gameObject.SetActive(true);
        _titleButtonPanel.gameObject.SetActive(true);
        _enterOnlinePanel.gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
        soloPlayBtn.onClick.RemoveAllListeners();
        onlinePlayBtn.onClick.RemoveAllListeners();
        settingBtn.onClick.RemoveAllListeners();
        exitBtn.onClick.RemoveAllListeners();
        _enterOnlineBackBtn.onClick.RemoveAllListeners();
    }

    private void OnClickSoloPlayBtn()
    {
        UI_Controller.Inst.UILobby.ActiveSoloPanel();
    }

    private void OnClickEnterOnlineBackBtn()
    {
        _fastTestPanel.gameObject.SetActive(true);
        _uiCreateNickName.gameObject.SetActive(true);
        _titleButtonPanel.gameObject.SetActive(true);
        _enterOnlinePanel.gameObject.SetActive(false);
    }

    private void OnClickOnlinePlayBtn()
    {
        _fastTestPanel.gameObject.SetActive(false);
        _uiCreateNickName.gameObject.SetActive(false);
        _titleButtonPanel.gameObject.SetActive(false);
        _enterOnlinePanel.gameObject.SetActive(true);
    }

    private void OnClickSettingBtn()
    {

    }

    private void OnClickExitBtn()
    {
        Application.Quit();
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #endif
    }
}
