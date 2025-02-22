using System.Text.Json.Serialization;

namespace ClientMessenger
{
    public record LoginRequest
    {
        [JsonPropertyName("email")]
        public string Email { get; init; } = "";

        [JsonPropertyName("password")]
        public string Password { get; init; } = "";

        [JsonPropertyName("token")]
        public string Token { get; init; } = "";

        [JsonPropertyName("stayLoggedIn")]
        public bool StayLoggedIn { get; init; }

        public LoginRequest(string token)
        {
            Token = token;
        }

        public LoginRequest(string email, string password, bool stayLoggedIn)
        {
            Email = email;
            Password = password;
            StayLoggedIn = stayLoggedIn;
        }
    }
}
