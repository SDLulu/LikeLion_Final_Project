using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using System.Linq;

[CreateAssetMenu(fileName = "GlobalSetting", menuName = "스크립터블/GlobalSetting")]
public class SO_GlobalSetting : ScriptableObject
{
    [field: SerializeField] public string GlobalSettingName {get; private set;} = "이름 지정되지 않음";
    [field: SerializeField] public bool IsShowGameUI {get; private set;}
    [field: SerializeField] public GameObject PlayerPrefab {get; private set;}
    [field: SerializeField] public string LobbyScenePath {get; private set;}
    [field: SerializeField] public string GameScenePath {get; private set;}
    [field: SerializeField] public Vector2 LobbySpawnPos {get; private set;}
    [field: SerializeField] public bool IsEnableBackend {get; private set;} = false;
    [field: SerializeField] public bool IsEnableVoice {get; private set;} = false;

#if UNITY_EDITOR
    [SerializeField] private List<SceneAsset> _sceneInfoList;
    public List<SceneAsset> SceneInfoList => _sceneInfoList;
    [field: SerializeField] public SceneAsset FocusScene {get; set;}
    [field: SerializeField] public bool IsFocusScene {get; set;}
    [SerializeField] private bool _isLoadScenes = false;
    public bool IsLoadScenes => _isLoadScenes = SceneInfoList?.Count > 0;
#endif

    [ContextMenu("RegisterAllSceneInfo")]
    public void RegisterAllSceneInfo()
    {
#if UNITY_EDITOR
        ClearSceneInfoList();
        string[] sceneGuids = AssetDatabase.FindAssets("t:Scene");

        _sceneInfoList = new List<SceneAsset>();
        foreach (string guid in sceneGuids)
        {
            string scenePath = AssetDatabase.GUIDToAssetPath(guid);
            SceneAsset sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(scenePath);
            if (sceneAsset == null)
                continue;

            _sceneInfoList.Add(sceneAsset);
            Debug.Log($"씬 등록됨: {scenePath}");
        }
        
        EditorUtility.SetDirty(this);
        AssetDatabase.SaveAssets();
        
        Debug.Log($"총 {_sceneInfoList.Count}개의 씬이 등록되었습니다.");
#endif
    }

    [ContextMenu("ClearSceneInfoList")]
    public void ClearSceneInfoList()
    {
#if UNITY_EDITOR
        FocusScene = null;
        IsFocusScene = false;
        _sceneInfoList.Clear();
        _sceneInfoList = null;
        EditorUtility.SetDirty(this);
        AssetDatabase.SaveAssets();
#endif
    }
}
