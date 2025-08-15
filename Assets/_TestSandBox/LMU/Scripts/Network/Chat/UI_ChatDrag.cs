using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class UI_ChatDrag : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    private void OnValidate()
    {
        if (_canvas == null)
            _canvas = GetComponentInParent<Canvas>();
        if (_rect == null)
            _rect = GetComponent<RectTransform>();
    }

    [Header("인스펙터 참조")]
    [SerializeField] private RectTransform _linkRect;

    [Header("디버그용")]
    [SerializeField] private Canvas _canvas;
    [SerializeField] private RectTransform _rect;
    [SerializeField] private bool _isDragging = false;

    private Vector2 _rectDragOffset;
    private Vector2 _linkRectDragOffset;

    public void OnBeginDrag(PointerEventData eventData)
    {
        _isDragging = true;
        Vector2 mousePos = default;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            _canvas.transform as RectTransform,
            eventData.position,
            _canvas.worldCamera,
            out mousePos
        );

        _rectDragOffset = _rect.anchoredPosition - mousePos;
        _linkRectDragOffset = _linkRect.anchoredPosition - mousePos;

        InputBlocker.IsDragging = true;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (_isDragging == false)
            return;

        Vector2 mousePos = default;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            _canvas.transform as RectTransform,
            eventData.position,
            _canvas.worldCamera,
            out mousePos
        );

        _rect.anchoredPosition = mousePos + _rectDragOffset;
        _linkRect.anchoredPosition = mousePos + _linkRectDragOffset;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        _isDragging = false;
        InputBlocker.IsDragging = false;
    }

    private void OnDestroy()
    {
        InputBlocker.IsDragging = false;
    }

    private void OnDisable()
    {
        InputBlocker.IsDragging = false;
    }
}
