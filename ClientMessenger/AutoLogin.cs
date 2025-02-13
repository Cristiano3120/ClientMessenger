namespace ClientMessenger
{
    public static class AutoLogin
    {
        /// <summary>
        /// Saves or updates the auto login data
        /// </summary>
        public static void UpsertData(string token)
           => Client.Config = Client.Config.SetString(JsonFile.Config, "Token", token);

        private static string GetData() 
            => Client.Config.GetProperty("Token").GetString()!;

        public static async Task<bool> TryToLoginAsync()
        {
            if (!Client.Config.GetProperty("AutoLogin").GetBoolean())
            {
                return false;
            }

            string? token = GetData();
            if (!string.IsNullOrEmpty(token))
            {
                var payload = new
                {
                    opCode = OpCode.AutoLoginRequest,
                    token
                };
                await Client.SendPayloadAsync(payload);
                return true;
            }
            return false;
        }

        public static void DeleteData()
        => Client.Config = Client.Config.SetString(JsonFile.Config, "Token", "");
    }
}
