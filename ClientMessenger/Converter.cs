using System.Windows.Media.Imaging;
using System.Drawing;
using System.IO;
using System.Drawing.Imaging;

namespace ClientMessenger
{
    internal static class Converter
    {
        public static BitmapImage ToBitmapImage(Bitmap bitmap)
        {
            using (MemoryStream memoryStream = new())
            {
                bitmap.Save(memoryStream, ImageFormat.Png);
                memoryStream.Seek(0, SeekOrigin.Begin);

                BitmapImage bitmapImage = new();
                bitmapImage.BeginInit();
                bitmapImage.StreamSource = memoryStream;
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapImage.EndInit();

                return bitmapImage;
            }
        }

        public static BitmapImage ToBitmapImage(byte[] bytes)
        {
            if (bytes.Length == 0)
                bytes = File.ReadAllBytes(ClientUI.GetDefaultProfilPic());

            BitmapImage bitmapImage = new();
            bitmapImage.BeginInit();
            bitmapImage.StreamSource = new MemoryStream(bytes);
            bitmapImage.EndInit();
            bitmapImage.Freeze();
            return bitmapImage;
        }

        public static byte[] ToByteArray(BitmapImage? bitmapImage)
        {
            if (bitmapImage is null)
                return [];

            using (MemoryStream memoryStream = new())
            {
                BmpBitmapEncoder encoder = new();
                encoder.Frames.Add(BitmapFrame.Create(bitmapImage));
                encoder.Save(memoryStream);

                return memoryStream.ToArray();
            }
        }
    }
}
