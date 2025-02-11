using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace ClientMessenger
{
    public partial class Home : Window
    {
        private static readonly HashSet<Relationship> _relationships = [];
        public Home()
        {
            InitializeComponent();
            ClientUI.RegisterWindowButtons(MinimizeBtn, MaximizeBtn, CloseBtn);
            InitAddFriendUsernameTextBox();
            InitNameAndProfilPic();
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

                if (charAmount >= maxChars)
                    args.Handled = true;
            };
        }

        #endregion

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
    }
}
