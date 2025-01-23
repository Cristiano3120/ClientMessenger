using Microsoft.Win32;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace ClientMessenger
{
    public partial class CreateAcc : Window
    {
        [GeneratedRegex(@"^(?("")("".+?""@)|(([0-9a-zA-Z](([\w-]*[0-9a-zA-Z])?)+)\@))([a-zA-Z0-9][\w-]*\.)+[a-zA-Z]{2,}$")]
        private static partial Regex EmailRegex();

        [GeneratedRegex(@"^[A-Za-z0-9._;,<>´^+#?\s\[\](){}=$€\/%!-]+$")]
        private static partial Regex BiographyRegex();

        [GeneratedRegex(@"^[A-Za-z0-9._\s]+$")]
        private static partial Regex UsernameRegex();

        [GeneratedRegex(@"^(?!Password$).{8,}$")]
        private static partial Regex PasswordRegex();

        private static string _profilPicFile = @"C:\Users\Crist\source\repos\ClientMessenger\ClientMessenger\Images\profilPic.png";
        private static readonly Brush _grayBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#343234"));

        public CreateAcc()
        {
            InitializeComponent();
            ClientUI.RegisterWindowButtons(MinimizeBtn, MaximizeBtn, CloseBtn);
            InitFirstStage();
            InitSecondStage();

            #region GoBackBtn

            GoBackBtn.MouseLeftButtonDown += (sender, args) =>
            {
                if (FirstStage.Visibility == Visibility.Visible)
                {
                    ClientUI.SwitchWindows<CreateAcc, Login>();
                }
                else
                {
                    FirstStage.Visibility = Visibility.Visible;
                    SecondStage.Visibility = Visibility.Collapsed;
                }
            };

            GoBackBtn.MouseEnter += (sender, args) => Cursor = Cursors.Hand;

            GoBackBtn.MouseLeave += (sender, args) => Cursor = Cursors.Arrow;

            #endregion
        }

        #region FirstStage
        private void InitFirstStage()
        {
            InitEmailTextBox();
            InitPasswordTextBox();
            InitializeComboBoxes();

            RandomPasswordBtn.Click += GeneratePassword_Click;

            ContinueBtn.Click += async (sender, args) =>
            {
                if (!CheckIfAllFieldsAreFilledStage1(out TextBlock? textBlock))
                {
                    await Error(textBlock);
                    return;
                }

                FirstStage.Visibility = Visibility.Collapsed;
                SecondStage.Visibility = Visibility.Visible;
            };
        }

        private void InitializeComboBoxes()
        {
            for (var i = 1; i <= 31; i++)
            {
                ComboBoxItem item = new()
                {
                    IsSelected = i == 1,
                    Background = _grayBrush,
                    BorderBrush = _grayBrush,
                    FontWeight = FontWeights.Bold,
                    Content = $"{i}"
                };
                DayBox.Items.Add(item);
            }

            for (var i = 1; i <= 12; i++)
            {
                ComboBoxItem item = new()
                {
                    IsSelected = i == 1,
                    Background = _grayBrush,
                    BorderBrush = _grayBrush,
                    FontWeight = FontWeights.Bold,
                    Content = $"{i}"
                };
                MonthBox.Items.Add(item);
            }

            for (var i = 2020; i >= 1950; i--)
            {
                ComboBoxItem item = new()
                {
                    IsSelected = i == 2020,
                    Background = _grayBrush,
                    BorderBrush = _grayBrush,
                    FontWeight = FontWeights.Bold,
                    Content = $"{i}"
                };
                YearBox.Items.Add(item);
            }
        }

        private void InitEmailTextBox()
        {
            EmailTextBox.GotFocus += (sender, args) =>
            {
                if (EmailTextBox.Text == "Email")
                    EmailTextBox.Text = "";
            };

            EmailTextBox.LostFocus += (sender, args) =>
            {
                if (EmailTextBox.Text == "")
                    EmailTextBox.Text = "Email";
            };
        }

        private void InitPasswordTextBox()
        {
            PasswordTextBox.GotFocus += (sender, args) =>
            {
                if (PasswordTextBox.Text == "Password")
                    PasswordTextBox.Text = "";
            };

            PasswordTextBox.LostFocus += (sender, args) =>
            {
                if (PasswordTextBox.Text == "")
                    PasswordTextBox.Text = "Password";
            };
        }

        #endregion

        #region SecondStage

        private void InitSecondStage()
        {
            InitProfilPic();
            InitUsernameTextBox();
            InitHashTagTextBox();
            InitBiographyTextBox();

            SignUpBtn.Click += async (sender, args) =>
            {
                if (!CheckIfAllFieldsAreFilledStage2())
                {
                    await Error(UsernameError);
                    return;
                }

                var biography = BiographyTextBox.Text == "Biography"
                ? ""
                : BiographyTextBox.Text;

                var dayItem = (ComboBoxItem)DayBox.SelectedItem;
                var monthItem = (ComboBoxItem)MonthBox.SelectedItem;
                var yearItem = (ComboBoxItem)YearBox.SelectedItem;

                var day = int.Parse((string)dayItem.Content).ToString("D2");
                var month = int.Parse((string)monthItem.Content).ToString("D2");
                var year = (string)yearItem.Content;

                if (_profilPicFile is "" or null)
                {
                    _profilPicFile = @"C:\Users\Crist\source\repos\ClientMessenger\ClientMessenger\Images\profilPic.png";
                }

                var user = new User()
                {
                    Email = EmailTextBox.Text,
                    Password = PasswordTextBox.Text,
                    Birthday = DateOnly.ParseExact($"{day}-{month}-{year}", "dd-MM-yyyy", CultureInfo.InvariantCulture),
                    Username = UsernameTextBox.Text,
                    HashTag = HashTagTextBox.Text,
                    Biography = biography,
                    ProfilePicture = ImageEditor.ScaleImage(_profilPicFile),
                };

                var payload = new
                {
                    code = OpCode.RequestCreateAccount,
                    user,
                };
                await Client.SendPayloadAsync(payload);
            };
        }

        private void InitUsernameTextBox()
        {
            UsernameTextBox.GotFocus += (sender, args) =>
            {
                if (UsernameTextBox.Text == "Username")
                    UsernameTextBox.Text = "";
            };

            UsernameTextBox.LostFocus += (sender, args) =>
            {
                if (UsernameTextBox.Text == "")
                    UsernameTextBox.Text = "Username";
            };

            UsernameTextBox.PreviewTextInput += (sender, args) =>
            {
                if (UsernameTextBox.Text.Length > 14 || !UsernameRegex().IsMatch(args.Text))
                    args.Handled = true;
            };
        }

        private void InitHashTagTextBox()
        {
            HashTagTextBox.PreviewTextInput += (sender, args) =>
            {
                if (HashTagTextBox.Text.Length >= 5)
                {
                    args.Handled = true;
                    return;
                }

                if (!UsernameRegex().IsMatch(args.Text))
                {
                    args.Handled = true;
                }
            };

            HashTagTextBox.TextChanged += (sender, args) =>
            {
                if (!HashTagTextBox.Text.StartsWith('#'))
                {
                    HashTagTextBox.Text = "#" + HashTagTextBox.Text.TrimStart('#');
                    HashTagTextBox.CaretIndex = HashTagTextBox.Text.Length;
                }
            };
        }

        private void InitBiographyTextBox()
        {
            BiographyTextBox.GotFocus += (sender, args) =>
            {
                if (BiographyTextBox.Text == "Biography")
                    BiographyTextBox.Text = "";
            };

            BiographyTextBox.LostFocus += (sender, args) =>
            {
                if (BiographyTextBox.Text == "")
                    BiographyTextBox.Text = "Biography";
            };

            BiographyTextBox.TextInput += (sender, args) =>
            {
                if (BiographyTextBox.Text.Length > 150 || !BiographyRegex().IsMatch(args.Text))
                    args.Handled = true;
            };
        }

        private void InitProfilPic()
        {
            ProfilPic.Fill = ImageEditor.CreateImageBrush(_profilPicFile);
            ProfilPic.MouseLeftButtonDown += (sender, args) =>
            {
                OpenFileDialog explorer = new()
                {
                    Filter = "Image files (*.png;*.jpeg;*.jpg)|*.png;*.jpeg;*.jpg",
                    Title = "Please select an image file.",
                    Multiselect = false
                };

                if (explorer.ShowDialog() == true)
                {
                    _profilPicFile = explorer.FileName;
                    BitmapImage croppedImage = ImageEditor.ScaleImage(_profilPicFile);
                    ProfilPic.Fill = ImageEditor.CreateImageBrush(croppedImage);
                }
            };
        }

        #endregion

        #region Validation

        private bool CheckIfAllFieldsAreFilledStage1([NotNullWhen(false)] out TextBlock? element)
        {
            string email;
            if ((email = EmailTextBox.Text) == "Email" || !EmailRegex().IsMatch(email))
            {
                element = EmailError;
                return false;
            }

            if (!PasswordRegex().IsMatch(PasswordTextBox.Text))
            {
                element = PasswordError;
                return false;
            }

            var day = (ComboBoxItem)MonthBox.SelectedItem;
            var month = (ComboBoxItem)MonthBox.SelectedItem;
            var year = (ComboBoxItem)YearBox.SelectedItem;

            if (!DateOnly.TryParse($"{day.Content}, {month.Content}, {year.Content}", out _))
            {
                element = DateOfBirthError;
                return false;
            }

            element = null;
            return true;    
        }

        private bool CheckIfAllFieldsAreFilledStage2() =>
            UsernameTextBox.Text != "Username" && HashTagTextBox.Text != "#";

        private static async Task Error(TextBlock textBlock)
        {
            textBlock.Visibility = Visibility.Visible;
            await Task.Delay(1500);
            textBlock.Visibility = Visibility.Collapsed;
        }

        private static async Task Error(TextBlock textBlock, string msg)
        {
            var oldMsg = textBlock.Text;
            textBlock.Text = msg;
            textBlock.Visibility = Visibility.Visible;
            await Task.Delay(1500);
            textBlock.Visibility = Visibility.Collapsed;
            textBlock.Text = oldMsg;
        }

        #endregion

        #region Generate Password

        private async void GeneratePassword_Click(object sender, RoutedEventArgs e)
        {
            const string validChars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789!@#$%^&*()";
            const int passwordLength = 30;

            var generator = RandomNumberGenerator.Create();
            var passwordChars = new char[passwordLength];
            var buffer = new byte[1];

            for (int i = 0; i < passwordLength; i++)
            {
                do
                {
                    generator.GetBytes(buffer);
                }
                while (buffer[0] >= validChars.Length);

                passwordChars[i] = validChars[buffer[0] % validChars.Length];
            }

            string password = new(passwordChars);
            PasswordTextBox.Text = password;
            Clipboard.SetText(password, TextDataFormat.Text);

            await ChangePasswordErrorText();
        }

        private async Task ChangePasswordErrorText()
        {
            string oldMsg = PasswordError.Text;
            PasswordError.Text = "Copied password to clipboard";
            await Task.Delay(TimeSpan.FromSeconds(3));
            PasswordError.Text = oldMsg;
        }

        #endregion

        public async Task AccCreationWentWrong(string column)
        {
            TextBlock causedErrorTextBlock;
            switch (column)
            {
                case "email":
                    if (FirstStage.Visibility == Visibility.Collapsed)
                    {
                        SecondStage.Visibility = Visibility.Collapsed;
                        FirstStage.Visibility = Visibility.Visible;
                    }

                    causedErrorTextBlock = EmailError;
                    await Error(causedErrorTextBlock, "This email is already in use");
                    break;
                case "username, hashtag":
                    if (SecondStage.Visibility == Visibility.Collapsed)
                    {
                        SecondStage.Visibility = Visibility.Visible;
                        FirstStage.Visibility = Visibility.Collapsed;
                    }

                    causedErrorTextBlock = UsernameError;
                    await Error(causedErrorTextBlock, "This username hashtag combination is already in use");
                    break;
            }
        }
    }
}
