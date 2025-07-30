using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using LMCore;

public class UI_Title : MonoBehaviour
{
    [Header("인스펙터 참조")]
    [SerializeField] private Button soloPlayBtn;
    [SerializeField] private Button onlinePlayBtn;
    [SerializeField] private Button settingBtn;
    [SerializeField] private Button exitBtn;
    [SerializeField] private RectTransform _titleButtonPanel;
    [SerializeField] private RectTransform _enterOnlinePanel;
    [SerializeField] private RectTransform _preventPanel;
    [SerializeField] private UI_CreateNickName _uiCreateNickName;

    [Header("EnterOnline 패널 뒤로가기")]
    [SerializeField] private Button _enterOnlineBackBtn;

    [Header("Tween 설정값")]
    [SerializeField] private float _moveDistance = 300f;
    [SerializeField] private float _moveDuration = 0.7f;
    [SerializeField] private float _moveOutDuration = 0.25f;
    [SerializeField] private float _scaleStart = 0.8f;
    [SerializeField] private float _scaleUp = 1.1f;
    [SerializeField] private float _scaleUpDuration = 0.25f;
    [SerializeField] private float _scaleDownDuration = 0.2f;

    private Vector3 _originPos;
    private Vector3 _offscreenPos;
    private Vector3 _nicknameOriginPos;
    private Vector3 _onlinePanelOriginPos;
    private Vector3 _onlinePanelOffscreenPos;
    private Tween _titleMoveTween;
    private Tween _titleScaleTween;
    private Tween _nicknameMoveTween;
    private Tween _onlinePanelMoveTween;
    private Tween _onlineBackBtnScaleTween;
    private float _canvasHeight;
    private float _canvasWidth;

    private void Awake()
    {
        soloPlayBtn.onClick.AddListener(OnClickSoloPlayBtn);
        onlinePlayBtn.onClick.AddListener(OnClickOnlinePlayBtn);
        settingBtn.onClick.AddListener(OnClickSettingBtn);
        exitBtn.onClick.AddListener(OnClickExitBtn);
        _enterOnlineBackBtn.onClick.AddListener(OnClickEnterOnlineBackBtn);

        _uiCreateNickName.gameObject.SetActive(true);
        _titleButtonPanel.gameObject.SetActive(true);
        _enterOnlinePanel.gameObject.SetActive(false);

        _originPos = _titleButtonPanel.anchoredPosition;
        _nicknameOriginPos = _uiCreateNickName.Holder.anchoredPosition;
        _onlinePanelOriginPos = _enterOnlinePanel.anchoredPosition;
        var canvas = GetComponent<Canvas>();
        _canvasHeight = canvas.pixelRect.height;
        _canvasWidth = canvas.pixelRect.width;
        _offscreenPos = _originPos + new Vector3(0, -_canvasHeight, 0);
        _onlinePanelOffscreenPos = _onlinePanelOriginPos + new Vector3(0, -_canvasHeight, 0);
        _titleButtonPanel.anchoredPosition = _offscreenPos;
        _titleButtonPanel.localScale = Vector3.one * _scaleStart;
        _uiCreateNickName.Holder.anchoredPosition = (Vector2)_nicknameOriginPos + new Vector2(-_canvasWidth, 0);
        _uiCreateNickName.Holder.gameObject.SetActive(true);
    }

    public void Show()
    {
        if (gameObject.activeSelf == false)
        {
            gameObject.SetActive(true);
        }
        ShowTitlePanel_FromOutBottomToCenter();
        AnimateNicknamePanelIn(_uiCreateNickName.Holder);
    }

    public void Hide()
    {
        UI_HoverText.IsHoverBlocked = true;
        _titleMoveTween?.Kill();
        _titleScaleTween?.Kill();
        _titleMoveTween = _titleButtonPanel.DOAnchorPos(_originPos + new Vector3(0, _canvasHeight, 0), _moveOutDuration)
            .SetEase(Ease.InBack)
            .OnComplete(() =>
            {
                gameObject.SetActive(false);
                UI_HoverText.IsHoverBlocked = false;
            });
    }

    private void OnDestroy()
    {
        soloPlayBtn.onClick.RemoveListener(OnClickSoloPlayBtn);
        onlinePlayBtn.onClick.RemoveListener(OnClickOnlinePlayBtn);
        settingBtn.onClick.RemoveListener(OnClickSettingBtn);
        exitBtn.onClick.RemoveListener(OnClickExitBtn);
        _enterOnlineBackBtn.onClick.RemoveListener(OnClickEnterOnlineBackBtn);
        _titleMoveTween?.Kill();
        _titleScaleTween?.Kill();
        _nicknameMoveTween?.Kill();
        _onlinePanelMoveTween?.Kill();
        _onlineBackBtnScaleTween?.Kill();
    }

