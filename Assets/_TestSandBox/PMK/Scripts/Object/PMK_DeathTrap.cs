using UnityEngine;
using System.Collections.Generic;
using Fusion;

public class PMK_DeathTrap : NetworkBehaviour, IItemInteraction
{
    public bool IsHeld => throw new System.NotImplementedException();

    private Rigidbody2D rb;

    public override void Spawned()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    public void ApplyKnockback(Vector2 force, float duration = 0)
    {
        if (!HasStateAuthority) return;

        if (rb != null)
        {
            rb.AddForce(force, ForceMode2D.Impulse);
        }
    }

    public void OnPickedUp()
    {
    }

    public void OnReleased()
    {
    }

    public void OnUseHold(Vector2 mouseWorldPosition, Vector2 playerPosition)
    {
    }

    public void OnUsePress(Vector2 mouseWorldPosition, Vector2 playerPosition)
    {
    }

    public void OnUseRelease(Vector2 mouseWorldPosition, Vector2 playerPosition)
    {
    }
}