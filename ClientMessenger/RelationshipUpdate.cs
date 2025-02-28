﻿using System.Text.Json.Serialization;

namespace ClientMessenger
{
    internal sealed record RelationshipUpdate()
    {
        [JsonPropertyName("user")]
        public required User User { get; init; }
        [JsonPropertyName("relationship")]
        public required Relationship Relationship { get; init; }
        [JsonPropertyName("requestedRelationshipstate")]
        public required Relationshipstate RequestedRelationshipstate { get; init; }
    }
}
