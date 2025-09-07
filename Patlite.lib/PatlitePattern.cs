namespace Patlite.lib;
public record PatlitePattern
{
    public La6Colour Tier1;
    public La6Colour Tier2;
    public La6Colour Tier3;
    public La6Colour Tier4;
    public La6Colour Tier5;
    public Flash Flash;
    public BuzzerPattern Buzzer;
}

public struct PatliteConfig
{
    public string ip { get; set; }
    public int port { get; set; }
}


