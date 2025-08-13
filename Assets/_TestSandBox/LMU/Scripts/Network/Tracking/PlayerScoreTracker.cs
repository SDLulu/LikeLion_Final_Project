using System.Collections.Generic;
using Fusion;
using LMCore;
using UnityEngine;

public class PlayerScoreTracker : BaseManager<PlayerScoreTracker>
{
    [Header("디버그")]
    [SerializeField] private bool _isLogDebug = true;

    private readonly Dictionary<PlayerRef, int> _playerScore = new Dictionary<PlayerRef, int>();
    private NetworkRunner _runner;

    protected override void Awake()
    {
        base.Awake();

        if (NetworkEventSystem.Inst != null)
        {
            NetworkEventSystem.Inst.OnEnemyKilledEvent += OnEnemyKilled;
            NetworkEventSystem.Inst.OnItemCollectedEvent += OnItemCollected;
            NetworkEventSystem.Inst.OnPlayerJoinedEvent += OnPlayerJoined;
            NetworkEventSystem.Inst.OnPlayerLeftEvent += OnPlayerLeft;
            NetworkEventSystem.Inst.OnSceneLoadDoneEvent += OnSceneLoadDone;
        }
    }

    private void OnDestroy()
    {
        if (NetworkEventSystem.Inst != null)
        {
            NetworkEventSystem.Inst.OnEnemyKilledEvent -= OnEnemyKilled;
            NetworkEventSystem.Inst.OnItemCollectedEvent -= OnItemCollected;
            NetworkEventSystem.Inst.OnPlayerJoinedEvent -= OnPlayerJoined;
            NetworkEventSystem.Inst.OnPlayerLeftEvent -= OnPlayerLeft;
            NetworkEventSystem.Inst.OnSceneLoadDoneEvent -= OnSceneLoadDone;
        }
    }

    private bool IsServer()
    {
        if (_runner == null)
        {
            return true;
        }
        return _runner.IsServer;
    }

    private void OnSceneLoadDone(NetworkRunner runner, string sceneName)
    {
        _runner = runner;
    }

    private void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        if (IsServer() == false)
        {
            return;
        }

        if (_playerScore.ContainsKey(player) == false)
        {
            _playerScore[player] = 0;
        }
    }

    private void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
    {
        if (_playerScore.ContainsKey(player))
        {
            _playerScore.Remove(player);
        }
    }

    private void OnEnemyKilled(PlayerRef killer, int weight)
    {
        if (IsServer() == false)
        {
            return;
        }
        AddScore(killer, weight);
    }

    private void OnItemCollected(PlayerRef player, int weight)
    {
        if (IsServer() == false)
        {
            return;
        }
        AddScore(player, weight);
    }

    private void AddScore(PlayerRef player, int delta)
    {
        if (_playerScore.ContainsKey(player) == false)
        {
            _playerScore[player] = 0;
        }

        _playerScore[player] += delta;

        if (_isLogDebug)
        {
            Debug.Log($"[PlayerScoreTracker] Score - Player: {player}, New: {_playerScore[player]}");
        }

        NetworkEventSystem.Inst.TriggerScoreChanged(player, _playerScore[player]);
    }

    public bool TryGetScore(PlayerRef player, out int score)
    {
        return _playerScore.TryGetValue(player, out score);
    }
}


