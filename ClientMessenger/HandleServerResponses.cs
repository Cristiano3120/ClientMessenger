using Server_Messenger;
using System.Security.Cryptography;
using System.Text.Json;
using System.Windows;

namespace ClientMessenger
{
    public static class HandleServerResponses
    {
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
            Application.Current.Dispatcher.Invoke(() =>
            {
                Login login = new();
                login.Show();
                ClientUI.CloseAllWindowsExceptOne<Login>();
            });
        }

        public static async Task AnswerCreateAccount(JsonElement message)
        {
            Logger.LogInformation("Received answer to create acc from server");

            NpgsqlExceptionInfos error = message.GetProperty("error").GetNpgsqlExceptionInfos();
            await Application.Current.Dispatcher.InvokeAsync(async () =>
            {
                if (error.Exception == NpgsqlExceptions.None)
                {
                    Client.User = JsonSerializer.Deserialize<User>(message, Client.JsonSerializerOptions)!;
                    DatabaseInfos dbInfos = message.GetProperty("dbInfos").GetDatabaseInfos();
                    var home = new Home(dbInfos);
                    home.Show();
                    var createAcc = ClientUI.GetWindow<CreateAcc>()!;
                    createAcc.Close();
                    return;
                }

                await HandleNpgsqlError(error);
            });
        }

        public static async Task AnswerToLogin(JsonElement message)
        {
            Logger.LogInformation("Received answer to login from server");

            NpgsqlExceptionInfos error = message.GetProperty("error").GetNpgsqlExceptionInfos();
            await Application.Current.Dispatcher.InvokeAsync(async() =>
            {
                if (error.Exception == NpgsqlExceptions.None)
                {
                    Client.User = JsonSerializer.Deserialize<User>(message, Client.JsonSerializerOptions)!;
                    DatabaseInfos dbInfos =  message.GetProperty("dbPassword").GetDatabaseInfos();
                    var home = new Home(dbInfos);
                    home.Show();
                    ClientUI.CloseAllWindowsExceptOne<Home>();
                    return;
                }

                await HandleNpgsqlError(error);
            });
        }

        private static async Task HandleNpgsqlError(NpgsqlExceptionInfos errorInfos)
        {
            (NpgsqlExceptions error, string column) = errorInfos;

            switch (error)
            {
                case NpgsqlExceptions.UnknownError:
                    Logger.LogError("Unknown error");
                    break;
                case NpgsqlExceptions.ConnectionError:
                    Logger.LogError("Connection lost! Server is closing the connection");
                    break;
                case NpgsqlExceptions.AccCreationError:
                    Logger.LogError("Account creation error");
                    var createAcc = ClientUI.GetWindow<CreateAcc>() ?? new CreateAcc();
                    await createAcc.AccCreationWentWrong(column);
                    break;
                case NpgsqlExceptions.WrongLoginData:
                    Logger.LogError("Wrong login data");
                    var login = ClientUI.GetWindow<Login>() ?? new Login();
                    await login.LoginWentWrong();
                    break;
            }
        }
    }
}
