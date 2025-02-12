using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace ClientMessenger
{
    public partial class Home : Window
    {
        [GeneratedRegex(@"^[A-Za-z0-9._\s]+$")]
        private static partial Regex UsernameRegex();

        private static readonly HashSet<Relationship> _relationships = [];
        public Home()
        {
            InitializeComponent();
            ClientUI.RegisterWindowButtons(MinimizeBtn, MaximizeBtn, CloseBtn);
            InitAddFriendUsernameTextBox();
            InitAddFriendHashTagTextBox();
            InitNameAndProfilPic();
            InitAddFriendBtn();
            InitPanels();
            InitBtns();
        }

        #region Init

        private void InitBtns()
        {
            AddFriendBtn.Click += ChangePanelState;
            FriendsBtn.Click += ChangePanelState;
            BlockedBtn.Click += ChangePanelState;
            PendingBtn.Click += ChangePanelState;
        }

        private void InitNameAndProfilPic()
        {
            ProfilPic.ImageSource = Client.User.ProfilePicture;
            Username.Text = $"{Client.User.Username} {Client.User.HashTag}";
        }

        private void InitPanels()
        {
            HidePanels();
            FriendsPanel.Visibility = Visibility.Visible;
        }

        #region Init AddFriendPanel

        private void InitAddFriendUsernameTextBox()
        {
            byte maxChars = 14;
            ClientUI.RestrictClipboardPasting(AddFriendUsernameTextBox, maxChars);

            AddFriendUsernameTextBox.GotFocus += (sender, args) =>
            {
                if (AddFriendUsernameTextBox.Text == "Username")
                    AddFriendUsernameTextBox.Text = "";
            };

            AddFriendUsernameTextBox.LostFocus += (sender, args) =>
            {
                if (AddFriendUsernameTextBox.Text == "")
                    AddFriendUsernameTextBox.Text = "Username";
            };

            AddFriendUsernameTextBox.PreviewTextInput += (sender, args) =>
            {
                int charAmount = AddFriendUsernameTextBox.Text.Length;

                if (charAmount >= maxChars || !UsernameRegex().IsMatch(args.Text))
                    args.Handled = true;
            };
        }

        private void InitAddFriendHashTagTextBox()
        {
            const byte maxChars = 5;
            AddFriendHashTagTextBox.PreviewTextInput += (sender, args) =>
            {
                if (AddFriendHashTagTextBox.Text.Length >= maxChars || !UsernameRegex().IsMatch(args.Text))
                {
                    args.Handled = true;
                    return;
                }
            };

            AddFriendHashTagTextBox.TextChanged += (sender, args) =>
            {
                if (!AddFriendHashTagTextBox.Text.StartsWith('#'))
                {
                    AddFriendHashTagTextBox.Text = "#" + AddFriendHashTagTextBox.Text.TrimStart('#');
                    AddFriendHashTagTextBox.CaretIndex = AddFriendHashTagTextBox.Text.Length;
                }
            };

            ClientUI.RestrictClipboardPasting(AddFriendHashTagTextBox, maxChars);
        }

        private void InitAddFriendBtn()
        {
            AddFriendAddFriendBtn.Click += async (sender, args) =>
            {
                var username = AddFriendUsernameTextBox.Text;
                var hashTag = AddFriendHashTagTextBox.Text;

                if (!AddFriendValidateData(username, hashTag))
                {
                    await DisplayInfosAddFriendPanelAsync(Brushes.Red, "The username and/or password is invalid");
                    return;
                }

                Relationship relationship = new()
                {
                    Username = username,
                    HashTag = hashTag
                };

                RelationshipUpdate relationshipUpdate = new()
                {
                    RequestedRelationshipstate = Relationshipstate.Pending,
                    Relationship = relationship,
                    User = Client.User
                };

                var payload = new
                {
                    code = OpCode.UpdateRelationship,
                    relationshipUpdate
                };

                if (!AntiSpam.CheckIfCanSendDataPreLogin(out TimeSpan timeToWait))
                {
                    await DisplayInfosAddFriendPanelAsync(Brushes.Red, $"Pls wait another {timeToWait.TotalSeconds}s");
                }

                await Client.SendPayloadAsync(payload);
            };
        }

        #endregion

        #endregion

        public async Task DisplayInfosAddFriendPanelAsync(SolidColorBrush color, string msg)
        {
            AddFriendInfoTextBlock.Foreground = color;
            AddFriendInfoTextBlock.Text = msg;

            await Task.Delay(2000);

            AddFriendInfoTextBlock.Foreground = Brushes.Black;
            AddFriendInfoTextBlock.Text = "Enter valid data";
        }

        private static bool AddFriendValidateData(string username, string hashTag)
            => !string.IsNullOrEmpty(username) || !string.IsNullOrEmpty(hashTag);

        #region ChangePanel

        private void HidePanels()
        {
            AddFriendPanel.Visibility = Visibility.Collapsed;
            PendingPanel.Visibility = Visibility.Collapsed;
            FriendsPanel.Visibility = Visibility.Collapsed;
            BlockedPanel.Visibility = Visibility.Collapsed;
        }

        private void ChangePanelState(object sender, RoutedEventArgs args)
        {
            var btn = (Button)sender;
            switch ((string)btn.Tag)
            {
                case "AddFriend":
                    ChooseAnimation(AddFriendPanelTranslateTransform, AddFriendPanel);
                    break;
                case "Friends":
                    ChooseAnimation(FriendsPanelTranslateTransform, FriendsPanel);
                    break;
                case "Blocked":
                    ChooseAnimation(BlockedPanelTranslateTransform, BlockedPanel);
                    break;
                case "Pending":
                    ChooseAnimation(PendingPanelTranslateTransform, PendingPanel);
                    break;
            }
        }

        #region Animations

        private void ChooseAnimation(TranslateTransform translateTransform, Grid grid)
        {
            if (grid.Visibility == Visibility.Collapsed)
            {
                SlideInAnimation(translateTransform, grid);
            }
            else
            {
                SlideOutAnimation(translateTransform, grid);
            }
        }

        private void SlideInAnimation(TranslateTransform translateTransform, Grid grid)
        {
            HidePanels();
            grid.Visibility = Visibility.Visible;
            grid.UpdateLayout();

            var slideInAnimation = new DoubleAnimation
            {
                From = grid.ActualWidth,
                To = 0,
                Duration = TimeSpan.FromSeconds(0.3)
            };

            translateTransform.BeginAnimation(TranslateTransform.XProperty, slideInAnimation);
        }

        private static void SlideOutAnimation(TranslateTransform translateTransform, Grid grid)
        {
            var slideOutAnimation = new DoubleAnimation
            {
                From = 0,
                To = grid.ActualWidth,
                Duration = TimeSpan.FromSeconds(0.3)
            };

            slideOutAnimation.Completed += (s, a) =>
            {
                grid.Visibility = Visibility.Collapsed;
            };

            translateTransform.BeginAnimation(TranslateTransform.XProperty, slideOutAnimation);
        }

        #endregion

        #endregion
    }
}
