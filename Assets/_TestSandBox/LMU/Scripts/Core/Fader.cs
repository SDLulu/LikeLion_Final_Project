using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
using LMCore;
using System;

public class Fader : BaseManager<Fader>
{
    [Header("아이콘 팽창 페이드")]
    [SerializeField] private RectTransform  _contentsRoot;

    [Header("단순 검은화면")]
    [SerializeField] private RectTransform _blackRoot;
    [SerializeField] private Image _blackImage;


    [Header("페이드 설정")]
    [SerializeField] private Vector2 _startSize = new Vector2(10000, 10000);
    [SerializeField] private Vector2 _endSize = new Vector2(0, 0);
    [SerializeField] private float _fadeInTime = 1f;
    [SerializeField] private float _fadeOutTime = 1f;
    [SerializeField] private Ease _fadeInEase = Ease.InOutSine;
    [SerializeField] private Ease _fadeOutEase = Ease.InOutSine;

    private bool _isInitialized = false;
    private bool _isFading = false;

    public bool IsFading => _isFading;

    public void Awake()
    {
        InitializeFader();
    }

    private void InitializeFader()
    {
        if (_isInitialized)
            return;

        if (_contentsRoot != null)
            _contentsRoot.gameObject.SetActive(false);
        
        if (_blackRoot != null)
            _blackRoot.gameObject.SetActive(false);

        if (_blackImage != null)
        {
            Color startColor = _blackImage.color;
            startColor.a = 0f;
            _blackImage.color = startColor;
        }

        _isInitialized = true;
    }

    private void CheckAndInitialize()
    {
        if (!_isInitialized)
            InitializeFader();
    }

    #if UNITY_EDITOR
    [ContextMenu("페이드인")]
    private void FadeInTest()
    {
        _ = FadeInAsync();
    }

    [ContextMenu("페이드아웃")]
    private void FadeOutTest()
    {
        _ = FadeOutAsync();
    }
    #endif

            
    public async Awaitable FadeInAsync()
    {
        CheckAndInitialize();
        if (_contentsRoot == null)
            return;
        Debug.Log("FadeInAsync");

        _isFading = true;
        _contentsRoot.sizeDelta = _endSize;
        await _contentsRoot.DOSizeDelta(_startSize, _fadeInTime)
            .SetEase(_fadeInEase)
            .SetUpdate(true)
            .AsyncWaitForCompletion();
        _contentsRoot.gameObject.SetActive(false);
        _isFading = false;
    }
    
    public async Awaitable FadeOutAsync()
    {
        CheckAndInitialize();
        if (_contentsRoot == null)
            return;

        Debug.Log("FadeOutAsync");
        _isFading = true;
        _contentsRoot.sizeDelta = _startSize;
        _contentsRoot.gameObject.SetActive(true);
        await _contentsRoot
            .DOSizeDelta(_endSize, _fadeOutTime)
            .SetEase(_fadeOutEase)
            .SetUpdate(true)
            .AsyncWaitForCompletion();
        _isFading = false;
    }
    
    public async Awaitable BlackFadeInAsync()
    {
        CheckAndInitialize();
        if (_blackRoot == null || _contentsRoot == null)
            return;

        _isFading = true;
        _contentsRoot.gameObject.SetActive(false);
        
        Color startColor = _blackImage.color;
        startColor.a = 1f;
        _blackImage.color = startColor;
        
        _blackRoot.gameObject.SetActive(true);
        await _blackImage.DOFade(0f, _fadeInTime)
            .SetEase(_fadeInEase)
            .SetUpdate(true)
            .AsyncWaitForCompletion();
            
        _blackRoot.gameObject.SetActive(false);
        _contentsRoot.gameObject.SetActive(false);
        _isFading = false;
    }
    
    public async Awaitable BlackFadeOutAsync()
    {
        CheckAndInitialize();
        if (_blackRoot == null || _contentsRoot == null)
            return;

        _isFading = true;
        _contentsRoot.gameObject.SetActive(false);

        Color startColor = _blackImage.color;
        startColor = Color.black;
        startColor.a = 0.0f; 
        _blackImage.color = startColor;
        
        _blackRoot.gameObject.SetActive(true);
        await _blackImage.DOFade(1f, _fadeOutTime)
            .SetEase(_fadeOutEase)
            .SetUpdate(true)
            .AsyncWaitForCompletion();
        _isFading = false;
    }

    public async Awaitable BlackFadeImageToColorAsync(Color blackImageEndColor, float fadeTime)
    {
        CheckAndInitialize();
        if (_blackRoot == null || _contentsRoot == null)
            return;

        _isFading = true;
        _contentsRoot.gameObject.SetActive(false);

        Color startColor = _blackImage.color;
        startColor = Color.black;
        startColor.a = 1f;
        _blackImage.color = startColor;

        _blackRoot.gameObject.SetActive(true);
        await _blackImage.DOColor(blackImageEndColor, fadeTime)
            .SetEase(_fadeInEase)
            .SetUpdate(true)
            .AsyncWaitForCompletion();
        _isFading = false;
    }

    public async Awaitable DeactiveAllChildrenAsync()
    {
        CheckAndInitialize();
        if (_contentsRoot == null)
            return;

        _blackRoot.gameObject.SetActive(false);
        _contentsRoot.gameObject.SetActive(false);
        _isFading = false;
        await Awaitable.NextFrameAsync();
    }

    public async Awaitable WhiteFadeIn(Action onCenter = null, float whiteDelayTime = 1.0f, float whiteFadeInTime = 3.0f)
    {
        CheckAndInitialize();
        if (_blackRoot == null || _blackImage == null)
            return;

        _isFading = true;
        _contentsRoot.gameObject.SetActive(false);
        Color startColor = _blackImage.color;
        startColor = Color.white;
        startColor.a = 1f;
        _blackImage.color = startColor;
        _blackRoot.gameObject.SetActive(true);
        await Awaitable.NextFrameAsync();
        
        onCenter?.Invoke();

        await _blackImage.DOFade(0.0f, whiteFadeInTime)
            .SetEase(_fadeInEase)
            .SetUpdate(true)
            .AsyncWaitForCompletion();

        _blackRoot.gameObject.SetActive(false);
        _isFading = false;
    }

    internal async Awaitable WhiteFadeOutAndFadeInAsync(Action onCenter = null)
    {
        CheckAndInitialize();
        if (_blackRoot == null || _blackImage == null)
            return;

        _isFading = true;
        _contentsRoot.gameObject.SetActive(false);

        Color startColor = _blackImage.color;
        startColor = Color.white;
        startColor.a = 0f;
        _blackImage.color = startColor;
        _blackRoot.gameObject.SetActive(true);

        await _blackImage.DOFade(1f, _fadeOutTime)
            .SetEase(_fadeOutEase)
            .SetUpdate(true)
            .AsyncWaitForCompletion();

        startColor = Color.white;
        startColor.a = 1f;
        _blackImage.color = startColor;

        onCenter?.Invoke();

        await _blackImage.DOFade(0f, _fadeInTime)
            .SetEase(_fadeInEase)
            .SetUpdate(true)
            .AsyncWaitForCompletion();

        _blackRoot.gameObject.SetActive(false);
        _isFading = false;
    }
}
