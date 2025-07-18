using UnityEngine;

public abstract class UsableItemBase : MonoBehaviour, IUsableItem
{
    protected bool isUsing = false;
    protected bool useQueued = false;
    
    public virtual void OnUsePress(Vector2 mouseWorldPosition, Vector2 playerPosition)
    {
        if (isUsing)
        {
            useQueued = true;
            return;
        }
        useQueued = false;
        isUsing = true;
        StartUse(mouseWorldPosition, playerPosition);
    }

    // 실제 사용 로직은 각 무기에서 구현
    protected abstract void StartUse(Vector2 mouseWorldPosition, Vector2 playerPosition);

    // 사용(모션) 끝날 때 호출
    protected void EndUse(Vector2 mouseWorldPosition, Vector2 playerPosition)
    {
        isUsing = false;
        if (useQueued)
        {
            useQueued = false;
            OnUsePress(mouseWorldPosition, playerPosition);
        }
    }

    // Hold/Release는 필요에 따라 각 무기에서 오버라이드
    public virtual void OnUseHold(Vector2 mouseWorldPosition, Vector2 playerPosition) { }
    public virtual void OnUseRelease(Vector2 mouseWorldPosition, Vector2 playerPosition) { }

    public virtual bool CanUse => true;
} 