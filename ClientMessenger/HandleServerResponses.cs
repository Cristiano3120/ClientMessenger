using System.Net.WebSockets;
using System.Security.Cryptography;
using System.Text.Json;
using System.Windows;
using System.Windows.Threading;
using System.Windows.Media;
using ClientMessenger.LocalChatDatabase;

namespace ClientMessenger
{
    public static class HandleServerResponses
    {
        #region Pre logged in
        public static async Task ReceiveRSAAsync(JsonDocument jsonDocument)
        {
            JsonElement message = jsonDocument.RootElement;

            RSAParameters publicKey = message.GetPublicKey();
            AesKeyData aesKeyData = new()
            {
                Key = Convert.ToBase64String(Security.Aes.Key),
                Iv =  Convert.ToBase64String(Security.Aes.IV)
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

        public static async Task AnswerCreateAccountAsync(JsonDocument jsonDocument)
        {
            JsonElement message = jsonDocument.RootElement;

            NpgsqlExceptionInfos error = message.GetNpgsqlExceptionInfos();
            if (error.Exception == NpgsqlExceptions.None)
            {
                Client.User = JsonSerializer.Deserialize<User>(message)!;

                AutoLogin.UpsertData(Client.User.Token);
                ClientUI.SwitchWindows<CreateAcc, Verification>();

                return;
            }

            await HandleNpgsqlErrorAsync(error);
        }

        public static async Task AnswerToLoginAsync(JsonDocument jsonDocument)
        {
            JsonElement message = jsonDocument.RootElement;

            NpgsqlExceptionInfos error = message.GetNpgsqlExceptionInfos();
            if (error.Exception != NpgsqlExceptions.None)
            {
                await HandleNpgsqlErrorAsync(error);
                return;
            }

            Client.User = JsonSerializer.Deserialize<User>(message)!;
            AutoLogin.UpsertData(Client.User.Token);

            if (Client.User.FaEnabled)
            {
                ClientUI.SwitchWindows<Login, Verification>();
                return;
            }
            
            ClientUI.SwitchWindows<Login, Home>();
        }

        public static async Task AnswerToVerificationRequestAsync(JsonDocument jsonDocument)
        {
            JsonElement message = jsonDocument.RootElement;

            bool success = message.GetProperty("success").GetBoolean();
            await ClientUI.GetWindow<Verification>().AnswerToVerificationRequestAsync(success);
        }

        public static async Task VerificationWentWrongAsync()
        {
            await ClientUI.GetWindow<Verification>().AnswerToVerificationRequestAsync(null);
            await Client.CloseConnectionAsync(WebSocketCloseStatus.PolicyViolation, "");

            AutoLogin.DeleteToken();
            Application.Current.Dispatcher.Invoke(() => Application.Current.Shutdown());
        }

        public static async Task AnswerToAutoLoginRequestAsync(JsonDocument jsonDocument)
        {
            JsonElement message = jsonDocument.RootElement;

            NpgsqlExceptionInfos exceptionInfos = message.GetNpgsqlExceptionInfos();
            NpgsqlExceptions exception = exceptionInfos.Exception;

            if (exception is NpgsqlExceptions.None)
            {
                Client.User = JsonSerializer.Deserialize<User>(message)!;
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

        public static async Task AnswerToRelationshipUpdateRequestAsync(JsonDocument jsonDocument)
        { 
            JsonElement message = jsonDocument.RootElement;

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

        public static async Task ARelationshipWasUpdatedAsync(JsonDocument jsonDocument)
        {
            JsonElement message = jsonDocument.RootElement;

            RelationshipUpdate relationshipUpdate = JsonSerializer.Deserialize<RelationshipUpdate>(message);
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

        public static async Task ReceiveRelationshipsAsync(JsonDocument jsonDocument)
        {
            JsonElement message = jsonDocument.RootElement;

            HashSet<Relationship>? relationships = JsonSerializer.Deserialize<HashSet<Relationship>>(message.GetProperty("relationships"));
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
            }, DispatcherPriority.Render);
        }

        public static void ReceiveChatMessage(JsonDocument jsonDocument)
        {
            JsonElement message = jsonDocument.RootElement;

            Message chatMessage = JsonSerializer.Deserialize<Message>(message.GetProperty("chatMessage"));
            Application.Current.Dispatcher.Invoke(() =>
            {
                Home home = ClientUI.GetWindow<Home>();
                home.AddMessage(chatMessage);
            }, DispatcherPriority.Render);
        }

        public static void ReceiveChats(JsonDocument jsonDocument)
        {
            JsonElement message = jsonDocument.RootElement;

            List<ChatInfos>? chatInfos = JsonSerializer.Deserialize<List<ChatInfos>>(message.GetProperty("chats"));
            ChatDatabase chatDatabase = new();
            chatDatabase.AddChats(chatInfos!);
        }

        #endregion

        private static async Task HandleNpgsqlErrorAsync(NpgsqlExceptionInfos errorInfos)
        {
            (NpgsqlExceptions error, string column) = errorInfos;

            switch (error)
            {
                case NpgsqlExceptions.UnknownError:
                    Logger.LogError("Unknown error! Server is closing the connection");
                    break;
                case NpgsqlExceptions.ConnectionError:
                    Logger.LogError("Connection lost! Server is closing the connection");
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
                        await login.LoginWentWrongAsync();
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
                    AutoLogin.DeleteToken();
                    Application.Current.Dispatcher.Invoke(() => ClientUI.SwitchWindows<MainWindow, Login>());
                    break;
                case NpgsqlExceptions.NoDataEntrys:
                    Logger.LogError("The sent data was invalid! Restarting the application");
                    await Client.CloseConnectionAsync(WebSocketCloseStatus.InvalidPayloadData, "");
                    break;
                case NpgsqlExceptions.UnexpectedEx:
                    Logger.LogError("Unexpected exception");
                    break;
                case NpgsqlExceptions.RequestedUserIsBlocked:
                    await Application.Current.Dispatcher.InvokeAsync(async () =>
                    {
                        Home home = ClientUI.GetWindow<Home>();
                        await home.DisplayInfosAddFriendPanelAsync(Brushes.Red, "You are blocked by that user :(");
                    });
                    break;
                case NpgsqlExceptions.PayloadDataMissing:
                    Logger.LogError("Payload data missing");
                    break;
                default:
                    Logger.LogError($"The received database error has no case(Error: {error}).");
                    break;
            }
        }
    }
}
