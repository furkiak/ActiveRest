using Avalonia.Data.Converters;
using Avalonia.Media;
using System;
using System.Globalization;

namespace ActiveRest.Converters
{
    /// <summary>Maps a hue (0-360) to a solid brush; configurable saturation/lightness/alpha give "strong" and "soft" variants.</summary>
    public sealed class HueBrushConverter : IValueConverter
    {
        public double Saturation { get; set; } = 0.65;
        public double Lightness { get; set; } = 0.58;
        public byte Alpha { get; set; } = 255;

        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            double hue = value is double d ? d : 0;
            var color = new HslColor(Alpha / 255.0, hue, Saturation, Lightness).ToRgb();
            return new SolidColorBrush(color);
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
            throw new NotSupportedException();
    }
}
