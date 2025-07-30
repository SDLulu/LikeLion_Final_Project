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
        [SerializeField] private RectTransform _imageRoot;
        [SerializeField] private Image _bgImage;

        [Header("로딩 UI")]
        [SerializeField] private RectTransform _loadingPanel;
        [SerializeField] private RectTransform _loadingRotateIcon;

        [Header("FullScreen 페이드")]
        [SerializeField] private RectTransform _fullScreenImage;


        [Header("페이드 설정")]
        [SerializeField] private Vector2 _startSize = new Vector2(10000, 10000);
        [SerializeField] private Vector2 _endSize = new Vector2(0, 0);
        [SerializeField] private Ease _fadeInEase = Ease.InOutSine;
        [SerializeField] private Ease _fadeOutEase = Ease.InOutSine;
        [SerializeField] private float _loadingUiFadeDuration = 0.5f;
        [SerializeField] private float _loadingIconRotateSpeed = 1.0f;

        private bool _isInitialized = false;
        private bool _isFading = false;

        public bool IsFading => _isFading;

        private Tween _loadingRotateTween;
        private Tween _loadingScaleTween;

        public void Awake()
        {
            // 우선순위 제일 높게 설정
            GetComponent<Canvas>().sortingOrder = 1000;
            DontDestroyOnLoad(this);
            InitializeFader();
        }

        private void OnDestroy()
        {
            _loadingRotateTween?.Kill();
            _loadingScaleTween?.Kill();
        }

        public void ActiveBGImage( bool isActive = true, Color color = default)
        {
            _fullScreenImage.gameObject.SetActive(isActive);
            _fullScreenImage.GetComponent<Image>().color = color;
        }

#if UNITY_EDITOR
        [ContextMenu("BGImage 활성화")]
        private void ActiveBGImage()
        {
            ActiveBGImage(true, Color.black);
        }

        [ContextMenu("BGImage 비활성화")]
        private void DeactiveBGImage()
        {
            ActiveBGImage(false, Color.black);
        }

        bool _testFade = false;
        [ContextMenu("기본 FadeIn")]
        private async void FadeIn()
        {
            if (_testFade)
                return;
            _testFade = true;
            await FadeInAsync(Color.black, 1.0f);
            _testFade = false;
        }

        [ContextMenu("기본 FadeOut")]
        private async void FadeOut()
        {
            if (_testFade)
                return;
            _testFade = true;
            await FadeOutAsync(Color.black, 1.0f);
            _testFade = false;
        }

        [ContextMenu("팽창 FadeIn")]
        private async void FadeInExpand()
        {
            if (_testFade)
                return;
            _testFade = true;
            await FadeInExpandAsync(Color.white, 1.5f, Vector2.zero);
            _testFade = false;
        }

        [ContextMenu("팽창 FadeOut")]
        private async void FadeOutExpand()
        {
            if (_testFade)
                return;
            _testFade = true;
            await FadeOutExpandAsync(Color.white, 1.5f, Vector2.zero);
            _testFade = false;
        }

        [ContextMenu("로딩과 함께 FadeOut/In")]
        private async void FadeWithLoadingTest()
        {
            if (_testFade)
                return;
            _testFade = true;
            await FadeOutWithLoading(Color.black, 1.0f);
            // 실제 사용 시, 이 부분에서 로딩 작업을 수행합니다.
            await Awaitable.WaitForSecondsAsync(2.0f);
            await FadeInWithLoading(Color.black, 1.0f);
            _testFade = false;
        }
