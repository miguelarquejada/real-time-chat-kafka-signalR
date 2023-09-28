namespace backend;

public class MessageModel
{
    public string ConnectionId { get; set; }
    public string Message { get; set; }

    public MessageModel(string connectionId, string message)
    {
        ConnectionId = connectionId;
        Message = message;
    }
}