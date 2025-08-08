using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UI_WorldPlayer : MonoBehaviour
{
    [Header("인스펙터 참조")]
    [SerializeField] private PlayerData _ownerPlayerData;
    [SerializeField] private TMP_Text _playerName;
    [SerializeField] private Image _playerReadyIcon;

    [Header("설정")]
    [SerializeField] private Color _readyColor;
    [SerializeField] private Color _unReadyColor;

    [Header("디버그용")]
    [SerializeField] private Button _readyButton;


    public void Awake()
    {
        if (NetworkEventSystem.Inst.IsReady)
        {
            OnInit();
        }
        else
        {
            NetworkEventSystem.Inst.OnAllManagersReady += OnInit;
        }
    }

    public void OnDestroy()
    {
        _readyButton?.onClick.RemoveAllListeners();
    }

    public void OnInit()
    {
        PlayerManager.Inst.AddPlayerDataAction(UpdateData);
        NetworkEventSystem.Inst.OnSceneLoadDoneEvent += (runner, sceneName) =>
        {
            OnSceneLoadDone(sceneName);
        };
        OnSceneLoadDone(GlobalSetting.Inst.LobbyScenePath);
    }

    public async void OnSceneLoadDone(string sceneName)
    {
        if (sceneName == GlobalSetting.Inst.LobbyScenePath)
        {
            _readyButton?.onClick.RemoveAllListeners();
            while (_readyButton == null)
            {
                var buttons = FindObjectsByType<Button>(FindObjectsSortMode.None);
                _readyButton = buttons.FirstOrDefault((button) => button.tag == "LobbyReady");

                await Awaitable.WaitForSecondsAsync(0.1f);
            }

            _readyButton.onClick.AddListener(OnReadyButtonClicked);
        }
    }

    private void OnReadyButtonClicked()
    {
        _ownerPlayerData.RPC_ToggleReady(_ownerPlayerData.IsReady == false);
    }

    /// <summary>
    /// Note - 데이터가 변경이 될때 호출 / 매개변수의 players를 사용하지는 않음. 
    /// </summary>
    public void UpdateData(Dictionary<Fusion.PlayerRef, PlayerData> players)
    {
        bool hasNick = string.IsNullOrEmpty(_ownerPlayerData.NickName) == false;
        bool hasChar = string.IsNullOrEmpty(_ownerPlayerData.CharacterName) == false;
        bool hasSkin = string.IsNullOrEmpty(_ownerPlayerData.SkinPath) == false;
        bool isInitialized = hasNick && hasChar && hasSkin;

        if (isInitialized)
        {
            _playerName.text = _ownerPlayerData.NickName;
            _playerReadyIcon.color = _ownerPlayerData.IsReady ? _readyColor : _unReadyColor;
        }
    }
}
