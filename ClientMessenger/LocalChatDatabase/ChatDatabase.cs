using System.IO;
using LiteDB;

namespace ClientMessenger.LocalChatDatabase
{
    public class ChatDatabase
    {
        private readonly string _connString;

        public ChatDatabase()
        {
            string pathToDirectory = Client.GetDynamicPath("LocalChatDatabase");
            Directory.CreateDirectory(pathToDirectory);
            _connString = Client.GetDynamicPath("LocalChatDatabase/ChatDatabase.db");
        }

        public void DeleteChats()
        {
            using (LiteDatabase database = new(_connString))
            {
                database.DropCollection("Chats");
            }
        }

        public void AddChats(List<ChatInfos> chat)
        {
            using (LiteDatabase database = new(_connString))
            {
                database.GetCollection<ChatInfos>("Chats").InsertBulk(chat);
            }
        }

        /// <summary>
        /// <c>Id</c> has to be the id of the other chat user
        /// </summary>
        /// <param name="id"></param>
        /// <param name="message"></param>
        public void AddMessage(long id, Message message)
        {
            using (LiteDatabase database = new(_connString))
            {
                ILiteCollection<ChatInfos> chats = database.GetCollection<ChatInfos>("Chats");
                ChatInfos? chatInfos = chats.FindOne(x => x.Members.Contains(id));

                if (!chatInfos.HasValue)
                {
                    chatInfos = new ChatInfos
                    {
                        Members = new List<long> { Client.User.Id, id },
                        Messages = new List<Message> { message }
                    };
                    chats.Insert(chatInfos.Value);
                }
                else
                {
                    ChatInfos chatInfosValue = chatInfos.Value;
                    chatInfosValue.Messages ??= new List<Message>();
                    chatInfosValue.Messages.Add(message);

                    chats.DeleteMany(x => x.Members.Contains(id));
                    chats.Insert(chatInfosValue);
                }
            }
        }

        /// <summary>
        /// Gets the first 10 messages from the DB.
        /// Searches by the id of the other chat user.
        /// </summary>
        public Message[]? GetMessages(long id)
        {
            using (LiteDatabase database = new(_connString))
            {
                ChatInfos? chatInfos = database.GetCollection<ChatInfos>("Chats").FindOne(x => x.Members.Contains(id));
                return chatInfos.HasValue && chatInfos.Value.Messages is not null 
                    ? [.. chatInfos.Value.Messages.TakeLast(10)] 
                    : null;
            }
        }

        public Message? GetNextMessage(Message lastMessage, long id)
        {
            using (LiteDatabase database = new(_connString))
            {
                long clientID = Client.User.Id;
                ChatInfos? chatInfos = database.GetCollection<ChatInfos>("Chats").FindOne(x => x.Members.Contains(clientID) 
                    && x.Members.Contains(id));

                if (chatInfos.HasValue && chatInfos.Value.Messages is not null)
                {
                    bool messageFound = false;
                    foreach (Message message in chatInfos.Value.Messages.Reverse<Message>())
                    {
                        if (messageFound)
                            return message;

                        if (message == lastMessage)
                            messageFound = true;
                    }
                }
                return null;
            }
        }

        public Message GetLastLoadedMessage(Message lastMessage, long id)
        {
            using (LiteDatabase database = new(_connString))
            {
                long clientID = Client.User.Id;
                ChatInfos? chatInfos = database.GetCollection<ChatInfos>("Chats").FindOne(x => x.Members.Contains(clientID)
                    && x.Members.Contains(id));

                if (chatInfos?.Messages is { Count: > 0 } messages)
                {
                    IEnumerable<Message> filterdMessages = messages.SkipWhile(m => m != lastMessage);
                    return filterdMessages.Count() > 1 
                        ? filterdMessages.ElementAt(1) 
                        : filterdMessages.First();
                }

                throw new Exception("No message to load");
            }
        }
    }
}
