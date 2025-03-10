namespace ClientMessenger.Chat
{
    public record struct Message(MessageSender Sender, DateTime DateTime, string Content)
    {

    }
}
