namespace ClientMessenger
{
    public readonly record struct Message(long SenderId, DateTime DateTime, string Content) { }
}
