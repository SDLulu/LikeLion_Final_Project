using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UI_CharacterSlot : MonoBehaviour
{
    public UI_CharacterSlotContainer Holder {get; set;}
    
    [Header("캐릭터 슬롯")]
    [SerializeField] private Image characterImage;
    [SerializeField] private Button rightArrowButton;
    [SerializeField] private Button leftArrowButton;

    [Header("준비상태 - 설정")]
    [SerializeField] private Image readyPanel;
    [SerializeField] private TextMeshProUGUI readyText;
    [SerializeField] private Color readyColor;
    [SerializeField] private Color notReadyColor;
    [SerializeField] private string readyTextStr;
    [SerializeField] private string notReadyTextStr;

    [Header("디버그용")]
    [SerializeField] private int characterIndex;
    [SerializeField] private string characterName;
    [SerializeField] private bool isReady = false;

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
        var currentData = Holder.GetCurrentData();

        // 캐릭터 UI 업데이트
        characterImage.sprite = currentData.CharacterImage;
        characterImage.rectTransform.sizeDelta = currentData.UILayoutSize;

        // 준비상태 UI 업데이트
        readyText.text = isReady ? readyTextStr : notReadyTextStr;
        readyPanel.color = isReady ? readyColor : notReadyColor;

        // Todo - 변경될때마다 RPC로 알려주기
    }



    

    /// <summary>
    /// 네트워크 플레이어 데이터를 슬롯에 할당
    /// </summary>
    public void SetPlayerData(TempNetPlayer player)
    {
        if (player == null) return;

        // 플레이어 데이터에서 정보 추출
        characterName = player.PlayerData.CharacterName.ToString();
        
        // UI 업데이트 (현재는 Holder에서 캐릭터 데이터를 가져오므로 기존 방식 유지)
        UpdateUI();
    }

}
