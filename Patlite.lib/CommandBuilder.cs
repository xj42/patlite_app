

namespace Patlite.lib;
public interface IPatliteCommandBuilder
{
    byte[] GenerateUDPMessage(PatlitePattern pattern);
    ValueTask<byte[]> GenerateUDPMessageAsync(
    PatlitePattern pattern,
    CancellationToken cancellationToken = default);
}
public sealed class PatliteCommandBuilder : IPatliteCommandBuilder
{
    public byte[] GenerateUDPMessage(PatlitePattern pattern)
    {
        // 13 bytes total: 'A','B','D', 0x00,0x00, 0x07, then 7 data bytes
        return new byte[13]
        {
            (byte)Command.AHeader,
            (byte)Command.BHeader,
            (byte)Command.DMotion,
            (byte)Command.Reserved,
            (byte)Command.Reserved,
            (byte)Command.DataLength,
            (byte)pattern.Tier1,
            (byte)pattern.Tier2,
            (byte)pattern.Tier3,
            (byte)pattern.Tier4,
            (byte)pattern.Tier5,
            (byte)pattern.Flash,
            (byte)pattern.Buzzer
        };
    }
    public ValueTask<byte[]> GenerateUDPMessageAsync(
      PatlitePattern pattern,
      CancellationToken cancellationToken = default)
    {
        // No I/O here; return an already-completed ValueTask.
        // Respect cancellation early if you want:
        if (cancellationToken.IsCancellationRequested)
            return ValueTask.FromCanceled<byte[]>(cancellationToken);

        var bytes = GenerateUDPMessage(pattern);
        return ValueTask.FromResult(bytes);
    }
}
