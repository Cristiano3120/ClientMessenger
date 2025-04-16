namespace ClientMessenger
{
    public readonly record struct DeleteMessage(long SenderId, long ReceiverId, Guid MessageGuid) { }
}