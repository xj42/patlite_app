import Combine
import Foundation

public struct LogLine: Identifiable, Sendable {
    public let id = UUID()
    public let text: String
}

@MainActor
public final class Controller: ObservableObject {
    @Published public var host = ""
    @Published public var port = ""
    @Published public var selected = Pattern(.red)
    @Published public private(set) var applied = Pattern.off
    @Published public private(set) var isBusy = false
    @Published public private(set) var logs: [LogLine] = []
    private let transport: any PatternTransport

    public init(transport: any PatternTransport = UDPTransport()) { self.transport = transport }

    public var canSend: Bool { !isBusy && Endpoint(host: host, port: port) != nil }

    public func send() async { await transmit(selected, description: "Pattern") }
    public func off() async { await transmit(.off, description: "Off") }
    public func sendPreset(_ preset: LightPreset) async {
        await transmit(preset.pattern, description: "Preset '\(preset.name)'")
    }

    private func transmit(_ pattern: Pattern, description: String) async {
        guard !isBusy, let endpoint = Endpoint(host: host, port: port) else { return }
        isBusy = true
        defer { isBusy = false }
        do {
            try await transport.send(pattern, to: endpoint)
            applied = pattern
            log("\(description) sent.")
        } catch {
            log("Failed to send \(description.lowercased()): \(error.localizedDescription)")
        }
    }

    private func log(_ message: String) {
        logs.append(LogLine(text: "\(Date().formatted(date: .omitted, time: .standard))  \(message)"))
        if logs.count > 200 { logs.removeFirst(logs.count - 200) }
    }
}
