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
        public const string PathToConfig = @"C:\Users\Crist\source\repos\ClientMessenger\ClientMessenger\Settings\Settings.json";
        public static JsonSerializerOptions JsonSerializerOptions { get; private set; } = new();
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

            JsonSerializerOptions.Converters.Add(new JsonConverters.UserConverter());
            JsonSerializerOptions.WriteIndented = true;

            await ConnectToServer();
        }

        private static async Task ConnectToServer()
        {
            while (_server.State != WebSocketState.Open)
            {
                try
                {
                    await _server.ConnectAsync(GetUri(true), CancellationToken.None);
                }
                catch (OperationCanceledException ex)
                {
                    await Task.Delay(1000);
                    Logger.LogError(ex);
                    _server = new();
                }
            }

            Logger.LogWarning("Connected!");
            _ = Task.Run(ReceiveMessages);
        }

        #endregion

        #region Receive and handle message

        private static async Task ReceiveMessages()
        {
            var buffer = new byte[65536];
            var ms = new MemoryStream();

            while (_server.State == WebSocketState.Open)
            {
                try
                {
                    WebSocketReceiveResult receivedDataInfo = await _server.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                    Logger.LogInformation(ConsoleColor.Cyan, $"[RECEIVED]: The received payload is {receivedDataInfo.Count} bytes long");

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

                    await HandleReceivedMessage(JsonDocument.Parse(completeMessage).RootElement);
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex);
                    await CleanUpConnection();
                    break;
                }
            }
            await Start();
        }

        private static async Task HandleReceivedMessage(JsonElement message)
        {
            OpCode code = message.GetProperty("code").GetOpCode();
            switch (code)
            {
                case OpCode.ReceiveRSA:
                    await HandleServerResponses.ReceiveRSA(message);
                    break;
                case OpCode.ServerReadyToreceive:
                    HandleServerResponses.ServerReadyToReceive();
                    break;
                case OpCode.AnswerCreateAccount:
                    await HandleServerResponses.AnswerCreateAccount(message);
                    break;
                case OpCode.AnswerLogin:
                    await HandleServerResponses.AnswerToLogin(message);
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

            Logger.LogInformation(ConsoleColor.Blue, $"[SENDING(Aes)]: {jsonPayload}");
            Logger.LogInformation($"Buffer length: {encryptedData.Length}");
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

            Logger.LogInformation(ConsoleColor.Blue, $"[SENDING(Aes)]: {jsonPayload}");
            Logger.LogInformation($"Buffer length: {encryptedData.Length}");
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

        private static async Task CleanUpConnection()
        {
            using var cts = new CancellationTokenSource();
            cts.CancelAfter(TimeSpan.FromSeconds(10));
            if (_server.State != WebSocketState.Aborted)
                await _server.CloseAsync(WebSocketCloseStatus.Empty, null, cts.Token);
        }

        private static Uri GetUri(bool testing)
        {
            if (testing)
                return new Uri("ws://127.0.0.1:5000/");

            var fileContent = File.ReadAllText(PathToConfig);
            JsonElement element = JsonDocument.Parse(fileContent).RootElement;
            var serverUri = element.GetProperty("ServerUri").GetString();

            if (serverUri == null)
            {
                Application.Current.Shutdown();
                throw new NullReferenceException($"{nameof(serverUri)} is null");
            }

            return new Uri(serverUri);
        }

        #endregion
    }
}
