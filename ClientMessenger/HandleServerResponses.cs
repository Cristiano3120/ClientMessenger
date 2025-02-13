using System.Net.WebSockets;
using System.Security.Cryptography;
using System.Text.Json;
using System.Windows;
using System.Windows.Media;
using Microsoft.VisualBasic;

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

        public static async Task AnswerCreateAccountAsync(JsonElement message)
        {
            Logger.LogInformation("Received answer to create acc from server");

            NpgsqlExceptionInfos error = message.GetNpgsqlExceptionInfos();
            if (error.Exception == NpgsqlExceptions.None)
            {
                Client.User = JsonSerializer.Deserialize<User>(message, Client.JsonSerializerOptions)!;
                AutoLogin.UpsertData(Client.User.Token);
                ClientUI.SwitchWindows<CreateAcc, Verification>();
                return;
            }

            await HandleNpgsqlErrorAsync(error);
        }

        public static async Task AnswerToLoginAsync(JsonElement message)
        {
            Logger.LogInformation("Received answer to login from server");

            NpgsqlExceptionInfos error = message.GetNpgsqlExceptionInfos();
            if (error.Exception != NpgsqlExceptions.None)
            {
                await HandleNpgsqlErrorAsync(error);
                return;
            }

            Client.User = JsonSerializer.Deserialize<User>(message, Client.JsonSerializerOptions)!;
            AutoLogin.UpsertData(Client.User.Token);

            if (Client.User.FaEnabled)
            {
                ClientUI.SwitchWindows<Login, Verification>();
                return;
            }
            
            ClientUI.SwitchWindows<Login, Home>();
        }

        public static async Task AnswerToVerificationRequestAsync(JsonElement message)
        {
            Logger.LogInformation("Received answer to verification request");
            bool success = message.GetProperty("success").GetBoolean();
            await ClientUI.GetWindow<Verification>().AnswerToVerificationRequest(success);
        }

        public static async Task VerificationWentWrongAsync()
        {
            await ClientUI.GetWindow<Verification>().AnswerToVerificationRequest(null);
            await Client.CloseConnectionAsync(WebSocketCloseStatus.PolicyViolation, "");
            AutoLogin.UpsertData("");
            Application.Current.Dispatcher.Invoke(() => Application.Current.Shutdown());
        }

        public static async Task AnswerToAutoLoginRequestAsync(JsonElement message)
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

        #region Past Log in

        public static async Task AnswerToRelationshipUpdateRequest(JsonElement message)
        { 
            NpgsqlExceptionInfos exceptionInfos = message.GetNpgsqlExceptionInfos()!;
            if (exceptionInfos.Exception == NpgsqlExceptions.None)
            {
                await Application.Current.Dispatcher.InvokeAsync(async() =>
                {
                    Home home = ClientUI.GetWindow<Home>();
                    await home.DisplayInfosAddFriendPanelAsync(Brushes.Green, "Succesfully added");
                    Logger.LogError("AnswerToRelationshipUpdateRequest()[MUSS ZU PENDING GEADDET WERDEN]");
                });
                return;
            }
            await HandleNpgsqlErrorAsync(exceptionInfos);
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
                case NpgsqlExceptions.UserNotFound:
                    await Application.Current.Dispatcher.InvokeAsync(async() =>
                    {
                        Home home = ClientUI.GetWindow<Home>();
                        await home.DisplayInfosAddFriendPanelAsync(Brushes.Red, "Username or hashtag is wrong");
                    });
                    break;
                case NpgsqlExceptions.TokenInvalid:
                    AutoLogin.DeleteData();
                    Application.Current.Dispatcher.Invoke(() => ClientUI.SwitchWindows<MainWindow, Login>());
                    break;
                default:
                    Logger.LogError($"The received database error has no case(Error: {error}).");
                    break;
            }
        }
    }
}
