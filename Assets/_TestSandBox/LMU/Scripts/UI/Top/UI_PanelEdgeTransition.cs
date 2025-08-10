using System;
using DG.Tweening;
using UnityEngine;

public class UI_PanelEdgeTransition : MonoBehaviour
{
    public enum EdgeDirection
    {
        Left,
        Right,
        Top,
        Bottom
    }

    [Header("인스펙터 참조")]
    [SerializeField] private RectTransform _rect;
    [SerializeField] private Canvas _canvas;

    [Header("등장/퇴장 방향 설정")]
    [SerializeField] private EdgeDirection _showFrom = EdgeDirection.Bottom;
    [SerializeField] private EdgeDirection _hideTo = EdgeDirection.Top;

    [Header("시간 및 이징 설정")]
    [SerializeField] private float _showDuration = 0.7f;
    [SerializeField] private float _hideDuration = 0.25f;
    [SerializeField] private Ease _showEase = Ease.OutBack;
    [SerializeField] private Ease _hideEase = Ease.InBack;


    private Vector2 _originAnchoredPos;
    private Tween _moveTween;
    private Tween _scaleTween;

    private void Reset()
    {
        _rect = GetComponent<RectTransform>();
        _canvas = GetComponentInParent<Canvas>();
    }

    private void Awake()
    {
        _originAnchoredPos = _rect.anchoredPosition;
    }

    private void OnDisable()
    {
        KillTweens();
    }

    /// <summary>
    /// 현재 위치를 원점으로 재설정
    /// </summary>
    public void CaptureCurrentAsOrigin()
    {
        _originAnchoredPos = _rect.anchoredPosition;
    }

    /// <summary>
    /// 외부에서 원점(앵커드 포지션)을 명시적으로 지정
    /// </summary>
    public void SetOriginAnchoredPosition(Vector2 originAnchoredPosition)
    {
        _originAnchoredPos = originAnchoredPosition;
    }

    /// <summary>
    /// 지정된 방향에서 등장
    /// </summary>
    public void Show(Action onComplete = null)
    {
        if (_rect == null || _canvas == null)
        {
            Debug.LogError("UI_PanelEdgeTransition: RectTransform 또는 Canvas를 찾을 수 없습니다.", this);
            return;
        }

        gameObject.SetActive(true);

        KillTweens();

        Vector2 offscreen = GetOffscreenPosition(_showFrom);
        _rect.anchoredPosition = offscreen;

        _moveTween = _rect
            .DOAnchorPos(_originAnchoredPos, _showDuration)
            .SetEase(_showEase)
            .OnComplete(() =>
            {
                onComplete?.Invoke();
            });
    }


    /// <summary>
    /// 지정된 방향으로 퇴장
    /// </summary>
    public void Hide(Action onComplete = null)
    {
        if (_rect == null || _canvas == null)
        {
            Debug.LogError("UI_PanelEdgeTransition: RectTransform 또는 Canvas를 찾을 수 없습니다.", this);
            return;
        }

        KillTweens();

        Vector2 target = GetOffscreenPosition(_hideTo);
        _moveTween = _rect
            .DOAnchorPos(target, _hideDuration)
            .SetEase(_hideEase)
            .OnComplete(() =>
            {
                gameObject.SetActive(false);
                onComplete?.Invoke();
            });
    }

    public void ShowFrom(EdgeDirection from, Action onComplete)
    {
        _showFrom = from;
        Show(onComplete);
    }

    public void HideTo(EdgeDirection to, Action onComplete)
    {
        _hideTo = to;
        Hide(onComplete);
    }

    private void KillTweens()
    {
        _moveTween?.Kill();
        _scaleTween?.Kill();
    }

    private Vector2 GetOffscreenPosition(EdgeDirection direction)
    {
        float width = _canvas.pixelRect.width;
        float height = _canvas.pixelRect.height;

        if (direction == EdgeDirection.Left)
        {
            return _originAnchoredPos + new Vector2(-width, 0f);
        }

        if (direction == EdgeDirection.Right)
        {
            return _originAnchoredPos + new Vector2(width, 0f);
        }

        if (direction == EdgeDirection.Top)
        {
            return _originAnchoredPos + new Vector2(0f, height);
        }

        // Bottom
        return _originAnchoredPos + new Vector2(0f, -height);
    }

}


