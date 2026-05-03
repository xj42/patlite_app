use std::fmt;

pub const DETAILED_MOTION_HEADER: [u8; 6] = [0x41, 0x42, 0x44, 0x00, 0x00, 0x07];
pub const DETAILED_STATUS_REQUEST: [u8; 6] = [0x41, 0x42, 0x45, 0x00, 0x00, 0x00];

#[repr(u8)]
#[derive(Debug, Copy, Clone, PartialEq, Eq, Hash)]
pub enum La6Colour {
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

impl La6Colour {
    pub const ALL: [La6Colour; 10] = [
        La6Colour::Off,
        La6Colour::Red,
        La6Colour::Amber,
        La6Colour::Lemon,
        La6Colour::Green,
        La6Colour::SkyBlue,
        La6Colour::Blue,
        La6Colour::Purple,
        La6Colour::Pink,
        La6Colour::White,
    ];

    pub fn name(self) -> &'static str {
        match self {
            La6Colour::Off => "Off",
            La6Colour::Red => "Red",
            La6Colour::Amber => "Amber",
            La6Colour::Lemon => "Lemon",
            La6Colour::Green => "Green",
            La6Colour::SkyBlue => "SkyBlue",
            La6Colour::Blue => "Blue",
            La6Colour::Purple => "Purple",
            La6Colour::Pink => "Pink",
            La6Colour::White => "White",
        }
    }

    pub fn rgb(self) -> (f32, f32, f32) {
        match self {
            La6Colour::Off => (0.58, 0.62, 0.66),
            La6Colour::Red => (0.86, 0.20, 0.18),
            La6Colour::Amber => (1.00, 0.58, 0.12),
            La6Colour::Lemon => (1.00, 0.85, 0.00),
            La6Colour::Green => (0.00, 0.62, 0.34),
            La6Colour::SkyBlue => (0.18, 0.64, 0.86),
            La6Colour::Blue => (0.10, 0.32, 0.86),
            La6Colour::Purple => (0.42, 0.30, 0.76),
            La6Colour::Pink => (0.90, 0.34, 0.58),
            La6Colour::White => (0.98, 0.98, 0.95),
        }
    }
}

impl fmt::Display for La6Colour {
    fn fmt(&self, f: &mut fmt::Formatter<'_>) -> fmt::Result {
        f.write_str(self.name())
    }
}

#[repr(u8)]
#[derive(Debug, Copy, Clone, PartialEq, Eq, Hash)]
pub enum Flash {
    Off = 0x00,
    On = 0x01,
}

impl Flash {
    pub const ALL: [Flash; 2] = [Flash::Off, Flash::On];

    pub fn name(self) -> &'static str {
        match self {
            Flash::Off => "Off",
            Flash::On => "On",
        }
    }
}

impl fmt::Display for Flash {
    fn fmt(&self, f: &mut fmt::Formatter<'_>) -> fmt::Result {
        f.write_str(self.name())
    }
}

#[repr(u8)]
#[derive(Debug, Copy, Clone, PartialEq, Eq, Hash)]
pub enum BuzzerPattern {
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

impl BuzzerPattern {
    pub const ALL: [BuzzerPattern; 12] = [
        BuzzerPattern::Off,
        BuzzerPattern::Pattern1,
        BuzzerPattern::Pattern2,
        BuzzerPattern::Pattern3,
        BuzzerPattern::Pattern4,
        BuzzerPattern::Pattern5,
        BuzzerPattern::Pattern6,
        BuzzerPattern::Pattern7,
        BuzzerPattern::Pattern8,
        BuzzerPattern::Pattern9,
        BuzzerPattern::Pattern10,
        BuzzerPattern::Pattern11,
    ];

    pub fn name(self) -> &'static str {
        match self {
            BuzzerPattern::Off => "Off",
            BuzzerPattern::Pattern1 => "Pattern 1",
            BuzzerPattern::Pattern2 => "Pattern 2",
            BuzzerPattern::Pattern3 => "Pattern 3",
            BuzzerPattern::Pattern4 => "Pattern 4",
            BuzzerPattern::Pattern5 => "Pattern 5",
            BuzzerPattern::Pattern6 => "Pattern 6",
            BuzzerPattern::Pattern7 => "Pattern 7",
            BuzzerPattern::Pattern8 => "Pattern 8",
            BuzzerPattern::Pattern9 => "Pattern 9",
            BuzzerPattern::Pattern10 => "Pattern 10",
            BuzzerPattern::Pattern11 => "Pattern 11",
        }
    }
}

impl fmt::Display for BuzzerPattern {
    fn fmt(&self, f: &mut fmt::Formatter<'_>) -> fmt::Result {
        f.write_str(self.name())
    }
}

