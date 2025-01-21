using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace ClientMessenger
{
    internal static class ImageEditor
    {
        public static BitmapImage ScaleImage(string filepath)
        {
            using (Image originalImage = Image.FromFile(filepath))
            {
                const int ellipseSize = 150;

                using (Bitmap result = new(ellipseSize, ellipseSize))
                {
                    using (Graphics graphics = Graphics.FromImage(result))
                    {
                        graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                        graphics.CompositingQuality = CompositingQuality.HighQuality;
                        graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
                        graphics.SmoothingMode = SmoothingMode.HighQuality;

                        graphics.Clear(System.Drawing.Color.Transparent);
                        
                        using (GraphicsPath ellipsePath = new())
                        {
                            ellipsePath.AddEllipse(0, 0, ellipseSize, ellipseSize);
                            graphics.SetClip(ellipsePath);
                        }

                        graphics.DrawImage(originalImage, 0, 0, ellipseSize, ellipseSize);
                    }

                    return Converter.ToBitmapImage(result);
                }
            }
        }

        #region Create ImageBrush
       
        public static ImageBrush CreateImageBrush(string filepath)
        {
            BitmapImage bitmap = new(new Uri(filepath));
            return new ImageBrush(bitmap);
        }

        public static ImageBrush CreateImageBrush(BitmapImage image)
            => new(image);

        #endregion
    }
}
