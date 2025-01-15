using System.Collections;
using System.Globalization;
using System.Windows.Media.Imaging;

namespace ClientMessenger
{
    public sealed class User : IEnumerable<(string name, string value)>
    {
        public BitmapImage ProfilePicture { get; set; } = new();
        public string Username { get; set; } = "";
        public string HashTag { get; set; } = "";
        public string Email { get; init; } = "";
        public string Password { get; init; } = "";
        public string Biography { get; set; } = "";
        public long Id { get; init; } = -1;
        public DateOnly? Birthday { get; init; } = null;

        public IEnumerator<(string name, string value)> GetEnumerator()
        {
            yield return (nameof(Username), Username);
            yield return (nameof(HashTag), HashTag);
            yield return (nameof(Email), Email);
            yield return (nameof(Password), Password);
            yield return (nameof(Biography), Biography);
            yield return (nameof(Id), Id.ToString());
            yield return (nameof(Birthday), Birthday?.ToString(new CultureInfo("de-DE")) ?? "");
            yield return (nameof(ProfilePicture), Convert.ToBase64String(Converter.ToByteArray(ProfilePicture)));
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
