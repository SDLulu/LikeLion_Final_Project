using System;
using UnityEngine;
using LMCore;

public class UIEventSystem : BaseManager<UIEventSystem>
{
    // -- 로비 이벤트 목록
    public event Action OnPauseUIToggleEvent;
    public event Action<bool> OnPauseUIActiveEvent;
    public event Action<bool> OnGameUIActiveEvent;
    

    public void TriggerPauseUIToggle()
    {
        OnPauseUIToggleEvent?.Invoke();
    }

    public void TriggerPauseUIActive(bool active)
    {
        OnPauseUIActiveEvent?.Invoke(active);
    }

    public void TriggerGameUIActive(bool active)
    {
        OnGameUIActiveEvent?.Invoke(active);
    }
} 