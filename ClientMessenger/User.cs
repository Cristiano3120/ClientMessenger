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
            yield return (nameof(Username).ToCamelCase(), Username);
            yield return (nameof(HashTag).ToCamelCase(), HashTag);
            yield return (nameof(Email).ToCamelCase(), Email);
            yield return (nameof(Password).ToCamelCase(), Password);
            yield return (nameof(Biography).ToCamelCase(), Biography);
            yield return (nameof(Id).ToCamelCase(), Id.ToString());
            yield return (nameof(Birthday).ToCamelCase(), Birthday?.ToString(new CultureInfo("de-DE")) ?? "");
            yield return (nameof(ProfilePicture).ToCamelCase(), Convert.ToBase64String(Converter.ToByteArray(ProfilePicture)));
            yield return (nameof(FaEnabled).ToCamelCase(), FaEnabled.ToString());
            yield return (nameof(Token).ToCamelCase(), Token);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
