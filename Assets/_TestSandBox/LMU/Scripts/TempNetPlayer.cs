using Fusion;
using UnityEngine;
using static SO_LocalPlayerData;
using static SO_SkinData;

[System.Serializable]
public struct StaticPlayerData : INetworkInput
{
    public NetworkString<_16> NickName;
    
    public static StaticPlayerData CreateData(LocalPlayerInfo localPlayerData)
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
    
    public static DynamicCharacterData CreateData(SkinInfo skinData)
    {
        return new DynamicCharacterData()
        {
            CharacterName = skinData.SkinName,
            SkinPath = skinData.SkinPath,
        };
    }
}

public class TempNetPlayer : NetworkBehaviour
{
    public GameMode GameMode {get; set;}

    [Networked, UnitySerializeField]
    public ref StaticPlayerData Static_PlayerData => ref MakeRef<StaticPlayerData>();

    [Networked, UnitySerializeField]
    public ref DynamicCharacterData Dynamic_CharacterData => ref MakeRef<DynamicCharacterData>();
    [Networked] public bool IsReady {get; private set;} = false;

    [Header("로컬 데이터")]
    [field: SerializeField] public LocalPlayerInfo LocalPlayerData {get; private set;}   
    [field: SerializeField] public SkinInfo SkinData {get; private set;}

    public override void Spawned()
    {
        // 데이터 서버에서 생성후 전파
        if (Object.HasStateAuthority)
        {
            SkinData = SO_SkinData.GetDefaultCharacterData();
            LocalPlayerData = SO_LocalPlayerData.GetRandomLocalPlayerData();
            Static_PlayerData = StaticPlayerData.CreateData(LocalPlayerData);
            Dynamic_CharacterData = DynamicCharacterData.CreateData(SkinData);
        }
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
