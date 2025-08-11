using System.Collections.Generic;
using System.Linq;
using LMCore;
using UnityEditor.UIElements;
using UnityEngine;

[DefaultExecutionOrder(-100)]
public class DataManager : BaseManager<DataManager>
{
    public Dictionary<int, Stage.Data> StageData;
    public Dictionary<int, Skin.Data> SkinData;
    public Dictionary<int, FakeClient.Data> FakeClientData;
    public Dictionary<int, Sound.Data> SoundData;
    public Dictionary<int, Item.Data> ItemData;

    protected override void Awake()
    {
        StageData = Stage.Data.GetDictionary();
        SkinData = Skin.Data.GetDictionary();
        FakeClientData = FakeClient.Data.GetDictionary();
        SoundData = Sound.Data.GetDictionary();
        ItemData = Item.Data.GetDictionary();
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

    // --- 사운드 데이터 관련
    public Sound.Data GetSoundData(string name)
    {
        var result = SoundData.FirstOrDefault(x => x.Value.Name == name);
        if (result.Value != null)
        {
            return result.Value;
        }
        Debug.LogError($"사운드 데이터를 찾을 수 없습니다. name: {name}");
        return null;
    }

    // --- 스테이지 데이터 관련
    public Stage.Data GetStageData(int index)
    {
        if (StageData.TryGetValue(index, out var data))
        {
            return data;
        }
        Debug.LogError($"스테이지 데이터를 찾을 수 없습니다. index: {index}");
        var ret = StageData.First().Value;
        return ret;
    }

    // --- 아이템 데이터 관련
    public Item.Data GetItemData(int id)
    {
        if (ItemData.TryGetValue(id, out var data))
        {
            return data;
        }
        Debug.LogError($"아이템 데이터를 찾을 수 없습니다. id: {id}");
        return null;
    }
}
