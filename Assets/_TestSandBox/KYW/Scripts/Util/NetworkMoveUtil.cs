using Fusion;
using Fusion.Addons.Physics;
using UnityEngine;

public static class NetworkMoveUtil
{
    // StateAuthority에서만 호출해야 함
    public static void Teleport(NetworkBehaviour caller, Transform target, Vector3 worldPosition)
    {
        if (caller == null || target == null) return;
        if (!caller.HasStateAuthority) return;

        // 우선순위: NetworkRigidbody2D -> NetworkTransform -> Rigidbody2D -> Transform
        var nrb2D = target.GetComponent<NetworkRigidbody2D>();
        if (nrb2D != null && nrb2D.Rigidbody != null)
        {
            nrb2D.Teleport(new Vector2(worldPosition.x, worldPosition.y));
            return;
        }

        var ntx = target.GetComponent<NetworkTransform>();
        if (ntx != null)
        {
            Vector3 pos3 = new Vector3(worldPosition.x, worldPosition.y, target.position.z);
            ntx.Teleport(pos3, target.rotation);
            return;
        }

        var rb2D = target.GetComponent<Rigidbody2D>();
        if (rb2D != null)
        {
            rb2D.position = new Vector2(worldPosition.x, worldPosition.y);
            rb2D.linearVelocity = Vector2.zero;
            rb2D.angularVelocity = 0f;
            return;
        }

        target.position = new Vector3(worldPosition.x, worldPosition.y, target.position.z);
    }
}