#endif

        private void InitializeFader()
        {
            if (_isInitialized)
                return;

            if (_contentsRoot != null)
                _contentsRoot.gameObject.SetActive(false);
            
            if (_imageRoot != null)
                _imageRoot.gameObject.SetActive(false);

            if (_loadingPanel != null)
                _loadingPanel.gameObject.SetActive(false);

            if (_bgImage != null)
            {
                Color startColor = _bgImage.color;
                startColor.a = 0f;
                _bgImage.color = startColor;
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

            _isFading = true;
            _bgImage.color = color;
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
            CheckAndInitialize();
            if (_contentsRoot == null || IsFading)
                return;

            _isFading = true;
            _contentsRoot.sizeDelta = _startSize;
            
            var canvas = this.GetComponent<Canvas>();
            _contentsRoot.anchoredPosition = FaderUtil.GetUIPosition(canvas, worldPos);
            _bgImage.color = color;
            _contentsRoot.gameObject.SetActive(true);
            await _contentsRoot
                .DOSizeDelta(_endSize, seconds)
                .SetEase(_fadeOutEase)
                .SetUpdate(true)
                .AsyncWaitForCompletion();
            _isFading = false;
            await Awaitable.NextFrameAsync();
        }
        

        public async Awaitable FadeInAsync(Color color = default, float seconds = 0.5f)
        {
            CheckAndInitialize();
            if (_imageRoot == null || _contentsRoot == null || IsFading)
                return;

            _isFading = true;
            _contentsRoot.gameObject.SetActive(false);
            _imageRoot.gameObject.SetActive(false);

            var image = _fullScreenImage.GetComponent<Image>();
            image.gameObject.SetActive(true);
            image.color = new Color(color.r, color.g, color.b, 1f);

            // 더 정밀한 제어를 위해 더 많은 단계 사용
            int steps = Mathf.Max(30, Mathf.RoundToInt(seconds * 60)); 
            float stepTime = seconds / steps;
            float currentAlpha = 1f;
            float alphaStep = 1f / steps;

            for (int i = 0; i < steps; i++)
            {
                currentAlpha -= alphaStep;
                image.color = new Color(color.r, color.g, color.b, currentAlpha);
                await Awaitable.WaitForSecondsAsync(stepTime);
            }

            image.color = new Color(color.r, color.g, color.b, 0f);
            image.gameObject.SetActive(false);
            _contentsRoot.gameObject.SetActive(false);
            _isFading = false;
            await Awaitable.NextFrameAsync();
        }
        
        public async Awaitable FadeOutAsync(Color color = default, float seconds = 0.5f)
        {
            CheckAndInitialize();
            if (_imageRoot == null || _contentsRoot == null || IsFading)
                return;

            _isFading = true;
            _contentsRoot.gameObject.SetActive(false);
            _imageRoot.gameObject.SetActive(false);

            var image = _fullScreenImage.GetComponent<Image>();
            image.gameObject.SetActive(true);
            image.color = new Color(color.r, color.g, color.b, 0f);

            // 더 정밀한 제어를 위해 더 많은 단계 사용
            int steps = Mathf.Max(30, Mathf.RoundToInt(seconds * 60)); 
            float stepTime = seconds / steps;
            float currentAlpha = 0f;
            float alphaStep = 1f / steps;

            for (int i = 0; i < steps; i++)
            {
                currentAlpha += alphaStep;
                image.color = new Color(color.r, color.g, color.b, currentAlpha);
                await Awaitable.WaitForSecondsAsync(stepTime);
            }

            image.color = new Color(color.r, color.g, color.b, 1f);
            _isFading = false;
            await Awaitable.NextFrameAsync();
        }

        public async Awaitable FadeOutWithLoading(Color color = default, float seconds = 1f)
        {
            CheckAndInitialize();
            if (_fullScreenImage == null || IsFading)
                return;

            _isFading = true;

            var image = _fullScreenImage.GetComponent<Image>();
            image.gameObject.SetActive(true);
            image.color = new Color(color.r, color.g, color.b, 0f);
            var fadeOutTween = image.DOFade(1f, seconds).SetUpdate(true);

            if (_loadingPanel != null)
            {
                _loadingPanel.gameObject.SetActive(true);
                _loadingPanel.localScale = Vector3.zero;
                _loadingScaleTween?.Kill();
                _loadingScaleTween = _loadingPanel.DOScale(1f, _loadingUiFadeDuration).SetEase(Ease.OutBack).SetUpdate(true);
            }

            if (_loadingRotateIcon != null)
            {
                _loadingRotateTween?.Kill();
                _loadingRotateIcon.localRotation = Quaternion.identity;
                _loadingRotateTween = _loadingRotateIcon.DORotate(new Vector3(0, 0, -360), _loadingIconRotateSpeed, RotateMode.FastBeyond360)
                    .SetLoops(-1, LoopType.Restart)
                    .SetEase(Ease.Linear)
                    .SetUpdate(true);
            }

            await fadeOutTween.AsyncWaitForCompletion();
            _isFading = false;
            await Awaitable.NextFrameAsync();
        }

        public async Awaitable FadeInWithLoading(Color color = default, float seconds = 1f)
        {
            CheckAndInitialize();
            if (_fullScreenImage == null || IsFading)
                return;

            _isFading = true;

            var image = _fullScreenImage.GetComponent<Image>();
            image.gameObject.SetActive(true);
            image.color = new Color(color.r, color.g, color.b, 1f);
            var fadeInTween = image.DOFade(0f, seconds).SetUpdate(true).OnComplete(() => image.gameObject.SetActive(false));

            if (_loadingPanel != null)
            {
                _loadingScaleTween?.Kill();
                _loadingScaleTween = _loadingPanel.DOScale(0f, _loadingUiFadeDuration).SetEase(Ease.InBack).SetUpdate(true)
                    .OnComplete(() =>
                    {
                        _loadingPanel.gameObject.SetActive(false);
                        if (_loadingRotateIcon != null)
                            _loadingRotateIcon.localRotation = Quaternion.identity;
                    });
            }
            _loadingRotateTween?.Kill();

            await fadeInTween.AsyncWaitForCompletion();
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