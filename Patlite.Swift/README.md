# Patlite Swift

Native macOS SwiftUI version of `Patlite.Avalonia`, controlling one LA6-POE
Patlite over UDP. Requires macOS 13 or later and Swift 6 (Xcode or Apple's
Command Line Tools). There are no external package dependencies.

From the repository root:

```sh
swift run --package-path Patlite.Swift PatliteSwift
swift test --package-path Patlite.Swift
```

You can also open `Package.swift` in Xcode and run the `PatliteSwift` scheme.

Build a Finder-launchable app for the current Mac architecture:

```sh
bash Patlite.Swift/Packaging/build-app.sh
codesign --verify --deep --strict Patlite.Swift/dist/Patlite.app
open Patlite.Swift/dist/Patlite.app
```

The script builds in Release mode, copies the SwiftPM resource bundle and shared
Patlite icon, and applies a local ad hoc signature. The resulting app is for
local use; distribution requires Developer ID signing and notarization. Build
on an Intel Mac for x86_64 or an Apple Silicon Mac for arm64.

Configure the Patlite to accept UDP, then enter its IP address or hostname and
configured UDP port. The fields start empty, matching Avalonia. Select five
tier colours, Flash, and Buzzer, then press Send. Free, Concentrating, Busy,
Normal, and Fire send the same patterns as Avalonia. Off sends five off tiers
with flash and buzzer off.

The compact light-themed window includes colour swatches, flashing last-sent
indicators, current flash/buzzer values, a scrolling log capped at 200 entries,
and busy status. It uses native macOS controls and the system font. Presets and
Off leave the editable selections unchanged. All send actions are disabled
while a send is in progress or the endpoint is incomplete. Ports must be
between 1 and 65535; hostname resolution runs away from the UI thread.

Indicators show the last pattern successfully handed to the local UDP socket.
The protocol does not acknowledge delivery, so these are not device readback.
Settings are held in memory for the current session, as in Avalonia.

`PatliteCore` contains the typed protocol, presets, UDP transport, and
main-actor controller. `PatliteSwift` contains the SwiftUI interface. Tests
cover the exact 13-byte packet, presets, endpoint validation, selected/applied
state, failure handling, concurrent sends, the log limit, and loopback UDP.
Physical-device behavior and visual appearance need a manual check on a Mac
with access to the Patlite.

For that check, launch the packaged app, configure the endpoint, and send a
custom pattern, each preset, and Off. Confirm the five lights, flashing, and
buzzer on the device; check that presets and Off preserve the editable
selections. Resize the window and check the pickers, indicators, and log.
If macOS asks for local-network access, allow it to communicate with the device.
