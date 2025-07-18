using UnityEngine;

public class UI_Game : MonoBehaviour
{
    [Header("인스펙터 참조")]
    [SerializeField] private UI_PlayerSlotContainer _playerSlotContainer;

    public async Awaitable FadeInPlayerSlotsAsync() => await _playerSlotContainer.FadeInAsync();
    public async Awaitable FadeOutPlayerSlotsAsync() => await _playerSlotContainer.FadeOutAsync();
}
