using System;
using System.Collections.Generic;
using Fusion;
using Fusion.Sockets;
using UnityEngine;

// 🎮 스펠렁키 플레이어 입력 수집기
// 기존 LocalInputPoller를 참고하되 SpelunkyPlayerData를 사용하도록 수정
public class SpelunkyLocalInputPoller : NetworkBehaviour, INetworkRunnerCallbacks
{
    // 🎯 입력을 수집할 스펠렁키 플레이어 컨트롤러
    [SerializeField] private SpelunkyPlayerController spelunkyPlayer;

    public override void Spawned()
    {
        // 로컬 플레이어인 경우에만 입력 수집 등록
        if (Object.HasInputAuthority)
        {
            Runner.AddCallbacks(this);
            
            // 플레이어 컨트롤러가 없으면 자동으로 찾기
            if (spelunkyPlayer == null)
                spelunkyPlayer = GetComponent<SpelunkyPlayerController>();
        }
    }

    // 🎮 네트워크 입력 수집 (매 틱마다 호출)
    public void OnInput(NetworkRunner runner, NetworkInput input)
    {
        if (runner != null && runner.IsRunning && spelunkyPlayer != null)
        {
            // SpelunkyPlayerController에서 입력 데이터 가져오기
            var data = spelunkyPlayer.GetNetworkInputData();
            input.Set(data);
        }
    }

    // === 네트워크 콜백들 (현재는 빈 구현) ===
    
    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player) { }
    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player) { }
    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason) { }
    public void OnConnectedToServer(NetworkRunner runner) { }
    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason) { }
    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }
    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason) { }
    public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }
    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList) { }
    public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }
    public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { }
    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data) { }
    public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { }
    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ArraySegment<byte> data) { }
    public void OnSceneLoadDone(NetworkRunner runner) { }
    public void OnSceneLoadStart(NetworkRunner runner) { }
} 