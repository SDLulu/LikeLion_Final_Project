using System;
using Fusion;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 기획 변경으로 더이상 사용하지 않음.
/// </summary>
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
    [SerializeField] private PlayerData connectedPlayer;
    
    public PlayerData ConnectedPlayer => connectedPlayer;

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

    private float _lastReadyChangeTime = 0f;
    private float _readyChangeCooldown = 0.3f;

    public void OnReadyChange()
    {
        if (Time.time - _lastReadyChangeTime < _readyChangeCooldown)
        {
            Debug.Log("레디 변경이 너무빠름");
            return;
        }

        _lastReadyChangeTime = Time.time;

        // // 연결된 플레이어가 있고, 로컬 플레이어인 경우에만 Ready 상태 변경
        // if (connectedPlayer != null && connectedPlayer.Object.HasInputAuthority)
        // {
        //     connectedPlayer.RPC_ToggleReady(connectedPlayer.IsReady == false);
        // }
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

    public void UpdatePlayerData(PlayerData player)
    {
        connectedPlayer = player;
        if (player != null)
        {
            isReady = player.IsReady;
            UpdateUI();
        }
    }

    public void ClearSlotData()
    {
        connectedPlayer = null;
        isReady = false;
        playerNameText.text = "";
        characterImage.sprite = null;
        readyText.text = notReadyTextStr;
        readyPanel.color = notReadyColor;
    }

    private void UpdateUI()
    {
        if (connectedPlayer != null)
        {
            playerNameText.text = connectedPlayer.NickName;
            isReady = connectedPlayer.IsReady;
        }
        
        readyText.text = isReady ? readyTextStr : notReadyTextStr;
        readyPanel.color = isReady ? readyColor : notReadyColor;
    }

    public void ActiveArrowButtons(bool active)
    {
        rightArrowButton.gameObject.SetActive(active);
        leftArrowButton.gameObject.SetActive(active);
    }
}
