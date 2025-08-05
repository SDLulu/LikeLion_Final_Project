using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.InputSystem;

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
    [SerializeField] private Button _sendButton;
    [SerializeField] private ScrollRect _chatView;
    [SerializeField] private GameObject _contentPanel;
    [SerializeField] private GameObject _messagePrefab;
    [SerializeField] private InputActionReference _enterAction;

    [Header("채팅창 페이드 설정")]
    [SerializeField] private float _hideChatViewSeconds = 5f;
    [SerializeField] private float _fadeInChatViewSeconds = 0.3f;

    [Header("디버그용")]
    [SerializeField] private string _curChatString;
    public string CurChatString => _curChatString = _inputField?.text;

    /// <summary>
    /// 전역 채팅 입력 상태
    /// </summary>
    public static bool IsAnyChatActive { get; private set; } = false;

    private ChatClient _chatClient;
    private List<string> _chatHistories = new List<string>();
    private CanvasGroup _chatViewCanvasGroup;
    private Coroutine _hideChatCoroutine;

    private bool _isDelayNextFrame = false;

    public void OnInit(ChatClient chatClient)
    {
        _chatClient = chatClient;
        _inputField.onSubmit.AddListener((string text) =>
        {
            _inputField.ActivateInputField();
            UpdateChat(text);
            _inputField.text = "";
        });

        // 엔터키 입력시 채팅창만 활성화
        _enterAction.action.performed += (ctx) =>
        {
            _inputField.ActivateInputField();
        };
    }

    public void UpdateChat(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return;

        var chatHistory = new ChatHistory()
        {
            Sender = default,
            Message = text,
            Channel = ChatChannel.All,
        };
        CreateMessageUI(chatHistory);
    }

    private void OnDestroy()
    {
        _chatClient = null;
        _inputField.onSubmit.RemoveAllListeners();
    }

    public void OnSend()
    {
        if (_inputField == null || string.IsNullOrWhiteSpace(_inputField.text))
            return;

        _chatClient?.SendChatMessage(_inputField.text);

        HideChatViewAsync();
    }

    /// <summary>
    /// 텍스트 입력 처리 함수
    /// </summary>
    public string InputText(string inputText)
    {
        if (_inputField != null)
        {
            _inputField.text = inputText;
            ShowChatView();
        }
        return inputText;
    }

    public void UpdateChatHistories(Dictionary<int, ChatHistory> chatHistories)
    {
        if (chatHistories == null || _contentPanel == null)
            return;

        for (int i = _contentPanel.transform.childCount - 1; i >= 0; i--)
        {
            DestroyImmediate(_contentPanel.transform.GetChild(i).gameObject);
        }

        _chatHistories.Clear();

        foreach (var kvp in chatHistories)
        {
            CreateMessageUI(kvp.Value);
            _chatHistories.Add($"Player{kvp.Value.Sender}: {kvp.Value.Message}");
        }

        // 스크롤을 맨 아래로 이동
        if (_chatView != null)
        {
            Canvas.ForceUpdateCanvases();
            _chatView.verticalNormalizedPosition = 0f;
        }

        // 새 메시지가 있으면 채팅창을 보이고 타이머 시작
        ShowChatView();
        HideChatViewAsync();
    }

    private void HideChatViewAsync()
    {
        if (_hideChatCoroutine != null)
        {
            StopCoroutine(_hideChatCoroutine);
        }

        _hideChatCoroutine = StartCoroutine(HideChatViewCoroutine());
    }

    private IEnumerator HideChatViewCoroutine()
    {
        yield return new WaitForSeconds(_hideChatViewSeconds);

        if (_chatViewCanvasGroup != null)
        {
            float startAlpha = _chatViewCanvasGroup.alpha;
            float time = 0f;

            while (time < _fadeInChatViewSeconds)
            {
                time += Time.deltaTime;
                float alpha = Mathf.Lerp(startAlpha, 0f, time / _fadeInChatViewSeconds);
                _chatViewCanvasGroup.alpha = alpha;
                yield return null;
            }

            _chatViewCanvasGroup.alpha = 0f;
        }
    }

    private void ShowChatView()
    {
        if (_chatViewCanvasGroup != null)
        {
            _chatViewCanvasGroup.alpha = 1f;
        }
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

        string displayText = "";

        switch (chatHistory.Channel)
        {
            case ChatChannel.All:
                displayText = $"[전체] Player{chatHistory.Sender}: {chatHistory.Message}";
                messageText.color = Color.black;
                break;

            case ChatChannel.Whisper:
                displayText = $"[귓속말] Player{chatHistory.Sender} → Player{chatHistory.Receiver}: {chatHistory.Message}";
                messageText.color = Color.yellow;
                break;

            case ChatChannel.Mine:
                displayText = $"[나에게만] Player{chatHistory.Sender}: {chatHistory.Message}";
                messageText.color = Color.blue;
                break;
        }

        messageText.text = displayText;
    }
}