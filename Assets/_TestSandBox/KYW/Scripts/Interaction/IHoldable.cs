public interface IHoldable
{
    bool IsHoldable { get; }
    void OnPickedUp(UnityEngine.Transform holder);
    void OnReleased();
} 