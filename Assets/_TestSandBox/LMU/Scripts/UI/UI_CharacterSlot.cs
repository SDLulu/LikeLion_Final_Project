using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static SO_LocalPlayerData;
using static SO_SkinData;

public class UI_CharacterSlot : MonoBehaviour
{
    public UI_CharacterSlotContainer Holder {get; set;}
    
    [Header("캐릭터 슬롯")]
    [SerializeField] private Image characterImage;
    [SerializeField] private Button rightArrowButton;
    [SerializeField] private Button leftArrowButton;

    [Header("준비상태 - 설정")]
    [SerializeField] private Image readyPanel;
    [SerializeField] private TextMeshProUGUI playerNameText;
    [SerializeField] private TextMeshProUGUI readyText;
    [SerializeField] private Color readyColor;
    [SerializeField] private Color notReadyColor;
    [SerializeField] private string readyTextStr;
    [SerializeField] private string notReadyTextStr;

    [Header("디버그용")]
    [SerializeField] private bool isReady = false;
    [SerializeField] private LocalPlayerInfo currentPlayer;
    [SerializeField] private SkinInfo currentSkinData;

    private void Awake()
    {
        rightArrowButton.onClick.AddListener(OnClickRightArrowButton);
        leftArrowButton.onClick.AddListener(OnClickLeftArrowButton);
    }

    private void OnDestroy()
    {
        rightArrowButton.onClick.RemoveAllListeners();
        leftArrowButton.onClick.RemoveAllListeners();
    }

    public void OnReadyChange()
    {
        isReady = !isReady;
        UpdateUI();
    }

    private void OnClickRightArrowButton()
    {
        Holder.AddCurrentCharacterIndex();
        UpdateUI();
    }   

    private void OnClickLeftArrowButton()
    {
        Holder.SubCurrentCharacterIndex();
        UpdateUI();
    }

    private void UpdateUI()
    {
        playerNameText.text = currentPlayer?.NickName;
        characterImage.sprite = currentSkinData?.SkinImage;
        readyText.text = isReady ? readyTextStr : notReadyTextStr;
        readyPanel.color = isReady ? readyColor : notReadyColor;
    }

    /// <summary>
    /// 네트워크 플레이어 데이터를 슬롯에 할당
    /// </summary>
    public void UpdatePlayerData(LocalPlayerInfo localPlayerData, SkinInfo skinData)
    {
        if (localPlayerData == null || skinData == null) 
        {
            ClearSlotData();
            return;
        }

        currentSkinData = skinData;
        currentPlayer = localPlayerData;
        
        UpdateUI();
    }

    /// <summary>
    /// 슬롯 데이터 초기화
    /// </summary>
    private void ClearSlotData()
    {
        currentPlayer = null;
        currentSkinData = null;
        isReady = false;
        characterImage.sprite = null;
        readyText.text = notReadyTextStr;
        readyPanel.color = notReadyColor;
        rightArrowButton.gameObject.SetActive(false);
        leftArrowButton.gameObject.SetActive(false);
    }
}
