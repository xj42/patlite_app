#!/bin/bash
set -euo pipefail
cd "$(dirname "$0")/.."
swift build -c release
binary_dir="$(swift build -c release --show-bin-path)"
app_dir="$PWD/dist/Patlite.app"
mkdir -p "$app_dir/Contents/MacOS" "$app_dir/Contents/Resources"
cp "$binary_dir/PatliteSwift" "$app_dir/Contents/MacOS/PatliteSwift"
# PatliteApp loads the packaged bundle from Contents/Resources explicitly.
# ditto also makes repeated builds update the bundle without nesting copies.
ditto "$binary_dir/PatliteSwift_PatliteSwift.bundle" "$app_dir/Contents/Resources/PatliteSwift_PatliteSwift.bundle"
cp Sources/PatliteSwift/Resources/patlite.icns "$app_dir/Contents/Resources/"
cp Packaging/Info.plist "$app_dir/Contents/Info.plist"
codesign --force --deep --sign - "$app_dir"
printf 'Built %s\n' "$app_dir"
