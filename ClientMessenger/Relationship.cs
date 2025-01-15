using System.Windows.Media.Imaging;

namespace ClientMessenger
{
    public sealed record Relationship
    {
        public required string Username { get; init; } = "";
        public required BitmapImage ProfilPicture { get; init; }
        public required Relationshipstate Relationshipstate { get; init; }
    }
}
