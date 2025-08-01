using Fusion;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UI_EnterOnline : MonoBehaviour
{

    [Header("입장 패널")]
    [SerializeField] private RectTransform joinRoomPanel;
    [SerializeField] private Button joinRoomBtn;    
    [SerializeField] private Button createRoomBtn;
    [SerializeField] private Button randomJoinRoomBtn;

    [Header("Prevent 패널")]
    [SerializeField] private RectTransform preventPanel;

    private void Awake()
    {
        preventPanel.gameObject.SetActive(false);
        joinRoomBtn.onClick.AddListener(OnClickJoinRoomBtn);
        createRoomBtn.onClick.AddListener(() => _ = OnClickCreateRoomBtn());
        randomJoinRoomBtn.onClick.AddListener(OnClickRandomJoinRoomBtn);

        ActiveCreateNickNamePanel();
    }

    private void OnDestroy()
    {
        joinRoomBtn.onClick.RemoveAllListeners();
        createRoomBtn.onClick.RemoveAllListeners();
        randomJoinRoomBtn.onClick.RemoveAllListeners();
    }

    private void ActiveCreateNickNamePanel()
    {
        joinRoomPanel.gameObject.SetActive(false);
    }

    private void ActiveJoinRoomPanel()
    {
        joinRoomPanel.gameObject.SetActive(true);
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

    public async Awaitable RunFastMode()
    {
        preventPanel.gameObject.SetActive(true);
        await LobbyManager.Inst.JoinOrCreateLobby(
            mode: GameMode.AutoHostOrClient,
            roomName: "TestRoom",
            OnEnterLobby: () =>
            {
                LobbyUI_Manager.Inst.ActiveLobbyOnLineUI();
                preventPanel.gameObject.SetActive(false);
            }
        );
    }

    public async Awaitable OnClickCreateRoomBtn()
    {
        preventPanel.gameObject.SetActive(true);
        await LobbyManager.Inst.JoinOrCreateLobby(
            mode: GameMode.Host,
            roomName: "TestRoom",
            OnEnterLobby: () =>
            {
                LobbyUI_Manager.Inst.ActiveLobbyOnLineUI();
                preventPanel.gameObject.SetActive(false);
            }
        );
    }

    private async void OnClickRandomJoinRoomBtn()
    {
        preventPanel.gameObject.SetActive(true);
        await LobbyManager.Inst.JoinOrCreateLobby(
            mode: GameMode.AutoHostOrClient,
            roomName: "TestRoom",
            OnEnterLobby: () =>
            {
                LobbyUI_Manager.Inst.ActiveLobbyOnLineUI();
                preventPanel.gameObject.SetActive(false);
            }
        );
    }


}
