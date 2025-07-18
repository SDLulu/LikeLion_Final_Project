using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
using System;

namespace LMCore
{
    public static class FaderUtil
    {
        public static Vector2 GetUIPosition(Canvas canvas, Vector2 worldPos)
        {
            var screenPos = Camera.main.WorldToScreenPoint(worldPos);
            var canvasRect = canvas.GetComponent<RectTransform>();
            Vector2 localPoint;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRect, 
                screenPos, 
                canvas.worldCamera, 
                out localPoint);
            return localPoint;
        }

    }
    public class Fader : BaseManager<Fader>
    {
        [Header("아이콘 팽창 페이드")]
        [SerializeField] private RectTransform  _contentsRoot;

        [Header("화면 이미지")]
        [SerializeField] private RectTransform _imageRoot;
        [SerializeField] private Image _whiteImage;


        [Header("페이드 설정")]
        [SerializeField] private Vector2 _startSize = new Vector2(10000, 10000);
        [SerializeField] private Vector2 _endSize = new Vector2(0, 0);
        [SerializeField] private Ease _fadeInEase = Ease.InOutSine;
        [SerializeField] private Ease _fadeOutEase = Ease.InOutSine;

        private bool _isInitialized = false;
        private bool _isFading = false;

        public bool IsFading => _isFading;

        public void Awake()
        {
            DontDestroyOnLoad(this);
            InitializeFader();
        }

        private void InitializeFader()
        {
            if (_isInitialized)
                return;

            if (_contentsRoot != null)
                _contentsRoot.gameObject.SetActive(false);
            
            if (_imageRoot != null)
                _imageRoot.gameObject.SetActive(false);

            if (_whiteImage != null)
            {
                Color startColor = _whiteImage.color;
                startColor.a = 0f;
                _whiteImage.color = startColor;
            }

            _isInitialized = true;
        }

        private void CheckAndInitialize()
        {
            if (!_isInitialized)
                InitializeFader();
        }

        // Note - AsyncWaitForCompletion / Task 사용중
        public async Awaitable FadeInExpandAsync(Color color, float seconds = 1f, Vector2 worldPos = default)
        {
            CheckAndInitialize();
            if (_contentsRoot == null || IsFading)
                return;
            Debug.Log("FadeInExpandAsync");

            _isFading = true;
            _whiteImage.color = color;
            _contentsRoot.sizeDelta = _endSize;
            await _contentsRoot.DOSizeDelta(_startSize, seconds)
                .SetEase(_fadeInEase)
                .SetUpdate(true)
                .AsyncWaitForCompletion();     
            _contentsRoot.gameObject.SetActive(false);
            _isFading = false;
            await Awaitable.NextFrameAsync();
        }
        
        // Note - AsyncWaitForCompletion / Task 사용중
        public async Awaitable FadeOutExpandAsync(Color color, float seconds = 1f, Vector2 worldPos = default)
        {
            try
            {
                

            CheckAndInitialize();
            if (_contentsRoot == null || IsFading)
                return;

            Debug.Log("FadeOutExpandAsync");
            _isFading = true;
            _contentsRoot.sizeDelta = _startSize;
            
            var canvas = this.GetComponent<Canvas>();
            _contentsRoot.anchoredPosition = FaderUtil.GetUIPosition(canvas, worldPos);
            _whiteImage.color = color;
            _contentsRoot.gameObject.SetActive(true);
            await _contentsRoot
                .DOSizeDelta(_endSize, seconds)
                .SetEase(_fadeOutEase)
                .SetUpdate(true)
                .AsyncWaitForCompletion();
            _isFading = false;
            await Awaitable.NextFrameAsync();
                        }
            catch (System.Exception E)
            {
                Debug.LogError("FadeOutExpandAsync 오류");
                Debug.LogError(E.Message);
            }
        }
        

        // Note - AsyncWaitForCompletion / Task 사용중
        public async Awaitable FadeInAsync(float seconds = 1f)
        {
            CheckAndInitialize();
            if (_imageRoot == null || _contentsRoot == null || IsFading)
                return;

            _isFading = true;
            _contentsRoot.gameObject.SetActive(false);
            
            Color startColor = _whiteImage.color;
            startColor.a = 1f;
            _whiteImage.color = startColor;
            
            _imageRoot.gameObject.SetActive(true);
            await _whiteImage.DOFade(0f, seconds)
                .SetEase(_fadeInEase)
                .SetUpdate(true)
                .AsyncWaitForCompletion();
                
            _imageRoot.gameObject.SetActive(false);
            _contentsRoot.gameObject.SetActive(false);
            _isFading = false;
            await Awaitable.NextFrameAsync();
        }
        
        // Note - AsyncWaitForCompletion / Task 사용중
        public async Awaitable FadeOutAsync(Color color = default, float seconds = 1f)
        {
            CheckAndInitialize();
            if (_imageRoot == null || _contentsRoot == null || IsFading)
                return;

            _isFading = true;
            _contentsRoot.gameObject.SetActive(false);

            Color startColor = _whiteImage.color;
            startColor = color;
            startColor.a = 0.0f; 
            _whiteImage.color = startColor;
            
            _imageRoot.gameObject.SetActive(true);
            await _whiteImage.DOFade(1f, seconds)
                .SetEase(_fadeOutEase)
                .SetUpdate(true)
                .AsyncWaitForCompletion();
            _isFading = false;
            await Awaitable.NextFrameAsync();
        }

        public void DeactiveAllChildren()
        {
            CheckAndInitialize();
            if (_contentsRoot == null)
                return;

            _imageRoot.gameObject.SetActive(false);
            _contentsRoot.gameObject.SetActive(false);
            _isFading = false;
        }
    }
}