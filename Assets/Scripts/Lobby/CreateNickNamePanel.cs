using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CreateNickNamePanel : LobbyPanelBase
{
    [Header("CreateNickNamePanel Vars")] [SerializeField]
    private TMP_InputField inputField;

    [SerializeField] private Button createNicknameBtn;
    private const int MAX_CHAR_FOR_NICKNAME = 2;

    public override void InitPanel(LobbyUIManager lobbyUIManager)
    {
        base.InitPanel(lobbyUIManager);
        createNicknameBtn.interactable = false;
        createNicknameBtn.onClick.AddListener(OnClickCreateNickname);
        inputField.onValueChanged.AddListener(OnInputValueChanged);
    }

    private void OnInputValueChanged(string arg0)
    {
        createNicknameBtn.interactable = arg0.Length >= MAX_CHAR_FOR_NICKNAME;
    }

    private void OnClickCreateNickname()
    {
        var nickName = inputField.text;
        if (nickName.Length >= MAX_CHAR_FOR_NICKNAME)
        {
            // 🌐 네트워크 관련: 로컬 플레이어의 닉네임을 NetworkRunnerController에 저장
            // 이 닉네임은 나중에 다른 플레이어들에게 표시될 예정
            GlobalManagers.Instance.NetworkRunnerController.SetPlayerNickname(nickName);

            base.ClosePanel();
            lobbyUIManager.ShowPanel(LobbyPanelType.MiddleSectionPanel);
        }
    }
}