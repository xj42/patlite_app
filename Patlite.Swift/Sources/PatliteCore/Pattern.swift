import Foundation

public enum LightColour: UInt8, CaseIterable, Sendable {
    case off, red, amber, lemon, green, skyBlue, blue, purple, pink, white

    public var label: String {
        switch self {
        case .skyBlue: return "SkyBlue"
        default: return String(describing: self).capitalized
        }
    }
}

public enum Flash: UInt8, CaseIterable, Sendable {
    case off, on
    public var label: String { self == .off ? "Off" : "On" }
}

public enum Buzzer: UInt8, CaseIterable, Sendable {
    case off, pattern1, pattern2, pattern3, pattern4, pattern5, pattern6
    case pattern7, pattern8, pattern9, pattern10, pattern11
    public var label: String { self == .off ? "Off" : "Pattern\(rawValue)" }
}

public struct Pattern: Equatable, Sendable {
    public var tier1: LightColour
    public var tier2: LightColour
    public var tier3: LightColour
    public var tier4: LightColour
    public var tier5: LightColour
    public var flash: Flash
    public var buzzer: Buzzer

    public init(_ tier1: LightColour = .off, _ tier2: LightColour = .off,
                _ tier3: LightColour = .off, _ tier4: LightColour = .off,
                _ tier5: LightColour = .off, flash: Flash = .off, buzzer: Buzzer = .off) {
        self.tier1 = tier1; self.tier2 = tier2; self.tier3 = tier3
        self.tier4 = tier4; self.tier5 = tier5
        self.flash = flash; self.buzzer = buzzer
    }

    public static let off = Pattern()
    public var tiers: [LightColour] { [tier1, tier2, tier3, tier4, tier5] }
    public var packet: [UInt8] {
        var bytes: [UInt8] = [0x41, 0x42, 0x44, 0, 0, 7]
        bytes.append(contentsOf: tiers.map(\.rawValue))
        bytes.append(flash.rawValue)
        bytes.append(buzzer.rawValue)
        return bytes
    }
}

public struct LightPreset: Identifiable, Sendable {
    public let name: String
    public let pattern: Pattern
    public var id: String { name }

    public static let all: [LightPreset] = [
        .init(name: "Free", pattern: Pattern(.green)),
        .init(name: "Concentrating", pattern: Pattern(.amber, .amber, .amber, .white, .white)),
        .init(name: "Busy", pattern: Pattern(.red, .red, .red, .white, .white)),
        .init(name: "Normal", pattern: Pattern(.green, .purple, .blue, .pink, .lemon)),
        .init(name: "Fire", pattern: Pattern(.red, .red, .red, .red, .red, flash: .on))
    ]
}

public struct Endpoint: Sendable, Equatable {
    public let host: String
    public let port: UInt16

    public init?(host: String, port: String) {
        let host = host.trimmingCharacters(in: .whitespacesAndNewlines)
        guard !host.isEmpty, !host.utf8.contains(0),
              let port = UInt16(port.trimmingCharacters(in: .whitespacesAndNewlines)), port > 0 else {
            return nil
        }
        self.host = host
        self.port = port
    }
}
