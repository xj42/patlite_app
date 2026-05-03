use crate::protocol::DETAILED_STATUS_REQUEST;
use crate::transport::{resolve_endpoint, DEFAULT_COMMAND_PORT};
use std::collections::{HashMap, HashSet};
use std::io::{Read, Write};
use std::net::{IpAddr, Ipv4Addr, SocketAddr, TcpStream, ToSocketAddrs, UdpSocket};
use std::thread;
use std::time::Duration;

const DEFAULT_PATLITE_IP: &str = "192.168.10.1";
const DEFAULT_PATLITE_HOST: &str = "la6-poe";
const HTTP_PORT: u16 = 80;
const MAX_PARALLEL_PROBES: usize = 32;
const PROBE_TIMEOUT: Duration = Duration::from_millis(180);

#[derive(Debug, Clone, PartialEq, Eq)]
pub struct DiscoveryConfig {
    pub current_host: String,
    pub current_port: u16,
    pub scan_local_subnet: bool,
}

impl Default for DiscoveryConfig {
    fn default() -> Self {
        Self {
            current_host: String::new(),
            current_port: DEFAULT_COMMAND_PORT,
            scan_local_subnet: true,
        }
    }
}

#[derive(Debug, Clone, PartialEq, Eq)]
pub struct DiscoveredDevice {
    pub host: String,
    pub command_port: u16,
    pub mac: Option<String>,
    pub source: String,
    pub status: String,
}

impl DiscoveredDevice {
    pub fn label(&self) -> String {
        match &self.mac {
            Some(mac) => format!("{}:{} ({mac})", self.host, self.command_port),
            None => format!("{}:{}", self.host, self.command_port),
        }
    }
}

pub fn discover_devices(config: DiscoveryConfig) -> Vec<DiscoveredDevice> {
    let candidates = discovery_candidates(&config.current_host, config.scan_local_subnet);
    let ports = command_ports(config.current_port);
    let mut devices = Vec::new();

    for chunk in candidates.chunks(MAX_PARALLEL_PROBES) {
        let mut handles = Vec::with_capacity(chunk.len());

        for candidate in chunk {
            let candidate = candidate.clone();
            let ports = ports.clone();
            handles.push(thread::spawn(move || {
                probe_host(&candidate, &ports, PROBE_TIMEOUT)
            }));
        }

        for handle in handles {
            if let Ok(Some(device)) = handle.join() {
                devices.push(device);
            }
        }
    }

    dedupe(devices)
}

pub fn discovery_candidates(current_host: &str, scan_local_subnet: bool) -> Vec<String> {
    let mut seen = HashSet::new();
    let mut candidates = Vec::new();

    push_candidate(current_host.trim(), &mut seen, &mut candidates);
    push_candidate(DEFAULT_PATLITE_IP, &mut seen, &mut candidates);
    push_candidate(DEFAULT_PATLITE_HOST, &mut seen, &mut candidates);

    if scan_local_subnet {
        if let Some(local_ip) = local_ipv4() {
            let octets = local_ip.octets();
            for host in 1..=254 {
                if host == octets[3] {
                    continue;
                }

                let candidate = format!("{}.{}.{}.{}", octets[0], octets[1], octets[2], host);
                push_candidate(&candidate, &mut seen, &mut candidates);
            }
        }
    }

    candidates
}

pub fn extract_mac_from_text(text: &str) -> Option<String> {
    let bytes = text.as_bytes();
    if bytes.len() < 17 {
        return None;
    }

    for start in 0..=(bytes.len() - 17) {
        let window = &bytes[start..start + 17];
        if is_mac_window(window) {
            return Some(String::from_utf8_lossy(window).to_ascii_uppercase());
        }
    }

    None
}

pub fn format_mac(bytes: &[u8]) -> Option<String> {
    if bytes.len() < 6 {
        return None;
    }

    let mac = &bytes[..6];
    if mac.iter().all(|byte| *byte == 0x00) || mac.iter().all(|byte| *byte == 0xFF) {
        return None;
    }

    Some(format!(
        "{:02X}:{:02X}:{:02X}:{:02X}:{:02X}:{:02X}",
        mac[0], mac[1], mac[2], mac[3], mac[4], mac[5]
    ))
}

fn command_ports(current_port: u16) -> Vec<u16> {
    if current_port == DEFAULT_COMMAND_PORT {
        vec![DEFAULT_COMMAND_PORT]
    } else {
        vec![current_port, DEFAULT_COMMAND_PORT]
    }
}

fn push_candidate(candidate: &str, seen: &mut HashSet<String>, candidates: &mut Vec<String>) {
    if candidate.is_empty() {
        return;
    }

    let normalized = candidate.to_ascii_lowercase();
    if seen.insert(normalized) {
        candidates.push(candidate.to_string());
    }
}

fn local_ipv4() -> Option<Ipv4Addr> {
    let socket = UdpSocket::bind(("0.0.0.0", 0)).ok()?;
    socket.connect(("8.8.8.8", 80)).ok()?;
    match socket.local_addr().ok()?.ip() {
        IpAddr::V4(ip) if !ip.is_loopback() => Some(ip),
        _ => None,
    }
}

fn probe_host(host: &str, ports: &[u16], timeout: Duration) -> Option<DiscoveredDevice> {
    if let Some(device) = probe_http_status(host, timeout) {
        return Some(device);
    }

    for port in ports {
        if let Some(device) = probe_pns_detailed_status(host, *port, timeout) {
            return Some(device);
        }
    }

    None
}

