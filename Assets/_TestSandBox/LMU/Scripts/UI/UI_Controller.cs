using System.Collections.Generic;
using LMCore;
using UnityEngine;

public class UI_Controller : BaseManager<UI_Controller>
{
    [Header("인스펙터 참조")]
    [SerializeField] private UI_Title uiTitle;
    [SerializeField] private UI_Lobby uiLobby;

    private void Awake()
    {
        DontDestroyOnLoad(this);
        ActiveTitleUI();
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

    public void DeactiveAllUI()
    {
        uiTitle.gameObject.SetActive(false);
        uiLobby.gameObject.SetActive(false);
    }

    public void UpdateData(Fusion.NetworkDictionary<int, PlayerData> players)
    {
        uiLobby?.UpdateData(players);
    }
}
