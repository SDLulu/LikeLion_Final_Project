using UnityEngine;
using TMPro;
using DG.Tweening;

namespace LMCore
{
    public class UI_HoverText : MonoBehaviour
    {
        [Header("인스펙터 할당")]
        [SerializeField] private TMP_Text _text;

        [Header("Tween 설정값")]
        [SerializeField] private float _hoverScale = 1.2f;
        [SerializeField] private float _normalScale = 1.0f;
        [SerializeField] private float _tweenDuration = 0.2f;

        [Header("색상 Tween (알파만 변경시 false)")]
        [SerializeField] private bool _tweenFullColor = false;
        [SerializeField] private Color _normalColor = new Color(1, 1, 1, 0.4f);
        [SerializeField] private Color _hoverColor = new Color(0, 0, 1, 1f);

        private Tween _scaleTween;
        private Tween _colorTween;
        private bool _isHovered = false;

        /// <summary>
        /// 전역 호버 플래그
        /// </summary>
        public static bool IsHoverBlocked = false;

        private void OnEnable()
        {
            RestoreOriginal();
        }

        private void OnDisable()
        {
            RestoreOriginal();
        }

        private void RestoreOriginal()
        {
            if (_text == null)
            {
                return;
            }
            _text.transform.localScale = Vector3.one * _normalScale;
            if (_tweenFullColor)
            {
                _text.color = _normalColor;
            }
            else
            {
                Color c = _text.color;
                c.a = _normalColor.a;
                _text.color = c;
            }
        }

        public void HoverEnter()
        {
            if (_text == null || _isHovered || IsHoverBlocked)
                return;

            _isHovered = true;

            SoundManager.Inst.PlaySFX("UIHover");

            _scaleTween?.Kill();
            float targetScale = _hoverScale;
            _scaleTween = _text.transform
                .DOScale(targetScale, _tweenDuration)
                .SetEase(Ease.OutQuad);

            _colorTween?.Kill();
            if (_tweenFullColor)
            {
                Color startColor = _text.color;
                Color targetColor = _hoverColor;
                _colorTween = DOTween.To(
                    () => _text.color,
                    c => _text.color = c,
                    targetColor,
                    _tweenDuration
                ).SetEase(Ease.OutQuad);
            }
            else
            {
                Color current = _text.color;
                float targetAlpha = _hoverColor.a;
                _colorTween = DOTween.To(
                    () => _text.color.a,
                    a => _text.color = new Color(current.r, current.g, current.b, a),
                    targetAlpha,
                    _tweenDuration
                ).SetEase(Ease.OutQuad);
            }
        }

        public void HoverExit()
        {
            if (_text == null)
            {
                return;
            }
            if (!_isHovered) return;
            _isHovered = false;

            _scaleTween?.Kill();
            float targetScale = _normalScale;
            _scaleTween = _text.transform
                .DOScale(targetScale, _tweenDuration)
                .SetEase(Ease.OutQuad);

            _colorTween?.Kill();
            if (_tweenFullColor)
            {
                Color startColor = _text.color;
                Color targetColor = _normalColor;
                _colorTween = DOTween.To(
                    () => _text.color,
                    c => _text.color = c,
                    targetColor,
                    _tweenDuration
                ).SetEase(Ease.OutQuad);
            }
            else
            {
                Color current = _text.color;
                float targetAlpha = _normalColor.a;
                _colorTween = DOTween.To(
                    () => _text.color.a,
                    a => _text.color = new Color(current.r, current.g, current.b, a),
                    targetAlpha,
                    _tweenDuration
                ).SetEase(Ease.OutQuad);
            }
        }
    }
} 