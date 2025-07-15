using Fusion;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ShopItemVisual : NetworkBehaviour
{
    [SerializeField] private GameObject priceTag;
    [SerializeField] private TextMeshProUGUI priceText; // TextMeshPro로 변경
    [SerializeField] private SpriteRenderer itemRenderer;
    [SerializeField] private Color availableColor = Color.white;
    [SerializeField] private Color pickedColor = Color.gray;
    [SerializeField] private Color soldColor = Color.red; // 판매된 아이템 색상

    // 아이템 스프라이트를 설정하는 함수
    public void SetItemSprite(Sprite sprite)
    {
        if (itemRenderer != null)
        {
            itemRenderer.sprite = sprite;
        }
    }

    public void UpdateVisual(ShopItemData itemData)
    {
        // 가격 표시
        if (priceText != null)
        {
            priceText.text = $"${itemData.Price}";
        }

        // 아이템 상태별 색상 변경 및 가격표 활성화/비활성화
        if (!itemData.IsAvailable) // 판매됨
        {
            if (itemRenderer != null) itemRenderer.color = soldColor;
            if (priceTag != null) priceTag.SetActive(false);
            // 아이템 오브젝트 자체를 비활성화하거나 파괴하는 로직은 ShopManager에서 처리해야 함
        }
        else if (itemData.IsPicked) // 들고 있는 중
        {
            if (itemRenderer != null) itemRenderer.color = pickedColor;
            if (priceTag != null) priceTag.SetActive(true);
        }
        else // 구매 가능 상태
        {
            if (itemRenderer != null) itemRenderer.color = availableColor;
            if (priceTag != null) priceTag.SetActive(true);
        }

        // 윤곽선은 플레이어가 상호작용 가능한 상태일 때 PlayerInteractionController에서 제어할 수 있습니다.
        // ShowOutline(bool show) 함수는 필요시 외부에서 호출
    }

    // 아이템이 들어올려졌을 때 윤곽선 표시 (선택 사항, PlayerInteraction에서 제어 가능)
    public void ShowOutline(bool show)
    {
        var outline = GetComponent<Outline>(); // 또는 다른 윤곽선 컴포넌트
        if (outline != null)
        {
            outline.enabled = show;
        }
    }
}