using UnityEngine;
using System.Collections.Generic;
using Fusion;

public class PMK_DeathTrap : MonoBehaviour
{
    private PMK_TileRPC_Manager tileRPC_Manager => PMK_TileRPC_Manager.Instance;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!tileRPC_Manager.HasStateAuthority) return;
        Debug.Log("들어옴");

        // 또는 collision.collider.gameObject 사용해도 됨
        if (collision.gameObject.layer != LayerMask.NameToLayer("Player")) return;

        NetworkObject netObj = collision.gameObject.GetComponentInParent<NetworkObject>();
        if (netObj == null) return;
        Debug.Log("실행");
        tileRPC_Manager.RPC_SetPlayerGravity(netObj.InputAuthority, 0.1f);
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (!tileRPC_Manager.HasStateAuthority) return;
        Debug.Log("나감");

        if (collision.gameObject.layer != LayerMask.NameToLayer("Player")) return;

        NetworkObject netObj = collision.gameObject.GetComponentInParent<NetworkObject>();
        if (netObj == null) return;

        tileRPC_Manager.RPC_SetPlayerGravity(netObj.InputAuthority, 0.1f);
    }
}
