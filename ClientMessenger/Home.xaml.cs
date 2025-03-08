using System.Collections.Specialized;
using System.Collections.ObjectModel;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;

namespace ClientMessenger
{
    public partial class Home : Window
    {
        [GeneratedRegex(@"^[A-Za-z0-9._\s]+$")]
        private static partial Regex UsernameRegex();

        public Lock Lock { get; private set; } = new();
        private ObservableCollection<Relationship> _friends = [];
        private ObservableCollection<Relationship> _blocked = [];
        private ObservableCollection<Relationship> _pending = [];

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
            InitCollections();
        }

        private void InitCollections()
        {
            Friends.CollectionChanged += (sender, args) =>
            {
                if (args.Action == NotifyCollectionChangedAction.Add)
                {
                    AddOneToFriendsList((Relationship)args.NewItems![0]!);
                }
                else if (args.Action == NotifyCollectionChangedAction.Remove)
                {
                    RemoveOneFromFriendsList(args.OldItems![0] as Relationship);
                }
            };

            Blocked.CollectionChanged += (sender, args) =>
            {
                if (args.Action == NotifyCollectionChangedAction.Add)
                {
                    AddOneToBlockedList((Relationship)args.NewItems![0]!);
                }
                else if (args.Action == NotifyCollectionChangedAction.Remove)
                {
                    RemoveOneFromBlockedList(args.OldItems![0] as Relationship);
                }
            };

            Pending.CollectionChanged += (sender, args) =>
            {
                if (args.Action == NotifyCollectionChangedAction.Add)
                {
                    AddOneToPendingList((Relationship)args.NewItems![0]!);
                }
                else if (args.Action == NotifyCollectionChangedAction.Remove)
                {
                    RemoveOneFromPendingList(args.OldItems![0] as Relationship);
                }
            };
        }

        #region Setter

        public ObservableCollection<Relationship> Friends
        {
            get => _friends;
            set
            {
                _friends = value;
                PopulateFriendsList(value);
            }
        }

        public ObservableCollection<Relationship> Blocked
        {
            get => _blocked;
            set
            {
                _blocked = value;
                PopulateBlockedList(value);
            }
        }

        public ObservableCollection<Relationship> Pending
        {
            get => _pending;
            set
            {
                _pending = value;
                PopulatePendingList(_pending);
            }
        }

        #endregion

        #region BlockedList

        private void PopulateBlockedList(IList<Relationship> friends)
        {
            foreach (Relationship friend in friends)
            {
                StackPanel stackPanel = BasicUserUI(friend);
                CreateBtnsForBlockedListUI(stackPanel, friend);
                BlockedList.Items.Add(stackPanel);
            }
            BlockedList.UpdateLayout();
        }

        private void CreateBtnsForBlockedListUI(StackPanel stackPanel, Relationship blocked)
        {
            RelationshipButtonsData readdButtonData = new(blocked.Id, Blocked, RelationshipState.Pending);
            var readdButton = new Button
            {
                Content = "Re-add",
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#288444")),
                Foreground = Brushes.White,
                Width = 80,
                Height = 30,
                Margin = new Thickness(5),
                Tag = readdButtonData
            };

            RelationshipButtonsData unblockButtonData = new(blocked.Id, Blocked, RelationshipState.None);
            var unblockButton = new Button
            {
                Content = "Unblock",
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#302c34")),
                Foreground = Brushes.White,
                Width = 80,
                Height = 30,
                Margin = new Thickness(5),
                Tag = unblockButtonData
            };

            readdButton.Click += RelationshipStateChange_Click;
            unblockButton.Click += RelationshipStateChange_Click;

            stackPanel.Children.Add(readdButton);
            stackPanel.Children.Add(unblockButton);
        }

        private void AddOneToBlockedList(Relationship blockedUser)
        {
            StackPanel stackPanel = BasicUserUI(blockedUser);
            CreateBtnsForBlockedListUI(stackPanel, blockedUser);
            BlockedList.Items.Add(stackPanel);
            BlockedList.UpdateLayout();
        }

        private void RemoveOneFromBlockedList(Relationship? friend)
        {
            ArgumentNullException.ThrowIfNull(friend);
            BlockedList.Items.Remove(BlockedList.Items.Cast<StackPanel>().FirstOrDefault(x => (x.Tag as (string, string)?) == (friend.Username, friend.HashTag)));
            BlockedList.UpdateLayout();
        }

        #endregion

        #region FriendsList

