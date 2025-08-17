using UnityEngine;
using TMPro;

public class UI_Score : MonoBehaviour
{
    [Header("인스펙터 참조")]
    [SerializeField] private TMP_Text _nickText;
    [SerializeField] private TMP_Text _killText;
    [SerializeField] private TMP_Text _itemText;
    [SerializeField] private TMP_Text _hpText;

    public void UpdateData(string nick, int kill, int item, int total, int health)
    {
        _nickText.text = "닉네임 : " + nick;
        _killText.text = "킬 점수 : " + kill.ToString();
        _itemText.text = "아이템 : " + item.ToString();
        _hpText.text = "체력 : " + health.ToString();
    }
}