fn probe_http_status(host: &str, timeout: Duration) -> Option<DiscoveredDevice> {
    let (mut stream, address) = connect_tcp(host, HTTP_PORT, timeout).ok()?;
    stream.set_read_timeout(Some(timeout)).ok()?;
    stream.set_write_timeout(Some(timeout)).ok()?;

    let request = format!(
        "GET /api/status?format=json HTTP/1.1\r\nHost: {host}\r\nConnection: close\r\n\r\n"
    );
    stream.write_all(request.as_bytes()).ok()?;

    let mut response = String::new();
    stream.take(16 * 1024).read_to_string(&mut response).ok()?;

    let looks_like_patlight = response.contains("MAC_Address")
        || response.contains("Unit_Status")
        || response.contains("Buzzer_Pattern")
        || response.contains("Signal Tower");

    if !looks_like_patlight {
        return None;
    }

    Some(DiscoveredDevice {
        host: address.ip().to_string(),
        command_port: DEFAULT_COMMAND_PORT,
        mac: extract_mac_from_text(&response),
        source: "HTTP status".to_string(),
        status: "HTTP status endpoint responded".to_string(),
    })
}

fn probe_pns_detailed_status(host: &str, port: u16, timeout: Duration) -> Option<DiscoveredDevice> {
    let endpoint = resolve_endpoint(host, port).ok()?;
    let socket = UdpSocket::bind(("0.0.0.0", 0)).ok()?;
    socket.set_read_timeout(Some(timeout)).ok()?;
    socket.set_write_timeout(Some(timeout)).ok()?;
    socket.send_to(&DETAILED_STATUS_REQUEST, endpoint).ok()?;

    let mut buffer = [0_u8; 128];
    let (len, source) = socket.recv_from(&mut buffer).ok()?;
    let response = &buffer[..len];

    if !looks_like_detailed_status(response) {
        return None;
    }

    Some(DiscoveredDevice {
        host: source.ip().to_string(),
        command_port: port,
        mac: format_mac(response),
        source: "PNS detailed status".to_string(),
        status: format!("PNS detailed status response: {len} bytes"),
    })
}

fn connect_tcp(
    host: &str,
    port: u16,
    timeout: Duration,
) -> std::io::Result<(TcpStream, SocketAddr)> {
    let mut last_error = None;
    for address in (host, port).to_socket_addrs()? {
        match TcpStream::connect_timeout(&address, timeout) {
            Ok(stream) => return Ok((stream, address)),
            Err(error) => last_error = Some(error),
        }
    }

    Err(last_error.unwrap_or_else(|| {
        std::io::Error::new(std::io::ErrorKind::NotFound, "no address resolved")
    }))
}

fn looks_like_detailed_status(response: &[u8]) -> bool {
    if response.len() == 40 || response.len() == 45 {
        return true;
    }

    matches!(format_mac(response).as_deref(), Some(mac) if mac.starts_with("80:39:E5"))
}

fn is_mac_window(window: &[u8]) -> bool {
    if window.len() != 17 {
        return false;
    }

    let separator = window[2];
    if separator != b':' && separator != b'-' {
        return false;
    }

    for (index, byte) in window.iter().enumerate() {
        match index {
            2 | 5 | 8 | 11 | 14 => {
                if *byte != separator {
                    return false;
                }
            }
            _ => {
                if !byte.is_ascii_hexdigit() {
                    return false;
                }
            }
        }
    }

    true
}

fn dedupe(devices: Vec<DiscoveredDevice>) -> Vec<DiscoveredDevice> {
    let mut deduped = HashMap::new();

    for device in devices {
        let key = device
            .mac
            .clone()
            .unwrap_or_else(|| format!("{}:{}", device.host, device.command_port));
        deduped.entry(key).or_insert(device);
    }

    let mut devices = deduped.into_values().collect::<Vec<_>>();
    devices.sort_by(|left, right| left.host.cmp(&right.host));
    devices
}

#[cfg(test)]
mod tests {
    use super::*;

    #[test]
    fn candidates_include_manual_and_factory_defaults() {
        let candidates = discovery_candidates("10.1.2.3", false);

        assert!(candidates.contains(&"10.1.2.3".to_string()));
        assert!(candidates.contains(&DEFAULT_PATLITE_IP.to_string()));
        assert!(candidates.contains(&DEFAULT_PATLITE_HOST.to_string()));
    }

    #[test]
    fn mac_extraction_finds_colon_or_dash_separated_values() {
        assert_eq!(
            extract_mac_from_text(r#"{"MAC_Address":"80:39:E5:00:1A:2F"}"#),
            Some("80:39:E5:00:1A:2F".to_string())
        );
        assert_eq!(
            extract_mac_from_text("mac=80-39-e5-00-1a-2f"),
            Some("80-39-E5-00-1A-2F".to_string())
        );
    }

    #[test]
    fn pns_mac_formatting_uses_first_six_bytes() {
        assert_eq!(
            format_mac(&[0x80, 0x39, 0xE5, 0x00, 0x1A, 0x2F, 0x00]),
            Some("80:39:E5:00:1A:2F".to_string())
        );
    }

    #[test]
    fn pns_status_request_matches_manual() {
        assert_eq!(DETAILED_STATUS_REQUEST, [0x41, 0x42, 0x45, 0, 0, 0]);
    }
}
