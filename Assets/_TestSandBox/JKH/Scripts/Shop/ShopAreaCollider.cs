using Fusion;
using System.Collections.Generic;
using UnityEngine;
using static Unity.Collections.Unicode;

public class ShopAreaCollider : NetworkBehaviour
{
    private List<Collider2D> _objectsInShopArea = new List<Collider2D>();

    private void OnTriggerStay2D(Collider2D other)
    {
        if (Runner != null && Object.HasStateAuthority)
        {
            Debug.Log($"Host: Object staying in ShopAreaTrigger: {other.gameObject.name}");
        }
    }
    // --- OnTriggerEnter2D ---
    // 다른 콜라이더가 이 콜라이더(ShopAreaTrigger)에 진입했을 때 호출됩니다.
    void OnTriggerEnter2D(Collider2D other)
    {
        // Host에서만 로그를 출력하거나 로직을 처리하는 것이 일반적입니다.
        // 클라이언트에서도 로컬 로그는 가능하지만, 실제 게임 로직은 Host에서.
        if (Runner != null && Object.HasStateAuthority) // Host/Server에서만 동작
        {
            Debug.Log($"Host: Object entered ShopAreaTrigger: {other.gameObject.name}, Tag: {other.tag}");

            // 목록에 추가 (중복 방지)
            if (!_objectsInShopArea.Contains(other))
            {
                _objectsInShopArea.Add(other);
            }
        }
    }

    // --- OnTriggerExit2D ---
    // 다른 콜라이더가 이 콜라이더(ShopAreaTrigger)에서 벗어났을 때 호출됩니다.
    void OnTriggerExit2D(Collider2D other)
    {
        if (Runner != null && Object.HasStateAuthority) // Host/Server에서만 동작
        {
            Debug.Log($"Host: Object exited ShopAreaTrigger: {other.gameObject.name}, Tag: {other.tag}");
            Debug.Log($"Runner: Object exited ShopAreaTrigger: {Runner}, Tag: {other.tag}");

            // 목록에서 제거
            if (_objectsInShopArea.Contains(other))
            {
                _objectsInShopArea.Remove(other);
            }
        }
    }
}
