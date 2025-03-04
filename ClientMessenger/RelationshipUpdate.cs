using System.Text.Json.Serialization;

namespace ClientMessenger
{
    internal readonly record struct RelationshipUpdate()
    {
        [JsonPropertyName("userId")]
        public required long UserId { get; init; }

        [JsonPropertyName("relationship")]
        public required Relationship Relationship { get; init; }

        [JsonPropertyName("requestedRelationshipState")]
        public required RelationshipState RequestedRelationshipState { get; init; }
    }
}
