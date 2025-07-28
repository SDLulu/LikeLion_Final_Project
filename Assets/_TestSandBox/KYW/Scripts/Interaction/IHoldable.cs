public interface IHoldable
{
    bool IsHoldable { get; }
    void OnPickedUp();
    void OnReleased();
} 