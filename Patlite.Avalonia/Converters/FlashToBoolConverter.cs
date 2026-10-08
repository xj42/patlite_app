using System.Globalization;
using Avalonia.Data.Converters;
using Patlite.lib;

namespace Patlite.Avalonia.Converters;

public sealed class FlashToBoolConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is Flash.On;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
