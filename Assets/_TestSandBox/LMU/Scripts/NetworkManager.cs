using LMCore;
using UnityEngine;

public class NetworkManager : BaseManager<NetworkManager>
{
    public async Awaitable StartGame()
    {
        Debug.Log("StartGame");
    }
}
