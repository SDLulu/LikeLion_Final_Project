using Fusion;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UI_EnterOnline : MonoBehaviour
{
    [Header("입장 패널")]
    [SerializeField] private RectTransform _joinRoomPanel;
    [SerializeField] private Button _joinRoomBtn;    
    [SerializeField] private Button _createRoomBtn;
    [SerializeField] private Button _randomJoinRoomBtn;

    [Header("Prevent 패널")]
    [SerializeField] private RectTransform _preventPanel;

    private void Awake()
    {
        _preventPanel.gameObject.SetActive(false);
        _joinRoomBtn.onClick.AddListener(OnClickJoinRoomBtn);
        _createRoomBtn.onClick.AddListener(() => _ = OnClickCreateRoomBtn());
        _randomJoinRoomBtn.onClick.AddListener(OnClickRandomJoinRoomBtn);

        ActiveCreateNickNamePanel();
    }

    private void OnDestroy()
    {
        _joinRoomBtn.onClick.RemoveAllListeners();
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


    // --- 입장 패널
    private void OnClickJoinRoomBtn()
    {
        // Todo - 방의 세션코드를 맞춰서 입장
        return;
        // Debug.Log("입장 패널 활성화");
        // preventPanel.gameObject.SetActive(true);
        // await LobbyManager.Inst.NetRunner.JoinOrCreateLobby(
        //     mode: GameMode.Host,
        //     roomName: "TestRoom",
        //     OnEnterLobby: () =>
        //     {
        //         UIController.ActiveLobbyUI();
        //         preventPanel.gameObject.SetActive(false);
        //     }
        // );
    }

    /// <summary>
    /// 풀백용 함수 진입함수
    /// </summary>
    public async Awaitable FallbackRun()
    {
        string roomName = "TestRoom" + Random.Range(1000, 9999);
        _preventPanel.gameObject.SetActive(true);
        await LobbyManager.Inst.JoinOrCreateLobby(
            mode: GameMode.AutoHostOrClient,
            roomName: roomName,
            OnEnterLobby: () =>
            {
                LobbyUI_Manager.Inst.ActiveLobbyOnLineUI();
                _preventPanel.gameObject.SetActive(false);
            },
            OnCancel: () =>
            {
                _preventPanel.gameObject.SetActive(false);
            }
        );
    }

    public async Awaitable RunFastMode()
    {
        _preventPanel.gameObject.SetActive(true);
        await LobbyManager.Inst.JoinOrCreateLobby(
            mode: GameMode.AutoHostOrClient,
            roomName: "TestRoom",
            OnEnterLobby: () =>
            {
                LobbyUI_Manager.Inst.ActiveLobbyOnLineUI();
                _preventPanel.gameObject.SetActive(false);
            },
            OnCancel: () =>
            {
                _preventPanel.gameObject.SetActive(false);
            }
        );
    }

    public async Awaitable OnClickCreateRoomBtn()
    {
        _preventPanel.gameObject.SetActive(true);
        await LobbyManager.Inst.JoinOrCreateLobby(
            mode: GameMode.Host,
            roomName: "TestRoom",
            OnEnterLobby: () =>
            {
                LobbyUI_Manager.Inst.ActiveLobbyOnLineUI();
                _preventPanel.gameObject.SetActive(false);
            },
            OnCancel: () =>
            {
                _preventPanel.gameObject.SetActive(false);
            }
        );
    }

    private async void OnClickRandomJoinRoomBtn()
    {
        _preventPanel.gameObject.SetActive(true);
        await LobbyManager.Inst.JoinOrCreateLobby(
            mode: GameMode.AutoHostOrClient,
            roomName: "TestRoom",
            OnEnterLobby: () =>
            {
                LobbyUI_Manager.Inst.ActiveLobbyOnLineUI();
                _preventPanel.gameObject.SetActive(false);
            },
            OnCancel: () =>
            {
                _preventPanel.gameObject.SetActive(false);
            }
        );
    }


}
