using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using System.Collections.Generic;

public class UI_Hover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("인스펙터 할당")]
    [SerializeField] private List<TMP_Text> _texts;
    [SerializeField] private List<Image> _images;
    
    [Header("호버 알파값 설정")]
    [SerializeField] private float _nonHoverAlpha = 0.4f;
    [SerializeField] private float _hoverAlpha = 1f;
    

    // 만약을 위해 2프레임뒤에
    private async void Start()
    {
        await Awaitable.NextFrameAsync();
        await Awaitable.NextFrameAsync();
        OnPointerExit(null);
    }

    private void OnEnable()
    {
        OnHoverExit();
    }

    private void OnDisable()
    {
        OnHoverExit();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        OnHoverEnter();
    }
    
    public void OnPointerExit(PointerEventData eventData)
    {
        OnHoverExit();
    }
    
    // 호버 진입 시 실행될 함수 (오버라이드 가능)
    protected virtual void OnHoverEnter()
    {
        // 텍스트 리스트에 있는 모든 텍스트에 효과 적용
        if (_texts != null)
        {
            foreach (var text in _texts)
            {
                if (text != null)
                {
                    Color textColor = text.color;
                    textColor.a = _hoverAlpha;
                    text.color = textColor;
                }
            }
        }
        
        // 이미지 리스트에 있는 모든 이미지에 효과 적용
        if (_images != null)
        {
            foreach (var image in _images)
            {
                if (image != null)
                {
                    Color imageColor = image.color;
                    imageColor.a = _hoverAlpha;
                    image.color = imageColor;
                }
            }
        }
    }
    
    // 호버 탈출 시 실행될 함수 (오버라이드 가능)
    protected virtual void OnHoverExit()
    {
        // 텍스트 리스트에 있는 모든 텍스트를 원래 상태로 복원
        if (_texts != null)
        {
            foreach (var text in _texts)
            {
                if (text != null)
                {
                    Color textColor = text.color;
                    textColor.a = _nonHoverAlpha;
                    text.color = textColor;
                }
            }
        }
        
        // 이미지 리스트에 있는 모든 이미지를 원래 상태로 복원
        if (_images != null)
        {
            foreach (var image in _images)
            {
                if (image != null)
                {
                    Color imageColor = image.color;
                    imageColor.a = _nonHoverAlpha;
                    image.color = imageColor;
                }
            }
        }
    }
    
}
