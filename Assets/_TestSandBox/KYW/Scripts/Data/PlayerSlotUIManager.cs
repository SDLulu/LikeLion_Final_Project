using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PlayerSlotUIManager : MonoBehaviour
{
    #region Singleton
    private static PlayerSlotUIManager _instance;
    private static readonly object _lock = new object();
    private static bool _shuttingDown = false;

    public static PlayerSlotUIManager Inst
    {
        get
        {
            if (_shuttingDown)
            {
                Debug.LogWarning("[Singleton] PlayerSlotUIManager 인스턴스가 이미 파괴되었습니다. null을 반환합니다.");
                return null;
            }

            lock (_lock)
            {
                if (_instance == null)
                {
                    _instance = FindAnyObjectByType<PlayerSlotUIManager>();
                    if (_instance == null)
                    {
                        var singletonObject = new GameObject();
                        _instance = singletonObject.AddComponent<PlayerSlotUIManager>();
                        singletonObject.name = "PlayerSlotUIManager (Singleton)";
                        DontDestroyOnLoad(singletonObject);
                    }
                }
                return _instance;
            }
        }
    }

    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(this.gameObject);
        }
        else if (_instance != this)
        {
            Debug.Log("[Singleton] PlayerSlotUIManager 인스턴스가 이미 존재합니다. 중복을 제거합니다.");
            Destroy(this.gameObject);
        }
    }

    private void OnApplicationQuit()
    {
        _shuttingDown = true;
    }

    private void OnDestroy()
    {
        if (_instance == this)
        {
            _instance = null;
            _shuttingDown = true;
        }
    }
    #endregion

    [Header("UI 설정")]
    [SerializeField] private HorizontalLayoutGroup _playerUIContainer;

    private List<UI_PlayerSlot> _playerUIs = new();

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
    }

    // 현재 등록된 플레이어 UI 개수
    public int GetPlayerUICount()
    {
        return _playerUIs.Count;
    }
}
