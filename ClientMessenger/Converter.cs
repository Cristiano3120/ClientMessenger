﻿using System.Windows.Media.Imaging;
using System.Drawing;
using System.IO;

namespace ClientMessenger
{
    internal static class Converter
    {
        public static BitmapImage ToBitmapImage(Bitmap bitmap)
        {
            using var memoryStream = new MemoryStream();
            bitmap.Save(memoryStream, System.Drawing.Imaging.ImageFormat.Png);
            memoryStream.Seek(0, SeekOrigin.Begin);

            var bitmapImage = new BitmapImage();
            bitmapImage.BeginInit();
            bitmapImage.StreamSource = memoryStream;
            bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
            bitmapImage.EndInit();

            return bitmapImage;
        }

        public static BitmapImage ToBitmapImage(byte[] bytes)
        {
            if (bytes.Length == 0)
                bytes = File.ReadAllBytes(ClientUI.DefaultProfilPic);

            var bitmapImage = new BitmapImage();
            bitmapImage.BeginInit();
            bitmapImage.StreamSource = new MemoryStream(bytes);
            bitmapImage.EndInit();
            bitmapImage.Freeze();
            return bitmapImage;
        }

        public static byte[] ToByteArray(BitmapImage bitmapImage)
        {
            if (bitmapImage == null)
                return [];

            using var memoryStream = new MemoryStream();
            var encoder = new BmpBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(bitmapImage));
            encoder.Save(memoryStream);

            return memoryStream.ToArray();
        }
    }
}
