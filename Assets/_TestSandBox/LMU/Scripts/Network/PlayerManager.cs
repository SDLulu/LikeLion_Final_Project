using System;
using System.Collections.Generic;
using System.Linq;
using Fusion;
using LMCore;
using UnityEngine;

[NetworkSpawnDelay(typeof(PlayerManager))]
public class PlayerManager : NetworkBehaviour
{
    public static PlayerManager Inst => BaseManager<PlayerManager>.Inst;
    public static bool HasInstance => BaseManager<PlayerManager>.HasInstance;

    // -- 서버 전용 필드
    private List<NetworkObject> _alivePlayers = new();
    private Dictionary<PlayerRef, ChangeDetector> _changeDetectors = new();

    // -- 네트워크 필드
    [Networked, Capacity(4)]
    public NetworkDictionary<PlayerRef, NetworkObject> Players => default;

    // ---
    private Dictionary<PlayerRef, PlayerData> _cacheDatas = new();

    public override void Spawned()
    {
        DontDestroyOnLoad(this.gameObject);
        NetworkEventSystem.Inst.RegisterNetDelay(this);
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        Players.Clear();
        _alivePlayers.Clear();
        _changeDetectors.Clear();
        _cacheDatas.Clear();
    }

    public override void Render()
    {
        if (CheckPlayerDataChanged())
        {
            OnChangedData();
        }
    }

    #region 유틸리티
    public void AddPlayer(PlayerRef player)
    {
        if (Runner.IsServer == false)
            return;

        if (Players.ContainsKey(player))
        {
            Debug.LogError($"이미 존재하는 플레이어 입니다 {player}");
            return;
        }

        var playerObj = Runner.GetPlayerObject(player);
        if (playerObj == null)
        {
            foreach (var activePlayer in Runner.ActivePlayers)
            {
                if (Players.ContainsKey(activePlayer))
                    continue;

                playerObj = Runner.TryGetPlayerObject(activePlayer, out var obj) ? obj : null;
                if (playerObj == null)
                    continue;

                Players.Add(activePlayer, playerObj);
                if (_changeDetectors.ContainsKey(activePlayer) == false)
                {
                    _changeDetectors[activePlayer] = playerObj.GetComponent<PlayerData>().GetChangeDetector(ChangeDetector.Source.SimulationState);
                }
                return;
            }
        }

        Players.Add(player, playerObj);
        if (_changeDetectors.ContainsKey(player) == false)
        {
            var playerData = playerObj.GetComponent<PlayerData>();
            if (playerData != null)
            {
                _changeDetectors[player] = playerData.GetChangeDetector(ChangeDetector.Source.SimulationState);
            }
        }

        // 플레이어 추가시 데이터 변경 이벤트 발생
        OnChangedData();
    }

    public List<(PlayerRef, NetworkObject)> TryRemoveAllPlayer()
    {
        var list = new List<(PlayerRef, NetworkObject)>();
        foreach (var player in Players)
        {
            list.Add(TryRemovePlayer(player.Key));
        }
        return list;
    }

    public (PlayerRef, NetworkObject) TryRemovePlayer(PlayerRef player)
    {
        if (Players.Count <= 0)
        {
            Debug.LogError("플레이어를 제거하는데 실패했습니다. 등록된 플레이어가 존재하지 않습니다.");
            return (PlayerRef.None, null);
        }

        foreach (var tempPlayer in Players)
        {
            if (tempPlayer.Key == player)
            {
                Players.Remove(tempPlayer.Key);
                if (_changeDetectors.ContainsKey(tempPlayer.Key))
                {
                    _changeDetectors.Remove(tempPlayer.Key);
                }
                return (tempPlayer.Key, tempPlayer.Value);
            }
        }

        Debug.LogError("플레이어를 제거하는데 실패했습니다.");
        return (PlayerRef.None, null);
    }



    public PlayerData GetPlayerData(PlayerRef player)
    {
        if (Players.ContainsKey(player))
        {
            return Players[player].GetComponent<PlayerData>();
        }
        return null;
    }

    public PlayerVoice GetPlayerVoice(PlayerRef player)
    {
        if (Players.ContainsKey(player))
        {
            return Players[player].GetComponentInChildren<PlayerVoice>();
        }
        return null;
    }

