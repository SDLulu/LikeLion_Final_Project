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
    private Tween _titleMoveTween;
    private Tween _titleScaleTween;
    private Tween _nicknameMoveTween;
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
        var canvas = GetComponent<Canvas>();
        _canvasHeight = canvas.pixelRect.height;
        _canvasWidth = canvas.pixelRect.width;
        _offscreenPos = _originPos + new Vector3(0, -_canvasHeight, 0);
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
        ShowTitlePanelWithTween();
        AnimateNicknamePanelIn(_uiCreateNickName.Holder);
    }

    public void Hide()
    {
        _titleMoveTween?.Kill();
        _titleScaleTween?.Kill();
        _titleMoveTween = _titleButtonPanel.DOAnchorPos(_originPos + new Vector3(0, _canvasHeight, 0), _moveOutDuration)
            .SetEase(Ease.InBack)
            .OnComplete(() =>
            {
                gameObject.SetActive(false);
            });
    }


    private void ShowTitlePanelWithTween()
    {
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
                _titleButtonPanel.DOScale(1f, _scaleDownDuration).SetEase(Ease.InQuad);
            });
    }


    private void HideTitlePanelWithTween(System.Action onComplete = null)
    {
        _titleMoveTween?.Kill();
        _titleScaleTween?.Kill();
        _titleMoveTween = _titleButtonPanel.DOAnchorPos(_originPos + new Vector3(0, _canvasHeight, 0), _moveOutDuration)
            .SetEase(Ease.InBack)
            .OnComplete(() =>
            {
                _titleButtonPanel.gameObject.SetActive(false);
                onComplete?.Invoke();
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
    }

    private void OnClickSoloPlayBtn()
    {
        LobbyUI_Manager.Inst.UILobby.ActiveSoloPanel();
    }

    private UI_GlobalSetting _uiGlobalSetting = null;
    public UI_GlobalSetting UIGlobalSetting => _uiGlobalSetting ??= FindAnyObjectByType<UI_GlobalSetting>();
    private void OnClickEnterOnlineBackBtn()
    {
        AnimateNicknamePanelIn(_uiCreateNickName.Holder);
        ShowTitlePanelWithTween();

        _enterOnlinePanel.gameObject.SetActive(false);
        UIGlobalSetting.ActiveUI(true);
    }

    private void OnClickOnlinePlayBtn()
    {
        AnimateNicknamePanelOut(_uiCreateNickName.Holder);
        HideTitlePanelWithTween(() =>
        {
            _enterOnlinePanel.gameObject.SetActive(true);
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
        _nicknameMoveTween?.Kill();
        panel.anchoredPosition = (Vector2)_nicknameOriginPos + new Vector2(-_canvasWidth, 0);
        panel.gameObject.SetActive(true);
        _nicknameMoveTween = panel.DOAnchorPos(_nicknameOriginPos, _moveDuration).SetEase(Ease.OutBack);
    }

    private void AnimateNicknamePanelOut(RectTransform panel)
    {
        _nicknameMoveTween?.Kill();
        _nicknameMoveTween = panel.DOAnchorPos((Vector2)_nicknameOriginPos + new Vector2(0, _canvasHeight), _moveOutDuration)
            .SetEase(Ease.InBack)
            .OnComplete(() => panel.gameObject.SetActive(false));
    }
}