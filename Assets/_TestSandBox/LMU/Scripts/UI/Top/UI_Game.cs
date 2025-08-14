using System;
using UnityEngine;

public class UI_Game : MonoBehaviour
{
    [Header("인스펙터 참조")]
    [SerializeField] private UI_PlayerSlotContainer _playerSlotContainer;
    [SerializeField] private UI_Pause _uiPause;
    public void Awake()
    {
        ActivePauseUI(false);
        
        UIEventSystem.Inst.OnPauseUIToggleEvent += TogglePauseUI;
        UIEventSystem.Inst.OnPauseUIActiveEvent += ActivePauseUI;
        UIEventSystem.Inst.OnGameUIActiveEvent += SetGameUIActive;
        UIEventSystem.Inst.OnCutSceneActiveEvent += SetGameUIByCutSceneActive;
    }

    private void OnDestroy()
    {
        if (UIEventSystem.HasInstance)
        {
            UIEventSystem.Inst.OnPauseUIToggleEvent -= TogglePauseUI;
            UIEventSystem.Inst.OnPauseUIActiveEvent -= ActivePauseUI;
            UIEventSystem.Inst.OnGameUIActiveEvent -= SetGameUIActive;
            UIEventSystem.Inst.OnCutSceneActiveEvent -= SetGameUIByCutSceneActive;
        }
    }


    public void TogglePauseUI()
    {
        if (IsValid() == false)
            return;

        var value = _uiPause.gameObject.activeSelf == false;
        _uiPause.gameObject.SetActive(value);
    }

    public void ActivePauseUI(bool value)
    {
        if (IsValid() == false)
            return;
        _uiPause.gameObject.SetActive(value);
    }
    
    private void SetGameUIActive(bool active)
    {
        if (IsValid() == false)
            return;
        this.gameObject.SetActive(active);
    }

    private void SetGameUIByCutSceneActive(bool isCutSceneActive)
    {
        if (IsValid() == false)
            return;
        this.gameObject.SetActive(isCutSceneActive == false);
    }

    private bool IsValid()
    {
        if (this == null)
            return false;
        if (_uiPause == null || _uiPause.Equals(null))
            return false;
        if (_playerSlotContainer == null || _playerSlotContainer.Equals(null))
            return false;
        return true;
    }
}
