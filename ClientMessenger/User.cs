using LiteDB;
using System.Collections;
using System.Globalization;
using System.Windows.Media.Imaging;

namespace ClientMessenger
{
    public sealed class User : IEnumerable<(string name, string value)>
    {
        [BsonId]
        public long Id { get; init; }
        public BitmapImage? ProfilePicture { get; set; }
        public string Username { get; set; }
        public string HashTag { get; set; }
        public string Email { get; init; }
        public string Password { get; init; }
        public string Biography { get; set; }
        public DateOnly? Birthday { get; init; }
        public bool FaEnabled {  get; set; } 
        public string Token { get; set; }

        public User()
        {
            Id = -1;
            ProfilePicture = null;
            Username = "";
            HashTag = "";
            Email = "";
            Password = "";
            Biography = "";
            Birthday = null;
            Token = "";
        }

        public IEnumerator<(string name, string value)> GetEnumerator()
        {
            yield return (nameof(Username), Username);
            yield return (nameof(HashTag), HashTag);
            yield return (nameof(Email), Email);
            yield return (nameof(Password), Password);
            yield return (nameof(Biography), Biography);
            yield return (nameof(Id), Id.ToString());
            yield return (nameof(Birthday), Birthday?.ToString(new CultureInfo("de-DE")) ?? "");
            if (ProfilePicture != null)
            {
                yield return (nameof(ProfilePicture), Convert.ToBase64String(Converter.ToByteArray(ProfilePicture)));
            }
            yield return (nameof(FaEnabled), FaEnabled.ToString());
            yield return (nameof(Token), Token);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
