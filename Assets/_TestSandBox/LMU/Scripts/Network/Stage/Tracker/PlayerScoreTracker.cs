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

    public override void Spawned()
    {
        _localPlayer = Runner.LocalPlayer; 

		LobbyManager.Inst.PlayerScoreTracker = this;
        if (Runner.IsServer)
        {
            NetworkEventSystem.Inst.OnEnemyKilledEvent += OnEnemyKilled;
            NetworkEventSystem.Inst.OnItemCollectedEvent += OnItemCollected;
        }
    }

	public override void Despawned(NetworkRunner runner, bool hasState)
	{
		LobbyManager.Inst.PlayerScoreTracker = null;
		if (NetworkEventSystem.HasInstance)
		{
			NetworkEventSystem.Inst.OnEnemyKilledEvent -= OnEnemyKilled;
			NetworkEventSystem.Inst.OnItemCollectedEvent -= OnItemCollected;
		}
	}

	public void ClearScore()
	{
        Debug.Log("<color=#FF0000>[PlayerScoreTracker] 모든 점수 초기화</color>");
		ItemScores.Clear();
		MonsterScores.Clear();
	}

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

	/// <summary>
	/// 플레이어의 총 점수를 반환 - 아이템 점수 + 킬 점수
	/// </summary>
	public int GetTotalScoreOf(PlayerRef player)
	{
		int itemScore = GetItemScore(player);
		int killScore = GetMonsterScore(player);
		return itemScore + killScore;
	}

    private void OnEnemyKilled(PlayerRef killer, int scoreWeight)
    {
		AddMonsterScore(killer, scoreWeight);
        int currentKillScore = GetMonsterScore(killer);
        Debug.Log($"적 처치 - Player: {killer}, 현재 킬 점수: {currentKillScore}</color>");
        NetworkEventSystem.Inst.TriggerScoreChanged(killer);
    }

    private void OnItemCollected(PlayerRef player, int weight)
    {
		AddItemScore(player, 1);
        int currentItemScore = GetItemScore(player);
        Debug.Log($"아이템 수집 - Player: {player}, 현재 아이템 점수: {currentItemScore}");
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