using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;

namespace ClientMessenger
{
    public partial class Login : Window
    {
        [GeneratedRegex(@"^(?("")("".+?""@)|(([0-9a-zA-Z](([\w-]*[0-9a-zA-Z])?)+)\@))([a-zA-Z0-9][\w-]*\.)+[a-zA-Z]{2,}$")]
        private static partial Regex EmailRegex();

        [GeneratedRegex(@"^(?!Password$).{8,}$")]
        private static partial Regex PasswordRegex();

        public Login()
        {
            InitializeComponent();
            ClientUI.RegisterWindowButtons(MinimizeBtn, MaximizeBtn, CloseBtn);
            InitPasswordTextBox();
            InitEmailTextBox();

            CreateAccLink.Click += ((sender, args) 
                => ClientUI.SwitchWindows<Login, CreateAcc>());

            LoginBtn.Click += async(sender, args) => 
            { 
                string email = EmailTextBox.Text;
                string password = PasswordTextBox.Text;

                if (!await ValidateUserInput(email, password))
                    return;

                var payload = new
                {
                    code = OpCode.RequestLogin,
                    email,
                    password,
                };
                await Client.SendPayloadAsync(payload);
            };
        }

        private async Task<bool> ValidateUserInput(string email, string password)
        {
            if (!EmailRegex().IsMatch(email))
            {
                await ShowError(EmailError);
                return false;
            }

            if (!PasswordRegex().IsMatch(password))
            {
                await ShowError(PasswordError);
                return false;
            }

            return true;
        }

        #region Init

        private void InitEmailTextBox()
        {
            EmailTextBox.GotFocus += (sender, e) =>
            {
                if (EmailTextBox.Text == "Email")
                    EmailTextBox.Text = "";
            };

            EmailTextBox.LostFocus += (sender, e) =>
            {
                if (EmailTextBox.Text == "")
                    EmailTextBox.Text = "Email";
            };
        }

        private void InitPasswordTextBox()
        {
            PasswordTextBox.GotFocus += (sender, e) =>
            {
                if (PasswordTextBox.Text == "Password")
                    PasswordTextBox.Text = "";
            };

            PasswordTextBox.LostFocus += (sender, e) =>
            {
                if (PasswordTextBox.Text == "")
                    PasswordTextBox.Text = "Password";
            };
        }

        #endregion

        #region HandleError

        public async Task LoginWentWrong()
        {
            var oldMsg = EmailError.Text;
            EmailError.Text = "Email or password is wrong!";
            await ShowError(EmailError);
            EmailError.Text = oldMsg;
        }

        private static async Task ShowError(TextBlock errorUI)
        {
            errorUI.Visibility = Visibility.Visible;
            await Task.Delay(1500);
            errorUI.Visibility = Visibility.Hidden;
        }

        #endregion
    }
}
