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
        // var slots = GetComponentsInChildren<UI_PlayerSlot>().ToList();
        // _playerSlots = slots;
        // foreach (var slot in _playerSlots)
        // {
        //     slot.gameObject.SetActive(false);
        // }
    }

    /// <summary>
    /// 기획 변경으로 사용되지 않음
    /// </summary>
    public void UpdateData(Dictionary<Fusion.PlayerRef, PlayerData> players)
    {
        if (GlobalSetting.Inst.IsShowGameUI == false)
        {
            this.gameObject.SetActive(false);
            return;
        }
        
        // 정렬 및 슬롯 개수 동기화
        var sortedKeys = players.Select(p => p.Key).OrderBy(x => x).ToArray();
        SyncCharacterSlotCount(sortedKeys.Length);

        foreach (var player in players)
        {
            // _playerSlots[player.Key.AsIndex].UpdateData(player.Value);
            _playerSlots[player.Key.AsIndex].gameObject.SetActive(true);
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


    private bool _isFadeIn = false;
    private bool _isFadeOut = false;
    public async Awaitable FadeInAsync()
    {
        if(_isFadeIn)
            return;
        _isFadeIn = true;
        var canvasGroup = GetComponent<CanvasGroup>();
        canvasGroup.alpha = 0;
        canvasGroup.DOFade(1, 0.5f);
        await Awaitable.WaitForSecondsAsync(0.5f);
        _isFadeIn = false;
        await Awaitable.NextFrameAsync();
    }

    public async Awaitable FadeOutAsync()
    {
        if(_isFadeOut)
            return;
        _isFadeOut = true;
        var canvasGroup = GetComponent<CanvasGroup>();
        canvasGroup.alpha = 1;
        canvasGroup.DOFade(0, 0.5f);
        await Awaitable.WaitForSecondsAsync(0.5f);
        _isFadeOut = false;
        await Awaitable.NextFrameAsync();
    }

}
