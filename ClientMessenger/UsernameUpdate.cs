namespace ClientMessenger
{
    public readonly record struct UsernameUpdate(string Username, string Hashtag, long UserId)
    { 
        public DateTime LastChanged { get; private init; } = DateTime.Now;
    }
}
