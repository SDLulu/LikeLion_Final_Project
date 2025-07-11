using LMCore;
using UnityEngine;

public class LobbyManager : BaseManager<LobbyManager>
{
    [SerializeField] private NetRunner netRunner;
    public NetRunner NetRunner => netRunner ?? FindAnyObjectByType<NetRunner>();
}
