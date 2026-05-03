# Rust Development Milestones

## Summary
Build the Rust/Iced app incrementally, keeping every milestone runnable. The final feature addition before hardening is Patlite auto discovery.

## Milestones
1. **Runnable Iced Shell**
   - Replace `eframe` with a minimal Iced desktop app.
   - Show IP, port, tiers, presets, log area, `Off`, and `Send`.
   - Acceptance: `cargo run` opens the GUI; `cargo test` builds.

2. **Protocol Core**
   - Move LA6 enums, patterns, presets, and packet generation into Rust modules.
   - Match the .NET 13-byte UDP frame exactly.
   - Acceptance: packet tests match existing .NET behavior.

3. **Interactive Local UI**
   - Wire controls to Rust state without sending UDP yet.
   - Presets update/apply expected local patterns.
   - Acceptance: all controls work without crashing.

4. **Endpoint Validation**
   - Validate host and port.
   - Disable send actions when invalid or busy.
   - Acceptance: empty host, port `0`, port `70000`, and non-numeric ports are rejected.

5. **UDP Send Pipeline**
   - Send exact PNS bytes asynchronously via Iced `Task`.
   - Log success/failure and cap logs at 200 lines.
   - Acceptance: loopback UDP test receives exact bytes.

6. **Applied State Parity**
   - Update applied tier indicators only after successful sends.
   - Implement `Off`, custom send, and all existing presets.
   - Acceptance: behavior matches the .NET app.

7. **Visual Parity Pass**
   - Polish layout and busy feedback.
   - Keep the app utilitarian and controller-focused.
   - Acceptance: existing .NET workflows are available in Rust.

8. **Final Feature Add: Patlite Auto Discovery**
   - Add a `Discover` action that scans for LA6-POE devices.
   - Probe `192.168.10.1`, `la6-poe`, current manual IP, and local subnet candidates.
   - Prefer HTTP status probe: `/api/status?format=json`.
   - Fallback to PNS detailed status probe: `41 42 45 00 00 00` on default/current command port.
   - Display discovered IP, MAC, source, and status; let the user select a device.
   - Discovery must be read-only and must not change light/buzzer state.

9. **Cross-Platform Hardening**
   - Validate build/tests on macOS, Windows, and Linux.
   - Update README and add Rust CI.
   - Acceptance: `cargo fmt --check`, `cargo clippy -- -D warnings`, `cargo test`, and `cargo build --release` pass.

## Parity Definition
Rust reaches parity when it can configure the endpoint, choose tier colors, flash, buzzer, presets, send custom patterns, send all-off, show applied state after successful sends, display recent logs, and optionally discover a Patlite device on the LAN.

## Assumptions
- The .NET projects remain as reference during migration.
- Discovery is best-effort scan-and-verify; no documented Patlite broadcast discovery protocol is assumed.
- Installer packaging and signing come after parity.

## Implementation Progress

Status legend: `Done` is implemented and locally verified where noted; `In progress` is actively being worked; `Remaining` still needs implementation or verification.

| Milestone | Status | Progress | Remaining |
| --- | --- | --- | --- |
| 1. Runnable Iced Shell | In progress | `Cargo.toml` now targets Iced and `main.rs` launches the Iced app. | Run Cargo verification after dependencies resolve. |
| 2. Protocol Core | In progress | Protocol enums, presets, packet bytes, and unit tests are in `protocol.rs`. | Run Rust tests. |
| 3. Interactive Local UI | In progress | Iced state and control messages are implemented in `app.rs`. | Verify compile and GUI launch. |
| 4. Endpoint Validation | In progress | Host and port validation are implemented in `transport.rs`. | Run validation tests. |
| 5. UDP Send Pipeline | In progress | Async send task and UDP transport are implemented. | Run loopback UDP test. |
| 6. Applied State Parity | In progress | Applied tier, flash, and buzzer state update only on successful send. | Verify tests and manual UI behavior. |
| 7. Visual Parity Pass | In progress | Main workflow panels, presets, logs, and busy status are implemented. | Polish layout issues found after running. |
| 8. Patlite Auto Discovery | In progress | HTTP status and PNS detailed status discovery are implemented in `discovery.rs`. | Run tests and verify on a real LAN/device when available. |
| 9. Cross-Platform Hardening | Remaining | README now lists Rust verification commands. | Run fmt, clippy, tests, release build, and add CI later. |
