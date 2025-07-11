using TMPro;
using UnityEngine;

public class UI_PlayerSlot : MonoBehaviour
{
    [Header("인스펙터 참조")]
    [SerializeField] private TMP_Text _playerHP;
    [SerializeField] private RectTransform _itemHolder1;
    [SerializeField] private TMP_Text _item1CountText;
    [SerializeField] private RectTransform _itemHolder2;
    [SerializeField] private TMP_Text _item2CountText;

    public void UpdateData(PlayerData playerData)
    {
        _playerHP.text = playerData.NickName;
        // _item1CountText.text = playerData.Item1Count.ToString();
        // _item2CountText.text = playerData.Item2Count.ToString();
        // _itemHolder1.gameObject.SetActive(playerData.Item1Count > 0);
        // _itemHolder2.gameObject.SetActive(playerData.Item2Count > 0);
    }
}
