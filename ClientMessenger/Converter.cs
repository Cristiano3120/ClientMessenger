using System.IO;
using System.Windows.Media.Imaging;

namespace ClientMessenger
{
    internal static class Converter
    {
        public static BitmapImage ToBitmapImage(byte[] bytes)
        {
            if (bytes.Length == 0)
                bytes = File.ReadAllBytes(@"C:\Users\Crist\source\repos\ClientMessenger\ClientMessenger\Images\profilPic.png");

            var bitmapImage = new BitmapImage();
            bitmapImage.BeginInit();
            bitmapImage.StreamSource = new MemoryStream(bytes);
            bitmapImage.EndInit();
            return bitmapImage;
        }

        public static byte[] ToByteArray(BitmapImage bitmapImage)
        {
            if (bitmapImage == null)
                return [];

            using (var memoryStream = new MemoryStream())
            {
                var encoder = new BmpBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(bitmapImage));
                encoder.Save(memoryStream);

                return memoryStream.ToArray();
            }
        }
    }
}
