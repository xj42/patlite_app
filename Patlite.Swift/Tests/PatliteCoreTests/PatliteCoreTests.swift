import Darwin
import Foundation
import Testing
@testable import PatliteCore

@Test func packetMatchesDotNetProtocol() {
    #expect(Pattern(.red, .amber, .lemon, .green, .skyBlue, flash: .on, buzzer: .pattern11).packet ==
            [0x41, 0x42, 0x44, 0, 0, 7, 1, 2, 3, 4, 5, 1, 11])
    #expect(Pattern.off.packet == [0x41, 0x42, 0x44, 0, 0, 7, 0, 0, 0, 0, 0, 0, 0])
    #expect(LightColour.allCases.map(\.rawValue) == Array(UInt8(0)...UInt8(9)))
    #expect(Buzzer.allCases.map(\.rawValue) == Array(UInt8(0)...UInt8(11)))
}

@Test func presetsMatchAvalonia() {
    #expect(LightPreset.all.map(\.name) == ["Free", "Concentrating", "Busy", "Normal", "Fire"])
    #expect(LightPreset.all.map { $0.pattern.packet } == [
        [65, 66, 68, 0, 0, 7, 4, 0, 0, 0, 0, 0, 0],
        [65, 66, 68, 0, 0, 7, 2, 2, 2, 9, 9, 0, 0],
        [65, 66, 68, 0, 0, 7, 1, 1, 1, 9, 9, 0, 0],
        [65, 66, 68, 0, 0, 7, 4, 7, 6, 8, 3, 0, 0],
        [65, 66, 68, 0, 0, 7, 1, 1, 1, 1, 1, 1, 0]
    ])
}

@Test func endpointValidation() {
    for port in ["", "0", "-1", "65536", "hello", "1.5"] {
        #expect(Endpoint(host: "localhost", port: port) == nil)
    }
    #expect(Endpoint(host: "   ", port: "1234") == nil)
    #expect(Endpoint(host: "bad\0host", port: "1234") == nil)
    #expect(Endpoint(host: " ::1 ", port: " 65535 ")?.port == 65535)
    #expect(Endpoint(host: "localhost", port: "1")?.host == "localhost")
}

actor RecordingTransport: PatternTransport {
    var sent: [Pattern] = []
    var fail = false
    func setFailure() { fail = true }
    func send(_ pattern: Pattern, to endpoint: Endpoint) async throws {
        if fail { throw TransportError(message: "Test failure") }
        sent.append(pattern)
    }
}

@Test @MainActor func controllerKeepsSelectionSeparateAndPreservesAppliedOnFailure() async {
    let transport = RecordingTransport()
    let controller = Controller(transport: transport)
    #expect(controller.selected == Pattern(.red))
    #expect(controller.applied == .off)
    #expect(!controller.canSend)
    await controller.send()
    #expect(await transport.sent.isEmpty)
    controller.host = "localhost"
    controller.port = "1234"
    await controller.send()
    #expect(controller.applied == Pattern(.red))
    await controller.sendPreset(LightPreset.all[4])
    #expect(controller.applied == LightPreset.all[4].pattern)
    #expect(controller.selected == Pattern(.red))
    await transport.setFailure()
    await controller.off()
    #expect(controller.applied == LightPreset.all[4].pattern)
    #expect(controller.logs.last?.text.contains("Test failure") == true)
    #expect(!controller.isBusy)
}

@Test @MainActor func offAndLogLimit() async {
    let controller = Controller(transport: RecordingTransport())
    controller.host = "localhost"
    controller.port = "1234"
    for _ in 0..<205 { await controller.send() }
    #expect(controller.logs.count == 200)
    await controller.off()
    #expect(controller.applied == .off)
    #expect(controller.selected == Pattern(.red))
}

actor GatedTransport: PatternTransport {
    var continuation: CheckedContinuation<Void, Never>?
    var count = 0
    var endpoint: Endpoint?
    func send(_ pattern: Pattern, to endpoint: Endpoint) async throws {
        count += 1
        self.endpoint = endpoint
        await withCheckedContinuation { continuation = $0 }
    }
    func release() { continuation?.resume(); continuation = nil }
}

@Test @MainActor func busyRejectsConcurrentSendsAndUsesSnapshot() async {
    let transport = GatedTransport()
    let controller = Controller(transport: transport)
    controller.host = "localhost"
    controller.port = "1234"
    let send = Task { await controller.send() }
    while await transport.continuation == nil { await Task.yield() }
    #expect(controller.isBusy)
    #expect(!controller.canSend)
    controller.selected = Pattern(.blue)
    controller.host = "changed.example"
    controller.port = "4321"
    await controller.off()
    #expect(await transport.count == 1)
    #expect(await transport.endpoint == Endpoint(host: "localhost", port: "1234"))
    await transport.release()
    await send.value
    #expect(controller.applied == Pattern(.red))
    #expect(controller.selected == Pattern(.blue))
    #expect(!controller.isBusy)
}

@Test func udpLoopbackSendsExactDatagram() async throws {
    let receiver = socket(AF_INET, SOCK_DGRAM, IPPROTO_UDP)
    #expect(receiver >= 0)
    guard receiver >= 0 else { return }
    defer { close(receiver) }
    var address = sockaddr_in()
    address.sin_len = UInt8(MemoryLayout<sockaddr_in>.size)
    address.sin_family = sa_family_t(AF_INET)
    address.sin_addr.s_addr = inet_addr("127.0.0.1")
    let bound = withUnsafePointer(to: &address) {
        $0.withMemoryRebound(to: sockaddr.self, capacity: 1) {
            bind(receiver, $0, socklen_t(MemoryLayout<sockaddr_in>.size))
        }
    }
    #expect(bound == 0)
    var length = socklen_t(MemoryLayout<sockaddr_in>.size)
    let named = withUnsafeMutablePointer(to: &address) {
        $0.withMemoryRebound(to: sockaddr.self, capacity: 1) { getsockname(receiver, $0, &length) }
    }
    #expect(named == 0)
    var timeout = timeval(tv_sec: 2, tv_usec: 0)
    #expect(setsockopt(receiver, SOL_SOCKET, SO_RCVTIMEO, &timeout,
                       socklen_t(MemoryLayout<timeval>.size)) == 0)
    let endpoint = try #require(Endpoint(host: "127.0.0.1", port: String(UInt16(bigEndian: address.sin_port))))
    let pattern = Pattern(.purple, .pink, .white, .blue, .green, flash: .on, buzzer: .pattern7)
    try await UDPTransport().send(pattern, to: endpoint)
    var packet = [UInt8](repeating: 0, count: 32)
    let count = packet.withUnsafeMutableBytes { recv(receiver, $0.baseAddress, $0.count, 0) }
    #expect(count == 13)
    #expect(Array(packet.prefix(max(0, count))) == pattern.packet)
}
