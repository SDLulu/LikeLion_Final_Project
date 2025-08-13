using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
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

    [SerializeField] private List<UI_PlayerSlot> _playerUIs = new();

    private void Start()
    {
        // 씬 활성화 여부 / 컷씬 활성화 여부
        SceneManager.activeSceneChanged += (preScene, nextScene) =>
        {
            OnSceneLoadDone(nextScene.name);
        };
        NetworkEventSystem.Inst.OnCutSceneActiveEvent += (isActive) =>
        {
            OnCutSceneActive(isActive);
        };
    }

    private void OnSceneLoadDone(string sceneName = "")
    {
        // 게임 씬일때만 활성화
        if (string.IsNullOrEmpty(sceneName))
            sceneName = LocalSceneManager.Inst.GetActiveScene().name;

        if (sceneName == GlobalSetting.Inst.GameScenePath)
        {
            ActivePlayerSlots();
        }
        else if (sceneName == GlobalSetting.Inst.LobbyScenePath)
        {
            DeactivePlayerSlots();
        }
        else
        {
            DeletePlayerSlots();
        }
    }

    public void OnCutSceneActive(bool isCutSceneActive)
    {
        // 현재씬 체크
        OnSceneLoadDone();
        
        if (isCutSceneActive)
        {
            DeactivePlayerSlots();
        }
        else
        {
            ActivePlayerSlots();
        }
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

        foreach (var playerUI in _playerUIs)
        {
            if (playerUI == null)
                continue;
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

        // 추가될때 현재씬을 확인후 확인
        OnSceneLoadDone();
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
