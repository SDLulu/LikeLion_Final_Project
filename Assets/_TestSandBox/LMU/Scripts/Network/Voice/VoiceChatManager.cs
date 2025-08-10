using System.Collections.Generic;
using Fusion;
using UnityEngine;

/// <summary>
/// Photon Fusion 2 환경에서 보이스챗 UI 항목을 관리합니다.
/// </summary>
public class VoiceChatManager : NetworkBehaviour, IPlayerJoined, IPlayerLeft
{
    [Header("플레이어 보이스 UI 프리팹")]
    [SerializeField] private GameObject _playerVoiceEntryPrefab;

    [Header("프리팹 부모 오브젝트 (Content)")]
    [SerializeField] private RectTransform _contentParent;

    [Header("플레이어별 UI 인스턴스 관리")]
    private Dictionary<PlayerRef, PlayerVoiceEntry> _playerEntries = new Dictionary<PlayerRef, PlayerVoiceEntry>();

    /// <summary>
    /// 플레이어 접속시 호출됩니다.
    /// 로컬 플레이어가 아닐 경우에만 UI 항목을 생성합니다.
    /// </summary>
    public void PlayerJoined(PlayerRef player)
    {
        if (player == Runner.LocalPlayer)
        {
            return;
        }

        if (_playerEntries.ContainsKey(player))
        {
            return;
        }

        if (_playerVoiceEntryPrefab == null)
        {
            Debug.LogWarning("playerVoiceEntryPrefab이 설정되지 않았습니다.");
            return;
        }

        if (_contentParent == null)
        {
            Debug.LogWarning("contentParent가 설정되지 않았습니다.");
            return;
        }

        GameObject entryObject = Instantiate(
            _playerVoiceEntryPrefab,
            _contentParent
        );

        PlayerVoiceEntry entry = entryObject.GetComponent<PlayerVoiceEntry>();
        if (entry == null)
        {
            Debug.LogWarning("생성된 프리팹에 PlayerVoiceEntry 컴포넌트가 없습니다.");
            return;
        }

        NetworkObject playerObject;
        bool hasPlayerObject = Runner.TryGetPlayerObject(player, out playerObject);
        if (hasPlayerObject == false)
        {
            Debug.LogWarning($"플레이어 {player}의 NetworkObject를 찾지 못했습니다.");
        }
        else
        {
            entry.Initialize(playerObject);
        }

        _playerEntries.Add(player, entry);
    }

    /// <summary>
    /// 플레이어 퇴장시 호출됩니다.
    /// 기존에 생성한 UI 항목을 제거합니다.
    /// </summary>
    public void PlayerLeft(PlayerRef player)
    {
        PlayerVoiceEntry entry;
        bool found = _playerEntries.TryGetValue(player, out entry);
        if (found)
        {
            if (entry != null)
            {
                Destroy(entry.gameObject);
            }

            _playerEntries.Remove(player);
        }
    }
}


