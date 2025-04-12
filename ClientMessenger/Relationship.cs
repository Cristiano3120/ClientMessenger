using System.Collections;
using System.Windows.Media.Imaging;

namespace ClientMessenger
{
    public sealed class Relationship: IEnumerable<(string name, string value)>
    {
        public long Id { get; init; } = -1;
        public BitmapImage? ProfilePicture { get; set; }
        public string Username { get; set; } = "";
        public string Hashtag { get; set; } = "";
        public string Biography { get; set; } = "";
        public RelationshipState RelationshipState { get; set; }

        #region IEnumerable
        public IEnumerator<(string name, string value)> GetEnumerator()
        {
            yield return (nameof(Username).ToCamelCase(), Username);
            yield return (nameof(Hashtag).ToCamelCase(), Hashtag);
            yield return (nameof(Biography).ToCamelCase(), Biography);
            yield return (nameof(Id).ToCamelCase(), Id.ToString());
            yield return (nameof(ProfilePicture).ToCamelCase(), Convert.ToBase64String(BitmapImageConverter.ToByteArray(ProfilePicture)));
            yield return (nameof(RelationshipState).ToCamelCase(), RelationshipState.ToString());
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        
        #endregion

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
