using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using ClientMessenger.LocalChatDatabase;
using Microsoft.Win32;
using SharpVectors.Converters;

namespace ClientMessenger
{
    public partial class Home : Window
    {
        [GeneratedRegex(@"^[A-Za-z0-9._\s]+$")]
        private partial Regex UsernameRegex();

        public Lock Lock { get; private set; } = new();
        private readonly ConcurrentDictionary<TagUserData, Chat> _chats = new();
        private ObservableCollection<Relationship> _friends = [];
        private ObservableCollection<Relationship> _blocked = [];
        private ObservableCollection<Relationship> _pending = [];
        private readonly CancellationTokenSource _cts = new();
        private TagUserData _currentOpenChat;

        public Home()
        {
            InitializeComponent();
            ClientUI.RegisterWindowButtons(MinimizeBtn, MaximizeBtn, CloseBtn);
            InitHashtagTextBox(AddFriendHashtagTextBox);
            InitAddFriendUsernameTextBox();
            InitPersonalInfoStackPanel();
            InitMessageCommandBindings();
            _ = CleanUpChatsAsync(_cts.Token);
            InitAddFriendBtn();
            InitCollections();
            InitChatPanel();
            InitDmList();
            InitPanels();
            InitBtns();
        }

        protected override void OnClosed(EventArgs args)
        {
            base.OnClosed(args);
            _cts.Cancel();
            _cts.Dispose();
        }

        #region Init

        private void InitMessageCommandBindings()
        {
            #region Copy

            CommandBinding copyCommandBinding = new(
                ApplicationCommands.Copy,
                CopyCommand_Executed,
                CopyCommand_CanExecute);

            void CopyCommand_Executed(object sender, ExecutedRoutedEventArgs args)
            {
                if (args.Source is TextBlock textBlock)
                {
                    Clipboard.SetText(textBlock.Text);
                }
            }

            void CopyCommand_CanExecute(object sender, CanExecuteRoutedEventArgs args)
            {
                args.CanExecute = true;
            }

            #endregion

            #region Delete

            CommandBinding deleteCommandBinding = new(
                ApplicationCommands.Delete,
                DeleteCommand_Executed,
                DeleteCommand_CanExecute);

            async void DeleteCommand_Executed(object sender, ExecutedRoutedEventArgs args)
            {
                if (args.Source is TextBlock textBlock && textBlock.Tag is Guid guid)
                {
                    long friendId = _friends.First(x => x == _currentOpenChat).Id;

                    ChatDatabase chatDatabase = new();
                    Message message = chatDatabase.GetMessage(friendId, guid);
                    chatDatabase.DeleteMessage(friendId, guid);

                    DeleteMessage(textBlock);

                    DeleteMessage deleteMessage = new(Client.User.Id, friendId, guid);
                    var payload = new
                    {
                        opCode = OpCode.DeleteMessage,
                        deleteMessage,
                    };

                    await Client.SendPayloadAsync(payload);
                }
            }

            void DeleteCommand_CanExecute(object sender, CanExecuteRoutedEventArgs args)
            {
                args.CanExecute = true;
            }

            #endregion

            CommandBindings.Add(deleteCommandBinding);
            CommandBindings.Add(copyCommandBinding);
        }

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

        private void InitPanels()
        {
            HidePanels();
            FriendsPanel.Visibility = Visibility.Visible;
        }

        private void InitCollections()
        {
            Friends.CollectionChanged += (_, args) =>
            {
                if (args.Action == NotifyCollectionChangedAction.Add)
                {
                    Relationship relationshipToAdd = (Relationship)args.NewItems![0]!;
                    AddOneToFriendsList(relationshipToAdd);
                }
                else if (args.Action == NotifyCollectionChangedAction.Remove)
                {
                    Relationship relationshipToRemove = (Relationship)args.OldItems![0]!;
                    RemoveOneFromFriendsList(relationshipToRemove);
                }
            };

            Blocked.CollectionChanged += (_, args) =>
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

            Pending.CollectionChanged += (_, args) =>
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
            Dms.SelectionChanged += (_, args) =>
            {
                if (Dms.SelectedItem is StackPanel stackPanel && stackPanel.Tag is TagUserData tagUserData)
                {
                    Relationship relationship = _friends.FirstOrDefault(x => x.Username == tagUserData.Username
                        && x.Hashtag == tagUserData.Hashtag)!;

                    CreateOrOpenChat(relationship);
                }
            };
        }

