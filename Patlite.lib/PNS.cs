using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Core;
using System.Net.Sockets;

namespace Patlite.lib;

public interface IPNS
{
    bool send_pns(PatliteConfig cfg, PatlitePattern pattern);
    Task<bool> SendPnsAsync(PatliteConfig cfg, PatlitePattern pattern, CancellationToken cancellationToken = default);
}

public class PNS : IPNS
{
    private readonly ILogger<PNS> _logger;
    private readonly IPatliteCommandBuilder _builder;
    public PNS(ILogger<PNS> logger, IPatliteCommandBuilder patliteCommandBuilder)
    {
        _logger = logger;
        _builder = patliteCommandBuilder;
    }
    public bool send_pns(PatliteConfig cfg, PatlitePattern pattern)
    {

        UdpClient udpClient = new UdpClient();

        try
        {
            // Convert the message string to a byte array
            var message = _builder.GenerateUDPMessage(pattern);

            // Send the data to the specified IP address and port
            udpClient.Send(message, message.Length, cfg.ip, cfg.port);
            
            _logger.LogDebug("Sent.");
            
            return true;
        }
        catch (Exception e)
        {
            _logger.LogError($"Error sending UDP packet: {e.ToString()}");
            return false;
        }
        finally
        {
            udpClient.Close();
        }
    }
    public async Task<bool> SendPnsAsync(
          PatliteConfig cfg,
          PatlitePattern pattern,
          CancellationToken cancellationToken = default)
    {
        using var udpClient = new UdpClient();
        try
        {
            var message = await _builder.GenerateUDPMessageAsync(pattern, cancellationToken);

            // UdpClient.SendAsync doesn't take a CancellationToken directly.
            await udpClient.SendAsync(message, message.Length, cfg.ip, cfg.port)
                           .WaitAsync(cancellationToken);

            _logger?.LogDebug("Sent (async).");
            return true;
        }
        catch (OperationCanceledException)
        {
            _logger?.LogWarning("Send canceled.");
            return false;
        }
        catch (Exception e)
        {
            _logger?.LogError(e, "Error sending UDP packet (async)");
            return false;
        }
    }
}
