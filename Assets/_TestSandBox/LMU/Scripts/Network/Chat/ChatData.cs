using Fusion;

public enum ChatChannel
{
    None,
    System,     // 입장알림 등...
    All,
    Whisper,    // 귓속말
    Mine,       // 나에게만 보이는 채팅
}

public struct ChatHistoryList : INetworkStruct
{
    [Networked, UnitySerializeField, Capacity(40)]
    public NetworkLinkedList<ChatHistory> ChatHistories => default;

    public void Add(ChatHistory chatHistory)
    {
        ChatHistories.Add(chatHistory);
    }

    public ChatHistory Get(Tick tick)
    {
        foreach (var chat in ChatHistories)
        {
            // if (chat.Tick == tick)
            //     return chat;
        }
        return default;
    }

    public void Clear()
    {
        ChatHistories.Clear();
    }

    public void Remove(ChatHistory chatHistory)
    {
        ChatHistories.Remove(chatHistory);
    }
}

public struct ChatHistory : INetworkStruct
{
    public ChatChannel Channel { get; set; }
    public PlayerRef Sender { get; set; }
    public PlayerRef Receiver { get; set; }
    public NetworkString<_32> Message { get; set; }
    public int TickTime { get; set; }

    public ChatHistory(ChatChannel channel, PlayerRef sender, PlayerRef receiver, string message)
    {
        Channel = channel;
        Sender = sender;
        Receiver = receiver;
        Message = message;
        TickTime = default;
    }
}