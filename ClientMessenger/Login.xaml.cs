using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;

namespace ClientMessenger
{
    public partial class Login : Window
    {
        [GeneratedRegex(@"^(?("")("".+?""@)|(([0-9a-zA-Z](([\w-]*[0-9a-zA-Z])?)+)\@))([a-zA-Z0-9][\w-]*\.)+[a-zA-Z]{2,}$")]
        private partial Regex EmailRegex();

        [GeneratedRegex(@"^(?!Password$).{8,}$")]
        private partial Regex PasswordRegex();

        public Login()
        {
            InitializeComponent();
            ClientUI.RegisterWindowButtons(MinimizeBtn, MaximizeBtn, CloseBtn);
            InitPasswordTextBox();
            InitEmailTextBox();
            InitLoginBtn();

            CreateAccLink.Click += ((_, _) => ClientUI.SwitchWindows<Login, CreateAcc>());
        }

        private async Task<bool> ValidateUserInputAsync(string email, string password)
        {
            if (!EmailRegex().IsMatch(email))
            {
                await ShowErrorAsync(EmailError);
                return false;
            }

            if (!PasswordRegex().IsMatch(password))
            {
                await ShowErrorAsync(PasswordError);
                return false;
            }

            return true;
        }

        private async Task ActivateCooldownErrorAsync(TimeSpan cooldown)
        {
            CooldownError.Visibility = Visibility.Visible;
            await Task.Delay(cooldown);
            CooldownError.Visibility = Visibility.Hidden;
        }

        private async void SendLoginRequestAsync(object sender, RoutedEventArgs args)
        {
            string email = EmailTextBox.Text;
            string password = PasswordTextBox.Text;

            if (!await ValidateUserInputAsync(email, password))
                return;

            if (!AntiSpam.CheckIfCanSendData(1.5f, out TimeSpan timeToWait))
            {
                await ActivateCooldownErrorAsync(timeToWait);
                return;
            }

            bool stayLoggedIn = (bool)AutoLoginCheckBox.IsChecked!;
            Client.Config = Client.Config.SetBoolean(JsonFile.Config, "AutoLogin", stayLoggedIn);

            var payload = new
            {
                opCode = OpCode.RequestToLogin,
                loginRequest = new LoginRequest(email, password, stayLoggedIn)
            };

            await Client.SendPayloadAsync(payload);
        }

        #region Init

        private void InitEmailTextBox()
        {
            EmailTextBox.GotFocus += (_, _) =>
            {
                if (EmailTextBox.Text == "Email")
                    EmailTextBox.Text = "";
            };

            EmailTextBox.LostFocus += (_, _) =>
            {
                if (EmailTextBox.Text == "")
                    EmailTextBox.Text = "Email";
            };
        }

        private void InitPasswordTextBox()
        {
            PasswordTextBox.GotFocus += (_, _) =>
            {
                if (PasswordTextBox.Text == "Password")
                    PasswordTextBox.Text = "";
            };

            PasswordTextBox.LostFocus += (_, _) =>
            {
                if (PasswordTextBox.Text == "")
                    PasswordTextBox.Text = "Password";
            };
        }

        private void InitLoginBtn()
           => LoginBtn.Click += SendLoginRequestAsync;

        #endregion

        #region HandleError

        public async Task LoginWentWrongAsync()
        {
            string oldMsg = EmailError.Text;
            EmailError.Text = "Email or password is wrong!";
            await ShowErrorAsync(EmailError);
            EmailError.Text = oldMsg;
        }

        private static async Task ShowErrorAsync(TextBlock errorUI)
        {
            errorUI.Visibility = Visibility.Visible;
            await Task.Delay(1500);
            errorUI.Visibility = Visibility.Hidden;
        }

        #endregion
    }
}
