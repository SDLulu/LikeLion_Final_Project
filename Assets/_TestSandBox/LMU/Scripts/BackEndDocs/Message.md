Backend.Message.GetSentMessageList((callback) => {
  // 이후 처리
});public class MessageItem
{
    public string receiver;
    public string sender;
    public string content;
    public string inDate;
    public string senderNickname;
    public bool isRead;
    public string receiverNickname;
    public bool isReceiverDelete;
    public bool isSenderDelete;

    public override string ToString()
    {
        return $"receiver : {receiver}\n" +
        $"sender : {sender}\n" +
        $"content : {content}\n" +
        $"inDate : {inDate}\n" +
        $"senderNickname : {senderNickname}\n" +
        $"isRead : {isRead}\n" +
        $"receiverNickname : {receiverNickname}\n" +
        $"isReceiverDelete : {isReceiverDelete}\n" +
        $"isSenderDelete : {isSenderDelete}\n";
    }
};public void GetSentMessageListTest()
{
    var bro = Backend.Message.GetSentMessageList();

    if(!bro.IsSuccess())
        return;

    LitJson.JsonData json = bro.FlattenRows();

    List<MessageItem> messageList = new List<MessageItem>();

    for(int i = 0; i < json.Count; i++)
    {
        MessageItem messageItem = new MessageItem();

        messageItem.receiver = json[i]["receiver"].ToString();
        messageItem.sender = json[i]["sender"].ToString();
        messageItem.content = json[i]["content"].ToString();
        messageItem.inDate = json[i]["inDate"].ToString();
        messageItem.senderNickname = json[i]["senderNickname"].ToString();
        messageItem.isRead = json[i]["isRead"].ToString() == "true" ? true : false;
        messageItem.receiverNickname = json[i]["receiverNickname"].ToString();
        messageItem.isReceiverDelete = json[i]["isReceiverDelete"].ToString() == "true" ? true : false;
        messageItem.isSenderDelete = json[i]["isSenderDelete"].ToString() == "true" ? true : false;

        messageList.Add(messageItem);
        Debug.Log(messageItem.ToString());
    }
}// 읽고자 하는 쪽지의 inDate를 얻습니다.  
Backend.Message.GetSentMessageList(callback =>
{
      string messageIndate = callback.Rows()[0]["inDate"]["S"].ToString();

      //읽고자 하는 쪽지의 내용을 읽어옵니다.  
      Backend.Message.GetSentMessage(messageIndate, callback2 =>
      {
        string content = callback2.GetReturnValuetoJSON()["row"]["content"]["S"].ToString();
      });

});public class MessageItem
{
    public string receiver;
    public string sender;
    public string content;
    public string inDate;
    public string senderNickname;
    public bool isRead;
    public string receiverNickname;
    public bool isReceiverDelete;
    public bool isSenderDelete;

    public override string ToString()
    {
        return $"receiver : {receiver}\n" +
        $"sender : {sender}\n" +
        $"content : {content}\n" +
        $"inDate : {inDate}\n" +
        $"senderNickname : {senderNickname}\n" +
        $"isRead : {isRead}\n" +
        $"receiverNickname : {receiverNickname}\n" +
        $"isReceiverDelete : {isReceiverDelete}\n" +
        $"isSenderDelete : {isSenderDelete}\n";
    }
};public void GetSentMessageTest()
{
    var bro = Backend.Message.GetSentMessage("2022-03-14T07:21:01.953Z");

    if(!bro.IsSuccess())
        return;

    LitJson.JsonData json = bro.GetFlattenJSON();

    MessageItem messageItem = new MessageItem();

    messageItem.receiver = json["row"]["receiver"].ToString();
    messageItem.sender = json["row"]["sender"].ToString();
    messageItem.content = json["row"]["content"].ToString();
    messageItem.inDate = json["row"]["inDate"].ToString();
    messageItem.senderNickname = json["row"]["senderNickname"].ToString();
    messageItem.isRead = json["row"]["isRead"].ToString() == "true" ? true : false;
    messageItem.receiverNickname = json["row"]["receiverNickname"].ToString();
    messageItem.isReceiverDelete = json["row"]["isReceiverDelete"].ToString() == "true" ? true : false;
    messageItem.isSenderDelete = json["row"]["isSenderDelete"].ToString() == "true" ? true : false;

    Debug.Log(messageItem.ToString());
}