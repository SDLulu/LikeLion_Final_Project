using TMPro;
using UnityEngine;
using DG.Tweening;
using LMCore;

public class UI_ReadyText : MonoBehaviour
{
    [Header("인스펙터 참조")]
    [SerializeField] private TMP_Text _readyText;
    [SerializeField] private CanvasGroup _canvasGroup;

    [Header("설정")]
    [SerializeField] private float _showDuration = 0.6f;
    [SerializeField] private float _hideDuration = 0.4f;
    [SerializeField] private float _hideScale = 0.9f;
    [SerializeField] private Ease _showEase = Ease.OutBack;
    [SerializeField] private Ease _hideEase = Ease.InSine;

    private Vector3 _originScale;
    private Tween _showTween;
    private Tween _hideTween;

    private void Awake()
    {
        _originScale = transform.localScale;
        _canvasGroup = this.GetOrAddComponent<CanvasGroup>();
        gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
        _showTween?.Kill();
        _hideTween?.Kill();
        _canvasGroup = null;
    }


    [ContextMenu("레디")]
    public void Ready()
    {
        SetReady(true);
    }

    [ContextMenu("레디 해제")]
    public void UnReady()
    {
        SetReady(false);
    }

    public bool IsReady {get; private set;} = false;
    public void SetReady(bool isReady)
    {
        IsReady = isReady;
        if (isReady == true)
        {
            ShowReadyText();
        }
        else
        {
            HideReadyText();
        }
    }


    public void ShowReadyText()
    {
        _showTween?.Kill();
        _hideTween?.Kill();

        gameObject.SetActive(true);

        _canvasGroup.alpha = 0f;
        transform.localScale = Vector3.zero;

        _showTween = DOTween.Sequence()
            .Join(_canvasGroup.DOFade(1f, _showDuration))
            .Join(transform.DOScale(_originScale, _showDuration).SetEase(_showEase));
    }

    /// <summary>
    /// "준비 완료" 텍스트를 사라지게 합니다.
    /// </summary>
    public void HideReadyText()
    {
        _showTween?.Kill();
        _hideTween?.Kill();

        _hideTween = DOTween.Sequence()
            .Join(_canvasGroup.DOFade(0f, _hideDuration))
            .Join(transform.DOScale(_originScale * _hideScale, _hideDuration).SetEase(_hideEase))
            .OnComplete(() =>
            {
                gameObject.SetActive(false);
            });
    }

}