using Fusion;
using LMCore;
using UnityEngine;
using System.Collections.Generic;

public class LobbyManager : BaseManager<LobbyManager>
{
    [SerializeField] private NetRunner netRunner;
    public NetRunner NetRunner
    {
        get
        {
            if (netRunner == null)
            {
                var resource = Resources.Load<NetRunner>("Prefabs/@NetworkRunner");
                if (resource == null)
                {
                    Debug.LogError("NetworkRunner 프리팹을 찾을 수 없습니다.");
                    return null;
                }


                var netRunnerObj = Instantiate(resource.gameObject);
                if (netRunnerObj == null)
                {
                    Debug.LogError("인스턴스화 불가 - 네트워크 러너 프리팹 없음");
                    return null;
                }

                netRunner = netRunnerObj.GetComponent<NetRunner>();
                DontDestroyOnLoad(netRunnerObj);
            }
            return netRunner;
        }
    }
}
