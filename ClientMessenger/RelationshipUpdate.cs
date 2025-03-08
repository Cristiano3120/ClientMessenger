using System.Text.Json.Serialization;

namespace ClientMessenger
{
    internal readonly record struct RelationshipUpdate()
    {
        [JsonPropertyName("user")]
        public required User User { get; init; }

        [JsonPropertyName("relationship")]
        public required Relationship? Relationship { get; init; }

        [JsonPropertyName("requestedRelationshipState")]
        public required RelationshipState RequestedRelationshipState { get; init; }
    }
}
