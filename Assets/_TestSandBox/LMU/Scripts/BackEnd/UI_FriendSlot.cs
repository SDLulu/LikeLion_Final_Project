using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static Friends;


public class UI_FriendSlot : MonoBehaviour
{
    [Header("인스펙터 참조")]
    [SerializeField] private Image _profileImage;
    [SerializeField] private TMP_Text _nicknameText;
    [SerializeField] private Image _gameStateImage;
    [SerializeField] private Button _inviteButton;
    
    private FriendData _currentFriendData;

    private void Awake()
    {
        _inviteButton.onClick.AddListener(OnInviteButtonClicked);
        NetworkEventSystem.Inst.OnGameStateChangedEvent += OnGameStateChanged;
    }

    private void OnDestroy()
    {
        _inviteButton.onClick.RemoveAllListeners();
    }

    private E_StateName _curState;
    private void OnGameStateChanged(Fusion.NetworkRunner runner, E_StateName prevState, E_StateName nextState)
    {
        if (nextState == E_StateName.LobbyState)
            _curState = nextState;
    }

    private void OnInviteButtonClicked()
    {
        if (_currentFriendData == null)
        {
            Debug.LogError("친구 데이터가 없습니다.");
            return;
        }

        Debug.Log($"<color=yellow>{_currentFriendData.NickName} 님에게 게임 초대를 보냅니다...</color>");

        // Friends 매니저를 통해 게임 초대 전송
        Friends.Inst.SendGameInvite(
            _currentFriendData.InDate,
            _currentFriendData.NickName,
            onSuccess: () =>
            {
                Debug.Log($"<color=green>{_currentFriendData.NickName} 님에게 초대를 보냈습니다!</color>");
                // TODO: UI 피드백 (토스트 메시지 등)
            },
            onFail: (errorMessage) =>
            {
                Debug.LogError($"초대 전송 실패: {errorMessage}");
                // TODO: UI 피드백 (에러 메시지 등)
            }
        );
    }

    public void UpdateData(E_FriendSlotForm form, FriendData data)
    {
        _currentFriendData = data;
        
        switch (form)
        {
            case E_FriendSlotForm.Friend:
                UpdateFriendForm(data);
                break;
            case E_FriendSlotForm.Response:
                UpdateResponseForm(data);
                break;
            case E_FriendSlotForm.Request:
                UpdateRequestForm(data);
                break;
        }
    }

    private void Update()
    {
        if (_curState == E_StateName.LobbyState)
            _inviteButton.gameObject.SetActive(true);
        else
            _inviteButton.gameObject.SetActive(false);
    }

    public void UpdateFriendForm(FriendData data)
    {
        _profileImage.sprite = Resources.Load<Sprite>($"ProfileImages/{data.ProfileImageID}");
        _nicknameText.text = data.NickName;
        _inviteButton.gameObject.SetActive(true);
        //_gameStateImage.color = data.GameStates == "Online" ? Color.green : Color.red;
    }

    public void UpdateRequestForm(FriendData data)
    {
        _profileImage.sprite = Resources.Load<Sprite>($"ProfileImages/{data.ProfileImageID}");
        _nicknameText.text = data.NickName;
        _inviteButton.gameObject.SetActive(false);
        _gameStateImage.color = Color.gray;
    }

    public void UpdateResponseForm(FriendData data)
    {
        _profileImage.sprite = Resources.Load<Sprite>($"ProfileImages/{data.ProfileImageID}");
        _nicknameText.text = data.NickName;
        _inviteButton.gameObject.SetActive(false);
        _gameStateImage.color = Color.yellow;
    }


}
