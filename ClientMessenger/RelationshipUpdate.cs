namespace ClientMessenger
{
    internal readonly record struct RelationshipUpdate()
    {
        public required User User { get; init; }

        public required Relationship? Relationship { get; init; }

        public required RelationshipState RequestedRelationshipState { get; init; }
    }
}
