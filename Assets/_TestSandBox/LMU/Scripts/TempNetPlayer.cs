using Fusion;
using UnityEngine;

public class TempNetPlayer : NetworkBehaviour
{
    public GameMode GameMode {get; set;}

    [Networked]
    public ref NetPlayerData PlayerData => ref MakeRef<NetPlayerData>();

    public void OnInitData(SO_LocalPlayerData localPlayerData, SO_CharacterData characterData)
    {   
        PlayerData.NickName = localPlayerData.NickName;
        PlayerData.CharacterName = characterData.CharacterName;
    }
}
