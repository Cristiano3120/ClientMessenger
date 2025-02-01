using System.Net.WebSockets;
using System.Security.Cryptography;
using System.Text.Json;
using System.Windows;

namespace ClientMessenger
{
    public static class HandleServerResponses
    {
        #region Pre logged in
        public static async Task ReceiveRSAAsync(JsonElement message)
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

        public static async Task ServerReadyToReceiveAsync()
        {
            Logger.LogInformation("Server is ready to receive data");
            
            if (!await AutoLogin.TryToLoginAsync())
            {
                ClientUI.SwitchWindows<MainWindow, Login>();
            }
        }

        public static async Task AnswerCreateAccountAsnyc(JsonElement message)
        {
            Logger.LogInformation("Received answer to create acc from server");

            NpgsqlExceptionInfos error = message.GetNpgsqlExceptionInfos();
            if (error.Exception == NpgsqlExceptions.None)
            {
                Client.User = JsonSerializer.Deserialize<User>(message, Client.JsonSerializerOptions)!;
                AutoLogin.UpsertData(message.GetProperty("token").GetString()!);
                ClientUI.SwitchWindows<CreateAcc, Verification>();
                return;
            }

            await HandleNpgsqlErrorAsync(error);
        }

        public static async Task AnswerToLoginAsnyc(JsonElement message)
        {
            Logger.LogInformation("Received answer to login from server");

            NpgsqlExceptionInfos error = message.GetNpgsqlExceptionInfos();
            if (error.Exception != NpgsqlExceptions.None)
            {
                await HandleNpgsqlErrorAsync(error);
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

        public static async Task AnswerToVerificationRequestAsnyc(JsonElement message)
        {
            Logger.LogInformation("Received answer to verification request");
            bool success = message.GetProperty("success").GetBoolean();
            await ClientUI.GetWindow<Verification>().AnswerToVerificationRequest(success);
        }

        public static async Task VerificationWentWrongAsnyc()
        {
            await ClientUI.GetWindow<Verification>().AnswerToVerificationRequest(null);
            await Client.CloseConnectionAsync(WebSocketCloseStatus.PolicyViolation, "");
            AutoLogin.UpsertData("");
            Application.Current.Dispatcher.Invoke(() => Application.Current.Shutdown());
        }

        public static async Task AnswerToAutoLoginRequestAsnyc(JsonElement message)
        {
            Logger.LogInformation("Received answer to login from server");

            NpgsqlExceptionInfos exceptionInfos = message.GetNpgsqlExceptionInfos();
            NpgsqlExceptions exception = exceptionInfos.Exception;

            if (exception is NpgsqlExceptions.None)
            {
                Client.User = JsonSerializer.Deserialize<User>(message, Client.JsonSerializerOptions)!;
                ClientUI.SwitchWindows<MainWindow, Home>();
                return;
            }

            if (exception is not NpgsqlExceptions.WrongLoginData)
            {
                await HandleNpgsqlErrorAsync(exceptionInfos);
                return;
            }

            ClientUI.SwitchWindows<MainWindow, Login>();
        }

        #endregion

        private static async Task HandleNpgsqlErrorAsync(NpgsqlExceptionInfos errorInfos)
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
