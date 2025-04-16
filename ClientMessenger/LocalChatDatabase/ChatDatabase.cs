using System.IO;
using LiteDB;

namespace ClientMessenger.LocalChatDatabase
{
    public class ChatDatabase
    {
        private const string _chatCollectionName = "Chats";
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
                database.DropCollection(_chatCollectionName);
            }
        }

        public void AddChats(List<ChatInfos> chat)
        {
            using (LiteDatabase database = new(_connString))
            {
                database.GetCollection<ChatInfos>(_chatCollectionName).InsertBulk(chat);
            }
        }

        /// <summary>
        /// <c>Id</c> has to be the id of the other chat user
        /// </summary>
        /// <param name="id"></param>
        /// <param name="message"></param>
        public void AddMessage(long id, Message message)
        {
            using LiteDatabase database = new(_connString);
            ILiteCollection<ChatInfos> chats = database.GetCollection<ChatInfos>(_chatCollectionName);

            long userId = Client.User.Id;
            ChatInfos? chatInfos = chats.FindOne(x => x.Members.Contains(userId) && x.Members.Contains(id));

            if (!chatInfos.HasValue || chatInfos is null 
                || chatInfos.Value.Messages is null || chatInfos.Value.Members is null)
            {
                ChatInfos newChat = new()
                {
                    Members = new List<long> { Client.User.Id, id },
                    Messages = [message]
                };
                chats.Insert(newChat);
            }
            else
            {
                chatInfos.Value.Messages.Add(message);
                chats.Update(chatInfos.Value);
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
                ChatInfos? chatInfos = database.GetCollection<ChatInfos>(_chatCollectionName).FindOne(x => x.Members.Contains(id));
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
                ChatInfos? chatInfos = database.GetCollection<ChatInfos>(_chatCollectionName).FindOne(x => x.Members.Contains(clientID)
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
                ChatInfos? chatInfos = database.GetCollection<ChatInfos>(_chatCollectionName).FindOne(x => x.Members.Contains(clientID)
                    && x.Members.Contains(id));

                if (chatInfos?.Messages is { Count: > 0 } messages)
                {
                    IEnumerable<Message> filteredMessages = messages.SkipWhile(m => m != lastMessage);

                    if (!filteredMessages.Any())
                    {
                        return lastMessage;
                    }

                    return filteredMessages.Count() > 1
                        ? filteredMessages.ElementAt(1)
                        : filteredMessages.First();
                }

                return lastMessage;
            }
        }

        public void DeleteMessage(long id, Guid guid)
        {
            using (LiteDatabase database = new(_connString))
            {
                long clientID = Client.User.Id;
                ILiteCollection<ChatInfos> collection = database.GetCollection<ChatInfos>(_chatCollectionName);

                ChatInfos? chatInfos = collection.FindOne(x =>
                    x.Members.Contains(clientID) && x.Members.Contains(id));

                if (chatInfos.HasValue && chatInfos is { Messages.Count: > 0 })
                {
                    int removed = chatInfos.Value.Messages.RemoveAll(x => x.Guid == guid);

                    if (removed > 0)
                    {
                        collection.Update(chatInfos.Value);
                    }
                }
            }
        }

        /// <summary>
        /// Id has to be the id of the other chat user
        /// </summary>
        public Message GetMessage(long id, Guid guid)
        {
            using (LiteDatabase database = new(_connString))
            {
                long clientID = Client.User.Id;
                ChatInfos? chatInfos = database.GetCollection<ChatInfos>(_chatCollectionName)
                    .FindOne(x => x.Members.Contains(clientID)
                        && x.Members.Contains(id));

                if (chatInfos.HasValue && chatInfos.Value.Messages is { Count: > 0 } messages)
                {
                    return messages.First(x => x.Guid == guid);
                }

                throw new Exception("Message not found even tho there should be in the Db");
            }
        }

        public static string CombineIds(long[] ids)
        {
            Array.Sort(ids);
            return string.Join("-", ids);
        }
    }
}
