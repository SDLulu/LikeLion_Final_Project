using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UI_GameInvitePopup : MonoBehaviour
{
    [Header("UI 참조")]
    [SerializeField] private GameObject _popupPanel;
    [SerializeField] private TMP_Text _inviterNameText;
    [SerializeField] private TMP_Text _messageText;
    [SerializeField] private Button _acceptButton;
    [SerializeField] private Button _declineButton;
    
    private GameInvitePayload _currentInvite;
    
    private void Awake()
    {
        _acceptButton.onClick.AddListener(OnAcceptButtonClicked);
        _declineButton.onClick.AddListener(OnDeclineButtonClicked);
        GameInviteManager.Inst.OnGameInviteReceived += ShowInvitePopup;
        
        HidePopup();
    }
    
    private void OnDestroy()
    {
        _acceptButton.onClick.RemoveAllListeners();
        _declineButton.onClick.RemoveAllListeners();
    }
    
    /// <summary>
    /// 게임 초대 팝업 표시
    /// </summary>
    public void ShowInvitePopup(GameInvitePayload invitePayload)
    {
        if (invitePayload == null)
            return;
        
        _currentInvite = invitePayload;
        
        _inviterNameText.text = invitePayload.InviterName;
        _messageText.text = $"{invitePayload.InviterName}님이 게임에 초대했습니다.";
        _popupPanel.SetActive(true);
        
        Debug.Log($"<color=cyan>게임 초대 팝업 표시: {invitePayload.InviterName}</color>");
    }
    
    public void HidePopup()
    {
        _popupPanel.SetActive(false);
        _currentInvite = null;
    }
    
    private void OnAcceptButtonClicked()
    {
        if (_currentInvite == null)
        {
            Debug.LogError("현재 초대 정보가 없습니다.");
            return;
        }
        
        Debug.Log($"<color=green>게임 초대 수락: {_currentInvite.InviterName}</color>");
        
        // 초대 수락 처리
        GameInviteManager.Inst.AcceptGameInvite(_currentInvite);
        HidePopup();
    }
    
    private void OnDeclineButtonClicked()
    {
        if (_currentInvite == null)
        {
            Debug.LogError("현재 초대 정보가 없습니다.");
            return;
        }
        
        Debug.Log($"<color=red>게임 초대 거절: {_currentInvite.InviterName}</color>");
        
        GameInviteManager.Inst.DeclineGameInvite(_currentInvite);
        HidePopup();
    }
}
