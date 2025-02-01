using System.Windows;
using System.Windows.Media;

namespace ClientMessenger
{
    public partial class Verification : Window
    {
        public Verification()
        {
            InitializeComponent();
            ClientUI.RegisterWindowButtons(MinimizeBtn, MaximizeBtn, CloseBtn);

            VerificationTextBox.PreviewTextInput += (sender, args) =>
               args.Handled = !int.TryParse(args.Text, out _);

            SignUpBtn.Click += async (sender, args) =>
            {
                if (string.IsNullOrEmpty(VerificationTextBox.Text))
                {
                    await ChangeMsg("Enter the verification code");
                    return;
                }

                var payload = new
                {
                    code = OpCode.VerificationProcess,
                    verificationCode = int.Parse(VerificationTextBox.Text)
                };
                await Client.SendPayloadAsync(payload);
            };
        }

        public async Task AnswerToVerificationRequest(bool? success)
        {
            if (!success.HasValue)
            {
                await ChangeMsg("You enterd the code wrong to often!");
                await Client.CloseConnection(System.Net.WebSockets.WebSocketCloseStatus.PolicyViolation, "");
                return;
            }

            if (success.Value)
                ClientUI.SwitchWindows<Verification, Home>();
            else
                await ChangeMsg("The enterd code is incorrect");
        }

        private async Task ChangeMsg(string msg)
        {
            await Application.Current.Dispatcher.InvokeAsync(async () =>
            {
                string oldMsg = InformationTextBlock.Text;
                Brush oldColor = InformationTextBlock.Foreground;

                InformationTextBlock.Foreground = Brushes.Red;
                InformationTextBlock.Text = msg;

                await Task.Delay(2000);

                InformationTextBlock.Text = oldMsg;
                InformationTextBlock.Foreground = oldColor;
            });
        }
    }
}
