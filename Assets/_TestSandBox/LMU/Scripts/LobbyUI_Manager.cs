using Fusion;
using LMCore;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LobbyUI_Manager : BaseManager<LobbyUI_Manager>
{
    [Header("로비 UI 참조")]
    [field: SerializeField] public UI_EnterOnline UIEnterOnline {get; private set;}
    [field: SerializeField] public UI_Lobby UILobby {get; private set;}
    [field: SerializeField] public UI_Title UITitle {get; private set;}
    [field: SerializeField] public UI_Chating UIChatting {get; private set;}
    [field: SerializeField] public UI_BackEnd UIBackEnd {get; private set;}
    [field: SerializeField] public UI_Friend UI_Friend {get; private set;}

    [Header("인스펙터 참조")]
    [SerializeField] private TMP_Text _curSceneNameText;
    protected override void Awake()
    {
#if UNITY_EDITOR
        _curSceneNameText.text = "현재 씬 : " + SceneManager.GetActiveScene().name + "\n" +
        "백엔드 활성화 여부 : " + GlobalSetting.Inst.IsEnableBackend + "\n" +
        "보이스 활성화 여부 : " + GlobalSetting.Inst.IsEnableVoice;
#else
        _curSceneNameText.gameObject.SetActive(false);
#endif
    }
    
    /// <summary>
    /// 메인 타이틀 화면 활성화
    /// </summary>
    public void ActiveTitlePanel(bool showTitleTween = false)
    {
        UIEnterOnline.gameObject.SetActive(true);
        UILobby.gameObject.SetActive(false);
        UIChatting.gameObject.SetActive(false);
        if (showTitleTween)
            UITitle.ShowTitle();
    }

    public void ActiveEnterOnlinePanel(bool value)
    {
        if (value)
        {
            UITitle.gameObject.SetActive(true);
            UIEnterOnline.gameObject.SetActive(true);
            UILobby.gameObject.SetActive(false);
            UIChatting.gameObject.SetActive(false);
            UITitle.TitleTransition.gameObject.SetActive(false);
            UITitle.ActiveBackButton(true);
            UI_Friend.Clear();
        }
        else
        {
            Debug.LogError("추가 로직필요");
        }
    }

    public void ActiveLobbyOnLineUI()
    {
        UILobby.gameObject.SetActive(true);
        UIEnterOnline.gameObject.SetActive(false);
        UILobby.ActiveOnlinePanel();
        UIChatting.gameObject.SetActive(true);
        UI_Friend.Clear();
    }

    public void DeactiveAllLobbyUI()
    {
        UIEnterOnline.gameObject.SetActive(false);
        UILobby.gameObject.SetActive(false);
        UIChatting.gameObject.SetActive(false);
        UI_Friend.Clear();
    }

    public void UpdateData(NetworkDictionary<PlayerRef, PlayerData> players)
    {
        UILobby?.UpdateData(players);
    }
}
