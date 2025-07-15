using UnityEngine;

[CreateAssetMenu(fileName = "GlobalSetting", menuName = "스크립터블/GlobalSetting")]
public class SO_GlobalSetting : ScriptableObject
{
    [field: SerializeField] public string GlobalSettingName {get; private set;} = "이름 지정되지 않음";
    [field: SerializeField] public bool IsShowGameUI {get; private set;}
    [field: SerializeField] public GameObject PlayerPrefab {get; private set;}
    [field: SerializeField] public string LobbyScenePath {get; private set;}
    [field: SerializeField] public string GameScenePath {get; private set;}
}
