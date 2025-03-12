using System.Collections.Specialized;
using System.Collections.ObjectModel;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using SharpVectors.Converters;
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
        private readonly Dictionary<TagUserData, Chat> _chats = new();

        public Home()
        {
            InitializeComponent();
            ClientUI.RegisterWindowButtons(MinimizeBtn, MaximizeBtn, CloseBtn);
            InitAddFriendUsernameTextBox();
            InitAddFriendHashTagTextBox();
            InitNameAndProfilPic();
            _ = CleanUpChats();
            InitAddFriendBtn();
            InitCollections();
            InitDmList();
            InitPanels();
            InitBtns();
        }

        #region Init

        private void InitBtns()
        {
            AddFriendBtn.Click += ChangePanelState;
            AddFriendBtn.Tag = Panels.AddFriend;

            FriendsBtn.Click += ChangePanelState;
            FriendsBtn.Tag = Panels.Friends;

            BlockedBtn.Click += ChangePanelState;
            BlockedBtn.Tag = Panels.Blocked;

            PendingBtn.Click += ChangePanelState;
            PendingBtn.Tag = Panels.Pending;
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

        private void InitCollections()
        {
            Friends.CollectionChanged += (sender, args) =>
            {
                if (args.Action == NotifyCollectionChangedAction.Add)
                {
                    var relationshipToAdd = (Relationship)args.NewItems![0]!;
                    AddOneToFriendsList(relationshipToAdd);
                }
                else if (args.Action == NotifyCollectionChangedAction.Remove)
                {
                    var relationshipToRemove = (Relationship)args.OldItems![0]!;
                    RemoveOneFromFriendsList(relationshipToRemove);
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

        private void InitDmList()
        {
            Dms.SelectionChanged += (sender, args) =>
            {
                if (Dms.SelectedItem is StackPanel stackPanel)
                {
                    var tagUserData = (TagUserData)stackPanel.Tag;
                    Relationship relationship = _friends.FirstOrDefault(x => x.Username == tagUserData.Username
                        && x.HashTag == tagUserData.HashTag)!;

                    CreateOrOpenChat(relationship);
                }
            };
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

        #endregion

        #region Chat

        private void CreateOrOpenChat(Relationship relationship)
        {
            HidePanels();

            if (_chats.TryGetValue(new TagUserData(relationship.Username, relationship.HashTag), out Chat? chat))
            {
                chat.LastOpend = DateTime.Now;

                ChatPanel.Children.Clear();
                ChatPanel.Children.Add(chat.ChatPanel);
                ChatPanel.UpdateLayout();
                SlideInAnimation(ChatPanelTranslateTransform, ChatPanel);

                return;
            }

            CreateChat(relationship);
        }
    
        private void CreateChat(Relationship relationship)
        {
            //HOL DATA AUS LOCAL DATABASE WENN VORHANDEN!!!!
            var scrollViewer = new ScrollViewer
            {
                VerticalScrollBarVisibility = ScrollBarVisibility.Hidden,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
            };

            var chatPanel = new StackPanel
            {
                Orientation = Orientation.Vertical,
                VerticalAlignment = VerticalAlignment.Top,
                Margin = new Thickness(10),
            };

            scrollViewer.Content = chatPanel;

            ChatPanel.Children.Clear();
            ChatPanel.Children.Add(scrollViewer);
            ChatPanel.UpdateLayout();

            SlideInAnimation(ChatPanelTranslateTransform, ChatPanel);
            _chats.Add(new TagUserData(relationship.Username, relationship.HashTag), new Chat(scrollViewer, DateTime.Now));
        }

        private static void AddMessage(ScrollViewer scrollViewer, Relationship relationship, Message message)
        {
            var chatPanel = (StackPanel)scrollViewer.Content;
            var outerStackPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                VerticalAlignment = VerticalAlignment.Top,
                Margin = new Thickness(10),
            };

            Relationship sender = message.SenderId == Client.User.Id
                ? (Relationship)Client.User
                : relationship;

            var ellipse = new Ellipse
            {
                Width = 45,
                Height = 45,
                Margin = new Thickness(0, 0, 10, 0),
                Fill = new ImageBrush
                {
                    ImageSource = sender.ProfilePicture,
                    Stretch = Stretch.UniformToFill
                }
            };

            var innerStackPanel = new StackPanel
            {
                Orientation = Orientation.Vertical,
                MaxWidth = 400
            };

            var nameAndTimePanel = new DockPanel();

            var nameTextBlock = new TextBlock
            {
                Text = sender.Username,
                FontWeight = FontWeights.Bold,
                FontSize = 16,
                Foreground = Brushes.LightGray,
                Margin = new Thickness(0, 0, 20, 0)
            };
            DockPanel.SetDock(nameTextBlock, Dock.Left);

            var dateTimeTextBlock = new TextBlock
            {
                Text = message.DateTime.ToLocalTime().ToString("dd.MM.yyyy HH:mm"),
                FontSize = 12,
                Foreground = Brushes.Gray,
                VerticalAlignment = VerticalAlignment.Center
            };
            DockPanel.SetDock(dateTimeTextBlock, Dock.Right);

            nameAndTimePanel.Children.Add(nameTextBlock);
            nameAndTimePanel.Children.Add(dateTimeTextBlock);

            var messageTextBlock = new TextBlock
            {
                Text = message.Content,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 5, 0, 0),
                FontSize = 14,
                Foreground = Brushes.LightGray
            };

            innerStackPanel.Children.Add(nameAndTimePanel);
            innerStackPanel.Children.Add(messageTextBlock);
            outerStackPanel.Children.Add(ellipse);
            outerStackPanel.Children.Add(innerStackPanel);

            chatPanel.Children.Add(outerStackPanel);
        }

        private async Task CleanUpChats()
        {
            while (true)
            {
                await Task.Delay(TimeSpan.FromSeconds(10));
                foreach (KeyValuePair<TagUserData, Chat> chat in _chats)
                {
                    if (ChatPanel.Children[0] != chat.Value.ChatPanel && DateTime.Now - chat.Value.LastOpend > TimeSpan.FromSeconds(10))
                    {
                        Logger.LogInformation($"Chat deleted from {nameof(_chats)}");
                        _chats.Remove(chat.Key);
                    }
                }
            }
        }


        #endregion

        #region Setter

        public ObservableCollection<Relationship> Friends
        {
            get => _friends;
            set
            {
                _friends = value;
                PopulateFriendsList(value);
                PopulateDmList(value);
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

        #region DmList

        private void PopulateDmList(IList<Relationship> relationships)
        {
            foreach (Relationship relationship in relationships)
            {
                StackPanel stackPanel = BasicUserUI(relationship);
                CreateBtnForDmListUI(stackPanel);
                Dms.Items.Add(stackPanel);
                Dms.UpdateLayout();
            }
        }

        private void AddOneToDmList(Relationship relationship)
        {
            List<StackPanel> stackPanels = [.. Dms.Items.Cast<StackPanel>()];
            StackPanel? match = stackPanels.FirstOrDefault(x => x.Tag is (string username, string hashTag)
                && username == relationship.Username && hashTag == relationship.HashTag);

            if (match == null)
            {
                StackPanel stackPanel = BasicUserUI(relationship);
                CreateBtnForDmListUI(stackPanel);
                Dms.Items.Add(stackPanel);
                Dms.UpdateLayout();
            }
        }

        private void RemoveOneFromDmList(StackPanel stackPanel)
        {
            Dms.Items.Remove(stackPanel);
            Dms.UpdateLayout();
        }

        private void CreateBtnForDmListUI(StackPanel stackPanel)
        {
            var deleteButton = new Button
            {
                Content = "X",
                Background = Brushes.Transparent,
                BorderBrush = Brushes.Transparent,
                Foreground = Brushes.White,
                Width = 30,
                Height = 30,
                Margin = new Thickness(0, 0, 0, 0),
            };

            deleteButton.Click += CloseChat_Click;
            stackPanel.Children.Add(deleteButton);
        }

        #endregion

        #region BlockedList

        private void PopulateBlockedList(IList<Relationship> friends)
        {
            Colors colors = new();
            foreach (Relationship friend in friends)
            {
                StackPanel stackPanel = BasicUserUI(friend);
                CreateBtnsForBlockedListUI(stackPanel, friend, in colors);
                BlockedList.Items.Add(stackPanel);
            }
            BlockedList.UpdateLayout();
        }

        private void AddOneToBlockedList(Relationship blockedUser)
        {
            StackPanel stackPanel = BasicUserUI(blockedUser);
            CreateBtnsForBlockedListUI(stackPanel, blockedUser, new Colors());
            BlockedList.Items.Add(stackPanel);
            BlockedList.UpdateLayout();
        }

        private void RemoveOneFromBlockedList(Relationship? friend)
        {
            ArgumentNullException.ThrowIfNull(friend);
            BlockedList.Items.Remove(BlockedList.Items.Cast<StackPanel>().FirstOrDefault(x => (x.Tag as (string, string)?) == (friend.Username, friend.HashTag)));
            BlockedList.UpdateLayout();
        }

        private void CreateBtnsForBlockedListUI(StackPanel stackPanel, Relationship blocked, in Colors colors)
        {
            RelationshipButtonsData readdButtonData = new(blocked.Id, Blocked, RelationshipState.Pending);
            var readdButton = new Button
            {
                Content = "Re-add",
                Background = new SolidColorBrush(colors.Green),
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
                Background = new SolidColorBrush(colors.Gray),
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

        #endregion

        #region FriendsList

        private void PopulateFriendsList(IList<Relationship> friends)
        {
            Colors colors = new();
            foreach (Relationship friend in friends)
            {
                StackPanel stackPanel = BasicUserUI(friend);
                CreateBtnsForFriendsListUI(stackPanel, friend, in colors);
                FriendsList.Items.Add(stackPanel);
            }
            FriendsList.UpdateLayout();
        }

        private void AddOneToFriendsList(Relationship friend)
        {
            StackPanel stackPanel = BasicUserUI(friend);
            CreateBtnsForFriendsListUI(stackPanel, friend, new Colors());
            FriendsList.Items.Add(stackPanel);
            FriendsList.UpdateLayout();
        }

        private void RemoveOneFromFriendsList(Relationship? friend)
        {
            ArgumentNullException.ThrowIfNull(friend);
            FriendsList.Items.Remove(FriendsList.Items.Cast<StackPanel>().FirstOrDefault(x => (x.Tag as (string, string)?) == (friend.Username, friend.HashTag)));
            FriendsList.UpdateLayout();
        }

        private void CreateBtnsForFriendsListUI(StackPanel stackPanel, Relationship friend, in Colors colors)
        {
            RelationshipButtonsData deleteButtonData = new(friend.Id, Friends, RelationshipState.None);
            var declineButton = new Button
            {
                Content = "Delete",
                Background = new SolidColorBrush(colors.Red),
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
                Background = new SolidColorBrush(colors.Gray),
                Foreground = Brushes.White,
                Width = 80,
                Height = 30,
                Margin = new Thickness(5),
                Tag = blockButtonData
            };

            var msgButton = new Button
            {
                Width = 40,
                Height = 30,
                Background = Brushes.Transparent,
                BorderBrush = Brushes.Transparent,
                Margin = new Thickness(5),
                Tag = friend.Id
            };

            var svgViewbox = new SvgViewbox
            {
                Source = new Uri(Client.GetDynamicPath(@"Images/msg.svg")),
                Stretch = Stretch.Uniform
            };

            msgButton.Content = svgViewbox;

            declineButton.Click += RelationshipStateChange_Click;
            blockButton.Click += RelationshipStateChange_Click;
            msgButton.Click += CreateChat_Click;

            stackPanel.Children.Add(declineButton);
            stackPanel.Children.Add(blockButton);
            stackPanel.Children.Add(msgButton);
        }

        #endregion

        #region PendingList

        private void PopulatePendingList(IList<Relationship> pendingRequests)
        {
            Colors colors = new();
            foreach (Relationship pending in pendingRequests)
            {
                StackPanel stackPanel = BasicUserUI(pending);
                CreateBtnsForPendingListUI(stackPanel, pending, in colors);
                PendingList.Items.Add(stackPanel);
            }
            PendingList.UpdateLayout();
        }

        private void AddOneToPendingList(Relationship pending)
        {
            StackPanel stackPanel = BasicUserUI(pending);
            CreateBtnsForPendingListUI(stackPanel, pending, new Colors());
            PendingList.Items.Add(stackPanel);
            PendingList.UpdateLayout();
        }

        private void RemoveOneFromPendingList(Relationship? pending)
        {
            ArgumentNullException.ThrowIfNull(pending);
            PendingList.Items.Remove(PendingList.Items.Cast<StackPanel>().FirstOrDefault(x => (x.Tag as (string, string)?) == (pending.Username, pending.HashTag)));
            PendingList.UpdateLayout();
        }

        private void CreateBtnsForPendingListUI(StackPanel stackPanel, Relationship pendingRequest, in Colors colors)
        {
            RelationshipButtonsData acceptButtonData = new(pendingRequest.Id, Pending, RelationshipState.Friend);
            var acceptButton = new Button
            {
                Content = "Accept",
                Background = new SolidColorBrush(colors.Green),
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
                Background = new SolidColorBrush(colors.Red),
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
                Background = new SolidColorBrush(colors.Gray),
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

        #endregion

        #region ChangePanel

        private void HidePanels()
        {
            AddFriendPanel.Visibility = Visibility.Collapsed;
            PendingPanel.Visibility = Visibility.Collapsed;
            FriendsPanel.Visibility = Visibility.Collapsed;
            BlockedPanel.Visibility = Visibility.Collapsed;
            ChatPanel.Visibility = Visibility.Collapsed;
        }

        private void ChangePanelState(object sender, RoutedEventArgs args)
        {
            var btn = (Button)sender;
            switch ((Panels)btn.Tag)
            {
                case Panels.AddFriend:
                    ChooseAnimation(AddFriendPanelTranslateTransform, AddFriendPanel);
                    break;
                case Panels.Friends:
                    ChooseAnimation(FriendsPanelTranslateTransform, FriendsPanel);
                    break;
                case Panels.Blocked:
                    ChooseAnimation(BlockedPanelTranslateTransform, BlockedPanel);
                    break;
                case Panels.Pending:
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
                SlideInAnimation(ChatPanelTranslateTransform, ChatPanel);
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

        #region ClickEvents

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
                            targetSet.Remove(relationshipUpdate.Relationship);
                        }
                        break;

                    case RelationshipState.Blocked:
                        lock (Lock)
                        {
                            _blocked.Add(relationshipUpdate.Relationship);
                            targetSet.Remove(relationshipUpdate.Relationship);
                        }
                        break;

                    case RelationshipState.Pending or RelationshipState.None:
                        lock (Lock)
                        {
                            targetSet.Remove(relationshipUpdate.Relationship);
                            RemoveOneFromDmList(stackPanelParent);
                        }
                        break;
                }
                PendingList.UpdateLayout();
            }
        }

        private void CreateChat_Click(object sender, RoutedEventArgs args)
        {
            var button = (Button)sender;
            Relationship relationship = _friends.FirstOrDefault(x => x.Id == (long)button.Tag)!;
            AddOneToDmList(relationship);
            CreateOrOpenChat(relationship);
        }

        private void CloseChat_Click(object sender, RoutedEventArgs args)
        {
            var button = (Button)sender;
            var stackPanel = (StackPanel)button.Parent;
            RemoveOneFromDmList(stackPanel);
            ChatPanel.Visibility = Visibility.Collapsed;
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

        private static StackPanel BasicUserUI(Relationship user)
        {
            var stackPanel = new StackPanel
            {
                Tag = new TagUserData(user.Username, user.HashTag),
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
            return stackPanel;
        }

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
    }
}
