using System.Collections.Generic;
using LMCore;
using UnityEngine;

[DefaultExecutionOrder(-100)]
public class DataManager : BaseManager<DataManager>
{
    public Dictionary<int, Stage.Data> StageData;
    public Dictionary<int, Skin.Data> SkinData;
    public Dictionary<int, FakeClient.Data> FakeClientData;

    [Header("현재 플레이어 데이터")]
    [SerializeField] private string currentPlayerNickName;
    [field: SerializeField] public FakeClient.Data CurrentPlayerData {get; private set;}

    public void Awake()
    {
        StageData = Stage.Data.GetDictionary();
        SkinData = Skin.Data.GetDictionary();
        FakeClientData = FakeClient.Data.GetDictionary();

        CurrentPlayerData = GetRandomFakeClientData();
        currentPlayerNickName = CurrentPlayerData.NickName;
    }

    // --- 클라이언트 관련
    public FakeClient.Data GetFakeClientData(int id)
    {
        if (FakeClientData.TryGetValue(id, out var data))
        {
            return data;
        }
        Debug.LogError($"팩크클라이언트 데이터를 찾을 수 없습니다. id: {id}");
        return null;
    }

    public FakeClient.Data GetRandomFakeClientData()
    {
        int id = FakeClientData[0].DataID;
        var randomIndex = Random.Range(id, id + FakeClientData.Count);
        return GetFakeClientData(randomIndex);
    }

    // --- 스킨데이터 관련
    public Skin.Data GetSkinData(int id)
    {
        if (SkinData.TryGetValue(id, out var data))
        {
            return data;
        }
        Debug.LogError($"스킨 데이터를 찾을 수 없습니다. id: {id}");
        return null;
    }

    public Skin.Data GetRandomSkinData()
    {
        int id = SkinData[0].DataID;
        var randomIndex = Random.Range(id, id + SkinData.Count);
        return GetSkinData(randomIndex);
    }
}
