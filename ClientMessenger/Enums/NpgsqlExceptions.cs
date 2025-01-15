namespace ClientMessenger.Enums
{
    internal enum NpgsqlExceptions : byte
    {
        None = 0,
        UnknownError = 1,
        ConnectionError = 2,
        AccCreationError = 3,
        WrongLoginData = 4,
    }
}
