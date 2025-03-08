namespace ClientMessenger
{
    public readonly record struct RelationshipButtonsData(long RelationshipId, IList<Relationship> Relationships, RelationshipState WantedState)
    {
        public void Deconstruct(out long relationshipId, out IList<Relationship> relationships, out RelationshipState relationshipState)
        {
            relationshipId = RelationshipId;
            relationships = Relationships;
            relationshipState = WantedState;
        }
    }
}
