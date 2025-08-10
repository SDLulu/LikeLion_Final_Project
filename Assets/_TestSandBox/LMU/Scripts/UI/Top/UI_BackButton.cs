using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class UI_BackButton : MonoBehaviour
{
    [Header("인스펙터 참조")]
    [SerializeField] private Button _button;

    [Header("스케일 애니메이션")]
    [SerializeField] private float _scaleUp = 1.1f;
    [SerializeField] private float _scaleUpDuration = 0.25f;
    [SerializeField] private float _scaleDownDuration = 0.2f;

    private Tween _scaleTween;

    private void Awake()
    {
        this.gameObject.SetActive(false);
        _button.onClick.AddListener(OnClickBackButton);
    }

    private void OnDestroy()
    {
        _button.onClick.RemoveAllListeners();
        _scaleTween?.Kill();
    }

    private void OnClickBackButton()
    {
        var enterOnlinePanel = FindAnyObjectByType<UI_EnterOnline>();
        if (enterOnlinePanel != null && enterOnlinePanel.gameObject.activeSelf == true)
        {
            LobbyUI_Manager.Inst.UITitle.OnClickEnterOnlineBackBtn();
            return;
        }
    }

    /// <summary>
    /// 백 버튼을 보여주고 지정된 타입을 활성화합니다.
    /// </summary>
    public void Show(Action onShown = null)
    {
        _button.transform.localScale = Vector3.zero;
        this.gameObject.SetActive(true);

        _scaleTween?.Kill();
        _scaleTween = _button.transform
            .DOScale(_scaleUp, _scaleUpDuration)
            .SetEase(Ease.OutQuad)
            .OnComplete(() =>
            {
                _button.transform
                    .DOScale(1f, _scaleDownDuration)
                    .SetEase(Ease.InQuad)
                    .OnComplete(() =>
                    {
                        onShown?.Invoke();
                    });
            });
    }

    /// <summary>
    /// 백 버튼을 숨깁니다.
    /// </summary>
    public void Hide(Action onHidden = null)
    {
        _scaleTween?.Kill();
        _scaleTween = _button.transform
            .DOScale(0f, _scaleDownDuration)
            .SetEase(Ease.InQuad)
            .OnComplete(() =>
            {
                if (gameObject.activeSelf)
                    gameObject.SetActive(false);

                onHidden?.Invoke();
            });
    }
}


