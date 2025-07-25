public interface IInvincible
{
    bool IsInvincible { get; }
    void SetInvincible(bool value, float duration = 0f);
} 