        private void PopulateFriendsList(IList<Relationship> friends)
        {
            foreach (Relationship friend in friends)
            {
                StackPanel stackPanel = BasicUserUI(friend);
                CreateBtnsForFriendsListUI(stackPanel, friend);
                FriendsList.Items.Add(stackPanel);
            }
            FriendsList.UpdateLayout();
        }

        private void CreateBtnsForFriendsListUI(StackPanel stackPanel, Relationship friend)
        {
            RelationshipButtonsData deleteButtonData = new(friend.Id, Friends, RelationshipState.None);
            var declineButton = new Button
            {
                Content = "Delete",
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#f44038")),
                Foreground = Brushes.White,
                Width = 80,
                Height = 30,
                Margin = new Thickness(5),
                Tag = deleteButtonData
            };

            RelationshipButtonsData blockButtonData = new(friend.Id, Friends, RelationshipState.Blocked);
            var blockButton = new Button
            {
                Content = "Block",
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#302c34")),
                Foreground = Brushes.White,
                Width = 80,
                Height = 30,
                Margin = new Thickness(5),
                Tag = blockButtonData
            };

            declineButton.Click += RelationshipStateChange_Click;
            blockButton.Click += RelationshipStateChange_Click;

            stackPanel.Children.Add(declineButton);
            stackPanel.Children.Add(blockButton);
        }

        private void AddOneToFriendsList(Relationship friend)
        {
            StackPanel stackPanel = BasicUserUI(friend);
            CreateBtnsForFriendsListUI(stackPanel, friend);
            FriendsList.Items.Add(stackPanel);
            FriendsList.UpdateLayout();
        }

        private void RemoveOneFromFriendsList(Relationship? friend)
        {
            ArgumentNullException.ThrowIfNull(friend);
            FriendsList.Items.Remove(FriendsList.Items.Cast<StackPanel>().FirstOrDefault(x => (x.Tag as (string, string)?) == (friend.Username, friend.HashTag)));
            FriendsList.UpdateLayout();
        }

        #endregion

        #region PendingList

        private void PopulatePendingList(IList<Relationship> pendingRequest)
        {
            foreach (Relationship pending in pendingRequest)
            {
                StackPanel stackPanel = BasicUserUI(pending);
                CreateBtnsForPendingListUI(stackPanel, pending);
                PendingList.Items.Add(stackPanel);
            }
            PendingList.UpdateLayout();
        }

        private void AddOneToPendingList(Relationship pending)
        {
            StackPanel stackPanel = BasicUserUI(pending);
            CreateBtnsForPendingListUI(stackPanel, pending);
            PendingList.Items.Add(stackPanel);
            PendingList.UpdateLayout();
        }

        private void CreateBtnsForPendingListUI(StackPanel stackPanel, Relationship pendingRequest)
        {
            RelationshipButtonsData acceptButtonData = new(pendingRequest.Id, Pending, RelationshipState.Friend);
            var acceptButton = new Button
            {
                Content = "Accept",
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#288444")),
                Foreground = Brushes.White,
                Width = 80,
                Height = 30,
                Margin = new Thickness(5),
                Tag = acceptButtonData
            };

            RelationshipButtonsData declineButtonData = new(pendingRequest.Id, Pending, RelationshipState.None);
            var declineButton = new Button
            {
                Content = "Decline",
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#f44038")),
                Foreground = Brushes.White,
                Width = 80,
                Height = 30,
                Margin = new Thickness(5),
                Tag = declineButtonData
            };

            RelationshipButtonsData blockButtonData = new(pendingRequest.Id, Pending, RelationshipState.Blocked);
            var blockButton = new Button
            {
                Content = "Block",
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#302c34")),
                Foreground = Brushes.White,
                Width = 80,
                Height = 30,
                Margin = new Thickness(5),
                Tag = blockButtonData
            };

            acceptButton.Click += RelationshipStateChange_Click;
            declineButton.Click += RelationshipStateChange_Click;
            blockButton.Click += RelationshipStateChange_Click;

            stackPanel.Children.Add(acceptButton);
            stackPanel.Children.Add(declineButton);
            stackPanel.Children.Add(blockButton);
        }

        private void RemoveOneFromPendingList(Relationship? pending)
        {
            ArgumentNullException.ThrowIfNull(pending);
            PendingList.Items.Remove(PendingList.Items.Cast<StackPanel>().FirstOrDefault(x => (x.Tag as (string, string)?) == (pending.Username, pending.HashTag)));
            PendingList.UpdateLayout();
        }

        #endregion

        private static StackPanel BasicUserUI(Relationship user)
        {
            var stackPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(5),
            };
            
