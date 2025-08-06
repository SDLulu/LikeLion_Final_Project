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

    [Header("채팅창 페이드 설정")]
    [SerializeField] private float _hideChatViewSeconds = 5f;
    [SerializeField] private float _fadeInChatViewSeconds = 0.3f;

    [Header("디버그용")]
    [SerializeField] private string _curChatString;
    public string CurChatString => _curChatString = _inputField?.text;
    private ChatClient _chatClient;
    private List<string> _chatHistories = new List<string>();
    private CanvasGroup _chatViewCanvasGroup;
    private Coroutine _hideChatCoroutine;

    public static bool IsFocusChat { get; private set; } = false;

    private PlayerRef LocalPlayer => LobbyManager.Inst.LocalPlayer;

    private void OnDestroy()
    {
        _chatClient = null;
        _originTicks.Clear();
        _inputField.onSubmit.RemoveAllListeners();
    }

    /// <summary>
    /// Note - 로컬 ChatClient가 네트워크 초기화가 이루어질때 호출
    /// </summary>
    public void OnInit(ChatClient chatClient)
    {
        var localNickName = chatClient.GetComponentInParent<PlayerData>().NickName;
        Debug.Log($"UI_Chating OnInit {localNickName}");
        _chatClient = chatClient;
        _inputField.onSubmit.AddListener((string text) =>
        {
            if (_inputField.isFocused && _inputField.text.Length > 0)
            {
                SendChat(text);
                ActiveChat();
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
        _inputField.ActivateInputField();
        _frameBG.gameObject.SetActive(true);
        IsFocusChat = true;
    }

    public void DeactiveChat()
    {
        _inputField.text = "";
        _inputField.DeactivateInputField();
        _frameBG.gameObject.SetActive(false);
        IsFocusChat = false;
    }

    public void SendChat(string text, ChatChannel channel = ChatChannel.None)
    {
        if (string.IsNullOrWhiteSpace(text))
            return;

        _chatClient.SendChatMessage(text, channel);
    }

    private List<Tick> _originTicks = new List<Tick>();
    public void UpdateChatHistories(ChatHistoryList chatHistories)
    {
        // // 서버의 틱정보가 로컬에 존재하지 않으면 추가
        // List<Tick> ticks = new();
        // foreach (var chat in chatHistories.ChatHistories)
        // {
        //     if (_originTicks.Contains(chat.TickTime) == false)
        //         continue;
        //     ticks.Add(chat.TickTime);
        // }

        // // 정렬 및 생성
        // ticks.Sort();
        // foreach (var t in ticks)
        // {
        //     var chat = chatHistories.Get(t);
        //     CreateMessageUI(chat);
        // }
        // _originTicks.AddRange(ticks);

        // // 스크롤을 맨 아래로 이동
        // Canvas.ForceUpdateCanvases();
        // _chatView.verticalNormalizedPosition = 0f;
        // // // 새 메시지가 있으면 채팅창을 보이고 타이머 시작
        // // ShowChatView();
        // // HideChatViewAsync();
    }

    public void OnSend()
    {
        if (_inputField == null || string.IsNullOrWhiteSpace(_inputField.text))
            return;

        //_chatClient?.SendChatMessage(chatHistory);

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

        var (inputText, msgColor) = _chatClient.GetMessage(chatHistory);
        messageText.text = inputText;
        messageText.color = msgColor;
    }
}