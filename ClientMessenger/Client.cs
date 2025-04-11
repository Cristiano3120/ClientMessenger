using System.IO;
using System.Net.WebSockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Windows;
using ClientMessenger.LocalChatDatabase;

namespace ClientMessenger
{
    public static class Client
    {
        
        public static JsonElement Config { get; set; } = JsonExtensions.ReadJsonFile(JsonFile.Config);
        private static ClientWebSocket _server = new();
        public static User User { get; set; } = new();

        #region Start

        public static async Task StartAsync()
        {
            Logger.LogWarning("Connecting to Server...");

            TaskScheduler.UnobservedTaskException += (_, args) =>
            {
                Logger.LogWarning("WARNING: CATCHED AN UNHANDELD EXCEPTION!");
                Logger.LogError(args);
                args.SetObserved();
            };

            await ConnectToServerAsync();

            ChatDatabase chatDatabase = new();
            chatDatabase.DeleteChats();
        }

        private static async Task ConnectToServerAsync()
        {
            while (_server.State != WebSocketState.Open)
            {
                try
                {
                    await _server.ConnectAsync(GetUri(true), CancellationToken.None);
                }
                catch (Exception ex)
                {
                    await Task.Delay(1000);
                    Logger.LogError(ex);
                    _server = new();
                }
            }

            Logger.LogWarning("Connected!");
            _ = Task.Run(ReceiveMessagesAsync);
        }

        #endregion

        #region Receive and handle message

        private static async Task ReceiveMessagesAsync()
        {
            Logger.LogInformation("Listening for messages!");
            byte[] buffer = new byte[65536];
            MemoryStream ms = new();

            while (_server.State == WebSocketState.Open)
            {
                try
                {
                    WebSocketReceiveResult receivedDataInfo = await _server.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                    Logger.LogInformation(ConsoleColor.Cyan, $"[RECEIVED]: {receivedDataInfo.Count} bytes", false);

                    if (receivedDataInfo.MessageType == WebSocketMessageType.Close)
                    {
                        Logger.LogWarning("Server requested to close the connection.");
                        await _server.CloseAsync(WebSocketCloseStatus.NormalClosure, "Client closing after server request", CancellationToken.None);
                        break;
                    }

                    await ms.WriteAsync(buffer.AsMemory(0, receivedDataInfo.Count));
                    if (!receivedDataInfo.EndOfMessage)
                    {
                        continue;
                    }

                    byte[] completeBytes = ms.ToArray();
                    byte[] decryptedBytes = await Security.DecryptMessageAsync(completeBytes);
                    byte[] decompressedBytes = Security.DecompressData(decryptedBytes);
                    string completeMessage = Encoding.UTF8.GetString(decompressedBytes);

                    Logger.LogPayload(ConsoleColor.Green, completeMessage, "[RECEIVED]:");
                    ClearMs(ms);

                    await HandleReceivedMessageAsync(JsonDocument.Parse(completeMessage));
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex);
                    break;
                }
            }

            await RestartAsync();
        }

        private static async Task HandleReceivedMessageAsync(JsonDocument jsonDocument)
        {
            OpCode opCode = jsonDocument.RootElement.GetOpCode();
            switch (opCode)
            {
                case OpCode.ReceiveRSA:
                    await HandleServerResponses.ReceiveRSAAsync(jsonDocument);
                    break;
                case OpCode.ServerReadyToReceive:
                    await HandleServerResponses.ServerReadyToReceiveAsync();
                    break;
                case OpCode.AnswerToCreateAccount:
                    await HandleServerResponses.AnswerCreateAccountAsync(jsonDocument);
                    break;
                case OpCode.AnswerToLogin:
                    await HandleServerResponses.AnswerToLoginAsync(jsonDocument);
                    break;
                case OpCode.VerificationProcess:
                    await HandleServerResponses.AnswerToVerificationRequestAsync(jsonDocument);
                    break;
                case OpCode.VerificationWentWrong:
                    await HandleServerResponses.VerificationWentWrongAsync();
                    break;
                case OpCode.AnswerToAutoLogin:
                    await HandleServerResponses.AnswerToAutoLoginRequestAsync(jsonDocument);
                    break;
                case OpCode.AnswerToRequestedRelationshipUpdate:
                    await HandleServerResponses.AnswerToRelationshipUpdateRequestAsync(jsonDocument);
                    break;
                case OpCode.ReceiveRelationships:
                    await HandleServerResponses.ReceiveRelationshipsAsync(jsonDocument);
                    break;
                case OpCode.ARelationshipWasUpdated:
                    await HandleServerResponses.ARelationshipWasUpdatedAsync(jsonDocument);
                    break;
                case OpCode.ReceiveChatMessage:
                    HandleServerResponses.ReceiveChatMessage(jsonDocument);
                    break;
                case OpCode.ReceiveChats:
                    HandleServerResponses.ReceiveChats(jsonDocument);
                    break;
                case OpCode.SettingsUpdate:
                    await HandleSettingsUpdate.HandleReceivedMessageAsync(jsonDocument);
                    break;
            }
        }
        
