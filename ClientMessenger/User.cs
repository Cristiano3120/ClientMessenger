using System.Collections;
using System.ComponentModel;
using System.Globalization;
using System.Windows;
using System.Windows.Media.Imaging;

namespace ClientMessenger
{
    public sealed class User : IEnumerable<(string name, string value)>, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;
        public long Id { get; init; }

        #region ProfilePicture

        private BitmapImage? _profilPicture;
        public BitmapImage? ProfilePicture
        {
            get => _profilPicture;
            set
            {
                _profilPicture = value;
                OnPropertyChanged(nameof(ProfilePicture));
            }
        }

        #endregion

        #region Username
        private string _username;
        public string Username
        {
            get => _username;
            set
            {
                if (_username != value)
                {
                    _username = value;
                    OnPropertyChanged(nameof(Username));
                }
            }
        }

        #endregion

        public string Hashtag { get; set; }
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
            Hashtag = "";
            Email = "";
            Password = "";
            Biography = "";
            Birthday = null;
            Token = "";
            _username = "";
        }

        public static explicit operator Relationship(User? user)
        {
            ArgumentNullException.ThrowIfNull(user, nameof(user));
            return new()
            {
                Username = user.Username,
                Hashtag = user.Hashtag,
                Id = user.Id,
                Biography = user.Biography,
                ProfilePicture = user.ProfilePicture,
            };
        }

        #region IEnumerable

        public IEnumerator<(string name, string value)> GetEnumerator()
        {
            yield return (nameof(Username).ToCamelCase(), Username);
            yield return (nameof(Hashtag).ToCamelCase(), Hashtag);
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

        #endregion
        private void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
