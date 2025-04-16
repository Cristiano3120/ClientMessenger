using System.Text.Json.Serialization;
using System.Windows.Media.Imaging;

namespace ClientMessenger
{
    public sealed class Relationship
    {
        [JsonConverter(typeof(JsonConverters.Base64ByteArrayJsonConverter))]
        public BitmapImage? ProfilePicture { get; set; }
        public RelationshipState RelationshipState { get; set; }
        public string Biography { get; set; } = "";
        public string Username { get; set; } = "";
        public string Hashtag { get; set; } = "";
        public long Id { get; init; } = -1;

        #region Operator override
        public static bool operator ==(Relationship? left, TagUserData right)
            => left?.Equals(right) ?? false;
        
        public static bool operator !=(Relationship? left, TagUserData right)
            => !(left == right);
        
        public static bool operator ==(TagUserData left, Relationship? right)
            => right?.Equals(left) ?? false;
        
        public static bool operator !=(TagUserData left, Relationship? right)
            => !(left == right);
        
        public bool Equals(TagUserData tagUserData)
            => Username == tagUserData.Username 
                && Hashtag == tagUserData.Hashtag;
        
        public override bool Equals(object? obj)
            => obj is TagUserData tagUserData 
            ? Equals(tagUserData) 
            : base.Equals(obj);
        
        public override int GetHashCode()
            => base.GetHashCode();
        
        #endregion
    }
}
