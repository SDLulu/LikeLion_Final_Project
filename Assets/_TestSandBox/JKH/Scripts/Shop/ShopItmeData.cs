using Fusion;
using UnityEngine; // Vector3 때문에 필요

[System.Serializable]
public struct ShopItemData : INetworkStruct
{
    public ItemType ItemType;
    public int Price;
    public bool IsAvailable;       // true: 구매 가능 / false: 판매됨 (원래 IsSold)
    public bool IsPicked;          // 들고 있는 중인가?
    public PlayerRef CurrentHolder; // 현재 들고 있는 플레이어 (PlayerRef.None 이면 아무도 안 들고 있음)
    public Vector3 OriginalPosition; // 이 상점 아이템의 원래 위치
    public NetworkId ItemNetworkId;  // 이 상점 아이템에 해당하는 실제 ShopItem NetworkObject의 ID
}