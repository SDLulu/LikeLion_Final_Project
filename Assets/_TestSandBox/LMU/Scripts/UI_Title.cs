using Fusion;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UI_Title : MonoBehaviour
{
    [Header("닉네임 패널")]
    [SerializeField] private RectTransform createNickNamePanel;
    [SerializeField] private Button createNickNameBtn;
    [SerializeField] private TMP_InputField nickNameInputField;

    [Header("입장 패널")]
    [SerializeField] private RectTransform joinRoomPanel;
    [SerializeField] private Button joinRoomBtn;    
    [SerializeField] private Button createRoomBtn;
    [SerializeField] private Button randomJoinRoomBtn;

    private void Awake()
    {
        createNickNameBtn.onClick.AddListener(OnClickCreateNickNameBtn);
        nickNameInputField.onValueChanged.AddListener(OnValueChangedNickName);

        joinRoomBtn.onClick.AddListener(OnClickJoinRoomBtn);
        createRoomBtn.onClick.AddListener(OnClickCreateRoomBtn);
        randomJoinRoomBtn.onClick.AddListener(OnClickRandomJoinRoomBtn);

        ActiveCreateNickNamePanel();
    }

    private void OnDestroy()
    {
        createNickNameBtn.onClick.RemoveAllListeners();
        nickNameInputField.onValueChanged.RemoveAllListeners();

        joinRoomBtn.onClick.RemoveAllListeners();
        createRoomBtn.onClick.RemoveAllListeners();
        randomJoinRoomBtn.onClick.RemoveAllListeners();
    }

    private void ActiveCreateNickNamePanel()
    {
        createNickNamePanel.gameObject.SetActive(true);
        joinRoomPanel.gameObject.SetActive(false);
    }

    private void ActiveJoinRoomPanel()
    {
        createNickNamePanel.gameObject.SetActive(false);
        joinRoomPanel.gameObject.SetActive(true);
    }


    // --- 닉네임 입력 패널
    private void OnValueChangedNickName(string value)
    {
        if(string.IsNullOrEmpty(value))
        {
            createNickNameBtn.interactable = false;
            return;
        }

        createNickNameBtn.interactable = true;
        //Debug.Log($"닉네임 변경 : {_nickNameInputField.text}");
    }

    private void OnClickCreateNickNameBtn()
    {
        if(string.IsNullOrEmpty(nickNameInputField.text))
        {
            Debug.LogWarning("닉네임이 비어있습니다.");
            return;
        }

        Debug.Log($"닉네임 확정 : {nickNameInputField.text}");
        ActiveJoinRoomPanel();
    }

    // --- 입장 패널
    private void OnClickJoinRoomBtn()
    {
        Debug.Log("입장 패널 활성화");
        LobbyManager.Inst.NetRunner.JoinOrCreateLobby();
    }

    private void OnClickCreateRoomBtn()
    {
        Debug.Log("방 생성 패널 활성화");
        LobbyManager.Inst.NetRunner.JoinOrCreateLobby();
    }

    private void OnClickRandomJoinRoomBtn()
    {
        Debug.Log("랜덤 입장 패널 활성화");

    }

}
