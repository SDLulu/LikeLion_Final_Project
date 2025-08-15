using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.InputSystem;
using Fusion;

public enum E_ChatInputState
{
    None,
    Focus,
    Input,
    Send,
}

public class UI_Chating : MonoBehaviour
{
    [Header("인스펙터 참조")]
    [SerializeField] private InputField _inputField;
    [SerializeField] private ScrollRect _chatView;
    [SerializeField] private GameObject _contentPanel;
    [SerializeField] private GameObject _messagePrefab;
    [SerializeField] private InputActionReference _enterAction;
    [SerializeField] private RectTransform _frameBG;

    private ChatClient _chatClient;
    private List<string> _chatHistories = new List<string>();

    public static bool IsFocusChat { get; private set; } = false;

    private PlayerRef LocalPlayer => LobbyManager.Inst.LocalPlayer;

    private void OnDestroy() => Clear();
    public void Clear()
    {
        RemoveAllMessageUI();
        _chatClient = null;
        _processedMessages.Clear();
        _inputField.onSubmit.RemoveAllListeners();
    }


    /// <summary>
    /// Note - 로컬 ChatClient가 네트워크 초기화가 이루어질때 호출
    /// </summary>
    public void OnInit(ChatClient chatClient)
    {
        Clear();
        var localNickName = chatClient.GetComponentInParent<PlayerData>().NickName;
        Debug.Log($"UI_Chating OnInit {localNickName}");
        _chatClient = chatClient;

        // InputField 설정 - 자동 활성화 방지
        _inputField.shouldHideMobileInput = true;

        _inputField.onSubmit.AddListener((string text) =>
        {
            if (_inputField.isFocused && _inputField.text.Length > 0)
            {
                SendChat(text);
                DeactiveChat();
            }
            else if (_inputField.isFocused && _inputField.text.Length <= 0)
            {
                DeactiveChat();
            }
        });

        // 엔터키 입력시 채팅창만 활성화
        _enterAction.action.performed += (ctx) =>
        {
            if (_inputField.isFocused == false)
            {
                ActiveChat();
            }
        };

        DeactiveChat();
        ChatManager.Inst.AddChatAction(UpdateChatHistories);
        SendChat(localNickName + "님이 입장하셨습니다!", ChatChannel.System);
    }

    public void ActiveChat()
    {
        _inputField.text = "";
        _inputField.interactable = true;
        _inputField.ActivateInputField();
        _frameBG.gameObject.SetActive(true);
        IsFocusChat = true;
    }

    public void DeactiveChat()
    {
        _inputField.text = "";
        _inputField.DeactivateInputField();
        _inputField.interactable = false;
        _frameBG.gameObject.SetActive(false);
        IsFocusChat = false;
    }

    public void SendChat(string text, ChatChannel channel = ChatChannel.None)
    {
        if (string.IsNullOrWhiteSpace(text))
            return;

        if (channel == ChatChannel.None)
            channel = ChatChannel.All;

        _chatClient.SendChatMessage(text, channel);
    }

    private HashSet<string> _processedMessages = new HashSet<string>();

    /// <summary>
    /// 채팅 히스토리 업데이트 처리
    /// </summary>
    public void UpdateChatHistories(ChatHistoryList chatHistories)
    {
        // 새로운 메시지만 필터링 (틱 + 메시지 내용 + 채널 조합으로 고유 식별)
        List<ChatHistory> newMessages = new List<ChatHistory>();
        foreach (var chat in chatHistories.ChatHistories)
        {
            // 메시지 고유 키 생성 (틱 + 발신자 + 메시지 + 채널)
            string messageKey = $"{chat.TickTime}_{chat.Sender}_{chat.Message}_{chat.Channel}";
            
            if (_processedMessages.Contains(messageKey) == false)
            {
                newMessages.Add(chat);
                _processedMessages.Add(messageKey);
            }
        }

        if (newMessages.Count == 0)
            return;

        // 틱 시간 기준으로 정렬후 메시지를 생성
        newMessages.Sort((a, b) => a.TickTime.CompareTo(b.TickTime));
        foreach (var chat in newMessages)
        {
            CreateMessageUI(chat);
            _chatHistories.Add($"Player{chat.Sender}: {chat.Message}");
        }

        // 스크롤을 맨 아래로 이동
        Canvas.ForceUpdateCanvases();
        _chatView.verticalNormalizedPosition = 0f;
    }

    private void CreateMessageUI(ChatHistory chatHistory)
    {
        if (_messagePrefab == null || _contentPanel == null)
            return;

        var messageObj = Instantiate(_messagePrefab, _contentPanel.transform);
        var messageText = messageObj.GetComponentInChildren<TextMeshProUGUI>();
        if (messageText == null)
        {
            Debug.LogError("메세지 프리팹에 TextMeshProUGUI 컴포넌트가 없습니다.");
            return;
        }

        var inputText = _chatClient.GetMessage(chatHistory);

        if (chatHistory.Channel == ChatChannel.System)
            messageText.alignment = TextAlignmentOptions.Center;
        else
            messageText.alignment = TextAlignmentOptions.Left;

        messageText.text = inputText;
    }

    public void RemoveAllMessageUI()
    {
        foreach (Transform child in _contentPanel.transform)
        {
            GameObject.Destroy(child.gameObject);
        }
    }

    public void ClearMessageCache()
    {
        _processedMessages.Clear();
    }

}