using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using System.Collections.Generic;
using System;
using System.Linq;

namespace LMCore
{
    public class UI_Hover_Group : MonoBehaviour, IPointerExitHandler
    {
        [Header("설정")]
        [SerializeField] private List<HoverableGroupItem> _hoverItems;

        [Header("호버 알파값 설정")]
        [SerializeField] private float _nonHoverAlpha = 0.4f;
        [SerializeField] private float _hoverAlpha = 1f;

        private int _lastHoveredIndex = 0; // 마지막으로 호버된 아이템의 인덱스

        [ContextMenu("탐색")]
        private void Search()
        {
            var eventTriggers = GetComponentsInChildren<EventTrigger>().ToList();
            foreach (var eventTrigger in eventTriggers)
            {
                if (eventTrigger == null)
                    continue;
                _hoverItems.Add(new HoverableGroupItem()
                {
                    EventTrigger = eventTrigger,
                });
            }
        }
        private void Awake()
        {
            if (_hoverItems == null || _hoverItems.Count <= 0)
            {
                Debug.LogWarning("UI_Hover_Group: 관리할 아이템이 없습니다.");
                return;
            }

            for (int i = 0; i < _hoverItems.Count; i++)
            {
                int index = i;
                HoverableGroupItem item = _hoverItems[i];

                if (item.EventTrigger == null)
                {
                    Debug.LogError($"UI_Hover_Group: {i}번 인덱스 아이템에 EventTrigger가 할당되지 않았습니다.");
                    continue;
                }

                var entry = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
                entry.callback.AddListener((data) => OnItemEnter(index));
                item.EventTrigger.triggers.Add(entry);
            }

            _lastHoveredIndex = 0;
            SetItemState(_lastHoveredIndex);
        }

        private void OnItemEnter(int index)
        {
            _lastHoveredIndex = index;
            SetItemState(index);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            SetItemState(_lastHoveredIndex);
        }

        private void SetItemState(int activeIndex)
        {
            for (int i = 0; i < _hoverItems.Count; i++)
            {
                float targetAlpha = (i == activeIndex) ? _hoverAlpha : _nonHoverAlpha;
                ApplyAlphaToItem(_hoverItems[i], targetAlpha);
            }
        }

        private void ApplyAlphaToItem(HoverableGroupItem item, float alpha)
        {
            // 텍스트 알파값 적용
            if (item.Texts != null)
            {
                foreach (var text in item.Texts)
                {
                    if (text == null) continue;
                    Color color = text.color;
                    color.a = alpha;
                    text.color = color;
                }
            }

            // 이미지 알파값 적용
            if (item.Images != null)
            {
                foreach (var image in item.Images)
                {
                    if (image == null) continue;
                    Color color = image.color;
                    color.a = alpha;
                    image.color = color;
                }
            }
        }
    }

    /// <summary>
    /// 호버 효과를 적용할 UI 요소들을 그룹화하는 직렬화 가능 클래스
    /// </summary>
    [Serializable]
    public class HoverableGroupItem
    {
        public string Name;
        public List<TMP_Text> Texts;
        public List<Image> Images;
        public EventTrigger EventTrigger;
    }
}