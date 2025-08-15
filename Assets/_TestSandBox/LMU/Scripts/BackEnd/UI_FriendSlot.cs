using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static Friends;


public class UI_FriendSlot : MonoBehaviour
{
    [Header("인스펙터 참조")]
    [SerializeField] private Image _profileImage;
    [SerializeField] private TMP_Text _nicknameText;
    [SerializeField] private TMP_Text _gameStatesText;
    [SerializeField] private Image _gameStateImage;

    public void UpdateData(E_FriendSlotForm form, FriendData data)
    {
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

    public void UpdateFriendForm(FriendData data)
    {
        _profileImage.sprite = Resources.Load<Sprite>($"ProfileImages/{data.ProfileImageID}");
        _nicknameText.text = data.NickName;
        _gameStatesText.text = data.GameStates;
        //_gameStateImage.color = data.GameStates == "Online" ? Color.green : Color.red;
    }

    /// <summary>
    /// 보낸 친구 요청 폼으로 업데이트
    /// </summary>
    public void UpdateRequestForm(FriendData data)
    {
        _profileImage.sprite = Resources.Load<Sprite>($"ProfileImages/{data.ProfileImageID}");
        _nicknameText.text = data.NickName;
        _gameStatesText.text = data.GameStates; // "요청 전송됨"
        
        // 보낸 요청은 회색으로 표시
        _gameStateImage.color = Color.gray;
    }

    /// <summary>
    /// 받은 친구 요청 폼으로 업데이트
    /// </summary>
    public void UpdateResponseForm(FriendData data)
    {
        _profileImage.sprite = Resources.Load<Sprite>($"ProfileImages/{data.ProfileImageID}");
        _nicknameText.text = data.NickName;
        _gameStatesText.text = data.GameStates; // "요청 대기중"
        
        // 받은 요청은 주황색으로 표시
        _gameStateImage.color = Color.yellow;
    }


}
