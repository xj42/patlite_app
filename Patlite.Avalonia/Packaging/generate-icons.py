#!/usr/bin/env python3
"""Convert the original logo into platform icon formats (requires Pillow)."""

from pathlib import Path

from PIL import Image


def main():
    project = Path(__file__).resolve().parent.parent
    assets = project / "Assets"
    assets.mkdir(parents=True, exist_ok=True)

    with Image.open(assets / "logo.png") as source:
        logo = source.convert("RGBA")
        if logo.width != logo.height:
            raise ValueError("logo.png must be square to preserve the artwork's proportions")

        sizes = (16, 24, 32, 48, 64, 128, 256)
        logo.resize((256, 256), Image.Resampling.LANCZOS).save(
            assets / "patlite.ico", sizes=[(size, size) for size in sizes]
        )
        # ICNS includes Retina sizes; the source artwork and transparency are retained.
        logo.resize((1024, 1024), Image.Resampling.LANCZOS).save(assets / "patlite.icns")

        for size in (*sizes, 512):
            folder = assets / "Linux" / "hicolor" / f"{size}x{size}" / "apps"
            folder.mkdir(parents=True, exist_ok=True)
            logo.resize((size, size), Image.Resampling.LANCZOS).save(folder / "patlite.png")

    print(f"Generated Windows, macOS and Linux icons in {assets}")


if __name__ == "__main__":
    main()
