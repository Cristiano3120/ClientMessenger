using System.Text.Json;
using System.Windows;
using System.Windows.Threading;

namespace ClientMessenger
{
    public static class HandleSettingsUpdate
    {
        public static async Task HandleReceivedMessageAsync(JsonDocument jsonDocument)
        {
            JsonElement message = jsonDocument.RootElement;

            SettingsUpdate settingsUpdate = message.GetSettingsUpdate();
            switch (settingsUpdate)
            {
                case SettingsUpdate.AnswerToUsernameChange:
                    UsernameUpdate usernameUpdate = JsonSerializer.Deserialize<UsernameUpdate>(message);
                    UsernameUpdateResult usernameUpdateResult = message.GetUsernameUpdateResult();
                    await AnswerToUsernameChangeRequestAsync(usernameUpdate, usernameUpdateResult);
                    break;
            }
        }

        public static async Task AnswerToUsernameChangeRequestAsync(UsernameUpdate usernameUpdate, UsernameUpdateResult usernameUpdateResult)
        {
            await Application.Current.Dispatcher.InvokeAsync(async () =>
            {
                Home home = ClientUI.GetWindow<Home>();
                await home.AnswerToUsernameChangeAsync(usernameUpdate, usernameUpdateResult);
            }, DispatcherPriority.Render);
        }
    }
}
