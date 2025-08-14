using System.Collections.Generic;
using System.Linq;
using Fusion;
using UnityEngine;

[DefaultExecutionOrder(-100)]
public class UI_TestVoiceSlotContainer : MonoBehaviour
{
    private const string VOICE_PREFAB_PATH = "Prefabs/UI_TestVoice";
    private GameObject _voicePrefab;

    [SerializeField] private List<UI_TestVoiceSlot> _voiceSlots = new List<UI_TestVoiceSlot>();

    private void Awake()
    {
        return;
        _voicePrefab = Resources.Load<GameObject>(VOICE_PREFAB_PATH);
        if (_voicePrefab == null)
        {
            Debug.LogWarning($"Voice prefab not found at path : {VOICE_PREFAB_PATH}");
        }

        if (GlobalSetting.Inst.IsEnableVoice == false)
        {
            gameObject.SetActive(false);
            return;
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
        PlayerManager.Inst.AddPlayerDataAction(OnPlayerDataChanged);
    }

    private void OnPlayerDataChanged(Dictionary<PlayerRef, PlayerData> playerData)
    {
        // 보이스 슬롯 생성
        foreach (var player in playerData)
        {
            if (_voiceSlots.Any(slot => slot.Owner == player.Key))
                continue;

            CreateVoiceSlot(player.Key);
        }

        // 보이스 데이터 업데이트
        foreach (var player in playerData)
        {
            var voiceSlot = _voiceSlots.Find(slot => slot.Owner == player.Key);
            if (voiceSlot == null)
                continue;

            voiceSlot.UpdateData(player.Value);
        }
    }

    private void CreateVoiceSlot(PlayerRef player)
    {
        if (_voicePrefab == null)
        {
            return;
        }

        var voiceSlot = Instantiate(_voicePrefab, transform).GetComponent<UI_TestVoiceSlot>();
        _voiceSlots.Add(voiceSlot);

        var playerVoice = PlayerManager.Inst.GetPlayerVoice(player);
        voiceSlot.Owner = player;
        if (playerVoice != null)
        {
            playerVoice.UIVoiceSlot = voiceSlot;
        }
        voiceSlot.UpdateData(PlayerManager.Inst.GetPlayerData(player));
    }   

    private void RemoveVoiceSlot(PlayerRef player)
    {
        var voiceSlot = _voiceSlots.Find(slot => slot.Owner == player);
        if (voiceSlot == null)
        {
            return;
        }

        var playerVoice = PlayerManager.Inst.GetPlayerVoice(player);
        if (playerVoice != null)
        {
            playerVoice.UIVoiceSlot = null;
        }
        voiceSlot.Owner = default;
        if (voiceSlot != null)
        {
            Destroy(voiceSlot.gameObject);
            _voiceSlots.Remove(voiceSlot);
        }
    }
}
