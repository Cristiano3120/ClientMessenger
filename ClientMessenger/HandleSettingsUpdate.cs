using System.Text.Json;
using System.Windows;
using System.Windows.Threading;

namespace ClientMessenger
{
    public static class HandleSettingsUpdate
    {
        public static async Task HandleReceivedMessage(JsonDocument jsonDocument)
        {
            JsonElement message = jsonDocument.RootElement;

            SettingsUpdate settingsUpdate = message.GetSettingsUpdate();
            switch (settingsUpdate)
            {
                case SettingsUpdate.AnswerToUsernameChange:
                    UsernameUpdate usernameUpdate = JsonSerializer.Deserialize<UsernameUpdate>(message.GetProperty("usernameUpdate"), Client.JsonSerializerOptions);
                    UsernameUpdateResult usernameUpdateResult = message.GetUsernameUpdateResult();
                    await AnswerToUsernameChangeRequest(usernameUpdate, usernameUpdateResult);
                    break;
            }
        }

        public static async Task AnswerToUsernameChangeRequest(UsernameUpdate usernameUpdate, UsernameUpdateResult usernameUpdateResult)
        {
            await Application.Current.Dispatcher.InvokeAsync(async () =>
            {
                Home home = ClientUI.GetWindow<Home>();
                await home.AnswerToUsernameChange(usernameUpdate, usernameUpdateResult);
            }, DispatcherPriority.Render);
        }
    }
}
