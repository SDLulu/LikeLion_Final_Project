using System;
using UnityEngine;
using LMCore;

public class UIEventSystem : BaseManager<UIEventSystem>
{
    // -- 로비 이벤트 목록록
    public event Action OnPauseUIToggleEvent;
    public event Action<bool> OnPauseUIActiveEvent;
    public event Action OnInventoryToggleEvent;
    public event Action<string> OnShowDialogEvent;
    public event Action OnHideDialogEvent;
    
    public event Action<bool> OnGameUIActiveEvent;
    
    public event Func<Awaitable> OnPlayerSlotsFadeInEvent;
    public event Func<Awaitable> OnPlayerSlotsFadeOutEvent;

    

    /// <summary>
    /// Pause UI 토글 이벤트 발생
    /// </summary>
    public void TriggerPauseUIToggle()
    {
        OnPauseUIToggleEvent?.Invoke();
    }

    /// <summary>
    /// Pause UI 활성화/비활성화 이벤트 발생
    /// </summary>
    public void TriggerPauseUIActive(bool active)
    {
        OnPauseUIActiveEvent?.Invoke(active);
    }

    /// <summary>
    /// 다이얼로그 표시 이벤트 발생
    /// </summary>
    public void TriggerShowDialog(string message)
    {
        OnShowDialogEvent?.Invoke(message);
    }

    /// <summary>
    /// 다이얼로그 숨김 이벤트 발생
    /// </summary>
    public void TriggerHideDialog()
    {
        OnHideDialogEvent?.Invoke();
    }

    /// <summary>
    /// 인벤토리 토글 이벤트 발생
    /// </summary>
    public void TriggerInventoryToggle()
    {
        OnInventoryToggleEvent?.Invoke();
    }

    /// <summary>
    /// 게임 UI 활성화/비활성화 이벤트 발생
    /// </summary>
    public void TriggerGameUIActive(bool active)
    {
        OnGameUIActiveEvent?.Invoke(active);
    }

    /// <summary>
    /// 플레이어 슬롯 페이드 인 이벤트 발생 (비동기)
    /// </summary>
    public async Awaitable TriggerPlayerSlotsFadeInAsync()
    {
        if (OnPlayerSlotsFadeInEvent != null)
        {
            await OnPlayerSlotsFadeInEvent.Invoke();
        }
    }

    /// <summary>
    /// 플레이어 슬롯 페이드 아웃 이벤트 발생 (비동기)
    /// </summary>
    public async Awaitable TriggerPlayerSlotsFadeOutAsync()
    {
        if (OnPlayerSlotsFadeOutEvent != null)
        {
            await OnPlayerSlotsFadeOutEvent.Invoke();
        }
    }
} 