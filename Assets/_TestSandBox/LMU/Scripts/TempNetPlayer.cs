using Fusion;
using UnityEngine;
using static SO_LocalPlayerData;
using static SO_SkinData;

public class TempNetPlayer : NetworkBehaviour
{
    public GameMode GameMode {get; set;}

    [Networked]
    public ref NetPlayerData PlayerData => ref MakeRef<NetPlayerData>();


    [Header("데이터")]
    [field: SerializeField] public LocalPlayerInfo LocalPlayerData {get; private set;}   
    [field: SerializeField] public SkinInfo SkinData {get; private set;}

    private void Awake()
    {
        SkinData = SO_SkinData.GetDefaultCharacterData();
        LocalPlayerData = SO_LocalPlayerData.GetRandomLocalPlayerData();
    }

    public override void Spawned()
    {
    }

}
