import Darwin
import Foundation

public protocol PatternTransport: Sendable {
    func send(_ pattern: Pattern, to endpoint: Endpoint) async throws
}

public struct UDPTransport: PatternTransport {
    public init() {}

    public func send(_ pattern: Pattern, to endpoint: Endpoint) async throws {
        // DNS lookup and socket operations run away from the UI actor.
        try await Task.detached {
            try Task.checkCancellation()
            var hints = addrinfo()
            hints.ai_family = AF_UNSPEC
            hints.ai_socktype = SOCK_DGRAM
            hints.ai_protocol = IPPROTO_UDP
            var addresses: UnsafeMutablePointer<addrinfo>?
            let result = getaddrinfo(endpoint.host, String(endpoint.port), &hints, &addresses)
            guard result == 0 else {
                throw TransportError(message: "Cannot resolve \(endpoint.host): \(String(cString: gai_strerror(result)))")
            }
            defer { if let addresses { freeaddrinfo(addresses) } }
            var current = addresses
            var lastError = "No UDP address found."
            while let address = current {
                let info = address.pointee
                current = info.ai_next
                let socketFD = socket(info.ai_family, info.ai_socktype, info.ai_protocol)
                guard socketFD >= 0 else {
                    lastError = String(cString: strerror(errno))
                    continue
                }
                defer { close(socketFD) }
                let packet = pattern.packet
                let count = packet.withUnsafeBytes {
                    sendto(socketFD, $0.baseAddress, $0.count, 0, info.ai_addr, info.ai_addrlen)
                }
                if count == packet.count { return }
                lastError = count < 0 ? String(cString: strerror(errno)) : "Incomplete UDP send."
            }
            throw TransportError(message: lastError)
        }.value
    }
}

public struct TransportError: LocalizedError, Sendable {
    public let message: String
    public init(message: String) { self.message = message }
    public var errorDescription: String? { message }
}
