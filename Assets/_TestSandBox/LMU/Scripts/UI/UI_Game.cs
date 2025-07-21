using System;
using UnityEngine;

public class UI_Game : MonoBehaviour
{
    [Header("인스펙터 참조")]
    [SerializeField] private UI_PlayerSlotContainer _playerSlotContainer;
    [SerializeField] private UI_Pause _uiPause;

    public async Awaitable FadeInPlayerSlotsAsync() => await _playerSlotContainer.FadeInAsync();
    public async Awaitable FadeOutPlayerSlotsAsync() => await _playerSlotContainer.FadeOutAsync();

    public void Awake()
    {
        ActivePauseUI(false);
        
        // 이벤트 구독
        UIEventSystem.Inst.OnPauseUIToggleEvent += TogglePauseUI;
        UIEventSystem.Inst.OnPauseUIActiveEvent += ActivePauseUI;
        UIEventSystem.Inst.OnGameUIActiveEvent += SetGameUIActive;
        
        // 비동기 이벤트 구독
        UIEventSystem.Inst.OnPlayerSlotsFadeInEvent += FadeInPlayerSlotsAsync;
        UIEventSystem.Inst.OnPlayerSlotsFadeOutEvent += FadeOutPlayerSlotsAsync;
    }

    private void OnDestroy()
    {
        // 이벤트 구독 해제
        if (UIEventSystem.HasInstance)
        {
            UIEventSystem.Inst.OnPauseUIToggleEvent -= TogglePauseUI;
            UIEventSystem.Inst.OnPauseUIActiveEvent -= ActivePauseUI;
            UIEventSystem.Inst.OnGameUIActiveEvent -= SetGameUIActive;
            
            // 비동기 이벤트 구독 해제
            UIEventSystem.Inst.OnPlayerSlotsFadeInEvent -= FadeInPlayerSlotsAsync;
            UIEventSystem.Inst.OnPlayerSlotsFadeOutEvent -= FadeOutPlayerSlotsAsync;
        }
    }

    public void TogglePauseUI()
    {
        var value = _uiPause.gameObject.activeSelf == false;
        _uiPause.gameObject.SetActive(value);
    }

    public void ActivePauseUI(bool value)
    {
        _uiPause.gameObject.SetActive(value);
    }
    
    /// <summary>
    /// 게임 UI 전체 활성화/비활성화 (UIEventSystem에서 호출)
    /// </summary>
    private void SetGameUIActive(bool active)
    {
        this.gameObject.SetActive(active);
    }
}