    public Dictionary<PlayerRef, PlayerData> GetPlayerDatas()
    {
        _cacheDatas.Clear();
        foreach (var player in Players)
        {
            _cacheDatas.Add(player.Key, player.Value.GetComponent<PlayerData>());
        }
        return _cacheDatas;
    }

    public NetworkDictionary<PlayerRef, NetworkObject> GetPlayers()
    {
        foreach (var player in Players)
        {
            if (player.Value == null)
            {
                Players.Remove(player.Key);
            }
        }
        return Players;
    }

    public List<InputBlocker> GetPlayerInputBlockers()
    {
        var list = new List<InputBlocker>();
        foreach (var player in Players)
        {
            list.Add(player.Value.GetComponent<InputBlocker>());
        }
        return list;
    }

    public List<NetworkObject> GetAlivePlayers()
    {
        _alivePlayers.Clear();
        foreach (var player in GetPlayerDatas())
        {
            if (player.Value.IsAlive)
                _alivePlayers.Add(player.Value.GetComponent<NetworkObject>());
        }

        return _alivePlayers;
    }

    public bool IsValidPlayer(PlayerRef player)
    {
        if (Players.ContainsKey(player))
            return true;
        return false;
    }
    #endregion

    #region 데이터 변경 알림
    public Action<Dictionary<PlayerRef, PlayerData>> OnPlayerDataChanged;
    public void AddPlayerDataAction(Action<Dictionary<PlayerRef, PlayerData>> action)
    {
        OnPlayerDataChanged += action;
        OnPlayerDataChanged?.Invoke(GetPlayerDatas());
    }
    public void RemovePlayerDataAction(Action<Dictionary<PlayerRef, PlayerData>> action)
    {
        OnPlayerDataChanged -= action;
    }
    private void OnChangedData()
    {
        OnPlayerDataChanged?.Invoke(GetPlayerDatas());
    }

    /// <summary>
    /// 데이터 변경을 감지하고 결과값을 반환하는 함수
    /// </summary>
    private bool CheckPlayerDataChanged()
    {
        // 최초 생성 시 초기 동기화를 위해 갱신 신호 반환
        bool isCreatedFrame = false;

        foreach (var kvp in Players)
        {
            NetworkObject netObj = kvp.Value;
            if (netObj == null)
                continue;

            PlayerData pData = netObj.GetComponent<PlayerData>();
            if (pData == null)
                continue;

            if (_changeDetectors.ContainsKey(kvp.Key) == false)
            {
                _changeDetectors[kvp.Key] = pData.GetChangeDetector(ChangeDetector.Source.SimulationState);
                isCreatedFrame = true; 
                continue;
            }

            var detector = _changeDetectors[kvp.Key];
            foreach (var _ in detector.DetectChanges(pData))
                return true;
        }

        if (isCreatedFrame)
            return true;

        return false;
    }
    #endregion

    #region 씬이동 및 RPC
    /// <summary>
    /// Note - 게임씬 로드완료시 호출 / Only Server
    /// </summary>
    public void MoveToGameScene(string sceneName)
    {
        if (sceneName == GlobalSetting.Inst.FocusScenePath)
        {
            RPC_MoveToGameScene();
        }
    }


    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_MoveToGameScene()
    {
        Internal_MoveToScene("GameScene");
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_MoveToLobbyScene()
    {
        Internal_MoveToScene("LobbyScene");
    }

    /// <summary>
    /// 게임오브젝트를 특정씬으로 이동시키는 함수
    /// </summary>
    private void Internal_MoveToScene(string sceneName)
    {
        foreach (var obj in this.Players.ToList().Select(x => x.Value.gameObject))
        {
            if (obj == null)
                continue;

            GameObject gameSceneObj = GameObject.Find(sceneName);
            if (gameSceneObj == null)
            {
                Debug.LogError("게임 씬 오브젝트를 찾을 수 없습니다.");
                continue;
            }
            Runner.MoveGameObjectToSameScene(obj, gameSceneObj);

            if (Runner.IsServer)
            {
                var teleporter = obj.GetComponent<PlayerStageController>();
                teleporter.SetPosition(new Vector2(0, 0));
            }
        }
    }

    #endregion
}
