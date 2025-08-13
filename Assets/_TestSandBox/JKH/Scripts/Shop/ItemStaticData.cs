using UnityEngine;
using Fusion; // NetworkPrefabRef를 사용하기 위해 추가

[System.Serializable]
public class ItemStaticData
{
    public ItemType itemType;
    public string itemName; // ⭐️ 추가: 아이템 이름
    public int basePrice;
    public NetworkPrefabRef itemPrefab; // ShopItem 컴포넌트가 붙은 프리팹
    public Sprite itemSprite;
}

public enum ItemType // 이 Enum은 ItemStaticData 또는 별도의 전역 파일에 정의되어야 합니다.
{
    None,
    Active,
    Passive,
    Healing
}