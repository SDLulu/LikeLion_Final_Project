using Fusion;
using UnityEngine;

public struct NetPlayerData : INetworkInput
{
    public NetworkString<_16> NickName;
    public NetworkString<_16> CharacterName;
}