// Patlite.Tests/ConverterTests.cs
using System.Globalization;
using System.Windows.Media;
using FluentAssertions;
using Patlite.lib;

using Xunit;

namespace Patlite.Tests;

public class ConverterTests
{
    [Fact]
    public void InverseBoolConverter_Should_Invert_Bool()
    {
        var c = new Patlite.InverseBoolConverter();
        c.Convert(true, typeof(bool), null, CultureInfo.InvariantCulture).Should().Be(false);
        c.Convert(false, typeof(bool), null, CultureInfo.InvariantCulture).Should().Be(true);
    }

    [Theory]
    [InlineData(La6Colour.Off, "Gray")]
    [InlineData(La6Colour.Red, "Red")]
    [InlineData(La6Colour.Amber, "Orange")]
    [InlineData(La6Colour.Lemon, "Yellow")]
    [InlineData(La6Colour.Green, "Green")]
    [InlineData(La6Colour.SkyBlue, "SkyBlue")]
    [InlineData(La6Colour.Blue, "Blue")]
    [InlineData(La6Colour.Purple, "Purple")]
    [InlineData(La6Colour.Pink, "Pink")]
    [InlineData(La6Colour.White, "White")]
    public void La6ColourToBrushConverter_Should_Map_Enum_To_Brush(La6Colour c, string brushName)
    {
        var conv = new Patlite.La6ColourToBrushConverter();
        var brush = conv.Convert(c, typeof(Brush), null, CultureInfo.InvariantCulture);

        brush.Should().BeOfType<SolidColorBrush>();
        ((SolidColorBrush)brush).Color.Should().Be(((SolidColorBrush)typeof(Brushes)
            .GetProperty(brushName)!.GetValue(null)!).Color);
    }
}