            var ellipse = new Ellipse
            {
                Width = 45,
                Height = 45,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 10, 0)
            };

            var imageBrush = new ImageBrush()
            {
                ImageSource = user.ProfilePicture,
                Stretch = Stretch.UniformToFill,
            };

            ellipse.Fill = imageBrush;

            var textBlockUsername = new TextBlock
            {
                Text = user.Username,
                Foreground = Brushes.White,
                FontSize = 18,
                Margin = new Thickness(10)
            };

            stackPanel.Children.Add(ellipse);
            stackPanel.Children.Add(textBlockUsername);
            stackPanel.Tag = (user.Username, user.HashTag);
            return stackPanel;
        }

        private async void RelationshipStateChange_Click(object sender, RoutedEventArgs args)
        {
            if (sender is Button button && button.Tag is RelationshipButtonsData buttonData)
            {
                var stackPanelParent = (StackPanel)button.Parent;
                var listboxParent = (ListBox)stackPanelParent.Parent;

                (long relationshipId, IList<Relationship> targetSet, RelationshipState wantedState) = buttonData;
                RelationshipUpdate relationshipUpdate = new()
                {
                    User = Client.User,
                    Relationship = targetSet.FirstOrDefault(x => x.Id == relationshipId)!,
                    RequestedRelationshipState = wantedState,
                };

                var payload = new
                {
                    opCode = OpCode.UpdateRelationship,
                    relationshipUpdate
                };

                await Client.SendPayloadAsync(payload);

                listboxParent.Items.Remove(stackPanelParent);
                switch (wantedState)
                {
                    case RelationshipState.Friend:
                        lock (Lock)
                        {
                            _friends.Add(relationshipUpdate.Relationship);
                            AddOneToFriendsList(relationshipUpdate.Relationship);
                            targetSet.Remove(relationshipUpdate.Relationship);
                        }
                        break;

                    case RelationshipState.Blocked:
                        lock (Lock)
                        {
                            _blocked.Add(relationshipUpdate.Relationship);
                            AddOneToBlockedList(relationshipUpdate.Relationship);
                            targetSet.Remove(relationshipUpdate.Relationship);
                        }
                        break;

                    case RelationshipState.Pending:
                        lock (Lock)
                        {
                            targetSet.Remove(relationshipUpdate.Relationship);
                        }
                        break;

                    case RelationshipState.None:
                        lock (Lock)
                        {
                            targetSet.Remove(relationshipUpdate.Relationship);
                            break;
                        }
                }

                PendingList.UpdateLayout();
            }
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
                await SendFriendRequest();
            };
        }

        #endregion

        private async Task SendFriendRequest()
        {
            var username = AddFriendUsernameTextBox.Text;
            var hashTag = AddFriendHashTagTextBox.Text;

            if (!AddFriendValidateData(username, hashTag))
            {
                await DisplayInfosAddFriendPanelAsync(Brushes.Red, "The username and/or password is invalid");
                return;
            }

            if (username == Client.User.Username && hashTag == Client.User.HashTag)
            {
                await DisplayInfosAddFriendPanelAsync(Brushes.Red, "You can´t add yourself :(");
                return;
            }

            Func<Relationship, bool> relationshipInListFunc = x => x.Username == username && x.HashTag == hashTag;

            if (Friends.Any(relationshipInListFunc))
            {
                await DisplayInfosAddFriendPanelAsync(Brushes.Red, "You are already friends with this person");
                return;
            }

            if (Pending.Any(relationshipInListFunc))
            {
                await DisplayInfosAddFriendPanelAsync(Brushes.Red, "You already have a pending friend request from this person");
                return;
            }

            if (Blocked.Any(relationshipInListFunc))
            {
                foreach (object item in BlockedList.Items)
                {
                    var stackPanel = (StackPanel)item;
                    (string usernameInList, string hashTagInList) = (ValueTuple<string, string>)stackPanel.Tag;
                    if (usernameInList == username && hashTagInList == hashTag)
                    {
                        BlockedList.Items.Remove(item);
                        break;
                    }
                }
            }

            Relationship relationship = new()
            {
                Username = username,
                HashTag = hashTag
            };

            RelationshipUpdate relationshipUpdate = new()
            {
                RequestedRelationshipState = RelationshipState.Pending,
                Relationship = relationship,
                User = Client.User
            };

            var payload = new
            {
                opCode = OpCode.UpdateRelationship,
                relationshipUpdate
            };

            if (!AntiSpam.CheckIfCanSendData(1.5f, out TimeSpan timeToWait))
            {
                await DisplayInfosAddFriendPanelAsync(Brushes.Red, $"Pls wait another {timeToWait.TotalSeconds}s");
                return;
            }

            await Client.SendPayloadAsync(payload);
        }

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
