namespace ClientMessenger
{
    public readonly struct Message
    {
        public Guid Guid { get; init; } = Guid.NewGuid();
        public long SenderId { get; init; }
        public DateTime DateTime { get; init; }
        public string Content { get; init; }

        public Message(long senderId, DateTime dateTime, string content)
        {
            SenderId = senderId;
            DateTime = dateTime;
            Content = content;
        }

        #region Operator override

        public override bool Equals(object? obj)
            => obj is Message other && Guid == other.Guid;

        public override int GetHashCode()
            => HashCode.Combine(SenderId, DateTime, Content);

        public static bool operator ==(Message a, Message b) => a.Equals(b);
        public static bool operator !=(Message a, Message b) => !a.Equals(b);

        #endregion
    }
}
