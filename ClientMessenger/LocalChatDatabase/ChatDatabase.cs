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
            if (!Directory.Exists(pathToDirectory))
            {
                Directory.CreateDirectory(pathToDirectory);
            }
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

        public Message[]? GetMessages(long id)
        {
            using (LiteDatabase database = new(_connString))
            {
                ChatInfos? chatInfos = database.GetCollection<ChatInfos>("Chats").FindOne(x => x.Members.Contains(id));
                return chatInfos.HasValue && chatInfos.Value.Messages != null
                    ? [.. chatInfos.Value.Messages]
                    : null;
            }
        }
    }
}
