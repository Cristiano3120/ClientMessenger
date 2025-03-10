using System.Windows.Media.Imaging;

namespace ClientMessenger.Chat
{
    public record struct MessageSender(string Username, BitmapImage ProfilPicture)
    {

    }
}
