using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;

namespace ClientMessenger
{
    public partial class Login : Window
    {
        [GeneratedRegex(@"^(?("")("".+?""@)|(([0-9a-zA-Z](([\w-]*[0-9a-zA-Z])?)+)\@))([a-zA-Z0-9][\w-]*\.)+[a-zA-Z]{2,}$")]
        private static partial Regex EmailRegex();

        public Login()
        {
            InitializeComponent();
            ClientUI.RegisterWindowButtons(MinimizeBtn, MaximizeBtn, CloseBtn);

            #region Email TextBox
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

            #endregion

            #region Password TextBox
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

            #endregion

            CreateAccLink.Click += ((sender, args) =>
            {
                var createAcc = ClientUI.GetWindow<CreateAcc>();
                createAcc ??= new CreateAcc();
                createAcc.Show();
                Hide();
            });

            LoginBtn.Click += async(sender, args) => 
            { 
                var email = EmailTextBox.Text;
                var password = PasswordTextBox.Text;

                if (!EmailRegex().IsMatch(email))
                {
                    await ShowError(EmailError);
                    return;
                }

                if (password == "Password" || string.IsNullOrEmpty(password))
                {
                    await ShowError(PasswordError);
                    return;
                }

                var payload = new
                {
                    code = OpCode.RequestLogin,
                    email = EmailTextBox.Text,
                    password = PasswordTextBox.Text,
                };
                await Client.SendPayloadAsync(payload);
            };
        }

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
