using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class UI_CharacterSlotContainer : MonoBehaviour
{
    [Header("설정")]
    [SerializeField] private int maxPlayerCount = 4;
    [SerializeField] private SO_CharacterData[] characterDatas;
    [SerializeField] private Button readyButton;
 
    [Header("디버그용")]
    [SerializeField] private UI_CharacterSlot[] characterSlots;
    [SerializeField] private UI_CharacterSlot localPlayerSlot;
    [field: SerializeField] public int CurrentCharacterIndex {get; private set;}

    private void Awake()
    {
        // 초기화 시점에 슬롯이 존재하는 경우 제거
        var slots = GetComponentsInChildren<UI_CharacterSlot>().ToList();
        slots.ForEach(slot => Destroy(slot.gameObject));

        CurrentCharacterIndex = 0;

        readyButton.onClick.AddListener(OnClickReadyButton);
    }



    private void OnDestroy()
    {
        readyButton.onClick.RemoveAllListeners();

        if (characterSlots != null || characterSlots.Length > 0)
        {
            foreach (var slot in characterSlots)
            {
                slot.Holder = null;
                Destroy(slot.gameObject);
            }
        }
    }
    private void OnClickReadyButton()
    {
        localPlayerSlot.OnReadyChange();
    }

    public void CreateCharacterSlot()
    {

    }

    public SO_CharacterData GetCurrentData()
    {
        var index = Mathf.Clamp(CurrentCharacterIndex, 0, characterDatas.Length - 1);
        return characterDatas[index];
    }

    public void AddCurrentCharacterIndex()
    {
        if (CurrentCharacterIndex >= characterDatas.Length - 1)
            CurrentCharacterIndex = 0;
        else
            CurrentCharacterIndex++;
    }

    public void SubCurrentCharacterIndex()
    {
        if (CurrentCharacterIndex <= 0)
            CurrentCharacterIndex = characterDatas.Length - 1;
        else
            CurrentCharacterIndex--;
    }
}
