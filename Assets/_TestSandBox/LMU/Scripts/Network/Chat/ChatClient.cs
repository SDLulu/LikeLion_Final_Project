using Fusion;
using System;
using UnityEngine;

public class ChatClient : NetworkBehaviour
{
    private UI_Chating _uiChating;

    private const string WHISPER_COMMAND = "/w";

    public override void Spawned()
    {
        if (Object.HasInputAuthority)
        {
            _uiChating = LobbyUI_Manager.Inst.UIChatting;
            _uiChating.OnInit(this);
        }
    }

    private void OnDestroy()
    {
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

    public (string, Color) GetMessage(ChatHistory chatHistory)
    {
        string inputText = "";
        Color msgColor = Color.black;

        // 보내는 사람의 닉네임초기화
        string senderName = "";
        var players = PlayerManager.Inst.GetPlayers();
        foreach (var player in players)
        {
            if (player.Value.Object.InputAuthority == chatHistory.Sender)
            {
                senderName = player.Value.NickName;
            }
        }

        // 받는 사람의 닉네임초기화
        string receiverName = "";
        foreach (var player in players)
        {
            if (player.Value.Object.InputAuthority == chatHistory.Receiver)
            {
                receiverName = player.Value.NickName;
            }
        }

        switch (chatHistory.Channel)
        {
            case ChatChannel.All:
                inputText = $"[전체] {senderName}: {chatHistory.Message}";
                msgColor = Color.black;
                break;

            case ChatChannel.Whisper:
                inputText = $"[귓속말] {senderName} → {receiverName}: {chatHistory.Message}";
                msgColor = Color.yellow;
                break;

            case ChatChannel.Mine:
                inputText = $"[나에게만] : {chatHistory.Message}";
                msgColor = Color.blue;
                break;
        }

        return (inputText, msgColor);
    }

    public PlayerRef LocalPlayer => LobbyManager.Inst.LocalPlayer;

    public void SendChatMessage(string message, ChatChannel channel = ChatChannel.None)
    {
        if (Object.HasInputAuthority == false)
            return;

        if (ChatManager.HasInstance == false || _uiChating == null)
        {
            Debug.LogError("ChatManager 또는 UI_Chating이 존재하지 않습니다.");
            return;
        }
        
        // ChatManager.RPC_SendChatMessage(Runner, message, channel);
    }
}