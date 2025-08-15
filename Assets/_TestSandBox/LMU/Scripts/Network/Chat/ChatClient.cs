using Fusion;
using System;
using UnityEngine;

public class ChatClient : NetworkBehaviour
{
    [Header("디버그용")]
    [SerializeField] private UI_Chating _uiChating;
    private const string WHISPER_COMMAND = "/w";
    public PlayerRef LocalPlayer => LobbyManager.Inst.LocalPlayer;
    private bool _isFirstLobbyState = true;
    private bool _isFirstChat = true;
    public override void Spawned()
    {
        base.Spawned();
        NetworkEventSystem.Inst.OnGameStateChangedEvent -= OnGameStateChanged;
        NetworkEventSystem.Inst.OnGameStateChangedEvent += OnGameStateChanged;
        if (NetworkEventSystem.Inst.IsReady)
        {
            OnInit();
        }
        else
        {
            NetworkEventSystem.Inst.OnAllManagersReady += OnInit;
        }
    }

    private void OnGameStateChanged(Fusion.NetworkRunner runner, E_StateName prevState, E_StateName nextState)
    {
        // 로비 상태로 진입시
        if (runner.IsServer && nextState == E_StateName.LobbyState && _isFirstLobbyState == false)
        {
            this.SendChatMessage("재도전을 통해 더 재미있는 게임을 해보세요!", ChatChannel.System);
        }

        // 플레이모드 진입시 채팅 기록 정리
        if (runner.IsServer && nextState == E_StateName.PlayingState)
        {
            ChatManager.Inst.ClearChatHistories();
            RPC_AllRemoveChat();
        }

        if (runner.IsServer && nextState == E_StateName.LobbyState)
            _isFirstLobbyState = false;
    }


    public void OnInit()
    {
        // 환영 메시지 전송
        if (Runner.IsServer && _isFirstChat && Object.HasInputAuthority)
        {
            this.SendChatMessage("소환사의 협곡에 오신 것을 환영합니다.", ChatChannel.System);
            _isFirstChat = false;
        }
        if (Object.HasInputAuthority)
        {
            _uiChating = LobbyUI_Manager.Inst.UIChatting;
            _uiChating.OnInit(this);
        }
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        _uiChating?.Clear();
        _uiChating = null;
    }

    /// <summary>
    /// 입력된 문자열을 분석하여 채널과 수신자 정보를 반환
    /// </summary>
    public (ChatChannel channel, PlayerRef receiver, string message) GetParseInfo(string inputText)
    {
        if (string.IsNullOrWhiteSpace(inputText))
        {
            var ret = (ChatChannel.All, default(PlayerRef), "");
            return ret;
        }

        // "/w" 명령어로 시작하는지 확인 - 귓속말
        if (inputText.StartsWith(WHISPER_COMMAND))
        {
            var parts = inputText.Split(new char[] { ' ' }, 3, StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length >= 3 && int.TryParse(parts[1], out int playerId))
            {
                var receiver = PlayerRef.FromEncoded(playerId);
                var message = parts[2];
                var ret1 = (ChatChannel.Whisper, receiver, message);
                return ret1;
            }

            var ret2 = (ChatChannel.Mine, default(PlayerRef), $"잘못된 귓속말 형식입니다. {WHISPER_COMMAND} [플레이어ID] [메시지]");
            return ret2;
        }

        var ret3 = (ChatChannel.All, default(PlayerRef), inputText);
        return ret3;    
    }

    public string GetMessage(ChatHistory chatHistory)
    {
        string inputText = "";

        // 보내는 사람의 닉네임초기화
        string senderName = "";
        var players = PlayerManager.Inst.GetPlayerDatas();
        if (players.ContainsKey(chatHistory.Sender))
        {
            senderName = players[chatHistory.Sender].NickName;
        }

        // 받는 사람의 닉네임초기화
        string receiverName = "";
        if (chatHistory.Receiver != default && players.ContainsKey(chatHistory.Receiver))
        {
            receiverName = players[chatHistory.Receiver].NickName;
        }

        // 닉네임이 비어있으면 기본값 사용
        if (string.IsNullOrEmpty(senderName))
        {
            senderName = $"Player{chatHistory.Sender.AsIndex}";
        }
        if (string.IsNullOrEmpty(receiverName) && chatHistory.Receiver != default)
        {
            receiverName = $"Player{chatHistory.Receiver.AsIndex}";
        }

        switch (chatHistory.Channel)
        {
            case ChatChannel.All:
                inputText = $"<color=#42a5f5>[전체] {senderName}:</color> <color=white>{chatHistory.Message}</color>";
                break;

            case ChatChannel.Whisper:
                inputText = $"<color=yellow>[귓속말] {senderName} → {receiverName}:</color> <color=white>{chatHistory.Message}</color>";
                break;

            case ChatChannel.Mine:
                inputText = $"<color=blue>[나에게만] :</color> <color=white>{chatHistory.Message}</color>";
                break;

            case ChatChannel.System:
                inputText = $"<color=green>{chatHistory.Message}</color>";
                break;
        }

        return inputText;
    }


    public void SendChatMessage(string message, ChatChannel channel = ChatChannel.None)
    {
        // 서버에서 System 메시지를 보낼 때는 InputAuthority 체크를 하지 않음
        if (Runner.IsServer && channel == ChatChannel.System)
        {
            ChatManager.RPC_SendChatMessage(Runner, default, message, channel);
            return;
        }
        
        if (Object.HasInputAuthority == false)
            return;

        PlayerRef localPlayer = Object.InputAuthority;
        ChatManager.RPC_SendChatMessage(Runner, localPlayer, message, channel);
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_AllRemoveChat()
    {
        if (_uiChating != null)
        {
            _uiChating.RemoveAllMessageUI();
            _uiChating.ClearMessageCache(); 
        }
    }
}