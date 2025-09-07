// Patlite.Tests/MainVMTests.cs
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Patlite.lib;
using Xunit;

namespace Patlite.Tests;

public class MainVMTests
{
    private sealed class FakePns : IPNS
    {
        public PatliteConfig? LastConfig;
        public PatlitePattern? LastPattern;

        public bool send_pns(PatliteConfig cfg, PatlitePattern pattern)
        {
            LastConfig = cfg;
            LastPattern = pattern;
            return true;
        }

        public Task<bool> SendPnsAsync(PatliteConfig cfg, PatlitePattern pattern, CancellationToken cancellationToken = default)
        {
            LastConfig = cfg;
            LastPattern = pattern;
            return Task.FromResult(true);
        }
    }

    [Fact]
    public async Task SendCommand_Sends_CurrentPattern_And_Endpoint()
    {
        var fake = new FakePns();
        var vm = new Patlite.MainVM(NullLogger<Patlite.MainVM>.Instance, fake);

        // Match your XAML: set endpoint & some colours
        vm.Ip = "127.0.0.1";
        vm.Port = "12000";
        vm.Tier1 = La6Colour.Green;
        vm.Tier2 = La6Colour.Red;
        vm.Tier3 = La6Colour.Blue;
        vm.Tier4 = La6Colour.White;
        vm.Tier5 = La6Colour.Off;
        vm.Flash = Flash.On;
        vm.Buzzer = BuzzerPattern.Pattern4;

        vm.SendCommand.CanExecute(null).Should().BeTrue();
        vm.SendCommand.Execute(null);

        // Allow the async command to run
        await Task.Delay(50);

        fake.LastConfig.Should().NotBeNull();
        fake.LastConfig!.Value.ip.Should().Be("127.0.0.1");
        fake.LastConfig!.Value.port.Should().Be(12000);

        fake.LastPattern.Should().NotBeNull();
        fake.LastPattern!.Tier1.Should().Be(La6Colour.Green);
        fake.LastPattern!.Tier2.Should().Be(La6Colour.Red);
        fake.LastPattern!.Tier3.Should().Be(La6Colour.Blue);
        fake.LastPattern!.Tier4.Should().Be(La6Colour.White);
        fake.LastPattern!.Tier5.Should().Be(La6Colour.Off);
        fake.LastPattern!.Flash.Should().Be(Flash.On);
        fake.LastPattern!.Buzzer.Should().Be(BuzzerPattern.Pattern4);
    }

    [Fact]
    public async Task OffCommand_Sends_AllOff()
    {
        var fake = new FakePns();
        var vm = new Patlite.MainVM(NullLogger<Patlite.MainVM>.Instance, fake);

        vm.Ip = "127.0.0.1";
        vm.Port = "10001";

        vm.OffCommand.CanExecute(null).Should().BeTrue();
        vm.OffCommand.Execute(null);
        await Task.Delay(50);

        fake.LastConfig.Should().NotBeNull();
        fake.LastConfig!.Value.ip.Should().Be("127.0.0.1");
        fake.LastConfig!.Value.port.Should().Be(10001);

        fake.LastPattern.Should().NotBeNull();
        var p = fake.LastPattern!;
        p.Tier1.Should().Be(La6Colour.Off);
        p.Tier2.Should().Be(La6Colour.Off);
        p.Tier3.Should().Be(La6Colour.Off);
        p.Tier4.Should().Be(La6Colour.Off);
        p.Tier5.Should().Be(La6Colour.Off);
        p.Flash.Should().Be(Flash.Off);
        p.Buzzer.Should().Be(BuzzerPattern.Off);
    }

    [Theory]
    [InlineData("", "1000")]
    [InlineData("127.0.0.1", "0")]
    [InlineData("127.0.0.1", "70000")]
    [InlineData("127.0.0.1", "abc")]
    public void SendCommand_CanExecute_Should_Fail_When_Endpoint_Invalid(string ip, string port)
    {
        var fake = new FakePns();
        var vm = new Patlite.MainVM(NullLogger<Patlite.MainVM>.Instance, fake)
        {
            Ip = ip,
            Port = port
        };

        vm.SendCommand.CanExecute(null).Should().BeFalse();
    }
}
