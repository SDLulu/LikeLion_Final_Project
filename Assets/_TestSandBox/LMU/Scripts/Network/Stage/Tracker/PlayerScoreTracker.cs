using System.Collections.Generic;
using Fusion;
using UnityEngine;

public class PlayerScoreTracker : NetworkBehaviour
{
    [Header("인스펙터 참조")]
    [SerializeField] private UI_StageProgress _uiStageProgress;

    [Networked, OnChangedRender(nameof(OnItemScoreChanged))]
    private NetworkDictionary<PlayerRef, int> ItemScores { get; }

    [Networked, OnChangedRender(nameof(OnMonsterScoreChanged))]
    private NetworkDictionary<PlayerRef, int> MonsterScores { get; }

    private PlayerRef _localPlayer;

	private int GetMonsterScore(PlayerRef player)
	{
		if (MonsterScores.ContainsKey(player) == false)
			MonsterScores.Set(player, 0);

		return MonsterScores.Get(player);
	}

	private void SetMonsterScore(PlayerRef player, int score)
	{
		if (MonsterScores.ContainsKey(player) == false)
			MonsterScores.Set(player, score);
		else
			MonsterScores.Set(player, score);
	}

	private void AddMonsterScore(PlayerRef player, int delta)
	{
		int current = GetMonsterScore(player);
		SetMonsterScore(player, current + delta);
	}

	private int GetItemScore(PlayerRef player)
	{
		if (ItemScores.ContainsKey(player) == false)
			ItemScores.Set(player, 0);

		return ItemScores.Get(player);
	}

	private void SetItemScore(PlayerRef player, int score)
	{
		if (ItemScores.ContainsKey(player) == false)
			ItemScores.Set(player, score);
		else
			ItemScores.Set(player, score);
	}

	private void AddItemScore(PlayerRef player, int delta)
	{
		int current = GetItemScore(player);
		SetItemScore(player, current + delta);
	}

	/// <summary>
	/// 플레이어의 아이템 점수를 반환
	/// </summary>
	public int GetItemScoreOf(PlayerRef player)
	{
		int value = GetItemScore(player);
		return value;
	}

	/// <summary>
	/// 플레이어의 적 처치 점수를 반환
	/// </summary>
	public int GetMonsterScoreOf(PlayerRef player)
	{
		int value = GetMonsterScore(player);
		return value;
	}

    public override void Spawned()
    {
        _localPlayer = Runner.LocalPlayer; 

        if (Runner.IsServer)
        {
            NetworkEventSystem.Inst.OnEnemyKilledEvent += OnEnemyKilled;
            NetworkEventSystem.Inst.OnItemCollectedEvent += OnItemCollected;
        }
    }

    private void OnEnemyKilled(PlayerRef killer, EnemyData enemyData)
    {
		AddMonsterScore(killer, 1);
        NetworkEventSystem.Inst.TriggerScoreChanged(killer);
    }

    private void OnItemCollected(PlayerRef player, int weight)
    {
		AddItemScore(player, 1);
        NetworkEventSystem.Inst.TriggerScoreChanged(player);
    }

    public void OnItemScoreChanged()
    {
		_uiStageProgress.UpdateScoreData(GetMonsterScore(_localPlayer), GetItemScore(_localPlayer));
        UpdateMyScoreUI();
    }

    public void OnMonsterScoreChanged()
    {
		_uiStageProgress.UpdateScoreData(GetMonsterScore(_localPlayer), GetItemScore(_localPlayer));
        UpdateMyScoreUI();
    }

    private void UpdateMyScoreUI()
    {
		int myMonsterScore = GetMonsterScore(_localPlayer);
		int myItemScore = GetItemScore(_localPlayer);
        
        _uiStageProgress.UpdateScoreData(myMonsterScore, myItemScore);
    }
}