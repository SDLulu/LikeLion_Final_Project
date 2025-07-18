using System;
using System.Collections.Generic;
using Fusion;
using LMCore;
using UnityEngine;

public class UI_Controller : BaseManager<UI_Controller>
{
    [Header("인스펙터 참조")]
    [field: SerializeField] public UI_EnterOnline UIEnterOnline {get; private set;}
    [field: SerializeField] public UI_Lobby UILobby {get; private set;}
    [field: SerializeField] public UI_Game UIGame {get; private set;}
    [field: SerializeField] public UI_Title UITitle {get; private set;}

    private void Awake()
    {
        DontDestroyOnLoad(this);
        ActiveTitleUI();
        NetworkEventSystem.Inst.OnSceneLoadDoneEvent += OnChangedScene;
    }

    /// <summary>
    /// 씬변경시 메모리 정리
    /// Note : Lobby(titleUI / lobbyUI)는 Additive 씬으로 항상 유지
    /// </summary>
    private void OnChangedScene(NetworkRunner runner, string sceneName)
    {
        if (sceneName == "DevGame")
        {
            UIGame = FindAnyObjectByType<UI_Game>();
        }
        else 
        {
            UIGame = null;
        }
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

    public async void ActiveGameUI(bool value)
    {
        while (UIGame == null)
        {
            await Awaitable.NextFrameAsync();
        }
        UIGame.gameObject.SetActive(value);
    }

    public void UpdateData(Fusion.NetworkDictionary<int, PlayerData> players)
    {
        UILobby?.UpdateData(players);
    }
}
