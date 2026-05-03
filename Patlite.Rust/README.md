# Patlite Rust

A 100% Rust cross-platform GUI application for controlling a single LA6-POE Patlite over UDP.

## Build & Run

Requires Rust and Cargo.

```bash
cd Patlite.Rust
cargo run
```

For an optimized build:

```bash
cargo run --release
```

## Test

```bash
cargo fmt --check
cargo clippy -- -D warnings
cargo test
cargo build --release
```

## Features

- IP and port entry
- Five tier color selection
- Flash and buzzer patterns
- Presets matching the original Patlite app
- UDP packet generation compatible with the LA6-POE protocol
- Real-time send status and log output
- Best-effort LA6-POE discovery using HTTP status and PNS detailed status probes
