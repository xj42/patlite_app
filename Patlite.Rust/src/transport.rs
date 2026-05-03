use crate::protocol::PatlitePattern;
use std::fmt;
use std::net::{SocketAddr, ToSocketAddrs, UdpSocket};

pub const DEFAULT_COMMAND_PORT: u16 = 10000;

#[derive(Debug, Clone, PartialEq, Eq)]
pub enum EndpointError {
    EmptyHost,
    InvalidPort(String),
    ResolveFailed(String),
}

impl fmt::Display for EndpointError {
    fn fmt(&self, f: &mut fmt::Formatter<'_>) -> fmt::Result {
        match self {
            EndpointError::EmptyHost => f.write_str("Host is required"),
            EndpointError::InvalidPort(port) => {
                write!(f, "Port must be a number from 1 to 65535, got '{port}'")
            }
            EndpointError::ResolveFailed(endpoint) => {
                write!(f, "Could not resolve endpoint '{endpoint}'")
            }
        }
    }
}

impl std::error::Error for EndpointError {}

#[derive(Debug)]
pub enum TransportError {
    Endpoint(EndpointError),
    Io(std::io::Error),
    PartialSend { sent: usize, expected: usize },
}

impl fmt::Display for TransportError {
    fn fmt(&self, f: &mut fmt::Formatter<'_>) -> fmt::Result {
        match self {
            TransportError::Endpoint(error) => write!(f, "{error}"),
            TransportError::Io(error) => write!(f, "{error}"),
            TransportError::PartialSend { sent, expected } => {
                write!(f, "Sent {sent} of {expected} bytes")
            }
        }
    }
}

impl std::error::Error for TransportError {}

impl From<EndpointError> for TransportError {
    fn from(value: EndpointError) -> Self {
        TransportError::Endpoint(value)
    }
}

impl From<std::io::Error> for TransportError {
    fn from(value: std::io::Error) -> Self {
        TransportError::Io(value)
    }
}

pub fn parse_port(port: &str) -> Result<u16, EndpointError> {
    let trimmed = port.trim();
    match trimmed.parse::<u16>() {
        Ok(0) | Err(_) => Err(EndpointError::InvalidPort(port.to_string())),
        Ok(port) => Ok(port),
    }
}

pub fn validate_endpoint(host: &str, port: &str) -> Result<(String, u16), EndpointError> {
    let host = host.trim();
    if host.is_empty() {
        return Err(EndpointError::EmptyHost);
    }

    Ok((host.to_string(), parse_port(port)?))
}

pub fn resolve_endpoint(host: &str, port: u16) -> Result<SocketAddr, EndpointError> {
    let host = host.trim();
    if host.is_empty() {
        return Err(EndpointError::EmptyHost);
    }

    let endpoint = format!("{host}:{port}");
    endpoint
        .to_socket_addrs()
        .map_err(|_| EndpointError::ResolveFailed(endpoint.clone()))?
        .next()
        .ok_or(EndpointError::ResolveFailed(endpoint))
}

pub fn send_pattern(host: &str, port: u16, pattern: PatlitePattern) -> Result<(), TransportError> {
    send_packet(host, port, &pattern.bytes())
}

pub fn send_packet(host: &str, port: u16, packet: &[u8]) -> Result<(), TransportError> {
    let endpoint = resolve_endpoint(host, port)?;
    let socket = UdpSocket::bind(("0.0.0.0", 0))?;
    let sent = socket.send_to(packet, endpoint)?;

    if sent == packet.len() {
        Ok(())
    } else {
        Err(TransportError::PartialSend {
            sent,
            expected: packet.len(),
        })
    }
}

#[cfg(test)]
mod tests {
    use super::*;
    use crate::protocol::{BuzzerPattern, Flash, La6Colour, PatlitePattern};
    use std::net::{IpAddr, Ipv4Addr, UdpSocket};
    use std::time::Duration;

    #[test]
    fn endpoint_validation_rejects_bad_inputs() {
        assert_eq!(validate_endpoint("", "1000"), Err(EndpointError::EmptyHost));
        assert!(validate_endpoint("127.0.0.1", "0").is_err());
        assert!(validate_endpoint("127.0.0.1", "70000").is_err());
        assert!(validate_endpoint("127.0.0.1", "abc").is_err());
    }

    #[test]
    fn endpoint_validation_accepts_valid_inputs() {
        assert_eq!(
            validate_endpoint(" 127.0.0.1 ", "12000"),
            Ok(("127.0.0.1".to_string(), 12000))
        );
    }

    #[test]
    fn udp_send_writes_exact_bytes() {
        let listener = UdpSocket::bind((IpAddr::V4(Ipv4Addr::LOCALHOST), 0)).unwrap();
        listener
            .set_read_timeout(Some(Duration::from_secs(2)))
            .unwrap();
        let port = listener.local_addr().unwrap().port();

        let pattern = PatlitePattern::new(
            [
                La6Colour::Red,
                La6Colour::Green,
                La6Colour::Blue,
                La6Colour::White,
                La6Colour::Off,
            ],
            Flash::On,
            BuzzerPattern::Pattern4,
        );

        send_pattern("127.0.0.1", port, pattern).unwrap();

        let mut buffer = [0_u8; 32];
        let (len, _) = listener.recv_from(&mut buffer).unwrap();

        assert_eq!(&buffer[..len], &pattern.bytes());
    }
}
