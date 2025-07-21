using System;
using System.Collections.Generic;
using Fusion;
using LMCore;
using UnityEngine;

public class UI_Controller : BaseManager<UI_Controller>
{
    [Header("로비 UI 참조")]
    [field: SerializeField] public UI_EnterOnline UIEnterOnline {get; private set;}
    [field: SerializeField] public UI_Lobby UILobby {get; private set;}
    [field: SerializeField] public UI_Title UITitle {get; private set;}

    private void Awake()
    {
        DontDestroyOnLoad(this);
        ActiveTitleUI();
        // 게임 UI는 UIEventSystem이 관리하므로 NetworkEventSystem 구독 불필요
    }

    public void ActiveTitleUI()
    {
        UIEnterOnline.gameObject.SetActive(true);
        UILobby.gameObject.SetActive(false);
    }

    public void ActiveLobbyOnLineUI()
    {
        UIEnterOnline.gameObject.SetActive(false);
        UILobby.ActiveOnlinePanel();
    }

    public void DeactiveAllLobbyUI()
    {
        UIEnterOnline.gameObject.SetActive(false);
        UILobby.gameObject.SetActive(false);
    }

    public void UpdateData(Fusion.NetworkDictionary<int, PlayerData> players)
    {
        UILobby?.UpdateData(players);
    }
}
