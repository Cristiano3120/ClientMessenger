namespace ClientMessenger
{
    public readonly struct Message
    {
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
            => obj is Message other
                && DateTime.ToString("yyyy-MM-dd HH:mm:ss") == other.DateTime.ToString("yyyy-MM-dd HH:mm:ss")
                && SenderId == other.SenderId
                && Content == other.Content;

        public override int GetHashCode()
            => HashCode.Combine(SenderId, DateTime, Content);

        public static bool operator ==(Message a, Message b) => a.Equals(b);
        public static bool operator !=(Message a, Message b) => !a.Equals(b);

        #endregion
    }
}
