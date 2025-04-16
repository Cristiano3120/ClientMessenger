using System.Text.Json.Serialization;
using LiteDB;

namespace ClientMessenger
{
    public record struct ChatInfos
    {
        [BsonId]
        [JsonIgnore]
        public ObjectId Id { get; set; }
        public List<Message> Messages { get; set; }
        public List<long> Members { get; init; }

        public ChatInfos() : this(new List<long>(), new List<Message>())
        {
            Id = ObjectId.NewObjectId();
        }

        public ChatInfos(List<long> members, List<Message> messages)
        {
            Members = members;
            Messages = messages;
            Id = ObjectId.NewObjectId();
        }
    }
}
