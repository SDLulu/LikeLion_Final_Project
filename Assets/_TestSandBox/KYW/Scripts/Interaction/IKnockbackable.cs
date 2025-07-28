using UnityEngine;

public interface IKnockbackable
{
    void ApplyKnockback(Vector2 force, float duration = 0f);
} 