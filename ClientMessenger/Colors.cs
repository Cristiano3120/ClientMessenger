using System.Windows.Media;

namespace ClientMessenger
{
    public readonly ref struct Colors()
    {
        public Color Green { get; } = (Color)ColorConverter.ConvertFromString("#288444");
        public Color LightGray { get; } = (Color)ColorConverter.ConvertFromString("#7c7e84");
        public Color Gray { get; } = (Color)ColorConverter.ConvertFromString("#302c34");
        public Color DarkerGray { get; } = (Color)ColorConverter.ConvertFromString("#343234");
        public Color DarkGray { get; } = (Color)ColorConverter.ConvertFromString("#1f1e1f");
        public Color Red { get; } = (Color)ColorConverter.ConvertFromString("#f44038");

        public readonly SolidColorBrush ColorToSolidColorBrush(Color color)
             => new(color);
    }
}
