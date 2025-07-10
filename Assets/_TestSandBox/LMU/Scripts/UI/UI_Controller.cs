using System.Collections.Generic;
using LMCore;
using UnityEngine;

public class UI_Controller : BaseManager<UI_Controller>
{
    [Header("인스펙터 참조")]
    [SerializeField] private UI_Title uiTitle;
    [SerializeField] private UI_Lobby uiLobby;
    [SerializeField] private UI_Game uiGame;

    private void Awake()
    {
        DontDestroyOnLoad(this);
        ActiveTitleUI();
        LobbyManager.Inst.NetRunner.OnSceneLoadDoneAction += OnChangedScene;
    }

    /// <summary>
    /// 씬변경시 메모리 정리
    /// Note : Lobby(titleUI / lobbyUI)는 Additive 씬으로 항상 유지
    /// </summary>
    private void OnChangedScene(string sceneName)
    {
        if (sceneName == "Game")
        {
            uiGame = FindAnyObjectByType<UI_Game>();
        }
        else 
        {
            uiGame = null;
        }
    }

    public void ActiveTitleUI()
    {
        uiTitle.gameObject.SetActive(true);
        uiLobby.gameObject.SetActive(false);
    }

    public void ActiveLobbyUI()
    {
        uiTitle.gameObject.SetActive(false);
        uiLobby.gameObject.SetActive(true);
    }

    public void DeactiveAllLobbyUI()
    {
        uiTitle.gameObject.SetActive(false);
        uiLobby.gameObject.SetActive(false);
    }

    public async void ActiveGameUI(bool value)
    {
        while (uiGame == null)
        {
            await Awaitable.NextFrameAsync();
        }
        uiGame.gameObject.SetActive(value);
    }

    public void UpdateData(Fusion.NetworkDictionary<int, PlayerData> players)
    {
        uiLobby?.UpdateData(players);
    }
}
