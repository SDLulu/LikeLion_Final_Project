using UnityEngine;
using LMCore;
#if UNITY_EDITOR
using UnityEditor;
#endif

[DefaultExecutionOrder(-1000)]
public class GlobalSetting : BaseManager<GlobalSetting>
{
    [SerializeField] public bool _isShowGlobalSettingUI = false;

    [SerializeField] private SO_GlobalSetting _settingData;
    public SO_GlobalSetting SettingData => _settingData;

    public bool IsShowGameUI => _settingData.IsShowGameUI;
    
    public string LobbyScenePath 
    {
        get
        {
            if (string.IsNullOrEmpty(_settingData.LobbyScenePath))
            {
                Debug.LogError($"GlobalSetting[{_settingData.GlobalSettingName}]에 로비 씬 경로 할당되지 않았습니다.");
                return string.Empty;
            }

            if (IsValidScenePath(_settingData.LobbyScenePath) == false)
            {
                Debug.LogError($"GlobalSetting[{_settingData.GlobalSettingName}]의 로비 씬 경로가 유효하지 않습니다: {_settingData.LobbyScenePath}");
                return string.Empty;
            }

            return _settingData.LobbyScenePath;
        }
    }
    
    public string GameScenePath 
    {
        get
        {
            if (string.IsNullOrEmpty(_settingData.GameScenePath))
            {
                Debug.LogError($"GlobalSetting[{_settingData.GlobalSettingName}]에 게임 씬 패스가 할당되지 않았습니다.");
                return string.Empty;
            }

            if (IsValidScenePath(_settingData.GameScenePath) == false)
            {
                Debug.LogError($"GlobalSetting[{_settingData.GlobalSettingName}]의 게임 씬 경로가 유효하지 않습니다");
                return string.Empty;
            }

            return _settingData.GameScenePath;
        }
    }

    public string FocusScenePath
    {
        get
        {
            #if UNITY_EDITOR
            if (_settingData.FocusScene == null)
            {
                return string.Empty;
            }

            if (IsValidScenePath(_settingData.FocusScene.name) == false)
            {
                Debug.LogError($"GlobalSetting[{_settingData.GlobalSettingName}]의 포커스 씬 경로가 유효하지 않습니다");
                return string.Empty;
            }
            return _settingData.FocusScene.name;
            #endif
            return string.Empty;
        }
    }

    public GameObject PlayerPrefab
    {
        get
        {
            if (_settingData.PlayerPrefab == null)
            {
                Debug.LogError($"GlobalSetting[{_settingData.GlobalSettingName}]에 플레이어 프리팹이 할당되지 않았습니다.");
                return null;
            }

            return _settingData.PlayerPrefab;
        }
    }

    /// <summary>
    /// 씬 경로가 유효한지 확인하는 함수
    /// </summary>
    private bool IsValidScenePath(string scenePath)
    {

// #if UNITY_EDITOR
//         // 에디터에서 실제 씬 파일이 존재하는지 확인
//         var sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(scenePath);
//         if (sceneAsset == null)
//             return false;
// #endif

        return true;
    }

    public Vector2 LobbySpawnPos => _settingData.LobbySpawnPos;

    public Vector2 GetRandomLobbySpawnPos(float xFactor = 2.0f)
    {
        float randSpawnX = UnityEngine.Random.Range(LobbySpawnPos.x - xFactor, LobbySpawnPos.x + xFactor);
        return new Vector2(randSpawnX, LobbySpawnPos.y);
    }

    public bool IsShowTitleAnimation => _settingData.IsShowTitleAnimation;
}
