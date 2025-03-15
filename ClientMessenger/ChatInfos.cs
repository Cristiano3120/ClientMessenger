namespace ClientMessenger
{
    public record struct ChatInfos(List<long> Members, List<Message> Messages) { }
}