        #endregion

        #region Send data

        internal static async Task SendPayloadAsync(object payload, RSAParameters publicKey)
        {
            ArgumentNullException.ThrowIfNull(payload);

            if (_server.State != WebSocketState.Open)
            {
                return;
            }

            string jsonPayload = JsonSerializer.Serialize(payload);
            byte[] buffer = Encoding.UTF8.GetBytes(jsonPayload);
            byte[] compressedData = Security.CompressData(buffer);
            byte[] encryptedData = Security.EncryptRSA(publicKey, compressedData);

            await _server.SendAsync(encryptedData, WebSocketMessageType.Binary, true, CancellationToken.None);

            Logger.LogInformation(ConsoleColor.Cyan, $"[SENDING(RSA)]: {encryptedData.Length} bytes", false);
            Logger.LogPayload(ConsoleColor.Blue, jsonPayload, "[SENDING(RSA)]:");
        }

        internal static async Task SendPayloadAsync(object payload)
        {
            ArgumentNullException.ThrowIfNull(payload);

            if (_server.State != WebSocketState.Open)
            {
                return;
            }

            string? jsonPayload = JsonSerializer.Serialize(payload);
            byte[] buffer = Encoding.UTF8.GetBytes(jsonPayload);
            byte[] compressedData = Security.CompressData(buffer);
            byte[] encryptedData = await Security.EncryptAesAsync(compressedData);

            await _server.SendAsync(encryptedData, WebSocketMessageType.Binary, true, CancellationToken.None);

            Logger.LogInformation(ConsoleColor.Cyan, $"[SENDING(Aes)]: {encryptedData.Length} bytes", false);
            Logger.LogPayload(ConsoleColor.Blue, jsonPayload, "[SENDING(Aes)]:");
        }

        public static async Task CloseConnectionAsync(WebSocketCloseStatus closeStatus, string reason)
        {
            if (_server.State is not WebSocketState.Aborted and WebSocketState.Closed)
                await _server.CloseAsync(closeStatus, reason, CancellationToken.None);
        }

        private static async Task RestartAsync()
        {
            Logger.LogError("Server closed the connection. Restarting the application...");
            await CloseConnectionAsync(WebSocketCloseStatus.NormalClosure, "");

            //string appPath = Environment.ProcessPath!;
            //Process.Start(appPath);
            //Application.Current.Dispatcher.Invoke(Application.Current.Shutdown);
        }

        #endregion

        #region Helper methods

        private static void ClearMs(MemoryStream ms)
        {
            byte[] buffer = ms.GetBuffer();
            Array.Clear(buffer, 0, buffer.Length);
            ms.Position = 0;
            ms.SetLength(0);
        }

        private static Uri GetUri(bool testing)
        {
            if (testing)
                return new Uri("ws://127.0.0.1:5000/");

            string? serverUri = Config.GetProperty("ServerUri").GetString();

            if (serverUri is null)
            {
                Application.Current.Shutdown();
                throw new NullReferenceException($"{nameof(serverUri)} is null");
            }

            return new Uri(serverUri);
        }

        #endregion

        /// <summary>
        /// Resolves a relative path by dynamically adjusting the base directory of the project.
        /// It removes the portion of the base directory up to and including the specified segment 
        /// ("ClientMessenger/ClientMessenger/") and combines the remaining path with the given relative path.
        /// </summary>
        /// <param name="relativePath"> 
        /// Example: If you want to get the file "appsettings.json" that is in directory "Settings" you would give this as an param:
        /// the relativePath = "Settings/appsettings.json" </param>
        /// <returns>A fully resolved path based on the project's base directory and the given relative path.</returns>
        public static string GetDynamicPath(string relativePath)
        {
            string projectBasePath = AppDomain.CurrentDomain.BaseDirectory;

            int binIndex = projectBasePath.IndexOf(Path.DirectorySeparatorChar + "bin" + Path.DirectorySeparatorChar, StringComparison.Ordinal);

            if (binIndex != -1)
            {
                projectBasePath = projectBasePath[..binIndex];
            }

            return Path.Combine(projectBasePath, relativePath);
        }
    }
}
