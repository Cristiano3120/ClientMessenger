namespace ClientMessenger
{
    public readonly record struct LoginRequest
    {
        public string Email { get; init; }

        public string Password { get; init; }

        public string Token { get; init; }

        public bool StayLoggedIn { get; init; }

        public LoginRequest(string token)
        {
            Token = token;
            Email = string.Empty;
            Password = string.Empty;
            StayLoggedIn = false;
        }

        public LoginRequest(string email, string password, bool stayLoggedIn)
        {
            Email = email;
            Password = password;
            StayLoggedIn = stayLoggedIn;
            Token = string.Empty;
        }
    }
}
