using UnityEngine;
using DG.Tweening;
using TMPro;

public class UI_Continue : MonoBehaviour
{
    [Header("스케일 애니메이션")]
    [SerializeField] private float _scaleUp = 1.1f;

    [SerializeField] private float _scaleUpDuration = 0.25f;

    [SerializeField] private float _scaleDownDuration = 0.2f;

    [SerializeField] private TMP_Text _continueText;

    private Tween _scaleTween;

    public void Show()
    {
        this.transform.localScale = Vector3.zero;
        this.gameObject.SetActive(true);
        UpdateContinueText(UI_CreateNickName.InputFieldStr);

        _scaleTween?.Kill();

        _scaleTween = this.transform
            .DOScale(_scaleUp, _scaleUpDuration)
            .SetEase(Ease.OutQuad)
            .OnComplete(() =>
            {
                this.transform
                    .DOScale(1f, _scaleDownDuration)
                    .SetEase(Ease.InQuad)
                    .OnComplete(() =>
                    {
                        this.transform.localScale = Vector3.one;
                    });
            });
    }

    public void Hide()
    {
        _scaleTween?.Kill();

        _scaleTween = this.transform
            .DOScale(0f, _scaleDownDuration)
            .SetEase(Ease.InQuad)
            .OnComplete(() =>
            {
                this.gameObject.SetActive(false);
                this.transform.localScale = Vector3.one;
            });
    }

    public void UpdateContinueText(string nickName)
    {
        _continueText.text = $"{nickName}으로 설정 하시겠습니까?";
    }

    private void OnDestroy()
    {
        _continueText.text = string.Empty;
    }
}
