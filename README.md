# Patlite

This app was built to control a single LA6-POE patlite.

You will need to setup the Patlite to accept UDP.

## Projects

- `Patlite`: Original WPF desktop app (`net8.0-windows`).
- `Patlite.Avalonia`: Cross-platform Avalonia desktop app (`net10.0`) for Windows/macOS/Linux.
- `Patlite.lib`: Shared UDP command + transport library.

The Avalonia app uses a compact 300-pixel-wide window with the WPF controller's
controls: connection settings, flash/buzzer controls and current values at the top,
five narrow colour selectors with applied-state indicators and preset buttons
aligned beside them, a scrolling log, and busy status with Off/Send buttons at the bottom.
Colour selectors include swatches, and the indicators blink
when the applied flash setting is On. The controller uses a light theme; individual
control rendering and window decorations vary by platform.
All controller text, including inputs and logs, uses the bundled Inter font on
Windows, macOS and Linux. Labels retain their smaller text size.

The Avalonia app uses MVVM with compiled, typed XAML bindings. `MainViewModel`
owns controller state and commands, and `ViewModelBase` supplies property-change
notifications. UI dispatch is injected through `IUiDispatcher`; the window's
code-behind only initializes the view. Log scrolling is a reusable attached
behavior enabled in XAML, with subscriptions managed as the control enters and
leaves the visual tree.

Run the Avalonia app with the .NET 10 SDK:

```sh
dotnet run --project Patlite.Avalonia/Patlite.Avalonia.csproj
```

The app icons come from `Patlite.Avalonia/Assets/logo.png`. The original PNG is embedded
as the window icon, `Assets/patlite.ico` supplies the Windows executable icon, and
`Assets/patlite.icns` supplies the macOS bundle icon. Linux launcher icons are
provided in `Assets/Linux/hicolor` at sizes from 16 to 512 pixels.

Publish the Avalonia project for the required platform:

```sh
dotnet publish Patlite.Avalonia/Patlite.Avalonia.csproj -c Release -r win-x64 --self-contained true
dotnet publish Patlite.Avalonia/Patlite.Avalonia.csproj -c Release -r osx-arm64 --self-contained true
dotnet publish Patlite.Avalonia/Patlite.Avalonia.csproj -c Release -r linux-x64 --self-contained true
```

Use `osx-x64` for an Intel Mac. macOS publishing automatically creates
`Patlite.app` inside the publish directory, with its icon and `Info.plist`.
Windows publishing embeds the icon in `Patlite.Avalonia.exe`.

Linux publishing includes `patlite.desktop` and `icons/hicolor`. To register the
launcher, install the published app under `/opt/patlite`, copy `patlite.desktop`
to `/usr/share/applications`, and copy the contents of `icons/hicolor` into
`/usr/share/icons/hicolor`. The launcher and window both use the `patlite` identity.

After changing the source logo, regenerate the platform files with
`python3 Patlite.Avalonia/Packaging/generate-icons.py` (requires Pillow).
Normal builds use the generated files and do not require Python or Pillow.
