using Fusion;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MiddleSectionPanel : LobbyPanelBase
{
    [Header("MiddleSectionPanel Vars")] 
    [SerializeField] private Button joinRandomRoomBtn;
    [SerializeField] private Button joinRoomByArgBtn;
    [SerializeField] private Button createRoomBtn;

    [SerializeField] private TMP_InputField joinRoomByArgInputField;
    [SerializeField] private TMP_InputField createRoomInputField;
    // 🌐 네트워크 관련: 네트워크 게임 시작을 담당하는 컨트롤러
    private NetworkRunnerController networkRunnerController;
    
    public override void InitPanel(LobbyUIManager uiManager)
    {
        base.InitPanel(uiManager);

        // 🌐 네트워크 관련: 전역 네트워크 컨트롤러 참조 가져오기
        networkRunnerController = GlobalManagers.Instance.NetworkRunnerController;
        
        // 🌐 네트워크 관련: 각 버튼에 네트워크 게임 시작 함수 연결
        joinRandomRoomBtn.onClick.AddListener(JoinRandomRoom);
        joinRoomByArgBtn.onClick.AddListener(() => CreateRoom(GameMode.Client, joinRoomByArgInputField.text));
        createRoomBtn.onClick.AddListener(() => CreateRoom(GameMode.Host, createRoomInputField.text));
    }

    // 🌐 네트워크 관련: 게임 방 생성/참가 함수
    // GameMode.Host = 내가 방장(서버 역할)
    // GameMode.Client = 다른 사람 방에 참가
    private void CreateRoom(GameMode mode, string field)
    {
        if (field.Length >= 2)
        {
            Debug.Log($"------------{mode}------------");
            // 🌐 네트워크 관련: 실제 네트워크 게임 시작!
            // mode: Host/Client, field: 방 이름
            networkRunnerController.StartGame(mode, field);
        }
    }

    // 🌐 네트워크 관련: 랜덤 방 참가 함수
    private void JoinRandomRoom()
    {
        Debug.Log($"------------JoinRandomRoom!------------");
        // 🌐 네트워크 관련: AutoHostOrClient = 방이 있으면 참가, 없으면 생성
        // string.Empty = 방 이름 없음(랜덤 매칭)
        networkRunnerController.StartGame(GameMode.AutoHostOrClient, string.Empty);
    }
}