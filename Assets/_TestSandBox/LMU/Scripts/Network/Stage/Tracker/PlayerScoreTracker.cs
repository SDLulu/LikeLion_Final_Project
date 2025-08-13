using System.Collections.Generic;
using Fusion;
using UnityEngine;

public class PlayerScoreTracker : MonoBehaviour
{
    [Header("인스펙터 참조")]
    [SerializeField] private UI_StageProgress _uiStageProgress;

    private Dictionary<PlayerRef, int> _itemScore = new();
    private Dictionary<PlayerRef, int> _monsterScore = new();

    private NetworkRunner _runner;

    private void Awake()
    {
        NetworkEventSystem.Inst.OnEnemyKilledEvent += OnEnemyKilled;
        NetworkEventSystem.Inst.OnItemCollectedEvent += OnItemCollected;
        NetworkEventSystem.Inst.OnSceneLoadDoneEvent += OnSceneLoadDone;
    }

    private bool IsServer()
    {
        if (_runner == null)
            return true;
        return _runner.IsServer;
    }

    private void OnSceneLoadDone(NetworkRunner runner, string sceneName)
    {
        if (runner == null)
        {
            _runner = runner;
            return;
        }
    }

    // -- 적 처치
    private void OnEnemyKilled(PlayerRef killer, EnemyData enemyData)
    {
        if (IsServer() == false)
            return;

        int enemyScore = GetEnemyScore(killer);
        SetEnemyScore(killer, enemyScore + 1);

        int itemScore = GetItemScore(killer);

        _uiStageProgress.UpdateScoreData(enemyScore, itemScore);
        NetworkEventSystem.Inst.TriggerScoreChanged(killer);
    }

    private int GetEnemyScore(PlayerRef player)
    {
        if (_monsterScore.ContainsKey(player) == false)
            _monsterScore.Add(player, 0);

        return _monsterScore[player];
    }

    private void SetEnemyScore(PlayerRef player, int score)
    {
        if (_monsterScore.ContainsKey(player) == false)
            _monsterScore.Add(player, 0);

        _monsterScore[player] = score;
    }

    // --- 아이템 획득
    private void OnItemCollected(PlayerRef player, int weight)
    {
        if (IsServer() == false)
            return;

        int itemScore = GetItemScore(player);
        SetItemScore(player, itemScore + 1);
        int monsterScore = GetEnemyScore(player);

        _uiStageProgress.UpdateScoreData(monsterScore, itemScore);
        NetworkEventSystem.Inst.TriggerScoreChanged(player);
    }

    private int GetItemScore(PlayerRef player)
    {
        if (_itemScore.ContainsKey(player) == false)
            _itemScore.Add(player, 0);

        return _itemScore[player];
    }

    private void SetItemScore(PlayerRef player, int score)
    {
        if (_itemScore.ContainsKey(player) == false)
            _itemScore.Add(player, 0);

        _itemScore[player] = score;
    }
}


