using System;
using Fusion;
using LMCore;
using UnityEngine;

public class LobbyManager : BaseManager<LobbyManager>
{
    public Action OnExitButtonClicked;
    [SerializeField] private NetworkRunner netRunner;
    public NetworkRunner NetRunner 
    {
        get
        {
            if (netRunner == null)
            {
                netRunner = FindAnyObjectByType<NetworkRunner>();
            }

            if (netRunner == null)
            {
                var obj = Resources.Load<GameObject>("Prefabs/LobbyBatchModule/@NetworkRunner");
                netRunner = Instantiate(obj).GetComponent<NetworkRunner>();
            }

            return netRunner;
        }
    }
    
    /// <summary>
    /// 외부에서 강제로 Runner 설정
    /// </summary>
    public NetworkRunner SetForcingRunner(string runnerID, INetworkRunnerCallbacks callbacks)
    {
        var obj = Resources.Load<GameObject>("Prefabs/LobbyBatchModule/@NetworkRunner");
        var runner = Instantiate(obj).GetComponent<NetworkRunner>();
        runner.name += $"_{runnerID}";
        runner.AddCallbacks(callbacks);
        runner.ProvideInput = true;
        netRunner = runner;
        return runner;
    }

    [SerializeField] private NetCallbacks netCallbacks;
    public NetCallbacks NetCallbacks 
    {
        get
        {
            if (netCallbacks == null)
            {
                netCallbacks ??= this.GetComponent<NetCallbacks>();
                netCallbacks ??= this.gameObject.AddComponent<NetCallbacks>();
                AddAction();
            }

            void AddAction()
            {
                if (netCallbacks != null)
                {
                    OnExitButtonClicked -= netCallbacks.OnPlayerLeftAction;
                    OnExitButtonClicked += netCallbacks.OnPlayerLeftAction;
                }
            }

            return netCallbacks;
        }
    }

    public void RemoveAction()
    {
        if (netCallbacks != null)
        {
            OnExitButtonClicked -= netCallbacks.OnPlayerLeftAction;
        }
    }

    private void OnDestroy()
    {
        OnExitButtonClicked = null;
    }


}
