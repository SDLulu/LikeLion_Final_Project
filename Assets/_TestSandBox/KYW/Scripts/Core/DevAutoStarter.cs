using System.Collections;
using System.Collections.Generic;
using Fusion;
using Fusion.Sockets;
using UnityEngine;
using System;

// 🛠️ 개발용 플레이어 자동 소환 전용 클래스 (로비/네트워크/씬 이동 로직 제거)
// 역할: 네트워크에 참가된 후 플레이어가 없으면 자동으로 소환만 담당
// 네트워크 연결, 방 생성/참가, 씬 이동 등은 별도 로비 시스템에서 처리
public class DevAutoStarter : MonoBehaviour, INetworkRunnerCallbacks
{
    [Header("👤 플레이어 설정")]
    [SerializeField] private NetworkPrefabRef playerNetworkPrefab = NetworkPrefabRef.Empty; // 소환할 플레이어 프리팹
    [SerializeField] private Vector3 spawnPosition = Vector3.zero; // 플레이어 소환 위치
    [SerializeField] private bool autoSpawnPlayer = true; // 자동 소환 여부

    private NetworkRunner networkRunner;
    private bool isPlayerSpawned = false;

    private void Awake()
    {
        networkRunner = FindObjectOfType<NetworkRunner>();
        if (networkRunner != null)
            networkRunner.AddCallbacks(this);
    }

    // INetworkRunnerCallbacks 구현
    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        // 서버(Host)에서만 새로 들어온 플레이어를 소환
        if (runner.IsServer && autoSpawnPlayer && playerNetworkPrefab != NetworkPrefabRef.Empty)
        {
            if (!runner.TryGetPlayerObject(player, out var playerObject) || playerObject == null)
            {
                SpawnPlayerForRef(player);
            }
        }
    }

    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player) { }
    public void OnInput(NetworkRunner runner, NetworkInput input) { }
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
    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }

    // 플레이어 소환 (표준 Fusion 패턴)
    private void SpawnPlayerForRef(PlayerRef playerRef)
    {
        if (playerNetworkPrefab == NetworkPrefabRef.Empty || networkRunner == null)
            return;

        Debug.Log($"🛠️ [DevAutoStarter] 플레이어 소환 시작... PlayerRef: {playerRef}, IsLocalPlayer: {playerRef == networkRunner.LocalPlayer}");

        try
        {
            var player = networkRunner.Spawn(
                playerNetworkPrefab,
                spawnPosition,
                Quaternion.identity,
                playerRef
            );

            if (player != null)
            {
                networkRunner.SetPlayerObject(playerRef, player);
                Debug.Log($"🛠️ [DevAutoStarter] 플레이어 소환 완료! PlayerRef: {playerRef}, 위치: {player.transform.position}");
                if (playerRef == networkRunner.LocalPlayer)
                {
                    isPlayerSpawned = true;
                }
            }
            else
            {
                Debug.LogError($"🛠️ [DevAutoStarter] 플레이어 소환 실패: Spawn이 null 반환 (PlayerRef: {playerRef})");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"🛠️ [DevAutoStarter] 플레이어 소환 오류: {e.Message}");
        }
    }
} 