using Fusion;
using System;
using UnityEngine;

public enum ChatChannel
{
    All,
    Whisper,    // 귓속말
    Mine,       // 나에게만 보이는 채팅
}

public class ChatClient : NetworkBehaviour
{
    private UI_Chating _uiChating;

    private const string WHISPER_COMMAND = "/w";

    private void Awake()
    {
        _uiChating = LobbyUI_Manager.Inst.UIChatting;
        _uiChating.OnInit(this);
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
            return (ChatChannel.All, default(PlayerRef), "");
        }

        // "/w" 명령어로 시작하는지 확인 - 귓속말
        if (inputText.StartsWith(WHISPER_COMMAND))
        {
            var parts = inputText.Split(new char[] { ' ' }, 3, StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length >= 3)
            {
                if (int.TryParse(parts[1], out int playerId))
                {
                    var receiver = PlayerRef.FromEncoded(playerId);
                    var message = parts[2];
                    return (ChatChannel.Whisper, receiver, message);
                }
            }

            return (ChatChannel.Mine, default(PlayerRef), $"잘못된 귓속말 형식입니다. {WHISPER_COMMAND} [플레이어ID] [메시지]");
        }

        return (ChatChannel.All, default(PlayerRef), inputText);
    }

    public void SendChatMessage(string inputText)
    {
        if (Object.HasInputAuthority == false)
            return;

        if (ChatManager.HasInstance == false || _uiChating == null)
        {
            Debug.LogError("ChatManager 또는 UI_Chating이 존재하지 않습니다.");
            return;
        }

        var parseInfo = GetParseInfo(inputText);

        ChatManager.Inst.RPC_SendChatMessage(
            parseInfo.channel,
            parseInfo.receiver,
            parseInfo.message
        );
    }
}