        private void InitChatPanel()
        {
            ChatPanel.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) }); // For the messages
            ChatPanel.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // For the input textbox

            TextBox inputTextBox = new()
            {
                Width = 300,
                Height = 30,
                Margin = new Thickness(0, 0, 0, 15),
                BorderBrush = Brushes.Transparent,
            };

            inputTextBox.KeyDown += async (_, args) =>
            {
                if (args.Key == Key.Enter && !string.IsNullOrEmpty(inputTextBox.Text))
                {
                    Message message = new(Client.User.Id, DateTime.Now, inputTextBox.Text);
                    await SendChatMessageAsync(message);
                    inputTextBox.Text = string.Empty;
                }
            };

            Grid.SetRow(inputTextBox, 1);
            ChatPanel.Children.Add(inputTextBox);
            ChatPanel.UpdateLayout();
        }

        private void InitPersonalInfoStackPanel()
        {
            PersonalInfoStackPanel.MouseEnter += (_, _) => PersonalInfoStackPanel.Cursor = Cursors.Hand;
            PersonalInfoStackPanel.MouseLeave += (_, _) => PersonalInfoStackPanel.Cursor = Cursors.Arrow;

            PersonalInfoStackPanel.MouseDown += (_, _) =>
            {
                HidePanels();
                CreateSettingsPanel();
            };

            User user = Client.User;
            Binding usernameBinding = new(nameof(user.Username))
            {
                Source = user,
                Mode = BindingMode.OneWay
            };
            Username.SetBinding(TextBlock.TextProperty, usernameBinding);

            Binding profilePictureBinding = new(nameof(user.ProfilePicture))
            {
                Source = user,
                Mode = BindingMode.OneWay
            };
            BindingOperations.SetBinding(ProfilPic, ImageBrush.ImageSourceProperty, profilePictureBinding);


            KeyDown += (_, args) =>
            {
                if (args.Key == Key.Escape)
                {
                    StackPanel? changeUsernamePanel = SettingsPanel.Children.Cast<StackPanel>()
                        .FirstOrDefault(x => x.Name == "ChangeUsernamePanel");

                    if (changeUsernamePanel is not null)
                    {
                        SettingsPanel.Children.Remove(changeUsernamePanel);
                    }
                    else
                    {
                        HidePanels();
                        FriendsPanel.Visibility = Visibility.Visible;
                    }
                }
            };
        }

        #region Init AddFriendPanel

        private void InitAddFriendUsernameTextBox()
        {
            const byte maxChars = 14;
            ClientUI.RestrictClipboardPasting(AddFriendUsernameTextBox, maxChars);

            AddFriendUsernameTextBox.GotFocus += (_, _) =>
            {
                if (AddFriendUsernameTextBox.Text == "Username")
                    AddFriendUsernameTextBox.Text = "";
            };

            AddFriendUsernameTextBox.LostFocus += (_, _) =>
            {
                if (AddFriendUsernameTextBox.Text == "")
                    AddFriendUsernameTextBox.Text = "Username";
            };

            AddFriendUsernameTextBox.PreviewTextInput += (_, args) =>
            {
                int charAmount = AddFriendUsernameTextBox.Text.Length;

                if (charAmount >= maxChars || !UsernameRegex().IsMatch(args.Text))
                    args.Handled = true;
            };
        }

        private void InitHashtagTextBox(TextBox textBox)
        {
            const byte maxChars = 5;

            textBox.Text = "#";
            textBox.PreviewTextInput += (_, args) =>
            {
                if (textBox.Text.Length >= maxChars || !UsernameRegex().IsMatch(args.Text))
                {
                    args.Handled = true;
                    return;
                }
            };

            textBox.TextChanged += (_, _) =>
            {
                if (!textBox.Text.StartsWith('#'))
                {
                    textBox.Text = "#" + textBox.Text.TrimStart('#');
                    textBox.CaretIndex = textBox.Text.Length;
                }
            };

            ClientUI.RestrictClipboardPasting(textBox, maxChars);
        }

        private void InitAddFriendBtn()
            => AddFriendAddFriendBtn.Click += SendFriendRequestAsync;
        

        #endregion

        #endregion

        #region Chat

        private void CreateOrOpenChat(Relationship relationship)
        {
            ChangeNotificationAmount(relationship, true);
            HidePanels();

            _currentOpenChat = new TagUserData(relationship.Username, relationship.Hashtag);
            if (_chats.TryGetValue(_currentOpenChat, out Chat? chat))
            {
                chat.LastOpend = DateTime.Now;

                DeleteAllMessagesFromChat();
                ChatPanel.Children.Add(chat.ScrollViewer);
                chat.ScrollViewer.ScrollToEnd();
                ChatPanel.UpdateLayout();

                SlideInAnimation(ChatPanelTranslateTransform, ChatPanel);
                return;
            }

            CreateChat(relationship);
        }

        private void CreateChat(Relationship relationship)
        {
            ScrollViewer scrollViewer = new()
            {
                VerticalScrollBarVisibility = ScrollBarVisibility.Hidden,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
            };
            scrollViewer.PreviewMouseWheel += (_, args) =>
            {
                const byte scrollAmount = 15;
                if (args.Delta > 0)
                {
                    scrollViewer.ScrollToVerticalOffset(scrollViewer.VerticalOffset - scrollAmount);
                }
                else
                {
                    scrollViewer.ScrollToVerticalOffset(scrollViewer.VerticalOffset + scrollAmount);
                }
            };
            scrollViewer.ScrollChanged += (sender, args) =>
            {
                if (_chats.TryGetValue(_currentOpenChat, out Chat? chat))
                {
                    double lastOffset = chat.LastScrollViewerVerticalOffset;
                    double currentOffset = args.VerticalOffset;

                    if (currentOffset < lastOffset || currentOffset == 0)
                    {
                        AddAMessageToChat();
                    }
                    else if (currentOffset > lastOffset)
                    {
                        DeleteMessageFromChat(relationship.Id);
                    }

                    if (currentOffset != lastOffset)
                    {
                        chat.LastScrollViewerVerticalOffset = currentOffset;
                    }
                }
            };

            StackPanel chatPanel = new()
            {
                Orientation = Orientation.Vertical,
                VerticalAlignment = VerticalAlignment.Top,
                Margin = new Thickness(10),
            };

            scrollViewer.Content = chatPanel;
            Grid.SetRow(scrollViewer, 0);

            DeleteAllMessagesFromChat();

            Chat chat = new(scrollViewer, DateTime.Now);
            _chats.TryAdd(new TagUserData(relationship.Username, relationship.Hashtag), chat);

            ChatDatabase chatDatabase = new();
            Message[]? messages = chatDatabase.GetMessages(relationship.Id);
            if (messages is not null && messages.Length > 0)
            {
                foreach (Message message in messages)
                {
                    AddMessage(scrollViewer, relationship, message);
                }
                chat.LastMessage = messages.First();
            }
            
            ChatPanel.Children.Add(scrollViewer);
            scrollViewer.ScrollToEnd();
            ChatPanel.UpdateLayout();

            SlideInAnimation(ChatPanelTranslateTransform, ChatPanel);
        }

        public void AddMessage(Message message, bool addAsNew = true)
        {
            if (addAsNew)
            {
                ChatDatabase chatDatabase = new();
                chatDatabase.AddMessage(message.SenderId, message);
                PlaySound("Sounds/messageSound.wav");
            }
            
            lock (Lock)
            {
                Relationship? relationship = _friends.FirstOrDefault(x => x == _currentOpenChat);
                relationship ??= _friends.First(x => x.Id == message.SenderId);

                if (ChatPanel.Children.Count >= 2 && ChatPanel.Children[1] is ScrollViewer scrollViewer)
                {
                    AddMessage(scrollViewer, relationship, message, addAsNew);
                    return;
                }

                TagUserData tagUserData = new(relationship.Username, relationship.Hashtag);
                ChangeNotificationAmount(relationship, false);
                _chats.Remove(tagUserData, out _);
            }
        }

        private void AddMessage(ScrollViewer scrollViewer, Relationship relationship, Message message, bool addAsNew = true)
        {
            Chat chat = _chats[new TagUserData(relationship.Username, relationship.Hashtag)];
            chat.MessageCount++;

            
            if (chat.LastMessage == message)
            {
                return;
            }

            if (addAsNew && chat.LastMessage == default)
            {
                chat.LastMessage = message;
            }
            else if (!addAsNew)
            {
                chat.LastMessage = message;
            }
            
            StackPanel chatPanel = (StackPanel)scrollViewer.Content;
            StackPanel outerStackPanel = new()
            {
                Orientation = Orientation.Horizontal,
                VerticalAlignment = VerticalAlignment.Top,
                Margin = new Thickness(10),
            };

            Relationship sender = message.SenderId == Client.User.Id
                ? (Relationship)Client.User
                : relationship;

            Ellipse ellipse = new()
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

            StackPanel innerStackPanel = new()
            {
                Orientation = Orientation.Vertical,
                MaxWidth = 400
            };

            DockPanel nameAndTimePanel = new();
            TextBlock nameTextBlock = new()
            {
                Text = sender.Username,
                FontWeight = FontWeights.Bold,
                FontSize = 16,
                Foreground = Brushes.LightGray,
                Margin = new Thickness(0, 0, 20, 0)
            };
            DockPanel.SetDock(nameTextBlock, Dock.Left);

            TextBlock dateTimeTextBlock = new()
            {
                Text = message.DateTime.ToLocalTime().ToString("dd.MM.yyyy HH:mm"),
                FontSize = 12,
                Foreground = Brushes.Gray,
                VerticalAlignment = VerticalAlignment.Center
            };
            DockPanel.SetDock(dateTimeTextBlock, Dock.Right);

            nameAndTimePanel.Children.Add(nameTextBlock);
            nameAndTimePanel.Children.Add(dateTimeTextBlock);

            TextBlock messageTextBlock = new()
            {
                Text = message.Content,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 5, 0, 0),
                FontSize = 14,
                Foreground = Brushes.LightGray,
                Tag = message.Guid,
            };
            messageTextBlock.ContextMenu = CreateMessageContextMenu(message, messageTextBlock);

            innerStackPanel.Children.Add(nameAndTimePanel);
            innerStackPanel.Children.Add(messageTextBlock);

            outerStackPanel.Children.Add(ellipse);
            outerStackPanel.Children.Add(innerStackPanel);

            if (addAsNew)
            {
                chatPanel.Children.Add(outerStackPanel);
            }
            else
            {
                chatPanel.Children.Insert(0, outerStackPanel);
            }

            scrollViewer.ScrollToEnd();
            chatPanel.UpdateLayout();
        }

        private static ContextMenu CreateMessageContextMenu(Message message, TextBlock textBlock)
        {
            Colors colors = new();
            MenuItem copyItem = new()
            {
                Header = "Copy",
                CommandTarget = textBlock,
                Command = ApplicationCommands.Copy,
                Background = colors.DarkerGray,
            };

            MenuItem deleteItem = new()
            {
                Header = "Delete",
                Background = colors.Red,
                CommandTarget = textBlock,
                Command = ApplicationCommands.Delete,
            };

            ContextMenu contextMenu = new()
            {
                Items =      
                {
                    copyItem,
                    deleteItem,
                },
                Background = colors.DarkerGray,
            };
            
            if (message.SenderId != Client.User.Id)
            {
                contextMenu.Items.Remove(deleteItem);
            }

            return contextMenu;
        }

        private void ChangeNotificationAmount(Relationship relationship, bool removeNotifications)
        {
            IEnumerable<StackPanel> openDms = Dms.Items.Cast<StackPanel>();

            foreach (StackPanel stackPanel in openDms)
            {
                if (stackPanel.Tag is TagUserData tagUserData && relationship == tagUserData)
                {
                    TextBlock notificationTextBlock = stackPanel.Children.OfType<TextBlock>()
                        .First(tb => tb.Tag is string tag && tag == "Notification");

                    if (removeNotifications)
                    {
                        notificationTextBlock.Text = string.Empty;
                        return;
                    }

                    if (notificationTextBlock.Text == "99+")
                        return;

                    if (notificationTextBlock.Text == "")
                    {
                        notificationTextBlock.Text = "1";
                        return;
                    }

                    if (byte.TryParse(notificationTextBlock.Text, out byte notificationAmount))
                    {
                        notificationAmount++;

                        notificationTextBlock.Text = notificationAmount <= 99
                            ? notificationAmount.ToString()
                            : "99+";
                    }                
                }
            }
        }

        private static void PlaySound(string pathToSound)
        {
            MediaPlayer mediaPlayer = new();
            mediaPlayer.Open(new Uri(Client.GetDynamicPath(pathToSound)));
            mediaPlayer.Play();
        }

        private void DeleteAllMessagesFromChat()
        {
            foreach (UIElement child in ChatPanel.Children.Cast<UIElement>().Where(child => Grid.GetRow(child) == 0).ToArray())
            {
                ChatPanel.Children.Remove(child);
            }
        }

        private void DeleteMessageFromChat(long id)
        {
            List<StackPanel>? messages = GetMessagesFromChat(out StackPanel? chatPanel);
            
            if (messages is null || messages.Count < 8 || chatPanel is null)
                return;

            if (!_chats.TryGetValue(_currentOpenChat, out Chat? chat))
                return;

            StackPanel? lastMessage = messages.FirstOrDefault();

            if (lastMessage is not null)
            {
                chatPanel.Children.Remove(lastMessage);
                chatPanel.UpdateLayout();

                chat.MessageCount--;

                ChatDatabase chatDatabase = new();
                chat.LastMessage = chatDatabase.GetLastLoadedMessage(chat.LastMessage, id);
            }
        }

        private void DeleteMessage(TextBlock textBlock)
        {
            if (_chats.TryGetValue(_currentOpenChat, out Chat? chat))
            {
                StackPanel chatPanel = (StackPanel)chat.ScrollViewer.Content;
                StackPanel innerStackPanel = (StackPanel)textBlock.Parent;
                StackPanel outerStackPanel = (StackPanel)innerStackPanel.Parent;
                chatPanel.Children.Remove(outerStackPanel);
            }
        }

        public void DeleteMessage(DeleteMessage deleteMessage)
        {
            ChatDatabase chatDatabase = new();
            chatDatabase.DeleteMessage(deleteMessage.SenderId, deleteMessage.MessageGuid);

            Relationship relationship = _friends.First(x => x.Id == deleteMessage.SenderId);
            if (relationship != _currentOpenChat)
            {
                return;
            }

            List<StackPanel>? messages = GetMessagesFromChat(out StackPanel? chatPanel);
            if (messages is null || messages.Count == 0)
            {
                return;
            }

            foreach (StackPanel message in messages)
            {
                StackPanel innerStackPanel = message.Children.OfType<StackPanel>().First();
                TextBlock textBlock = innerStackPanel.Children.OfType<TextBlock>().First();

                if (textBlock.Tag is Guid messageGuid && messageGuid == deleteMessage.MessageGuid)
                {
                    DeleteMessage(textBlock);
                    break;
                }
            }
        }

        private void AddAMessageToChat()
        {
            Relationship relationship = _friends.First(x => x == _currentOpenChat);

            if (!_chats.TryGetValue(_currentOpenChat, out Chat? chat))
                return;

            ChatDatabase chatDatabase = new();
            Message? messageToLoad = chatDatabase.GetNextMessage(chat.LastMessage, relationship.Id);

            if (!messageToLoad.HasValue)
                return;

            AddMessage(messageToLoad.Value, false);
        }

        private List<StackPanel>? GetMessagesFromChat(out StackPanel? chatPanel)
        {
            chatPanel = null;
            if (!_chats.TryGetValue(_currentOpenChat, out Chat? chat))
            {
                return null;
            }
            
            ScrollViewer scrollViewer = chat.ScrollViewer;
            chatPanel = (StackPanel)scrollViewer.Content;
            return [.. chatPanel.Children.Cast<StackPanel>()];
        }

        private async Task CleanUpChatsAsync(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                await Task.Delay(TimeSpan.FromMinutes(5), token);
                List<TagUserData> toRemove = new();

                foreach (KeyValuePair<TagUserData, Chat> chat in _chats)
                {
                    ScrollViewer scrollViewer = ChatPanel.Children.OfType<ScrollViewer>().First();
                    if (scrollViewer != chat.Value.ScrollViewer &&
                        DateTime.Now - chat.Value.LastOpend > TimeSpan.FromMinutes(4, seconds: 30))
                    {
                        toRemove.Add(chat.Key);
                    }
                }

                foreach (TagUserData key in toRemove)
                {
                    Logger.LogInformation($"Chat deleted from {nameof(_chats)}: {key.Username} {key.Hashtag}");
                    _chats.Remove(key, out _);
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
                CreateUIForDmListUI(stackPanel);
                Dms.Items.Add(stackPanel);
                Dms.UpdateLayout();
            }
        }

        private void AddOneToDmList(Relationship relationship)
        {
            List<StackPanel> stackPanels = [.. Dms.Items.Cast<StackPanel>()];
            StackPanel? match = stackPanels.FirstOrDefault(x => x.Tag is TagUserData tagUserData
                && tagUserData == relationship);
            
            if (match is null)
            {
                StackPanel stackPanel = BasicUserUI(relationship);
                CreateUIForDmListUI(stackPanel);
                Dms.Items.Add(stackPanel);
                Dms.UpdateLayout();
            }

            CreateOrOpenChat(relationship);
        }

        private void RemoveOneFromDmList(StackPanel stackPanel)
        {
            Dms.Items.Remove(stackPanel);
            Dms.UpdateLayout();
        }

        private void CreateUIForDmListUI(StackPanel stackPanel)
        {
            Button deleteButton = new()
            {
                Content = "X",
                Background = Brushes.Transparent,
                BorderBrush = Brushes.Transparent,
                Foreground = Brushes.White,
                Width = 30,
                Height = 30,
                Margin = new Thickness(0, 0, 0, 0),
            };

            deleteButton.MouseEnter += (_, _) => deleteButton.Opacity = 0.5;
            deleteButton.MouseLeave += (_, _) =>  deleteButton.Opacity = 1;
            

            Colors colors = new();
            TextBlock textBlock = new()
            {
                Foreground = colors.Red,
                Tag = "Notification"
            };

            deleteButton.Click += CloseChat_Click;
            stackPanel.Children.Add(textBlock);
            stackPanel.Children.Add(deleteButton);
        }

        #endregion

        #region Settings

        private void CreateSettingsPanel()
        {
            StackPanel personalInfoStackPanel = new()
            {
                Orientation = Orientation.Horizontal,
                VerticalAlignment = VerticalAlignment.Top
            };

            personalInfoStackPanel.Children.Add(CreateProfilePictureUI());
            personalInfoStackPanel.Children.Add(CreateUsernameTextBlock());
            SettingsPanel.Children.Add(personalInfoStackPanel);

            SettingsPanel.UpdateLayout();
            SettingsPanel.Visibility = Visibility.Visible;
        }

        private Ellipse CreateProfilePictureUI()
        {
            Ellipse profileEllipse = new()
            {
                Width = 45,
                Height = 45,
                Margin = new Thickness(10, 2.5, 0, 0),
                Cursor = Cursors.Hand,
            };
            profileEllipse.MouseDown += ChangeProfilePictureAsync;

            ImageBrush profileImageBrush = new()
            {
                ImageSource = Client.User.ProfilePicture,
                Stretch = Stretch.UniformToFill
            };
            profileEllipse.Fill = profileImageBrush;

            Binding profilePictureBinding = new(nameof(Client.User.ProfilePicture))
            {
                Source = Client.User,
                Mode = BindingMode.OneWay
            };
            BindingOperations.SetBinding(profileImageBrush, ImageBrush.ImageSourceProperty, profilePictureBinding);

            return profileEllipse;
        }

        private TextBlock CreateUsernameTextBlock()
        {
            Colors colors = new();
            TextBlock usernameTextBox = new()
            {
                Text = Client.User.Username,
                Cursor = Cursors.Hand,
                Width = 120,
                Height = 20,
                Foreground = colors.LightGray,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(10, 2, 0, 0)
            };

            usernameTextBox.MouseDown += CreateChangeUsernameUI;

            Binding usernameBinding = new(nameof(Client.User.Username))
            {
                Source = Client.User,
                Mode = BindingMode.OneWay
            };
            usernameTextBox.SetBinding(TextBlock.TextProperty, usernameBinding);

            return usernameTextBox;
        }

        private void ClearSettingsPanel()
        {
            SettingsPanel.Visibility = Visibility.Collapsed;
            SettingsPanel.Children.Clear();
            SettingsPanel.UpdateLayout();
        }

        private async void ChangeProfilePictureAsync(object sender, RoutedEventArgs args)
        {
            OpenFileDialog openFileDialog = new();
            if (openFileDialog.ShowDialog() == true)
            {
                string filePath = openFileDialog.FileName;
                byte[] profilePictureBytes = File.ReadAllBytes(filePath);

                ProfilePictureUpdate profilePictureUpdate = new(Client.User.Id, profilePictureBytes);
                var payload = new
                {
                    opCode = OpCode.SettingsUpdate,
                    settingsUpdate = SettingsUpdate.ChangeProfilPicture,
                    profilePictureUpdate,
                };

                await Client.SendPayloadAsync(payload);

                Client.User.ProfilePicture = BitmapImageConverter.ToBitmapImage(profilePictureBytes);
                SettingsPanel.UpdateLayout();
            }
        }

        private void CreateChangeUsernameUI(object sender, RoutedEventArgs args)
        {
            Colors colors = new();
            StackPanel outerStackPanel = new()
            {
                Width = 350,
                Height = 200,
                Background = colors.DarkGray,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center,
                Name = "ChangeUsernamePanel"
            };

            StackPanel innerStackPanel = new()
            {
                Name = "InnerStackPanel",
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 75, 0, 0)
            };

            StackPanel usernamePanel = new()
            {
                Name = "UsernamePanel",
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center
            };

            TextBlock usernameText = new()
            {
                Name = "UsernameTextBlock",
                Text = "Username",
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 5)
            };

            TextBox changeUsernameTextBox = new()
            {
                Name = "ChangeUsername",
                Width = 100,
                Height = 30,
                Margin = new Thickness(5)
            };
            usernamePanel.Children.Add(usernameText);
            usernamePanel.Children.Add(changeUsernameTextBox);

            StackPanel hashtagPanel = new()
            {
                Name = "HashtagPanel",
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center
            };

            TextBlock hashtagText = new()
            {
                Name = "HashtagTextBlock",
                Text = "Hashtag",
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 5)
            };

            TextBox changeHashtagTextBox = new()
            {
                Name = "ChangeHashtag",
                Width = 100,
                Height = 30,
                Margin = new Thickness(5)
            };
            InitHashtagTextBox(changeHashtagTextBox);

            hashtagPanel.Children.Add(hashtagText);
            hashtagPanel.Children.Add(changeHashtagTextBox);

            innerStackPanel.Children.Add(usernamePanel);
            innerStackPanel.Children.Add(hashtagPanel);

            outerStackPanel.Children.Add(innerStackPanel);

            Button changeButton = new()
            {
                Content = "Change",
                Width = 100,
                Height = 30,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 20, 0, 0),
            };

            changeButton.Click += async (sender, args) =>
            {
                if (!string.IsNullOrEmpty(changeUsernameTextBox.Text) && !string.IsNullOrEmpty(changeHashtagTextBox.Text))
                {
                    await SendUsernameChangeRequestAsync(changeUsernameTextBox.Text, changeHashtagTextBox.Text);
                }
                else
                {
                    _ = DataInvalidAsync(usernameText, hashtagText);
                }
            };

            outerStackPanel.Children.Add(changeButton);
            SettingsPanel.Children.Add(outerStackPanel);
        }

        public static async Task DataInvalidAsync(TextBlock usernameText, TextBlock hashtagText)
        {
            hashtagText.Visibility = Visibility.Collapsed;
            usernameText.Text = "Cant be empty!";

            await Task.Delay(TimeSpan.FromSeconds(3));
            usernameText.Text = "Username";

        }

        public async Task AnswerToUsernameChangeAsync(UsernameUpdate usernameUpdate, UsernameUpdateResult usernameUpdateResult)
        {
            StackPanel? changeUsernamePanel = SettingsPanel.Children
                .OfType<StackPanel>().FirstOrDefault(x => x.Name == "ChangeUsernamePanel");

            if (usernameUpdateResult == UsernameUpdateResult.Successful)
            {
                Client.User.Username = usernameUpdate.Username;
                Client.User.Hashtag = usernameUpdate.Hashtag;

                SettingsPanel.Children.Remove(changeUsernamePanel);
                SettingsPanel.UpdateLayout();

                return;
            }

            if (changeUsernamePanel is not null)
            {
                StackPanel innerStackPanel = changeUsernamePanel.Children
                    .OfType<StackPanel>().First();

                StackPanel usernamePanel = innerStackPanel.Children
                    .OfType<StackPanel>().First(x => x.Name == "UsernamePanel");

                TextBlock usernameTextBlock = usernamePanel.Children
                    .OfType<TextBlock>().First(x => x.Name == "UsernameTextBlock");

                switch (usernameUpdateResult)
                {
                    case UsernameUpdateResult.OnCooldown:
                        usernameTextBlock.Text = "On cooldown";
                        break;
                    case UsernameUpdateResult.NameTaken:
                        usernameTextBlock.Text = "This username- hashtag combo is taken";
                        break;
                }

                await Task.Delay(TimeSpan.FromSeconds(3));
                usernameTextBlock.Text = "Username";
            }
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
            BlockedList.Items.Remove(BlockedList.Items.Cast<StackPanel>().FirstOrDefault(x => (x.Tag as (string, string)?) == (friend.Username, friend.Hashtag)));
            BlockedList.UpdateLayout();
        }

        private void CreateBtnsForBlockedListUI(StackPanel stackPanel, Relationship blocked, in Colors colors)
        {
            RelationshipButtonsData readdButtonData = new(blocked.Id, Blocked, RelationshipState.Pending);
            Button readdButton = new()
            {
                Content = "Re-add",
                Background = colors.Green,
                Foreground = Brushes.White,
                Width = 80,
                Height = 30,
                Margin = new Thickness(5),
                Tag = readdButtonData
            };

            RelationshipButtonsData unblockButtonData = new(blocked.Id, Blocked, RelationshipState.None);
            Button unblockButton = new()
            {
                Content = "Unblock",
                Background = colors.Gray,
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
            FriendsList.Items.Remove(FriendsList.Items.Cast<StackPanel>().FirstOrDefault(x => (x.Tag as (string, string)?) == (friend.Username, friend.Hashtag)));
            FriendsList.UpdateLayout();
        }

        private void CreateBtnsForFriendsListUI(StackPanel stackPanel, Relationship friend, in Colors colors)
        {
            RelationshipButtonsData deleteButtonData = new(friend.Id, Friends, RelationshipState.None);
            Button declineButton = new()
            {
                Content = "Delete",
                Background = colors.Red,
                Foreground = Brushes.White,
                Width = 80,
                Height = 30,
                Margin = new Thickness(5),
                Tag = deleteButtonData
            };

            RelationshipButtonsData blockButtonData = new(friend.Id, Friends, RelationshipState.Blocked);
            Button blockButton = new()
            {
                Content = "Block",
                Background = colors.Gray,
                Foreground = Brushes.White,
                Width = 80,
                Height = 30,
                Margin = new Thickness(5),
                Tag = blockButtonData
            };

            Button msgButton = new()
            {
                Width = 40,
                Height = 30,
                Background = Brushes.Transparent,
                BorderBrush = Brushes.Transparent,
                Margin = new Thickness(5),
                Tag = friend.Id
            };

            SvgViewbox svgViewbox = new()
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
            PendingList.Items.Remove(PendingList.Items.Cast<StackPanel>().FirstOrDefault(x => (x.Tag as (string, string)?) == (pending.Username, pending.Hashtag)));
            PendingList.UpdateLayout();
        }

        private void CreateBtnsForPendingListUI(StackPanel stackPanel, Relationship pendingRequest, in Colors colors)
        {
            RelationshipButtonsData acceptButtonData = new(pendingRequest.Id, Pending, RelationshipState.Friend);
            Button acceptButton = new()
            {
                Content = "Accept",
                Background = colors.Green,
                Foreground = Brushes.White,
                Width = 80,
                Height = 30,
                Margin = new Thickness(5),
                Tag = acceptButtonData
            };

            RelationshipButtonsData declineButtonData = new(pendingRequest.Id, Pending, RelationshipState.None);
            Button declineButton = new()
            {
                Content = "Decline",
                Background = colors.Red,
                Foreground = Brushes.White,
                Width = 80,
                Height = 30,
                Margin = new Thickness(5),
                Tag = declineButtonData
            };

            RelationshipButtonsData blockButtonData = new(pendingRequest.Id, Pending, RelationshipState.Blocked);
            Button blockButton = new()
            {
                Content = "Block",
                Background = colors.Gray,
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
            ClearSettingsPanel();
        }

        private void ChangePanelState(object sender, RoutedEventArgs args)
        {
            Button btn = (Button)sender;
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

            DoubleAnimation slideInAnimation = new()
            {
                From = grid.ActualWidth,
                To = 0,
                Duration = TimeSpan.FromSeconds(0.3)
            };

            translateTransform.BeginAnimation(TranslateTransform.XProperty, slideInAnimation);
        }

        private static void SlideOutAnimation(TranslateTransform translateTransform, Grid grid)
        {
            DoubleAnimation slideOutAnimation = new()
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
                StackPanel stackPanelParent = (StackPanel)button.Parent;
                ListBox listboxParent = (ListBox)stackPanelParent.Parent;

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
            Button button = (Button)sender;
            Relationship relationship = _friends.First(x => x.Id == (long)button.Tag);
            AddOneToDmList(relationship);
            CreateOrOpenChat(relationship);
        }

        private void CloseChat_Click(object sender, RoutedEventArgs args)
        {
            Button button = (Button)sender;
            StackPanel stackPanel = (StackPanel)button.Parent;
            RemoveOneFromDmList(stackPanel);
            ChatPanel.Visibility = Visibility.Collapsed;
        }

        #endregion

        #region Send Payloads

        private async Task SendChatMessageAsync(Message message)
        {
            Relationship relationship = _friends.First(x => x.Username == _currentOpenChat.Username && x.Hashtag == _currentOpenChat.Hashtag);
            var payload = new
            {
                opCode = OpCode.SendChatMessage,
                otherUserId = relationship.Id,
                message
            };

            await Client.SendPayloadAsync(payload);

            if (ChatPanel.Children.Count >= 2 && ChatPanel.Children[1] is ScrollViewer scrollViewer)
            {
                AddMessage(scrollViewer, relationship, message);
            }

            ChatDatabase chatDatabase = new();
            chatDatabase.AddMessage(relationship.Id, message);
        }

        private async void SendFriendRequestAsync(object sender, RoutedEventArgs args)
        {
            string username = AddFriendUsernameTextBox.Text;
            string hashtag = AddFriendHashtagTextBox.Text;

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(hashtag))
            {
                await DisplayInfosAddFriendPanelAsync(Brushes.Red, "The username and/or password is invalid");
                return;
            }

            if (username == Client.User.Username && hashtag == Client.User.Hashtag)
            {
                await DisplayInfosAddFriendPanelAsync(Brushes.Red, "You can´t add yourself :(");
                return;
            }

            Func<Relationship, bool> relationshipInListFunc = x => x.Username == username && x.Hashtag == hashtag;

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
                    StackPanel stackPanel = (StackPanel)item;
                    (string usernameInList, string hashtagInList) = (ValueTuple<string, string>)stackPanel.Tag;
                    if (usernameInList == username && hashtagInList == hashtag)
                    {
                        BlockedList.Items.Remove(item);
                        break;
                    }
                }
            }

            Relationship relationship = new()
            {
                Username = username,
                Hashtag = hashtag
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

        private static async Task SendUsernameChangeRequestAsync(string username, string hashtag)
        {
            UsernameUpdate usernameUpdate = new(username, hashtag, Client.User.Id);
            var payload = new
            {
                opCode = OpCode.SettingsUpdate,
                settingsUpdate = SettingsUpdate.ChangeUsername,
                usernameUpdate
            };

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

        private static StackPanel BasicUserUI(Relationship user)
        {
            StackPanel stackPanel = new()
            {
                Tag = new TagUserData(user.Username, user.Hashtag),
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(5),
            };

            Ellipse ellipse = new()
            {
                Width = 45,
                Height = 45,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 10, 0)
            };

            ImageBrush imageBrush = new()
            {
                ImageSource = user.ProfilePicture,
                Stretch = Stretch.UniformToFill,
            };

            ellipse.Fill = imageBrush;

            TextBlock textBlockUsername = new()
            {
                Text = user.Username,
                Foreground = Brushes.White,
                FontSize = 18,
                Margin = new Thickness(10),
            };

            stackPanel.Children.Add(ellipse);
            stackPanel.Children.Add(textBlockUsername);
            return stackPanel;
        }
    }
}
