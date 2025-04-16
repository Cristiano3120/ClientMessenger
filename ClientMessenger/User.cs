using System.ComponentModel;
using System.Text.Json.Serialization;
using System.Windows.Media.Imaging;

namespace ClientMessenger
{
    public sealed class User : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;   

        #region ProfilePicture

        private BitmapImage? _profilPicture;
        [JsonConverter(typeof(JsonConverters.Base64ByteArrayJsonConverter))]
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
        public long Id { get; init; }
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

        private void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
