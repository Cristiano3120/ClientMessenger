using System.Collections.Concurrent;

namespace ClientMessenger
{
    public readonly record struct RelationshipButtonsData(long RelationshipId, List<Relationship> Relationships, RelationshipState WantedState)
    {
        public void Deconstruct(out long relationshipId, out List<Relationship> relationships, out RelationshipState relationshipState)
        {
            relationshipId = RelationshipId;
            relationships = Relationships;
            relationshipState = WantedState;
        }
    }
}
