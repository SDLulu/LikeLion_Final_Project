using System.Linq;
using Fusion;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public enum E_WorldPlayerForm
{
    None,
    Lobby,
    World,
}

public class UI_WorldPlayer : NetworkBehaviour
{
    [Header("인스펙터 참조")]
    [SerializeField] private TMP_Text _playerName;
    [SerializeField] private Image _playerReadyIcon;

    [Header("설정")]
    [SerializeField] private Color _readyColor;
    [SerializeField] private Color _unReadyColor;

    [Header("디버그용")]
    [SerializeField] private Button _readyButton;

    public override void Spawned()
    {
        if (NetworkEventSystem.Inst.IsReady)
            OnInit();
        else
            NetworkEventSystem.Inst.OnAllManagersReady += OnInit;
    }
    public void OnInit()
    {
        if (this.Object.HasInputAuthority)
        {
            PlayerManager.Inst.AddPlayerDataAction(UpdateData);

            NetworkEventSystem.Inst.OnSceneLoadDoneEvent += (runner, sceneName) =>
            {
                OnSceneLoadDone(sceneName);
            };
            OnSceneLoadDone(GlobalSetting.Inst.LobbyScenePath);
        }
    }

    public async void OnSceneLoadDone(string sceneName)
    {
        if (sceneName == GlobalSetting.Inst.LobbyScenePath)
        {
            _readyButton?.onClick.RemoveAllListeners();
            while (_readyButton == null)
            {
                _readyButton = FindObjectsByType<Button>(FindObjectsSortMode.None)
                                .FirstOrDefault((button) =>
                                button.tag == "LobbyReady");

                await Awaitable.WaitForSecondsAsync(0.1f);
            }

            _readyButton.onClick.AddListener(OnReadyButtonClicked);
        }
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        _readyButton?.onClick.RemoveAllListeners();
    }

    private void OnReadyButtonClicked()
    {
        var playerData = GetComponentsInParent<PlayerData>().FirstOrDefault();
        playerData.RPC_ToggleReady(playerData.IsReady == false);
    }

    public void SetPlayerName(string name)
    {
        _playerName.text = name;
    }

    public void SetPlayerReadyIcon(bool isReady)
    {
        _playerReadyIcon.color = isReady ? _readyColor : _unReadyColor;
    }

    public void UpdateData(Fusion.NetworkDictionary<Fusion.PlayerRef, PlayerData> players)
    {
        foreach (var player in players)
        {
            if (player.Value.Object.InputAuthority != this.Object.InputAuthority)
                continue;

            SetPlayerName(player.Value.NickName);
            SetPlayerReadyIcon(player.Value.IsReady);
            break;
        }
    }
}