#[derive(Debug, Copy, Clone, PartialEq, Eq)]
pub struct PatlitePattern {
    pub tiers: [La6Colour; 5],
    pub flash: Flash,
    pub buzzer: BuzzerPattern,
}

impl PatlitePattern {
    pub const fn new(tiers: [La6Colour; 5], flash: Flash, buzzer: BuzzerPattern) -> Self {
        Self {
            tiers,
            flash,
            buzzer,
        }
    }

    pub const fn off() -> Self {
        Self {
            tiers: [La6Colour::Off; 5],
            flash: Flash::Off,
            buzzer: BuzzerPattern::Off,
        }
    }

    pub fn bytes(self) -> [u8; 13] {
        [
            DETAILED_MOTION_HEADER[0],
            DETAILED_MOTION_HEADER[1],
            DETAILED_MOTION_HEADER[2],
            DETAILED_MOTION_HEADER[3],
            DETAILED_MOTION_HEADER[4],
            DETAILED_MOTION_HEADER[5],
            self.tiers[0] as u8,
            self.tiers[1] as u8,
            self.tiers[2] as u8,
            self.tiers[3] as u8,
            self.tiers[4] as u8,
            self.flash as u8,
            self.buzzer as u8,
        ]
    }
}

impl Default for PatlitePattern {
    fn default() -> Self {
        Self::off()
    }
}

#[derive(Debug, Copy, Clone, PartialEq, Eq)]
pub struct LightPreset {
    pub name: &'static str,
    pub pattern: PatlitePattern,
}

pub const PRESETS: [LightPreset; 5] = [
    LightPreset {
        name: "Free",
        pattern: PatlitePattern::new(
            [
                La6Colour::Green,
                La6Colour::Off,
                La6Colour::Off,
                La6Colour::Off,
                La6Colour::Off,
            ],
            Flash::Off,
            BuzzerPattern::Off,
        ),
    },
    LightPreset {
        name: "Concentrating",
        pattern: PatlitePattern::new(
            [
                La6Colour::Amber,
                La6Colour::Amber,
                La6Colour::Amber,
                La6Colour::White,
                La6Colour::White,
            ],
            Flash::Off,
            BuzzerPattern::Off,
        ),
    },
    LightPreset {
        name: "Busy",
        pattern: PatlitePattern::new(
            [
                La6Colour::Red,
                La6Colour::Red,
                La6Colour::Red,
                La6Colour::White,
                La6Colour::White,
            ],
            Flash::Off,
            BuzzerPattern::Off,
        ),
    },
    LightPreset {
        name: "Normal",
        pattern: PatlitePattern::new(
            [
                La6Colour::Green,
                La6Colour::Purple,
                La6Colour::Blue,
                La6Colour::Pink,
                La6Colour::Lemon,
            ],
            Flash::Off,
            BuzzerPattern::Off,
        ),
    },
    LightPreset {
        name: "Fire",
        pattern: PatlitePattern::new(
            [
                La6Colour::Red,
                La6Colour::Red,
                La6Colour::Red,
                La6Colour::Red,
                La6Colour::Red,
            ],
            Flash::On,
            BuzzerPattern::Off,
        ),
    },
];

#[cfg(test)]
mod tests {
    use super::*;

    #[test]
    fn detailed_motion_packet_matches_dotnet_builder() {
        let pattern = PatlitePattern::new(
            [
                La6Colour::Red,
                La6Colour::Green,
                La6Colour::Blue,
                La6Colour::White,
                La6Colour::Off,
            ],
            Flash::On,
            BuzzerPattern::Pattern3,
        );

        assert_eq!(
            pattern.bytes(),
            [0x41, 0x42, 0x44, 0x00, 0x00, 0x07, 1, 4, 6, 9, 0, 1, 3]
        );
    }

    #[test]
    fn off_pattern_turns_everything_off() {
        assert_eq!(
            PatlitePattern::off().bytes(),
            [0x41, 0x42, 0x44, 0x00, 0x00, 0x07, 0, 0, 0, 0, 0, 0, 0]
        );
    }

    #[test]
    fn presets_match_reference_app() {
        assert_eq!(PRESETS[0].name, "Free");
        assert_eq!(PRESETS[0].pattern.tiers[0], La6Colour::Green);
        assert_eq!(PRESETS[1].pattern.tiers[3], La6Colour::White);
        assert_eq!(PRESETS[2].pattern.tiers[0], La6Colour::Red);
        assert_eq!(PRESETS[3].pattern.tiers[4], La6Colour::Lemon);
        assert_eq!(PRESETS[4].pattern.flash, Flash::On);
    }
}
