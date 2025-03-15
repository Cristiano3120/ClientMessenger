﻿using System.Windows.Media;

namespace ClientMessenger
{
    public ref struct Colors()
    {
        public Color Green { get; private set; } = (Color)ColorConverter.ConvertFromString("#288444");
        public Color Gray { get; private set; } = (Color)ColorConverter.ConvertFromString("#302c34");
        public Color Red { get; private set; } = (Color)ColorConverter.ConvertFromString("#f44038");

        public readonly SolidColorBrush ColorToSolidColorBrush(Color color)
        {
            return new SolidColorBrush(color);
        }
    }
}
