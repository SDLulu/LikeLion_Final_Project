using TMPro;
using UnityEngine;
using UnityEngine.UI;




public class UI_SkinViewSlot : MonoBehaviour
{
    [Header("인스펙터 참조")]
    [SerializeField] private Image _skinImage;
    [SerializeField] private TMP_Text _skinNameText;

    [Header("디버그용")]
    [SerializeField] private Skin.Data _skinData;
    public int DataID => _skinData.DataID;

    private void OnDestroy()
    {
        _skinData = null;
    }

    public void UpdateData(Skin.Data data)
    {       
        if (data == null)
        {
            _skinData = null;
            _skinImage.sprite = null;
            _skinNameText.text = string.Empty;
            this.gameObject.SetActive(false);
            Debug.LogWarning($"슬롯 데이터가 존재하지 않아 비활성화를 진행합니다.");
            return;
        }

        _skinData = data;
        _skinImage.sprite = Resources.Load<Sprite>(data.SkinPath);
        _skinNameText.text = data.SkinName;
    }

}