    private void OnClickSoloPlayBtn()
    {
        Debug.Log("OnClickSoloPlayBtn 호출됨!");
        LobbyUI_Manager.Inst.UILobby.ActiveSoloPanel();
    }

    private UI_GlobalSetting _uiGlobalSetting = null;
    public UI_GlobalSetting UIGlobalSetting => _uiGlobalSetting ??= FindAnyObjectByType<UI_GlobalSetting>();
    private void OnClickEnterOnlineBackBtn()
    {
        HideOnlinePanel_FromCenterToOutTop(() =>
        {
            AnimateNicknamePanelIn(_uiCreateNickName.Holder);
            ShowTitlePanel_FromOutBottomToCenter();
        });
        UIGlobalSetting.ActiveUI(true);
    }

    private void OnClickOnlinePlayBtn()
    {
        AnimateNicknamePanelOut(_uiCreateNickName.Holder);
        HideTitlePanel_FromCenterToOutTop(() =>
        {
            ShowOnlinePanel_FromOutBottomToCenter();
            UIGlobalSetting.ActiveUI(false);
        });
    }

    private void OnClickSettingBtn()
    {

    }

    private async void OnClickExitBtn()
    {
        await Fader.Inst.FadeOutAsync(seconds: 0.5f);

        Application.Quit();
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #endif
    }

    private void AnimateNicknamePanelIn(RectTransform panel)
    {
        UI_HoverText.IsHoverBlocked = true;
        _nicknameMoveTween?.Kill();
        panel.anchoredPosition = (Vector2)_nicknameOriginPos + new Vector2(-_canvasWidth, 0);
        panel.gameObject.SetActive(true);
        _nicknameMoveTween = panel.DOAnchorPos(_nicknameOriginPos, _moveDuration)
            .SetEase(Ease.OutBack)
            .OnComplete(() => 
            {
                UI_HoverText.IsHoverBlocked = false;
            });
    }

    private void AnimateNicknamePanelOut(RectTransform panel)
    {
        UI_HoverText.IsHoverBlocked = true;
        _nicknameMoveTween?.Kill();
        _nicknameMoveTween = panel.DOAnchorPos((Vector2)_nicknameOriginPos + new Vector2(0, _canvasHeight), _moveOutDuration)
            .SetEase(Ease.InBack)
            .OnComplete(() => 
            {
                panel.gameObject.SetActive(false);
                UI_HoverText.IsHoverBlocked = false;
            });
    }

    #region 타이틀 패널
    public void ShowTitlePanel_FromOutBottomToCenter()
    {
        UI_HoverText.IsHoverBlocked = true;
        _titleMoveTween?.Kill();
        _titleScaleTween?.Kill();
        _titleButtonPanel.anchoredPosition = _offscreenPos;
        _titleButtonPanel.gameObject.SetActive(true);
        _titleMoveTween = _titleButtonPanel.DOAnchorPos(_originPos, _moveDuration)
            .SetEase(Ease.OutBack);
        _titleButtonPanel.localScale = Vector3.one * _scaleStart;
        _titleScaleTween = _titleButtonPanel.DOScale(_scaleUp, _scaleUpDuration)
            .SetEase(Ease.OutQuad)
            .OnComplete(() =>
            {
                _titleButtonPanel.DOScale(1f, _scaleDownDuration)
                    .SetEase(Ease.InQuad)
                    .OnComplete(() => 
                    {
                        UI_HoverText.IsHoverBlocked = false;
                    });
            });
    }


    public void HideTitlePanel_FromCenterToOutTop(System.Action onComplete = null)
    {
        UI_HoverText.IsHoverBlocked = true;
        _titleMoveTween?.Kill();
        _titleScaleTween?.Kill();
        _titleMoveTween = _titleButtonPanel.DOAnchorPos(_originPos + new Vector3(0, _canvasHeight, 0), _moveOutDuration)
            .SetEase(Ease.InBack)
            .OnComplete(() =>
            {
                _titleButtonPanel.gameObject.SetActive(false);
                UI_HoverText.IsHoverBlocked = false;
                onComplete?.Invoke();
            });
    }

