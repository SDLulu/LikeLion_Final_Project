using UnityEngine;
using LMCore;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine.SceneManagement;

public class GlobalSetting : BaseManager<GlobalSetting>
{
    [SerializeField] private SO_GlobalSetting _globalSetting;

    public bool IsShowGameUI => _globalSetting.IsShowGameUI;
    
    public string LobbyScenePath 
    {
        get
        {
            if (string.IsNullOrEmpty(_globalSetting.LobbyScenePath))
            {
                Debug.LogError($"GlobalSetting[{_globalSetting.GlobalSettingName}]에 로비 씬 경로 할당되지 않았습니다.");
                return string.Empty;
            }

            if (CheckScenePath(_globalSetting.LobbyScenePath) == false)
            {
                Debug.LogError($"GlobalSetting[{_globalSetting.GlobalSettingName}]의 로비 씬 경로가 유효하지 않습니다: {_globalSetting.LobbyScenePath}");
                return string.Empty;
            }

            return _globalSetting.LobbyScenePath;
        }
    }
    
    public string GameScenePath 
    {
        get
        {
            if (string.IsNullOrEmpty(_globalSetting.GameScenePath))
            {
                Debug.LogError($"GlobalSetting[{_globalSetting.GlobalSettingName}]에 게임 씬 패스가 할당되지 않았습니다.");
                return string.Empty;
            }

            if (!CheckScenePath(_globalSetting.GameScenePath))
            {
                Debug.LogError($"GlobalSetting[{_globalSetting.GlobalSettingName}]의 게임 씬 경로가 유효하지 않습니다");
                return string.Empty;
            }

            return _globalSetting.GameScenePath;
        }
    }

    public GameObject PlayerPrefab
    {
        get
        {
            if (_globalSetting.PlayerPrefab == null)
            {
                Debug.LogError($"GlobalSetting[{_globalSetting.GlobalSettingName}]에 플레이어 프리팹이 할당되지 않았습니다.");
                return null;
            }

            return _globalSetting.PlayerPrefab;
        }
    }

    /// <summary>
    /// 씬 경로가 유효한지 확인하는 함수
    /// </summary>
    private bool CheckScenePath(string scenePath)
    {
        // 빌드 설정에 포함된 씬인지 확인
        int buildIndex = SceneUtility.GetBuildIndexByScenePath(scenePath);
        if (buildIndex >= 0)
        {
            return true;
        }

#if UNITY_EDITOR
        // 2. 에디터에서 실제 씬 파일이 존재하는지 확인
        var sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(scenePath);
        if (sceneAsset != null)
        {
            Debug.LogWarning($"씬 파일은 존재하지만 빌드 설정에 포함되지 않았습니다: {scenePath}");
            return true;
        }
#endif

        return false;
    }
}
