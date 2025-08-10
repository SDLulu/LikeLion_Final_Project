using System;
using System.Collections.Generic;
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
    [SerializeField] private TMP_Text _curSceneNameText;

    private static bool _inited = false;
    protected override async void Awake()
    {
#if UNITY_EDITOR
        _curSceneNameText.text = "현재 씬 : " + SceneManager.GetActiveScene().name;
#else
        _curSceneNameText.gameObject.SetActive(false);
#endif

        if (_inited == false)
        {
            DontDestroyOnLoad(this);
            _inited = true;
            if (GlobalSetting.Inst.IsShowTitleAnimation)
            {
                Fader.Inst.ActiveBGImage(true, Color.black);
                await Awaitable.WaitForSecondsAsync(0.5f);
                await Fader.Inst.FadeInAsync(seconds: 0.5f);
                ActiveTitleUI(true);
            }
        }
    }

    /// <summary>
    /// 메인 타이틀 화면 활성화
    /// </summary>
    public void ActiveTitleUI(bool showTitleTween = false)
    {
        UIEnterOnline.gameObject.SetActive(true);
        UILobby.gameObject.SetActive(false);
        UIChatting.gameObject.SetActive(false);
        if (showTitleTween)
            UITitle.ShowTitle();
    }

    public void ActiveLobbyOnLineUI()
    {
        UIEnterOnline.gameObject.SetActive(false);
        UILobby.ActiveOnlinePanel();
        UIChatting.gameObject.SetActive(true);
    }

    public void DeactiveAllLobbyUI()
    {
        UIEnterOnline.gameObject.SetActive(false);
        UILobby.gameObject.SetActive(false);
        UIChatting.gameObject.SetActive(false);
    }

    public void UpdateData(NetworkDictionary<PlayerRef, PlayerData> players)
    {
        UILobby?.UpdateData(players);
    }
}
