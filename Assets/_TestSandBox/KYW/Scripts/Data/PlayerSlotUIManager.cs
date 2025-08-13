using System.Collections.Generic;
using LMCore;
using Fusion;
using UnityEngine;
using UnityEngine.UI;

public class PlayerSlotUIManager : BaseManager<PlayerSlotUIManager>
{
    [Header("UI 설정")]
    [SerializeField] private HorizontalLayoutGroup _playerUIContainer;

    [SerializeField] private List<UI_PlayerSlot> _playerUIs = new();

    private bool _isCutSceneActive = false;
    private E_StateName _currentState = E_StateName.WaitingState;

    private void Start()
    {
         LobbyManager.Inst.OnNetworkEventsBound += RegisterEvents;
    }
    private void RegisterEvents()
    {
        NetworkEventSystem.Inst.OnCutSceneActiveEvent -= OnCutSceneActive;
        NetworkEventSystem.Inst.OnCutSceneActiveEvent += OnCutSceneActive;
        NetworkEventSystem.Inst.OnGameStateChangedEvent -= OnGameStateChanged;
        NetworkEventSystem.Inst.OnGameStateChangedEvent += OnGameStateChanged;
		NetworkEventSystem.Inst.OnShutdownEvent -= OnNetworkShutdown;
		NetworkEventSystem.Inst.OnShutdownEvent += OnNetworkShutdown;
        UpdateVisibility();
    }

    private void OnDisable()
    {
        if (NetworkEventSystem.HasInstance)
        {
            NetworkEventSystem.Inst.OnGameStateChangedEvent -= OnGameStateChanged;
            NetworkEventSystem.Inst.OnCutSceneActiveEvent -= OnCutSceneActive;
			NetworkEventSystem.Inst.OnShutdownEvent -= OnNetworkShutdown;
        }
    }

    private void OnDestroy()
    {
        if (NetworkEventSystem.HasInstance)
        {
            NetworkEventSystem.Inst.OnGameStateChangedEvent -= OnGameStateChanged;
            NetworkEventSystem.Inst.OnCutSceneActiveEvent -= OnCutSceneActive;
			NetworkEventSystem.Inst.OnShutdownEvent -= OnNetworkShutdown;
        }
    }

	private void OnNetworkEventsUnboundFromLobby()
	{
		DeletePlayerSlots();
	}

	private void OnNetworkShutdown(NetworkRunner runner, ShutdownReason reason)
	{
		DeletePlayerSlots();
	}

    private void OnGameStateChanged(NetworkRunner runner, E_StateName previous, E_StateName current)
    {
        _currentState = current;
        UpdateVisibility();
    }

    public void OnCutSceneActive(bool isCutSceneActive)
    {
        _isCutSceneActive = isCutSceneActive;
        UpdateVisibility();
    }

    private void UpdateVisibility()
    {
        if (_isCutSceneActive)
        {
            DeactivePlayerSlots();
            return;
        }

        if (_currentState == E_StateName.LobbyState)
        {
            DeactivePlayerSlots();
            return;
        }

        ActivePlayerSlots();
    }

    public void ActivePlayerSlots()
    {
        if (_playerUIs == null)
            return;
        foreach (var playerUI in _playerUIs)
        {
            if (playerUI == null)
                continue;
            playerUI.gameObject.SetActive(true);
        }
    }

    public void DeactivePlayerSlots()
    {
        if (_playerUIs == null)
            return;
        foreach (var playerUI in _playerUIs)
        {
            if (playerUI == null)
                continue;
            playerUI.gameObject.SetActive(false);
        }
    }

    public void DeletePlayerSlots()
    {
        if (_playerUIs == null)
            return;

        for (int i = _playerUIs.Count - 1; i >= 0; i--)
        {
            var playerUI = _playerUIs[i];
            if (playerUI == null)
            {
                _playerUIs.RemoveAt(i);
                continue;
            }
            UnregisterPlayerUI(playerUI);
        }
    }


    // 플레이어 UI 등록
    public void RegisterPlayerUI(UI_PlayerSlot playerUI)
    {
        if (playerUI == null) return;

        // 이미 등록되어 있는지 확인
        if (_playerUIs.Contains(playerUI))
        {
            Debug.LogWarning($"📱 PlayerSlotUIManager: 이미 등록된 플레이어 UI입니다 - {playerUI.name}");
            return;
        }

        // 리스트에 추가
        _playerUIs.Add(playerUI);

        // HorizontalLayoutGroup으로 이동
        if (_playerUIContainer != null)
        {
            playerUI.transform.SetParent(_playerUIContainer.transform, false);
            Debug.Log($"📱 PlayerSlotUIManager: 플레이어 UI 등록 완료 - {playerUI.name} (총 {_playerUIs.Count}개)");
        }
        else
        {
            Debug.LogError("📱 PlayerSlotUIManager: PlayerUIContainer가 설정되지 않았습니다!");
        }
    }

    // 플레이어 UI 제거
    public void UnregisterPlayerUI(UI_PlayerSlot playerUI)
    {
        if (playerUI == null) return;

        // 리스트에서 제거
        if (_playerUIs.Remove(playerUI))
        {
            Debug.Log($"📱 PlayerSlotUIManager: 플레이어 UI 제거 완료 - {playerUI.name} (총 {_playerUIs.Count}개)");
        }

        GameObject.Destroy(playerUI.gameObject);
        
    }

    // 현재 등록된 플레이어 UI 개수
    public int GetPlayerUICount()
    {
        return _playerUIs.Count;
    }
}
