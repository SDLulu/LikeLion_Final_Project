using System;
using Fusion;
using LMCore;
using UnityEngine;

public class ChatManager : NetworkBehaviour, IAfterSpawned
{
    public static ChatManager Inst => BaseManager<ChatManager>.Inst;
    public static bool HasInstance => BaseManager<ChatManager>.HasInstance;

    [Networked, OnChangedRender(nameof(OnChangedChatHistories)), UnitySerializeField]
    public ref ChatHistoryList ChatHistories => ref MakeRef<ChatHistoryList>();
    
    public void AfterSpawned()
    {
        NetworkEventSystem.Inst.RegisterManager(this);
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        ChatHistories.Clear();
        OnChatAction = null;
    }

    // --- 채팅 데이터 관련
    public event Action<ChatHistoryList> OnChatAction;
    public void AddChatAction(Action<ChatHistoryList> action)
    {
        OnChatAction += action;
    }
    public void RemoveChatAction(Action<ChatHistoryList> action)
    {
        OnChatAction -= action;
    }
    private void OnChangedChatHistories()
    {
        OnChatAction?.Invoke(ChatHistories);
    }




    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public static void RPC_SendChatMessage(NetworkRunner runner, PlayerRef sender, string message, ChatChannel channel = ChatChannel.None, RpcInfo rpcInfo = default)
    {
        if (runner.IsServer == false)
            return;

        Debug.Log($"RPC_SendChatMessage: {sender} {message} {channel}");
        var chat = new ChatHistory(channel, sender, default, message);
        chat.TickTime = runner.Tick;
        Inst.ChatHistories.Add(chat);
    }

}