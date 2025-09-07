// Patlite.Tests/PNSTests.cs
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Patlite.lib;

using Xunit;

namespace Patlite.Tests;

public class PNSTests
{
    private sealed class FakeBuilder : IPatliteCommandBuilder
    {
        private readonly byte[] _payload;
        public FakeBuilder(byte[] payload) => _payload = payload;

        public byte[] GenerateUDPMessage(PatlitePattern pattern) => _payload;

        public ValueTask<byte[]> GenerateUDPMessageAsync(PatlitePattern pattern, System.Threading.CancellationToken ct = default)
            => ValueTask.FromResult(_payload);
    }

    [Fact]
    public async Task SendPnsAsync_Sends_Bytes_And_Returns_True_On_Success()
    {
        // Arrange: bind a local UDP listener on loopback, ephemeral port
        using var listener = new UdpClient(new IPEndPoint(IPAddress.Loopback, 0));
        var endPoint = (IPEndPoint)listener.Client.LocalEndPoint!;
        var expected = new byte[] { 0x41, 0x42, 0x44, 0x00, 0x00, 0x07, 1, 2, 3, 4, 5, 6, 7 };

        var pns = new PNS(NullLogger<PNS>.Instance, new FakeBuilder(expected));
        var cfg = new PatliteConfig { ip = "127.0.0.1", port = endPoint.Port };
        var pattern = new PatlitePattern();

        // Act: send
        var ok = await pns.SendPnsAsync(cfg, pattern);

        // Assert: we should receive the exact bytes
        ok.Should().BeTrue();

        var receiveTask = listener.ReceiveAsync();
        var completed = await Task.WhenAny(receiveTask, Task.Delay(2000));
        completed.Should().Be(receiveTask, "UDP datagram should be received within timeout");

        var received = receiveTask.Result.Buffer;
        received.Should().Equal(expected);
    }

    [Fact]
    public async Task SendPnsAsync_Returns_True_When_No_Listener_Present()
    {
        var pns = new PNS(NullLogger<PNS>.Instance, new FakeBuilder(new byte[] { 1, 2, 3 }));
        var cfg = new PatliteConfig { ip = "127.0.0.2", port = 65000 }; // no listener
        var pattern = new PatlitePattern();

        var ok = await pns.SendPnsAsync(cfg, pattern);

        ok.Should().BeTrue("UDP send does not require a listener and typically succeeds");
    }
}
