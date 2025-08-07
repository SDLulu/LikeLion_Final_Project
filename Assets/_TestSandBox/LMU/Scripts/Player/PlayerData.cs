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
    [Header("인스펙터 참조")]
    [SerializeField] private UI_ReadyText _readyText;

    public GameMode GameMode {get; set;}

    [Networked, UnitySerializeField]
    public ref StaticPlayerData Static_PlayerData => ref MakeRef<StaticPlayerData>();

    [Networked, UnitySerializeField]
    public ref DynamicCharacterData Dynamic_CharacterData => ref MakeRef<DynamicCharacterData>();

    [Networked] public bool IsReady {get; private set;} = false;

    [Header("로컬 데이터")]
    [field: SerializeField] public FakeClient.Data FakeClientData {get; private set;}   
    [field: SerializeField] public Skin.Data SkinData {get; private set;}

    public string NickName => Static_PlayerData.NickName.ToString();
    public string CharacterName => Dynamic_CharacterData.CharacterName.ToString();
    public string SkinPath => Dynamic_CharacterData.SkinPath.ToString();

    public override void Spawned()
    {
        // 닉네임 - 중요한 정보가 아니므로 로컬에서 설정
        if (Object.HasInputAuthority)
        {
            FakeClientData = UI_CreateNickName.FakeClientData;
            RPC_SetNickName(FakeClientData.NickName);
        }

        // 데이터 서버에서 생성후 전파
        if (Runner.IsServer && Object.HasStateAuthority)
        {
            SkinData = DataManager.Inst.GetSkinData(10000);
            Dynamic_CharacterData = DynamicCharacterData.CreateData(SkinData);
            
            // 게임 상태변경시 Ready 상태 초기화
            NetworkEventSystem.Inst.OnGameStateChangedEvent += (previousState, currentState) => 
            {
                // if (currentState != E_StateName.GameStagePlayingState)
                //     return;

                // if (_readyText?.IsReady == false)
                //     return;

                // RPC_ReadyTween(false);
            };
        }

        FakeClientData = null;
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    public void RPC_SetNickName(NetworkString<_16> nickName)
    {
        Static_PlayerData = new StaticPlayerData()
        {
            NickName = nickName
        };
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    public void RPC_ChangeCharacter(NetworkString<_16> characterName, NetworkString<_64> skinPath)
    {
        Dynamic_CharacterData = new DynamicCharacterData()
        {
            CharacterName = characterName,
            SkinPath = skinPath
        };
        Debug.Log($"플레이어 {Static_PlayerData.NickName} 캐릭터 변경: {characterName}");
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    public void RPC_ToggleReady(bool isReady)
    {
        IsReady = isReady;
        Debug.Log($"플레이어 {Static_PlayerData.NickName} Ready 상태: {IsReady}");
    }
}
