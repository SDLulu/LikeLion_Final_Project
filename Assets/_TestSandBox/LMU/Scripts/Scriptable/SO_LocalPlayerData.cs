using UnityEngine;

[CreateAssetMenu(fileName = "SO_LocalPlayerData", menuName = "스크립터블/LocalPlayerData")]
public class SO_LocalPlayerData : ScriptableObject
{
    [field: SerializeField] public LocalPlayerInfo LocalPlayer_Info {get; private set;}

    public static LocalPlayerInfo GetRandomLocalPlayerData()
    {
        var localPlayerData = Resources.LoadAll<SO_LocalPlayerData>("Data/TestClient");
        if (localPlayerData == null)
        {
            Debug.LogError($"경로에 로컬 플레이어 데이터가 존재하지 않습니다 Data/TestClient");
        }

        var randIndex = Random.Range(0, localPlayerData.Length);
        return localPlayerData[randIndex]?.LocalPlayer_Info;
    }

    [System.Serializable]
    public class LocalPlayerInfo
    {
        public string NickName;
    }
}
