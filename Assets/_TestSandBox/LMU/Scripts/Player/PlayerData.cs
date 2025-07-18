using Fusion;
using UnityEngine;

[System.Serializable]
public struct StaticPlayerData : INetworkInput
{
    public NetworkString<_16> NickName;
    
    public static StaticPlayerData CreateData(FakeClient.Data localPlayerData)
    {
        return new StaticPlayerData()
        {
            NickName = localPlayerData.NickName,
        };
    }
}

[System.Serializable]
public struct DynamicCharacterData : INetworkInput
{
    public NetworkString<_16> CharacterName;
    public NetworkString<_64> SkinPath;
    
    public static DynamicCharacterData CreateData(Skin.Data skinData)
    {
        return new DynamicCharacterData()
        {
            CharacterName = skinData.SkinName,
            SkinPath = skinData.SkinPath,
        };
    }
}

public class PlayerData : NetworkBehaviour
{
    public GameMode GameMode {get; set;}

    [Networked, UnitySerializeField]
    public ref StaticPlayerData Static_PlayerData => ref MakeRef<StaticPlayerData>();

    [Networked, UnitySerializeField]
    public ref DynamicCharacterData Dynamic_CharacterData => ref MakeRef<DynamicCharacterData>();
    [Networked] public bool IsReady {get; private set;} = false;

    [Header("로컬 데이터")]
    [field: SerializeField] public FakeClient.Data FakeClientData {get; private set;}   
    [field: SerializeField] public Skin.Data SkinData {get; private set;}

    private void Awake()
    {
        FakeClientData = DataManager.Inst.CurrentPlayerData;
    }

    public override void Spawned()
    {
        // 데이터 서버에서 생성후 전파
        if (Object.HasStateAuthority)
        {
            SkinData = DataManager.Inst.GetSkinData(10000);
            Static_PlayerData = StaticPlayerData.CreateData(FakeClientData);
            Dynamic_CharacterData = DynamicCharacterData.CreateData(SkinData);
        }

        FakeClientData = null;
    }

    /// <summary>
    /// 캐릭터/스킨 변경
    /// </summary>
    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    public void ChangeCharacterRpc(NetworkString<_16> characterName, NetworkString<_64> skinPath)
    {
        Dynamic_CharacterData = new DynamicCharacterData()
        {
            CharacterName = characterName,
            SkinPath = skinPath
        };
        Debug.Log($"플레이어 {Static_PlayerData.NickName} 캐릭터 변경: {characterName}");
    }

    /// <summary>
    /// Ready 상태 토글
    /// </summary>
    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    public void ToggleReadyRpc()
    {
        IsReady = !IsReady;
        Debug.Log($"플레이어 {Static_PlayerData.NickName} Ready 상태: {IsReady}");
    }

    /// <summary>
    /// Ready 상태 직접 설정 (서버용)
    /// </summary>
    public void SetReadyState(bool ready)
    {
        if (Object.HasStateAuthority)
        {
            IsReady = ready;
        }
    }

    public string NickName => Static_PlayerData.NickName.ToString();
    public string CharacterName => Dynamic_CharacterData.CharacterName.ToString();
    public string SkinPath => Dynamic_CharacterData.SkinPath.ToString();
}
