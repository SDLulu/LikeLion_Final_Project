using Fusion;

public struct ChatHistoryList : INetworkStruct
{
    [Networked, UnitySerializeField, Capacity(40)]
    public NetworkLinkedList<ChatHistory> ChatHistories => default;

    public void Add(ChatHistory chatHistory)
    {
        ChatHistories.Add(chatHistory);
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
    public NetworkString<_128> Message { get; set; }

    public ChatHistory(ChatChannel channel, PlayerRef sender, PlayerRef receiver, string message)
    {
        Channel = channel;
        Sender = sender;
        Receiver = receiver;
        Message = message;
    }
}