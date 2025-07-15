using UnityEngine;

public enum ItemType
{
    None = 0,
    Bomb = 1,
    Rope = 2,
    Jetpack = 3,
    Compass = 4,
    // 필요에 따라 더 많은 아이템 타입 추가
}

// --- ItemStaticData.cs ---
// 각 아이템의 정적 데이터를 관리하는 ScriptableObject (프리팹, 스프라이트 등

[CreateAssetMenu(fileName = "ItemStaticData", menuName = "GameData/ItemStaticData")]
public class ItemStaticData : ScriptableObject
{
    public ItemType itemType;
    public GameObject itemPrefab; // 이 아이템 타입에 해당하는 ShopItem 프리팹
    public Sprite itemSprite;     // ShopItemVisual이 사용할 스프라이트
    public string itemName;       // 아이템 이름
    public string itemDescription; // 아이템 설명
    public int basePrice;         // 기본 가격 (상점에서 변동될 수 있음)
}