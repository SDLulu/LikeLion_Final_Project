using System.Collections.Generic;
using Fusion;
using LMCore;
using UnityEngine;

public class ChatManager : NetworkBehaviour, IsPollingSpawnable
{
    public static ChatManager Inst => BaseManager<ChatManager>.Inst;
    public static bool HasInstance => BaseManager<ChatManager>.HasInstance;

    [Networked, OnChangedRender(nameof(OnChangedChatHistories))]
    public ref ChatHistoryList ChatHistories => ref MakeRef<ChatHistoryList>();
    public bool IsSpawned { get; set; }
    private Dictionary<int, ChatHistory> _cachedChatHistories = new Dictionary<int, ChatHistory>();
    private UI_Chating _uiChating;

    private void Awake()
    {
        _uiChating = FindAnyObjectByType<UI_Chating>();
    }
    
    private void OnDestroy()
    {
        _uiChating = null;
        _cachedChatHistories.Clear();
    }

    public override void Spawned()
    {
        IsSpawned = true;
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        ChatHistories.Clear();
    }

    private void OnChangedChatHistories()
    {
        if (_uiChating == null)
            return;

        _cachedChatHistories.Clear();
        int index = 0;

        foreach (var chatHistory in ChatHistories.ChatHistories)
        {
            _cachedChatHistories[index] = chatHistory;
            index++;
        }

        _uiChating.UpdateChatHistories(_cachedChatHistories);
    }

    /// <summary>
    /// 채팅 메시지를 서버에 전송하는 RPC
    /// </summary>
    /// <param name="channel">채팅 채널</param>
    /// <param name="receiver">수신자 (귓속말용)</param>
    /// <param name="message">메시지 내용</param>
    /// <param name="rpcInfo">RPC 정보</param>
    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_SendChatMessage(ChatChannel channel, PlayerRef receiver, string message, RpcInfo rpcInfo = default)
    {
        if (Object.HasStateAuthority == false)
            return;

        var chatHistory = new ChatHistory(channel, rpcInfo.Source, receiver, message);

        ChatHistories.Add(chatHistory);
    }

    public async Awaitable<bool> IsPollingSpawned()
    {
        while (IsSpawned == false)
        {
            await Awaitable.NextFrameAsync();
        }
        return true;
    }
}