using System.Security.Cryptography;
using System.Text.Json;
using System.Windows;

namespace ClientMessenger
{
    public static class HandleServerResponses
    {
        #region Pre logged in
        public static async Task ReceiveRSA(JsonElement message)
        {
            Logger.LogInformation("Received RSA. Sending Aes now!");

            RSAParameters publicKey = message.GetPublicKey();
            var payload = new
            {
                code = OpCode.SendAes,
                key = Convert.ToBase64String(Security.Aes.Key),
                iv = Convert.ToBase64String(Security.Aes.IV),
            };
            await Client.SendPayloadAsync(payload, publicKey);
        }

        public static void ServerReadyToReceive()
        {
            Logger.LogInformation("Server is ready to receive data");
            ClientUI.SwitchWindows<MainWindow, Login>();
        }

        public static async Task AnswerCreateAccount(JsonElement message)
        {
            Logger.LogInformation("Received answer to create acc from server");

            NpgsqlExceptionInfos error = message.GetNpgsqlExceptionInfos();
            if (error.Exception == NpgsqlExceptions.None)
            {
                Client.User = JsonSerializer.Deserialize<User>(message, Client.JsonSerializerOptions)!;
                ClientUI.SwitchWindows<CreateAcc, Verification>();
                return;
            }

            await HandleNpgsqlError(error);
        }

        public static async Task AnswerToLogin(JsonElement message)
        {
            Logger.LogInformation("Received answer to login from server");

            NpgsqlExceptionInfos error = message.GetNpgsqlExceptionInfos();
            if (error.Exception != NpgsqlExceptions.None)
            {
                await HandleNpgsqlError(error);
                return;
            }

            Client.User = JsonSerializer.Deserialize<User>(message, Client.JsonSerializerOptions)!;

            if (Client.User.FaEnabled)
            {
                ClientUI.SwitchWindows<Login, Verification>();
                return;
            }
            
            ClientUI.SwitchWindows<Login, Home>();
        }

        public static async Task AnswerToVerificationRequest(JsonElement message)
        {
            Logger.LogInformation("Received answer to verification request");
            bool success = message.GetProperty("success").GetBoolean();
            await ClientUI.GetWindow<Verification>().AnswerToVerificationRequest(success);
        }

        #endregion

        private static async Task HandleNpgsqlError(NpgsqlExceptionInfos errorInfos)
        {
            (NpgsqlExceptions error, string column) = errorInfos;

            switch (error)
            {
                case NpgsqlExceptions.UnknownError:
                    Logger.LogError("Unknown error! Server is closing the connection");
                    Application.Current.Dispatcher.Invoke(() => Application.Current.Shutdown());
                    break;
                case NpgsqlExceptions.ConnectionError:
                    Logger.LogError("Connection lost! Server is closing the connection");
                    Application.Current.Shutdown();
                    break;
                case NpgsqlExceptions.AccCreationError:
                    Logger.LogError("Account creation error");
                    await Application.Current.Dispatcher.InvokeAsync(async() =>
                    {
                        CreateAcc createAcc = ClientUI.GetWindow<CreateAcc>();
                        await createAcc.AccCreationWentWrong(column);
                    }); 
                    break;
                case NpgsqlExceptions.WrongLoginData:
                    Logger.LogError("Wrong login data");
                    await Application.Current.Dispatcher.InvokeAsync(async () =>
                    {
                        Login login = ClientUI.GetWindow<Login>();
                        await login.LoginWentWrong();
                    });
                    break;
                default:
                    Logger.LogError("The received database error has no case.");
                    break;
            }
        }
    }
}
