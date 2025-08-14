using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using UnityEngine;


/// <summary>
/// 개발 기획 변경으로 사용되지 않음.
/// </summary>
public class UI_PlayerSlotContainer : MonoBehaviour
{
    [Header("설정")]
    [SerializeField] private int maxPlayerCount = 4;


    [Header("인스펙터 참조")]
    [SerializeField] private Transform _playerSlotParent;

    [Header("디버그용")]
    [SerializeField] private List<UI_PlayerSlot> _playerSlots;

    private void Awake()
    {
        var slots = GetComponentsInChildren<UI_PlayerSlot>().ToList();
        _playerSlots = slots;
        foreach (var slot in _playerSlots)
        {
            slot.gameObject.SetActive(false);
        }

        if (NetworkEventSystem.Inst.IsReady)
        {
            OnInit();
        }
        else
        {
            NetworkEventSystem.Inst.OnAllManagersReady += OnInit;
        }
    }

    private void OnInit()
    {
        PlayerManager.Inst.AddPlayerDataAction(UpdateData);
    }

    public void UpdateData(Dictionary<Fusion.PlayerRef, PlayerData> players)
    {
        // 정렬 및 슬롯 개수 동기화
        var sortedKeys = players.Keys.OrderBy(k => k.AsIndex).ToArray();
        SyncCharacterSlotCount(sortedKeys.Length);

        foreach (var player in players)
        {
            var playerRef = player.Key;
            var playerGo = PlayerManager.Inst.GetPlayers().ContainsKey(playerRef) ? PlayerManager.Inst.GetPlayers()[playerRef].gameObject : null;
            if (playerGo == null)
                continue;

            var slot = _playerSlots[playerRef.AsIndex];
            if (slot == null)
                continue;

            slot.BindOwner(playerGo);
            slot.UpdateData();
            slot.gameObject.SetActive(true);
        }
    }

    /// <summary>
    /// 슬롯 개수를 목표 개수에 맞춰 동기화
    /// </summary>
    private void SyncCharacterSlotCount(int targetCount)
    {
        targetCount = Mathf.Clamp(targetCount, 0, maxPlayerCount);
        int currentCount = _playerSlots.Count;
        if (currentCount < targetCount)
        {
            for (int i = currentCount; i < targetCount; i++)
            {
                _playerSlots[i].gameObject.SetActive(true);
            }
        }
        else if (currentCount > targetCount)
        {
            for (int i = currentCount - 1; i >= targetCount; i--)
            {
                _playerSlots[i].gameObject.SetActive(false);
            }
        }
    }
}
