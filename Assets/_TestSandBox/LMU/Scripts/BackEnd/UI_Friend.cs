using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UI_Friend : MonoBehaviour
{
    [Header("인스펙터 참조")]
    [SerializeField] private ToggleGroup _toggleGroup;
    [SerializeField] private List<UI_PanelEdgeTransition> _panelTransitions;
    [SerializeField] private InputField _inputField;
    [SerializeField] private Button _sendButton;
    [SerializeField] private UI_FriendSlotContainer _friendSlotContainer;
    [SerializeField] private Button _allAcceptButton;

    private bool _isRequestInProgress = false;

    private void Awake()
    {
        foreach (var p in _panelTransitions)
        {
            p.gameObject.SetActive(false);
        }

        NetworkEventSystem.Inst.OnGameStateChangedEvent -= OnGameStateChanged;
        NetworkEventSystem.Inst.OnGameStateChangedEvent += OnGameStateChanged;

        _inputField.onValueChanged.AddListener(OnInputValueChanged);
        _sendButton.onClick.AddListener(OnSendButtonClicked);
        _allAcceptButton.onClick.AddListener(OnAllAcceptButtonClicked);

        // 초기 상태 설정
        _sendButton.interactable = false;
    }

    private void OnDestroy()
    {
        _inputField.onValueChanged.RemoveAllListeners();
        _sendButton.onClick.RemoveAllListeners();
        _allAcceptButton.onClick.RemoveAllListeners();
    }

    /// <summary>
    /// 로비 상태가 아닌경우에는 비활성화
    /// </summary>
    private void OnGameStateChanged(Fusion.NetworkRunner runner, E_StateName prevState, E_StateName nextState)
    {
        if (nextState == E_StateName.LobbyState)
        {
            this.gameObject.SetActive(true);
        }
        else
        {
            this.gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// 모든 패널을 비활성화
    /// </summary>
    public void Clear()
    {
        foreach (var p in _panelTransitions)
        {
            p.gameObject.SetActive(false);
        }

        _inputField.text = "";
        
        _isRequestInProgress = false;
    }

    /// <summary>
    /// 입력 필드 값 변경 시 호출
    /// </summary>
    private void OnInputValueChanged(string value)
    {
        bool hasText = !string.IsNullOrEmpty(value.Trim());
        _sendButton.interactable = hasText && _isRequestInProgress == false;
    }

    /// <summary>
    /// 친구 요청 버튼 클릭 시 호출
    /// </summary>
    private void OnSendButtonClicked()
    {
        if (_isRequestInProgress)
            return;

        string targetNickname = _inputField.text.Trim();
        if (string.IsNullOrEmpty(targetNickname))
        {
            Debug.LogError("닉네임이 비어있습니다.");
            return;
        }

        // 요청 진행 상태로 변경
        _isRequestInProgress = true;
        _sendButton.interactable = false;

        Friends.Inst.SendFriendRequestByNickname(targetNickname, 
            onSuccess: () =>
            {
                Debug.Log($"친구 요청 성공: {targetNickname}");
                _inputField.text = "";
                _isRequestInProgress = false;
                _sendButton.interactable = false;
            }, 
            onFail: (error) =>
            {
                Debug.LogError($"친구 요청 실패: {error}");
                _isRequestInProgress = false;
                _sendButton.interactable = !string.IsNullOrEmpty(_inputField.text.Trim());
            }
        );
    }

    private void OnAllAcceptButtonClicked()
    {
        if (_isRequestInProgress)
        {
            Debug.Log("다른 요청이 진행 중입니다.");
            return;
        }

        _isRequestInProgress = true;
        Friends.Inst.AcceptAllFriendRequests(
            onSuccess: (acceptedCount) =>
            {
                Debug.Log($"<color=green>친구 요청 수락 완료: {acceptedCount}명</color>");
                _isRequestInProgress = false;
                
                _friendSlotContainer.UpdateFriendForm();
                _friendSlotContainer.UpdateResponseForm();
            },
            onFail: (error) =>
            {
                Debug.LogError($"친구 요청 수락 실패: {error}");
                _isRequestInProgress = false;
            }
        );
    }
}
