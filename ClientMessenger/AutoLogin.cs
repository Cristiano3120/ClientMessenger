namespace ClientMessenger
{
    public static class AutoLogin
    {
        /// <summary>
        /// Saves or updates the auto login data
        /// </summary>
        public static void UpsertData(string token)
           => Client.Config = Client.Config.SetString(JsonFile.Config, "Token", token);

        private static string GetToken() 
            => Client.Config.GetProperty("Token").GetString()!;

        public static async Task<bool> TryToLoginAsync()
        {
            if (!Client.Config.GetProperty("AutoLogin").GetBoolean())
                return false;

            string? token = GetToken();
            if (string.IsNullOrEmpty(token))
                return false;

            LoginRequest loginRequest = new(token);
            var payload = new
            {
                opCode = OpCode.RequestToLogin,
                loginRequest
            };

            await Client.SendPayloadAsync(payload);
            return true;
        }

        public static void DeleteToken()
            => Client.Config = Client.Config.SetString(JsonFile.Config, "Token", "");
    }
}
