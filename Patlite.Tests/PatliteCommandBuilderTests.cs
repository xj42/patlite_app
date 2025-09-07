// Patlite.Tests/PatliteCommandBuilderTests.cs
using FluentAssertions;
using Patlite.lib;
using Xunit;

namespace Patlite.Tests;

public class PatliteCommandBuilderTests
{
    [Fact]
    public void GenerateUDPMessage_Should_Build_13Byte_Frame_In_Correct_Order()
    {
        var builder = new PatliteCommandBuilder();
        var pattern = new PatlitePattern
        {
            Tier1 = La6Colour.Red,
            Tier2 = La6Colour.Green,
            Tier3 = La6Colour.Blue,
            Tier4 = La6Colour.White,
            Tier5 = La6Colour.Off,
            Flash = Flash.On,
            Buzzer = BuzzerPattern.Pattern3
        };

        var bytes = builder.GenerateUDPMessage(pattern);

        bytes.Should().HaveCount(13, "protocol is 13 bytes (AB D 00 00 07 + 7 data)");
        bytes.Should().Equal(new byte[]
        {
            (byte)Command.AHeader,           // 0x41
            (byte)Command.BHeader,           // 0x42
            (byte)Command.DMotion,           // 0x44
            (byte)Command.Reserved,          // 0x00
            (byte)Command.Reserved,          // 0x00
            (byte)Command.DataLength,        // 0x07
            (byte)La6Colour.Red,
            (byte)La6Colour.Green,
            (byte)La6Colour.Blue,
            (byte)La6Colour.White,
            (byte)La6Colour.Off,
            (byte)Flash.On,
            (byte)BuzzerPattern.Pattern3
        });
    }
}
