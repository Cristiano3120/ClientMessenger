﻿using System.Net.WebSockets;
using System.Security.Cryptography;
using System.Text.Json;
using System.Windows;
using System.Windows.Media;

namespace ClientMessenger
{
    public static class HandleServerResponses
    {
        #region Pre logged in
        public static async Task ReceiveRSAAsync(JsonElement message)
        {
            RSAParameters publicKey = message.GetPublicKey();
            AesKeyData aesKeyData = new()
            {
                Key = Convert.ToBase64String(Security.Aes.Key),
                IV =  Convert.ToBase64String(Security.Aes.IV)
            };

            var payload = new
            {
                opCode = OpCode.SendAes,
                aesKeyData
            };
            await Client.SendPayloadAsync(payload, publicKey);
        }

        public static async Task ServerReadyToReceiveAsync()
        {
            if (!await AutoLogin.TryToLoginAsync())
            {
                ClientUI.SwitchWindows<MainWindow, Login>();
            }
        }

        public static async Task AnswerCreateAccountAsync(JsonElement message)
        {
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
            bool success = message.GetProperty("success").GetBoolean();
            await ClientUI.GetWindow<Verification>().AnswerToVerificationRequest(success);
        }

        public static async Task VerificationWentWrongAsync()
        {
            await ClientUI.GetWindow<Verification>().AnswerToVerificationRequest(null);
            await Client.CloseConnectionAsync(WebSocketCloseStatus.PolicyViolation, "");
            AutoLogin.DeleteData();
            Application.Current.Dispatcher.Invoke(() => Application.Current.Shutdown());
        }

        public static async Task AnswerToAutoLoginRequestAsync(JsonElement message)
        {
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
                });
                return;
            }

            await HandleNpgsqlErrorAsync(exceptionInfos);
        }

        public static async Task ReceiveRelationships(JsonElement message)
        {
            HashSet<Relationship>? relationships = JsonSerializer.Deserialize<HashSet<Relationship>>(message.GetProperty("relationships"), Client.JsonSerializerOptions);
            NpgsqlExceptionInfos npgsqlExceptionInfos = message.GetNpgsqlExceptionInfos();

            if (npgsqlExceptionInfos.Exception != NpgsqlExceptions.None)
            {
                await HandleNpgsqlErrorAsync(npgsqlExceptionInfos);
                return;
            }

            Application.Current.Dispatcher.Invoke(() =>
            {
                Home home = ClientUI.GetWindow<Home>();
                lock (home.Lock)
                {
                    home.Blocked = [.. relationships!.Where(x => x.RelationshipState == RelationshipState.Blocked)];
                    home.Pending = [.. relationships!.Where(x => x.RelationshipState == RelationshipState.Pending)];
                    home.Friends = [.. relationships!.Where(x => x.RelationshipState == RelationshipState.Friend)];
                }
            });
        }

        public static async Task ARelationshipWasUpdated(JsonElement message)
        {
            RelationshipUpdate relationshipUpdate = JsonSerializer.Deserialize<RelationshipUpdate>(message.GetProperty("relationshipUpdate"), Client.JsonSerializerOptions);
            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                Home home = ClientUI.GetWindow<Home>();
                lock (home.Lock)
                {
                    switch (relationshipUpdate.RequestedRelationshipState)
                    {
                        case RelationshipState.Pending:
                            home.Pending.Add(relationshipUpdate.Relationship!);
                            break;
                        case RelationshipState.Friend:
                            home.Friends.Add(relationshipUpdate.Relationship!);
                            break;
                        case RelationshipState.None or RelationshipState.Blocked:
                            home.Pending.Remove(relationshipUpdate.Relationship!);
                            home.Friends.Remove(relationshipUpdate.Relationship!);
                            break;
                    }
                }
                
            });
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