    public void HideTitlePanel_FromCenterToOutLeft(System.Action onComplete = null)
    {
        UI_HoverText.IsHoverBlocked = true;
        _titleMoveTween?.Kill();
        _titleScaleTween?.Kill();
        _titleMoveTween = _titleButtonPanel.DOAnchorPos((Vector2)_originPos + new Vector2(-_canvasWidth, 0), _moveOutDuration)
            .SetEase(Ease.InBack)
            .OnComplete(() =>
            {
                _titleButtonPanel.gameObject.SetActive(false);
                UI_HoverText.IsHoverBlocked = false;
                onComplete?.Invoke();
            });
    }

    public void ShowTitlePanel_FromOutLeftToCenter()
    {
        UI_HoverText.IsHoverBlocked = true;
        _titleMoveTween?.Kill();
        _titleScaleTween?.Kill();
        _titleButtonPanel.anchoredPosition = (Vector2)_originPos + new Vector2(-_canvasWidth, 0);
        _titleButtonPanel.gameObject.SetActive(true);
        _titleMoveTween = _titleButtonPanel.DOAnchorPos((Vector2)_originPos, _moveDuration)
            .SetEase(Ease.OutBack);
        _titleButtonPanel.localScale = Vector3.one * _scaleStart;
        _titleScaleTween = _titleButtonPanel.DOScale(_scaleUp, _scaleUpDuration)
            .SetEase(Ease.OutQuad)
            .OnComplete(() =>
            {
                _titleButtonPanel.DOScale(1f, _scaleDownDuration)
                    .SetEase(Ease.InQuad)
                    .OnComplete(() =>
                    {
                        UI_HoverText.IsHoverBlocked = false;
                    });
            });
    }
    #endregion

    #region 온라인 패널
    public void ShowOnlinePanel_FromOutBottomToCenter()
    {
        UI_HoverText.IsHoverBlocked = true;
        _onlinePanelMoveTween?.Kill();
        _onlineBackBtnScaleTween?.Kill();
        
        // 패널을 화면 아래에서 시작
        _enterOnlinePanel.anchoredPosition = _onlinePanelOffscreenPos;
        _enterOnlinePanel.gameObject.SetActive(true);
        
        // 뒤로가기 버튼을 비활성화 상태(스케일 0)로 시작
        _enterOnlineBackBtn.transform.localScale = Vector3.zero;
        _enterOnlineBackBtn.gameObject.SetActive(true);
        
        // 패널을 아래에서 위로 이동
        _onlinePanelMoveTween = _enterOnlinePanel.DOAnchorPos(_onlinePanelOriginPos, _moveDuration)
            .SetEase(Ease.OutBack)
            .OnComplete(() =>
            {
                // 패널 이동 완료 후 뒤로가기 버튼을 스케일 애니메이션으로 나타내기
                _onlineBackBtnScaleTween = _enterOnlineBackBtn.transform.DOScale(_scaleUp, _scaleUpDuration)
                    .SetEase(Ease.OutQuad)
                    .OnComplete(() =>
                    {
                        _enterOnlineBackBtn.transform.DOScale(1f, _scaleDownDuration)
                            .SetEase(Ease.InQuad)
                            .OnComplete(() =>
                            {
                                UI_HoverText.IsHoverBlocked = false;
                            });
                    });
            });
    }

    public void HideOnlinePanel_FromCenterToOutTop(System.Action onComplete = null)
    {
        UI_HoverText.IsHoverBlocked = true;
        _onlinePanelMoveTween?.Kill();
        _onlineBackBtnScaleTween?.Kill();
        
        // 먼저 뒤로가기 버튼을 스케일 애니메이션으로 사라지게 하기
        _onlineBackBtnScaleTween = _enterOnlineBackBtn.transform.DOScale(0f, _scaleDownDuration)
            .SetEase(Ease.InQuad)
            .OnComplete(() =>
            {
                _enterOnlineBackBtn.gameObject.SetActive(false);
                
                // 뒤로가기 버튼이 사라진 후 패널을 위로 이동시켜 사라지게 하기
                _onlinePanelMoveTween = _enterOnlinePanel.DOAnchorPos(_onlinePanelOriginPos + new Vector3(0, _canvasHeight, 0), _moveOutDuration)
                    .SetEase(Ease.InBack)
                    .OnComplete(() =>
                    {
                        _enterOnlinePanel.gameObject.SetActive(false);
                        UI_HoverText.IsHoverBlocked = false;
                        onComplete?.Invoke();
                    });
            });
    }
    #endregion
}