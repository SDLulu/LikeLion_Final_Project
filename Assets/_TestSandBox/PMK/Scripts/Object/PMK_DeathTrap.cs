using UnityEngine;
using System.Collections.Generic;
using Fusion;

public class PMK_DeathTrap : NetworkBehaviour
{
    private HashSet<NetworkObject> affectedPlayers = new HashSet<NetworkObject>();

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!Object.HasStateAuthority) return;
        if (collision == null) return;

        // 충돌한 콜라이더의 게임오브젝트가 플레이어 레이어인지 체크
        GameObject other = collision.gameObject;

        // 또는 collision.collider.gameObject 사용해도 됨
        if (other.layer != LayerMask.NameToLayer("Player")) return;

        NetworkObject netObj = other.GetComponentInParent<NetworkObject>();
        Rigidbody2D rb = other.GetComponentInParent<Rigidbody2D>();

        if (netObj == null || rb == null) return;

        if (!affectedPlayers.Contains(netObj))
        {
            rb.linearVelocity = Vector2.zero;
            rb.gravityScale = 0.1f;
            affectedPlayers.Add(netObj);
        }
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        if (!Object.HasStateAuthority) return;
        if (collision == null) return;

        GameObject other = collision.gameObject;

        if (other.layer != LayerMask.NameToLayer("Player")) return;

        NetworkObject netObj = other.GetComponentInParent<NetworkObject>();
        Rigidbody2D rb = other.GetComponentInParent<Rigidbody2D>();

        if (netObj == null || rb == null) return;

        if (affectedPlayers.Contains(netObj))
        {
            rb.gravityScale = 1f;
            affectedPlayers.Remove(netObj);
        }
    }
}
