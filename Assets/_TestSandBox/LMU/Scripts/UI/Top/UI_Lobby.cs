using System.Collections.Generic;
using Fusion;
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
    [SerializeField] private Button _backButton;
    [SerializeField] private Button _soloPlayBtn;
    // [SerializeField] private UI_CharacterSlotContainer _characterSlotContainer;
    
    [Header("디버그용")]
    [SerializeField] private E_LobbyType _lobbyType;

    private void Awake()
    {
        _backButton.onClick.AddListener(OnClickBackButton);
        _soloPlayBtn.onClick.AddListener(OnClickSoloPlayBtn);
    }

    private void OnDestroy()
    {
        _backButton.onClick.RemoveAllListeners();
        _soloPlayBtn.onClick.RemoveAllListeners();
    }

    public void ActiveOnlinePanel()
    {
        _lobbyType = E_LobbyType.Online;
        this.gameObject.SetActive(true);
        _onlinePanel.gameObject.SetActive(true);
        _backButton.gameObject.SetActive(true);
        LobbyUI_Manager.Inst.UITitle.ActiveBackButton(false);
    }

    private bool _isTransition = false;
    public async void OnClickBackButton()        
    {
        if (_isTransition)
            return;

        _isTransition = true;
        if (_lobbyType == E_LobbyType.Online)
        {
            await LobbyManager.Inst.LeaveGame();
        }
        else
        {
            LobbyUI_Manager.Inst.UITitle.ShowTitle();
            LobbyUI_Manager.Inst.UITitle.ActiveBackButton(true);
            this.gameObject.SetActive(false);
        }
        _isTransition = false;
    }

    private void OnClickSoloPlayBtn()
    {
        this.gameObject.SetActive(false);
    }

    public void UpdateData(Fusion.NetworkDictionary<PlayerRef, PlayerData> players)
    {
        // _characterSlotContainer?.UpdateData(players);
    }
}
