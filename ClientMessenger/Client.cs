using System.IO;
using System.Net.WebSockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Windows;

namespace ClientMessenger
{
    internal static class Client
    {
        public static JsonSerializerOptions JsonSerializerOptions { get; private set; } = new();
        public static JsonElement Config { get; set; } = JsonExtensions.ReadJsonFile(JsonFile.Config);
        private static ClientWebSocket _server = new();
        public static User User { get; set; } = new();

        #region Start

        public static async Task Start()
        {
            Logger.LogWarning("Connecting to Server...");

            TaskScheduler.UnobservedTaskException += (sender, args) =>
            {
                Logger.LogWarning("WARNING: CATCHED AN UNHANDELD EXCEPTION!");
                Logger.LogError(args);
                args.SetObserved();
            };

            JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
            JsonSerializerOptions.Converters.Add(new JsonConverters.UserConverter());
            JsonSerializerOptions.Converters.Add(new JsonConverters.RelationshipConverter());
            JsonSerializerOptions.WriteIndented = true;

            await ConnectToServerAsync();
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
            var buffer = new byte[65536];
            var ms = new MemoryStream();

            while (_server.State == WebSocketState.Open)
            {
                try
                {
                    WebSocketReceiveResult receivedDataInfo = await _server.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                    Logger.LogInformation(ConsoleColor.Cyan, $"[RECEIVED]: {receivedDataInfo.Count} bytes", false);

                    if (receivedDataInfo.MessageType == WebSocketMessageType.Close)
                    {
                        Console.WriteLine("Server requested to close the connection.");
                        await _server.CloseAsync(WebSocketCloseStatus.NormalClosure, "Client closing after server request", CancellationToken.None);
                        break;
                    }

                    await ms.WriteAsync(buffer.AsMemory(0, receivedDataInfo.Count));
                    if (!receivedDataInfo.EndOfMessage)
                    {
                        continue;
                    }

                    byte[] completeBytes = ms.ToArray();
                    byte[] decryptedBytes = Security.DecryptMessage(completeBytes);
                    byte[] decompressedBytes = Security.DecompressData(decryptedBytes);
                    var completeMessage = Encoding.UTF8.GetString(decompressedBytes);

                    Logger.LogInformation(ConsoleColor.Green, logs: $"[RECEIVED]: {completeMessage}");
                    ClearMs(ms);

                    await HandleReceivedMessageAsync(JsonDocument.Parse(completeMessage).RootElement);
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex);
                    break;
                }
            }
            await Restart();
        }

        private static async Task HandleReceivedMessageAsync(JsonElement message)
        {
            OpCode code = message.GetOpCode();
            switch (code)
            {
                case OpCode.ReceiveRSA:
                    await HandleServerResponses.ReceiveRSAAsync(message);
                    break;
                case OpCode.ServerReadyToReceive:
                    await HandleServerResponses.ServerReadyToReceiveAsync();
                    break;
                case OpCode.AnswerCreateAccount:
                    await HandleServerResponses.AnswerCreateAccountAsync(message);
                    break;
                case OpCode.AnswerLogin:
                    await HandleServerResponses.AnswerToLoginAsync(message);
                    break;
                case OpCode.VerificationProcess:
                    await HandleServerResponses.AnswerToVerificationRequestAsync(message);
                    break;
                case OpCode.VerificationWentWrong:
                    await HandleServerResponses.VerificationWentWrongAsync();
                    break;
                case OpCode.AutoLoginResponse:
                    await HandleServerResponses.AnswerToAutoLoginRequestAsync(message);
                    break;
                case OpCode.AnswerToRequestedRelationshipUpdate:
                    await HandleServerResponses.AnswerToRelationshipUpdateRequest(message);
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

            var jsonPayload = JsonSerializer.Serialize(payload, JsonSerializerOptions);
            var buffer = Encoding.UTF8.GetBytes(jsonPayload);
            var compressedData = Security.CompressData(buffer);
            var encryptedData = Security.EncryptRSA(publicKey, compressedData);

            await _server.SendAsync(encryptedData, WebSocketMessageType.Binary, true, CancellationToken.None);

            Logger.LogInformation(ConsoleColor.Cyan, $"[SENDING(RSA)]: {encryptedData.Length} bytes", false);
            Logger.LogInformation(ConsoleColor.Blue, $"[SENDING(RSA)]: {jsonPayload}");
        }

        internal static async Task SendPayloadAsync(object payload)
        {
            ArgumentNullException.ThrowIfNull(payload);

            if (_server.State != WebSocketState.Open)
            {
                return;
            }

            var jsonPayload = JsonSerializer.Serialize(payload, JsonSerializerOptions);
            var buffer = Encoding.UTF8.GetBytes(jsonPayload);
            var compressedData = Security.CompressData(buffer);
            var encryptedData = Security.EncryptAes(compressedData);

            await _server.SendAsync(encryptedData, WebSocketMessageType.Binary, true, CancellationToken.None);

            Logger.LogInformation(ConsoleColor.Cyan, $"[SENDING(Aes)]: {encryptedData.Length} bytes", false);
            Logger.LogInformation(ConsoleColor.Blue, $"[SENDING(Aes)]: {jsonPayload}");
        }

        public static async Task CloseConnectionAsync(WebSocketCloseStatus closeStatus, string reason)
        {
            if (_server.State is not WebSocketState.Aborted and WebSocketState.Closed)
                await _server.CloseAsync(closeStatus, reason, CancellationToken.None);
        }

        private static async Task Restart()
        {
            await CloseConnectionAsync(WebSocketCloseStatus.NormalClosure, "");
            #if DEBUG == false

            string appPath = Environment.ProcessPath!;
            Process.Start(appPath);
            Application.Current.Dispatcher.Invoke(Application.Current.Shutdown);

            #endif
        }

        #endregion

        #region Helper methods

        private static void ClearMs(MemoryStream ms)
        {
            var buffer = ms.GetBuffer();
            Array.Clear(buffer, 0, buffer.Length);
            ms.Position = 0;
            ms.SetLength(0);
        }

        private static Uri GetUri(bool testing)
        {
            if (testing)
                return new Uri("ws://127.0.0.1:5000/");

            var serverUri = Config.GetProperty("ServerUri").GetString();

            if (serverUri == null)
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
            var projectBasePath = AppContext.BaseDirectory;

            var binIndex = projectBasePath.IndexOf(Path.DirectorySeparatorChar + "bin" + Path.DirectorySeparatorChar, StringComparison.Ordinal);

            if (binIndex == -1)
            {
                throw new Exception("Could not determine project base path!");
            }

            projectBasePath = projectBasePath[..binIndex];
            return Path.Combine(projectBasePath, relativePath);
        }
    }
}
