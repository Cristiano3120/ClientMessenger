using System.Windows.Media;

namespace ClientMessenger
{
    public readonly ref struct Colors()
    {
        public SolidColorBrush Green { get; } = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#288444"));
        public SolidColorBrush LightGray { get; } = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#7c7e84"));
        public SolidColorBrush Gray { get; } = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#302c34"));
        public SolidColorBrush DarkerGray { get; } = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#343234"));
        public SolidColorBrush DarkGray { get; } = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1f1e1f"));
        public SolidColorBrush Red { get; } = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#f44038"));
    }
}
