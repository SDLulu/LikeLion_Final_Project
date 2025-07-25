using UnityEngine;
using System.Collections.Generic;
using Fusion;

public class PMK_DeathTrap : NetworkBehaviour
{
    private HashSet<NetworkObject> affectedPlayers = new HashSet<NetworkObject>();

    private void OnTriggerEnter2D(Collider2D collision)
    {
        var netObj = collision.GetComponentInParent<NetworkObject>();
        if (netObj == null || !netObj.HasStateAuthority)
            return;

        var rb = collision.GetComponentInParent<Rigidbody2D>();

        if (!affectedPlayers.Contains(netObj))
        {
            rb.linearVelocity = Vector2.zero;
            rb.gravityScale = 0.1f;
            affectedPlayers.Add(netObj);
        }
    }


    private void OnTriggerExit2D(Collider2D collision)
    {
        var netObj = collision.GetComponentInParent<NetworkObject>();
        if (netObj == null || !netObj.HasStateAuthority)
            return;
;
        Rigidbody2D rb = collision.gameObject.GetComponentInParent<Rigidbody2D>();
        Debug.Log($"[DeathTrap] 충돌한 오브젝트: {collision.gameObject.name}, 부모: {collision.transform.parent?.name}");

        if (rb != null && affectedPlayers.Contains(netObj))
        {
            // 중력 복원 (기본 1으로 설정)
            rb.gravityScale = 1;
            affectedPlayers.Remove(netObj);
        }
    }
}
