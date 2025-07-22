using UnityEngine;
using System.Collections.Generic;
using Fusion;

public class PMK_DeathTrap : NetworkBehaviour
{
    private HashSet<NetworkObject> affectedPlayers = new HashSet<NetworkObject>();

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!Object.HasStateAuthority || collision.gameObject.layer != LayerMask.NameToLayer("Player"))
            return;

        NetworkObject netObj = collision.GetComponent<NetworkObject>();
        Rigidbody2D rb = collision.gameObject.GetComponent<Rigidbody2D>();

        if (rb != null && !affectedPlayers.Contains(netObj))
        {
            // 중력 제거
            rb.linearVelocity = Vector2.zero;
            rb.gravityScale = 0.1f;
            affectedPlayers.Add(netObj);
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (!Object.HasStateAuthority || collision.gameObject.layer != LayerMask.NameToLayer("Player"))
            return;

        NetworkObject netObj = collision.GetComponent<NetworkObject>();
        Rigidbody2D rb = collision.gameObject.GetComponent<Rigidbody2D>();
        if (rb != null && affectedPlayers.Contains(netObj))
        {
            // 중력 복원 (기본 1으로 설정)
            rb.gravityScale = 1;
            affectedPlayers.Remove(netObj);
        }
    }
}
