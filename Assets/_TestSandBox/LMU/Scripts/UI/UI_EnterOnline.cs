using Fusion;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UI_EnterOnline : MonoBehaviour
{
    [Header("입장 패널")]
    [SerializeField] private RectTransform _joinRoomPanel;
    [SerializeField] private Button _createRoomBtn;
    [SerializeField] private Button _randomJoinRoomBtn;
    [SerializeField] private InputField _roomNameInputField;

    [Header("Prevent 패널")]
    [SerializeField] private RectTransform _preventPanel;

    private void Awake()
    {
        _preventPanel.gameObject.SetActive(false);
        _createRoomBtn.onClick.AddListener(() => _ = OnClickCreateRoomBtn());
        _randomJoinRoomBtn.onClick.AddListener(OnClickRandomJoinRoomBtn);

        ActiveCreateNickNamePanel();
    }

    private void OnEnable()
    {
        SetRandomRoomName();
    }

    public void SetRandomRoomName()
    {
        int rand = Random.Range(1, 99999);
        string roomName = $"Room - {rand}";
        _roomNameInputField.text = roomName;
    }

    public string GetRoomName()
    {
        return _roomNameInputField.text;
    }

    private void OnDestroy()
    {
        _createRoomBtn.onClick.RemoveAllListeners();
        _randomJoinRoomBtn.onClick.RemoveAllListeners();
    }

    private void ActiveCreateNickNamePanel()
    {
        _joinRoomPanel.gameObject.SetActive(false);
    }

    private void ActiveJoinRoomPanel()
    {
        _joinRoomPanel.gameObject.SetActive(true);
    }


    /// <summary>
    /// 풀백용 함수 진입함수
    /// </summary>
    public async Awaitable FallbackRun()
    {
        SetRandomRoomName();
        string roomName = GetRoomName();
        _preventPanel.gameObject.SetActive(true);
        await LobbyManager.Inst.JoinOrCreateLobby(false, GameMode.AutoHostOrClient, roomName);
        _preventPanel.gameObject.SetActive(false);
    }

    /// <summary>
    /// 호스트로 방을 생성하며 입장
    /// </summary>
    public async Awaitable OnClickCreateRoomBtn()
    {
        _preventPanel.gameObject.SetActive(true);
        string roomName = GetRoomName();
        await LobbyManager.Inst.JoinOrCreateLobby(false, GameMode.Host, roomName);
        _preventPanel.gameObject.SetActive(false);
    }

    /// <summary>
    /// 랜덤방에 입장 - 만약 방이 없다면 방을 생성하면서 호스트로 자동입장
    /// </summary>
    private async void OnClickRandomJoinRoomBtn()
    {
        _preventPanel.gameObject.SetActive(true);
        string roomName = GetRoomName();
        await LobbyManager.Inst.JoinOrCreateLobby(false, GameMode.AutoHostOrClient, roomName);
        _preventPanel.gameObject.SetActive(false);
    }


}
