using TMPro; // TextMeshPro 사용
using UnityEngine;
using UnityEngine.UI; // UnityEngine.UI는 더 이상 필요 없을 수 있지만, 안전을 위해 유지

public class ShopItemVisual : MonoBehaviour // ⭐️ NetworkBehaviour 대신 MonoBehaviour 상속
{
    [SerializeField] private GameObject priceTag;
    [SerializeField] public TextMeshProUGUI priceText;
    // ⭐️ purchaseText 필드는 ShopItem에서 관리하는 _spawnedPurchaseUI와 겹치므로 제거
    // [SerializeField] private GameObject purchaseText; // 이 필드는 제거됩니다.

    [SerializeField] private SpriteRenderer itemRenderer;
    [SerializeField] private Color availableColor = Color.white;
    [SerializeField] private Color pickedColor = Color.gray;
    [SerializeField] private Color soldColor = Color.red; // 판매된 아이템 색상

   
    // ⭐️ ShopItem에서 호출하여 스프라이트를 설정하는 함수
    public void SetItemSprite(Sprite sprite)
    {
        if (itemRenderer != null)
        {
            itemRenderer.sprite = sprite;
        }
    }

    // ⭐️ ShopItem에서 호출하여 가격 텍스트를 업데이트하는 함수
    public void UpdatePriceText(int price)
    {
        if (priceText != null)
        {
            priceText.text = $"${price}";
        }
    }

    // ⭐️ ShopItem에서 호출하여 아이템 상태에 따라 비주얼을 업데이트하는 함수
    public void UpdateItemColorAndPriceTag(bool isAvailable, bool isPicked)
    {
        if (itemRenderer != null)
        {
            if (!isAvailable) // 판매됨 (또는 도난됨)
            {
                itemRenderer.color = soldColor;
                priceTag.SetActive(false);
            }
            else if (isPicked) // 들고 있는 중
            {
                itemRenderer.color = pickedColor;
                priceTag.SetActive(false);
            }
            else // 구매 가능 상태
            {
                itemRenderer.color = availableColor;
                priceTag.SetActive(true);
            }
        }

        // 가격표 활성화/비활성화 (판매/도난 시 숨김)
        if (priceTag != null)
        {
            priceTag.SetActive(isAvailable);
        }
    }

    // ⭐️ ShowOutline 함수는 그대로 유지 (외부에서 호출 가능)
    public void ShowOutline(bool show)
    {
        var outline = GetComponent<Outline>(); // 또는 다른 윤곽선 컴포넌트
        if (outline != null)
        {
            outline.enabled = show;
        }
    }

    // ⭐️ OnCollisionStay2D 및 OnCollisionExit2D는 ShopItemVisual에서 제거됩니다.
    // 이 로직은 ShopItem.cs에서 _isPlayerColliding networked 변수를 통해 관리됩니다.
    // private void OnCollisionStay2D(Collision2D collision) { /* 제거 */ }
    // private void OnCollisionExit2D(Collision2D collision) { /* 제거 */ }
}