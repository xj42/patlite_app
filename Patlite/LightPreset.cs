using Patlite.lib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Patlite;
public class LightPreset
{
    public string Name { get; set; } = "Preset";
    public La6Colour Tier1 { get; set; }
    public La6Colour Tier2 { get; set; }
    public La6Colour Tier3 { get; set; }
    public La6Colour Tier4 { get; set; }
    public La6Colour Tier5 { get; set; }
    public Flash Flash { get; set; }
    public BuzzerPattern Buzzer { get; set; }

    public static LightPreset From(string name,
        La6Colour t1, La6Colour t2, La6Colour t3, La6Colour t4, La6Colour t5,
        Flash flash, BuzzerPattern buzzer)
        => new LightPreset { Name = name, Tier1 = t1, Tier2 = t2, Tier3 = t3, Tier4 = t4, Tier5 = t5, Flash = flash, Buzzer = buzzer };
}
