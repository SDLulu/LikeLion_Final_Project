using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

/// <summary>
/// 기획변경으로 더이상사용하지않음.
/// </summary>
public class UI_CharacterSlotContainer : MonoBehaviour
{
    [Header("설정")]
    [SerializeField] private int maxPlayerCount = 4;
    [SerializeField] private SO_SkinData[] characterDatas;
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
        localPlayerSlot = null;
        CurrentCharacterIndex = 0;
        readyButton.onClick.AddListener(OnClickReadyButton);
    }

    private void OnDestroy()
    {
        readyButton.onClick.RemoveAllListeners();
        localPlayerSlot =  null;
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
        localPlayerSlot?.OnReadyChange();
    }

    public void UpdateData(Fusion.NetworkDictionary<int, PlayerData> players)
    {
        
        // 정렬 및 슬롯 개수 동기화
        var sortedKeys = players.Select(p => p.Key).OrderBy(x => x).ToArray();
        SyncCharacterSlotCount(sortedKeys.Length);
        localPlayerSlot = null;

        // 슬롯 데이터 할당
        for (int i = 0; i < characterSlots.Count; i++)
        {
            if (i < sortedKeys.Length)
            {
                int playerKey = sortedKeys[i];
                var playerData = players[playerKey];
                
                characterSlots[i].UpdatePlayerData(playerData);
                characterSlots[i].gameObject.SetActive(true);
                
                if (playerData.Object.HasInputAuthority)
                {
                    localPlayerSlot = characterSlots[i];
                    characterSlots[i].ActiveArrowButtons(true);
                }
                else
                {
                    characterSlots[i].ActiveArrowButtons(false);
                }
            }
            else
            {
                characterSlots[i].ClearSlotData();
                characterSlots[i].gameObject.SetActive(false);
            }
        }

        UpdateReadyButtonState();
    }

    /// <summary>
    /// 준비 버튼 활성화 상태 업데이트
    /// </summary>
    private void UpdateReadyButtonState()
    {
        readyButton.interactable = localPlayerSlot != null;
    }

    /// <summary>
    /// 슬롯 개수를 목표 개수에 맞춰 동기화
    /// </summary>
    private void SyncCharacterSlotCount(int targetCount)
    {
        targetCount = Mathf.Clamp(targetCount, 0, maxPlayerCount);
        int currentCount = characterSlots.Count;
        if (currentCount < targetCount)
        {
            for (int i = currentCount; i < targetCount; i++)
                CreateCharacterSlot();
        }
        else if (currentCount > targetCount)
        {
            for (int i = currentCount - 1; i >= targetCount; i--)
                RemoveCharacterSlot(i);
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
            // 로컬 플레이어 슬롯이었다면 참조 제거
            if (slotToRemove == localPlayerSlot)
            {
                localPlayerSlot = null;
            }
            
            slotToRemove.Holder = null;
            Destroy(slotToRemove.gameObject);
        }

        characterSlots.RemoveAt(index);
    }

    public void RemoveCharacterSlot()
    {
        if (characterSlots.Count > 0)
        {
            RemoveCharacterSlot(characterSlots.Count - 1);
        }
    }


    /// <summary>
    /// 캐릭터 인덱스 증가
    /// </summary>
    public void AddCurrentCharacterIndex()
    {
        if (characterDatas == null || characterDatas.Length == 0) return;
        
        if (CurrentCharacterIndex >= characterDatas.Length - 1)
            CurrentCharacterIndex = 0;
        else
            CurrentCharacterIndex++;
            
        UpdateLocalPlayerCharacter();
    }

    /// <summary>
    /// 캐릭터 인덱스 감소
    /// </summary>
    public void SubCurrentCharacterIndex()
    {
        if (characterDatas == null || characterDatas.Length == 0) return;
        
        if (CurrentCharacterIndex <= 0)
            CurrentCharacterIndex = characterDatas.Length - 1;
        else
            CurrentCharacterIndex--;
            
        UpdateLocalPlayerCharacter();
    }

    /// <summary>
    /// 로컬 플레이어의 캐릭터 선택을 네트워크에 반영
    /// </summary>
    private void UpdateLocalPlayerCharacter()
    {
        if (localPlayerSlot?.ConnectedPlayer != null && 
            localPlayerSlot.ConnectedPlayer.Object.HasInputAuthority &&
            characterDatas != null && 
            CurrentCharacterIndex >= 0 && CurrentCharacterIndex < characterDatas.Length)
        {
            var selectedCharacter = characterDatas[CurrentCharacterIndex].Character_Info;
            localPlayerSlot.ConnectedPlayer.RPC_ChangeCharacter(
                selectedCharacter.SkinName, 
                selectedCharacter.SkinPath
            );
        }
    }
}
