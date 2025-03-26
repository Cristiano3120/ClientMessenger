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
            yield return (nameof(ProfilePicture).ToCamelCase(), Convert.ToBase64String(Converter.ToByteArray(ProfilePicture)));
            yield return (nameof(RelationshipState).ToCamelCase(), RelationshipState.ToString());
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        #endregion

        #region == Operator override
        public static bool operator ==(Relationship? left, TagUserData right)
        {
            if (left is null)
                return false;

            return left.Username == right.Username &&
                left.Hashtag == right.Hashtag;
        }

        public static bool operator !=(Relationship? left, TagUserData right)
        {
            return !(left == right);
        }

        public static bool operator ==(TagUserData left, Relationship? right)
        {
            if (right is null)
                return false;

            return left.Username == right.Username &&
                left.Hashtag == right.Hashtag;
        }

        public static bool operator !=(TagUserData left, Relationship? right)
        {
            return !(left == right);
        }

        public override bool Equals(object? obj)
        {
            return base.Equals(obj);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        #endregion
    }
}
