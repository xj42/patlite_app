using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using Patlite.lib;
namespace Patlite
{
    public sealed class La6ColourToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is La6Colour c)
            {
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
            return Brushes.Transparent;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }
}
