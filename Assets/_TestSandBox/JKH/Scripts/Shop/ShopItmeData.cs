using Fusion;
using UnityEngine; // Vector3를 위해 추가

[System.Serializable]
public struct ShopItemData : INetworkStruct
{
    public NetworkId ItemNetworkId; // 실제 스폰된 ShopItem의 NetworkId
    public ItemType ItemType;
    public int Price;
    public bool IsAvailable;    // 상점에서 구매 가능한지 여부 (판매 완료/도난 시 false)
    public bool IsPicked;       // 플레이어가 들고 있는지 여부
    public PlayerRef CurrentHolder; // 아이템을 현재 들고 있는 플레이어 (PlayerRef.None이면 없음)
    public Vector3 OriginalPosition; // 아이템의 원래 스폰 위치
    public NetworkString<_32> ItemName;     // ⭐️ 추가: 아이템 이름

    // 물리 상태를 ShopItemData에서 직접 동기화할 수도 있습니다.
    // public NetworkBool IsPhysicsSimulated;
    // public NetworkBool IsColliderTrigger;
}