namespace Patlite.lib;
public enum La6Colour : byte
{
    Off = 0x00,
    Red = 0x01,
    Amber = 0x02,
    Lemon = 0x03,
    Green = 0x04,
    SkyBlue = 0x05,
    Blue = 0x06,
    Purple = 0x07,
    Pink = 0x08,
    White = 0x09,
}

public enum BuzzerPattern : byte
{
    Off = 0x00,
    Pattern1 = 0x01,
    Pattern2 = 0x02,
    Pattern3 = 0x03,
    Pattern4 = 0x04,
    Pattern5 = 0x05,
    Pattern6 = 0x06,
    Pattern7 = 0x07,
    Pattern8 = 0x08,
    Pattern9 = 0x09,
    Pattern10 = 0x0A,
    Pattern11 = 0x0B,
}

public enum Flash : byte
{
    Off = 0x00,
    On = 0x01
}

public enum Command : byte
{
    Reserved = 0x00,
    DataLength = 0x07,
    AHeader = 0x41,
    BHeader = 0x42,
    Clear = 0x43,
    DMotion = 0x44,
    DStatusAquisition = 0x45,
    StatusAquisition = 0x47,
    Smart = 0x54,
    Mute = 0x4D,
    Pulse = 0x50,
    Motion = 0x53
}

