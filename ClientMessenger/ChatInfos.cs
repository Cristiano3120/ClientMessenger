namespace ClientMessenger
{
    public readonly record struct ChatInfos(List<long> Members, List<Message> Messages)
    {
        public List<Message> Messages { get; init; } = Messages ?? new List<Message>();
        public List<long> Members { get; init; } = Members ?? new List<long>();
    }
}
