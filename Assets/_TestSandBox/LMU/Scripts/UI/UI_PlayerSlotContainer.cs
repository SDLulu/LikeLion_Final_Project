using System.Collections.Generic;
using System.Linq;
using UnityEngine;

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

        var playerM = FindAnyObjectByType<PlayerManager>();
        playerM.AddRenderingAction(UpdateData);
    }

    public void UpdateData(Fusion.NetworkDictionary<int, PlayerData> players)
    {
        // 정렬 및 슬롯 개수 동기화
        var sortedKeys = players.Select(p => p.Key).OrderBy(x => x).ToArray();
        SyncCharacterSlotCount(sortedKeys.Length);

        foreach (var player in players)
        {
            _playerSlots[player.Key].UpdateData(player.Value);
            _playerSlots[player.Key].gameObject.SetActive(true);
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
                _playerSlots[i].gameObject.SetActive(true);
        }
        else if (currentCount > targetCount)
        {
            for (int i = currentCount - 1; i >= targetCount; i--)
                _playerSlots[i].gameObject.SetActive(false);
        }
    }

}
