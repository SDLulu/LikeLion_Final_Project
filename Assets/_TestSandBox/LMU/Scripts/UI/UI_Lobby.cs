using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public enum E_LobbyType
{
    Online,
    Solo,
}
public class UI_Lobby : MonoBehaviour
{
    [Header("인스펙터 참조")]
    [SerializeField] private RectTransform _onlinePanel;
    [SerializeField] private RectTransform _soloPanel;
    [SerializeField] private List<Button> _backButtons;
    [SerializeField] private Button _soloPlayBtn;
    [SerializeField] private UI_CharacterSlotContainer _characterSlotContainer;
    [Header("디버그용")]
    [SerializeField] private E_LobbyType _lobbyType;

    private void Awake()
    {
        foreach (var button in _backButtons)
            button.onClick.AddListener(OnClickBackButton);
        _soloPlayBtn.onClick.AddListener(OnClickSoloPlayBtn);
    }

    private void OnDestroy()
    {
        foreach (var button in _backButtons)
            button.onClick.RemoveAllListeners();
        _soloPlayBtn.onClick.RemoveAllListeners();
    }

    public void ActiveOnlinePanel()
    {
        _lobbyType = E_LobbyType.Online;
        this.gameObject.SetActive(true);
        _onlinePanel.gameObject.SetActive(true);
        _soloPanel.gameObject.SetActive(false);
    }

    public void ActiveSoloPanel()
    {
        _lobbyType = E_LobbyType.Solo;
        this.gameObject.SetActive(true);
        _onlinePanel.gameObject.SetActive(false);
        _soloPanel.gameObject.SetActive(true);
    }

    public void OnClickBackButton()        
    {
        if (_lobbyType == E_LobbyType.Online)
        {
            _ = LobbyManager.Inst.LeaveGame();
        }
        else
        {
            UI_Controller.Inst.UITitle.gameObject.SetActive(true);
            this.gameObject.SetActive(false);
        }
    }

    private void OnClickSoloPlayBtn()
    {
        this.gameObject.SetActive(false);
    }

    public void UpdateData(Fusion.NetworkDictionary<int, PlayerData> players)
    {
        _characterSlotContainer?.UpdateData(players);
    }
}
