using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class UI_CharacterSlotContainer : MonoBehaviour
{
    [Header("설정")]
    [SerializeField] private int maxPlayerCount = 4;
    [SerializeField] private SO_CharacterData[] characterDatas;
    [SerializeField] private Button readyButton;
    [SerializeField] private UI_CharacterSlot slotPrefab;
    [SerializeField] private Transform slotParent;
 
    [Header("디버그용")]
    [SerializeField] private List<UI_CharacterSlot> characterSlots = new List<UI_CharacterSlot>();
    [SerializeField] private UI_CharacterSlot localPlayerSlot;
    [field: SerializeField] public int CurrentCharacterIndex {get; private set;}

    private void Awake()
    {
        // 초기화 시점에 슬롯이 존재하는 경우 제거
        var slots = GetComponentsInChildren<UI_CharacterSlot>().ToList();
        slots.ForEach(slot => Destroy(slot.gameObject));
        characterSlots.Clear();

        CurrentCharacterIndex = 0;

        readyButton.onClick.AddListener(OnClickReadyButton);
    }

    private void OnDestroy()
    {
        readyButton.onClick.RemoveAllListeners();

        if (characterSlots != null && characterSlots.Count > 0)
        {
            foreach (var slot in characterSlots)
            {
                if (slot != null)
                {
                    slot.Holder = null;
                    Destroy(slot.gameObject);
                }
            }
        }
    }

    private void OnClickReadyButton()
    {
        localPlayerSlot.OnReadyChange();
    }

    public void UpdateData(Fusion.NetworkDictionary<int, TempNetPlayer> players)
    {
        // key를 리스트로 수집 후 정렬
        var sortedKeys = players.Select(p => p.Key).OrderBy(x => x).ToArray();

        // 슬롯 개수 동기화
        SyncCharacterSlotCount(sortedKeys.Length);

        // 슬롯에 데이터 할당
        for (int i = 0; i < characterSlots.Count; i++)
        {
            if (i < sortedKeys.Length)
            {
                int playerKey = sortedKeys[i];
                var playerData = players[playerKey];
                characterSlots[i].SetPlayerData(playerData);
                characterSlots[i].gameObject.SetActive(true);
            }
            else
            {
                characterSlots[i].gameObject.SetActive(false);
            }
        }
    }

    /// <summary>
    /// 슬롯 개수를 목표 개수에 맞춰 동기화
    /// </summary>
    private void SyncCharacterSlotCount(int targetCount)
    {
        // 최대 개수 제한
        targetCount = Mathf.Clamp(targetCount, 0, maxPlayerCount);

        int currentCount = characterSlots.Count;

        if (currentCount < targetCount)
        {
            // 부족하면 생성
            for (int i = currentCount; i < targetCount; i++)
            {
                CreateCharacterSlot();
            }
        }
        else if (currentCount > targetCount)
        {
            // 많으면 제거
            for (int i = currentCount - 1; i >= targetCount; i--)
            {
                RemoveCharacterSlot(i);
            }
        }
    }

    /// <summary>
    /// 슬롯 생성함수
    /// </summary>
    public void CreateCharacterSlot()
    {
        if (slotPrefab == null || slotParent == null)
        {
            Debug.LogError("SlotPrefab 또는 SlotParent가 설정되지 않았습니다!");
            return;
        }

        if (characterSlots.Count >= maxPlayerCount)
        {
            Debug.LogWarning("최대 플레이어 수에 도달했습니다!");
            return;
        }

        var newSlot = Instantiate(slotPrefab, slotParent);
        newSlot.Holder = this;
        characterSlots.Add(newSlot);
    }

    /// <summary>
    /// 슬롯 제거함수
    /// </summary>
    public void RemoveCharacterSlot(int index)
    {
        if (index < 0 || index >= characterSlots.Count)
        {
            Debug.LogError($"잘못된 슬롯 인덱스: {index}");
            return;
        }

        var slotToRemove = characterSlots[index];
        if (slotToRemove != null)
        {
            slotToRemove.Holder = null;
            Destroy(slotToRemove.gameObject);
        }

        characterSlots.RemoveAt(index);
    }

    /// <summary>
    /// 슬롯 제거함수 (오버로드)
    /// </summary>
    public void RemoveCharacterSlot()
    {
        if (characterSlots.Count > 0)
        {
            RemoveCharacterSlot(characterSlots.Count - 1);
        }
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
