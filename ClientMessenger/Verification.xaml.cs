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
                await SendVerificationCodeAsync();
            };
        }

        private async Task SendVerificationCodeAsync()
        {
            if (string.IsNullOrEmpty(VerificationTextBox.Text))
            {
                await ChangeMsgAsync("Enter the verification code");
                return;
            }

            var payload = new
            {
                opCode = OpCode.VerificationProcess,
                verificationCode = int.Parse(VerificationTextBox.Text)
            };
            await Client.SendPayloadAsync(payload);
        }

        public async Task AnswerToVerificationRequestAsync(bool? success)
        {
            if (!success.HasValue)
            {
                await ChangeMsgAsync("You enterd the code wrong to often!");
                await Client.CloseConnectionAsync(System.Net.WebSockets.WebSocketCloseStatus.PolicyViolation, "");
                return;
            }

            if (success.Value)
                ClientUI.SwitchWindows<Verification, Home>();
            else
                await ChangeMsgAsync("The enterd code is incorrect");
        }

        private async Task ChangeMsgAsync(string msg)
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
