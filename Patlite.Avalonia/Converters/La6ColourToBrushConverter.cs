using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;
using Patlite.lib;

namespace Patlite.Avalonia.Converters;

public sealed class La6ColourToBrushConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not La6Colour c)
        {
            return Brushes.Transparent;
        }

        return c switch
        {
            La6Colour.Off => Brushes.Gray,
            La6Colour.Red => Brushes.Red,
            La6Colour.Amber => Brushes.Orange,
            La6Colour.Lemon => Brushes.Yellow,
            La6Colour.Green => Brushes.Green,
            La6Colour.SkyBlue => Brushes.SkyBlue,
            La6Colour.Blue => Brushes.Blue,
            La6Colour.Purple => Brushes.Purple,
            La6Colour.Pink => Brushes.Pink,
            La6Colour.White => Brushes.White,
            _ => Brushes.Transparent
        };
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
