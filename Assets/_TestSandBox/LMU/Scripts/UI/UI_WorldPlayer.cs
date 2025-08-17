using System.Collections.Generic;
using System.Linq;
using Fusion;
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
        // 게임 진행중일때만 비활성화
        UIEventSystem.Inst.OnGameUIActiveEvent -= ActiveReadyIcon;
        UIEventSystem.Inst.OnGameUIActiveEvent += ActiveReadyIcon;
        PlayerManager.Inst.AddPlayerDataAction(UpdateData);
        OnSceneLoadDone(GlobalSetting.Inst.LobbyScenePath);
    }

    public async void OnSceneLoadDone(string sceneName)
    {
        if (_ownerPlayerData == null || _ownerPlayerData.Object == null)
            return;

        if (_ownerPlayerData.Object.HasInputAuthority == false)
            return;

        // 비동기로 로비버튼을 찾아서 초기화
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

    public void OnReadyButtonClicked()
    {
        // 자신의 PlayerData만 찾아서 Ready 상태 변경
        var myPlayerData = GetMyPlayerData();
        if (myPlayerData != null && myPlayerData.Object.HasInputAuthority)
        {
            myPlayerData.RPC_ToggleReady(myPlayerData.IsReady == false);
        }
    }

    private PlayerData GetMyPlayerData()
    {
        var allPlayers = PlayerManager.Inst.GetPlayerDatas();
        foreach (var kvp in allPlayers)
        {
            if (kvp.Value.Object.HasInputAuthority)
            {
                return kvp.Value;
            }
        }
        return null;
    }

    /// <summary>
    /// Note - 데이터가 변경이 될때 호출 / 매개변수의 players를 사용하지는 않음. 
    /// </summary>
    public void UpdateData(Dictionary<Fusion.PlayerRef, PlayerData> players)
    {
        if (_ownerPlayerData.IsSpawned == false)
            return;

        _playerName.text = _ownerPlayerData.NickName;
        _playerReadyIcon.color = _ownerPlayerData.IsReady ? _readyColor : _unReadyColor;
    }

    private void ActiveReadyIcon(bool value)
    {
        if (this == null)
            return;
        if (_playerReadyIcon == null || _playerReadyIcon.Equals(null))
            return;

        _playerReadyIcon.gameObject.SetActive(value == false);
    }
}
