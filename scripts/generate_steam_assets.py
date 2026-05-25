#!/usr/bin/env python3
"""Resize source art into Windows .ico and Steamworks graphical assets."""

from __future__ import annotations

import shutil
import struct
import subprocess
import sys
import tempfile
from pathlib import Path

from PIL import Image, ImageDraw, ImageFilter, ImageFont

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "assets" / "source"
ICONS = ROOT / "assets" / "icons"
STEAM = ROOT / "assets" / "steam"

# Steamworks standard sizes (Aug 2024+)
STEAM_SIZES = {
    "header-capsule.png": (920, 430),
    "small-capsule.png": (462, 174),
    "main-capsule.png": (1232, 706),
    "vertical-capsule.png": (748, 896),
    "library-capsule.png": (600, 900),
    "library-header-capsule.png": (920, 430),
    "library-hero.png": (3840, 1240),
    "page-background.png": (1438, 810),
}

ICO_SIZES = (16, 32, 48, 64, 128, 256)


def _fit_cover(img: Image.Image, size: tuple[int, int]) -> Image.Image:
    """Center-crop resize (cover)."""
    target_w, target_h = size
    src_w, src_h = img.size
    scale = max(target_w / src_w, target_h / src_h)
    resized = img.resize(
        (int(src_w * scale), int(src_h * scale)), Image.Resampling.LANCZOS
    )
    left = (resized.width - target_w) // 2
    top = (resized.height - target_h) // 2
    return resized.crop((left, top, left + target_w, top + target_h))


def _fit_contain_on_bg(
    img: Image.Image, size: tuple[int, int], bg: tuple[int, int, int] = (7, 8, 11)
) -> Image.Image:
    """Letterbox onto solid background (library hero)."""
    target_w, target_h = size
    canvas = Image.new("RGB", size, bg)
    src_w, src_h = img.size
    scale = min(target_w / src_w, target_h / src_h)
    new_w, new_h = int(src_w * scale), int(src_h * scale)
    resized = img.resize((new_w, new_h), Image.Resampling.LANCZOS)
    x = (target_w - new_w) // 2
    y = (target_h - new_h) // 2
    if resized.mode == "RGBA":
        canvas.paste(resized, (x, y), resized)
    else:
        canvas.paste(resized, (x, y))
    return canvas


def _load_font(size: int) -> ImageFont.FreeTypeFont | ImageFont.ImageFont:
    candidates = [
        "/System/Library/Fonts/Supplemental/STIXTwoText-Bold.ttf",
        "/System/Library/Fonts/Supplemental/Arial Bold.ttf",
        "/Library/Fonts/Arial Bold.ttf",
    ]
    for path in candidates:
        if Path(path).exists():
            return ImageFont.truetype(path, size)
    return ImageFont.load_default()


def make_library_logo(icon: Image.Image) -> Image.Image:
    """Transparent PNG logo for library hero overlay."""
    w, h = 1280, 720
    canvas = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    emblem = icon.resize((220, 220), Image.Resampling.LANCZOS)
    if emblem.mode != "RGBA":
        emblem = emblem.convert("RGBA")
    canvas.paste(emblem, (48, (h - 220) // 2), emblem)

    draw = ImageDraw.Draw(canvas)
    font = _load_font(96)
    draw.text((300, h // 2 - 70), "CONSCRIPT", fill=(232, 228, 218, 255), font=font)
    return canvas


def write_ico(path: Path, sizes: tuple[int, ...], square: Image.Image) -> None:
    """Write multi-resolution .ico without extra dependencies."""
    images: list[tuple[int, bytes]] = []
    for dim in sizes:
        frame = square.resize((dim, dim), Image.Resampling.LANCZOS).convert("RGBA")
        images.append((dim, _png_bytes(frame)))

    offset = 6 + 16 * len(images)
    header = struct.pack("<HHH", 0, 1, len(images))
    entries = b""
    data = b""
    for dim, png in images:
        entries += struct.pack(
            "<BBBBHHII",
            dim if dim < 256 else 0,
            dim if dim < 256 else 0,
            0,
            0,
            1,
            32,
            len(png),
            offset,
        )
        offset += len(png)
        data += png

    path.write_bytes(header + entries + data)


def _png_bytes(img: Image.Image) -> bytes:
    import io

    buf = io.BytesIO()
    img.save(buf, format="PNG")
    return buf.getvalue()


def sharpen_for_small(img: Image.Image) -> Image.Image:
    """Slight sharpen when downscaling capsules."""
    if max(img.size) > 500:
        return img
    return img.filter(ImageFilter.UnsharpMask(radius=0.8, percent=120, threshold=2))


def main() -> int:
    icon_path = SOURCE / "conscript-icon-master.png"
    capsule_path = SOURCE / "conscript-capsule-master.png"
    vertical_path = SOURCE / "conscript-vertical-master.png"

    for p in (icon_path, capsule_path, vertical_path):
        if not p.exists():
            print(f"Missing source: {p}", file=sys.stderr)
            return 1

    ICONS.mkdir(parents=True, exist_ok=True)
    STEAM.mkdir(parents=True, exist_ok=True)

    icon = Image.open(icon_path).convert("RGBA")
    capsule = Image.open(capsule_path).convert("RGB")
    vertical = Image.open(vertical_path).convert("RGB")

    # --- Windows / Steam client icons ---
    icon_square = _fit_cover(icon, (512, 512))
    icon_square.save(ICONS / "conscript-256.png", optimize=True)
    icon_square.resize((256, 256), Image.Resampling.LANCZOS).save(
        ICONS / "shortcut-icon-256.png", optimize=True
    )
    write_ico(ICONS / "conscript.ico", ICO_SIZES, icon_square)

    app_icon = icon_square.resize((184, 184), Image.Resampling.LANCZOS)
    app_icon_rgb = Image.new("RGB", app_icon.size, (7, 8, 11))
    app_icon_rgb.paste(app_icon, mask=app_icon.split()[3])
    app_icon_rgb.save(ICONS / "app-icon-184.jpg", quality=92, optimize=True)

    # --- Store & library capsules ---
    for name, size in STEAM_SIZES.items():
        if name == "library-hero.png":
            out = _fit_contain_on_bg(capsule, size)
        elif name in ("vertical-capsule.png", "library-capsule.png"):
            out = _fit_cover(vertical, size)
        else:
            out = _fit_cover(capsule, size)
        out = sharpen_for_small(out)
        out.save(STEAM / name, optimize=True)
        print(f"Wrote {STEAM / name} ({size[0]}x{size[1]})")

    logo = make_library_logo(icon)
    logo.save(STEAM / "library-logo.png", optimize=True)
    print(f"Wrote {STEAM / 'library-logo.png'} (1280x720)")

    # Preview tiny auto-generated small capsule size
    small = Image.open(STEAM / "small-capsule.png")
    tiny = small.resize((120, 45), Image.Resampling.LANCZOS)
    tiny.save(STEAM / "_preview-small-120x45.png")
    print(f"Wrote preview {STEAM / '_preview-small-120x45.png'} (readability check)")

    icns_path = ICONS / "conscript.icns"
    if shutil.which("iconutil") and sys.platform == "darwin":
        _write_icns(icns_path, icon_square)
        print(f"Wrote {icns_path}")
    else:
        print("Skipped .icns (iconutil only on macOS)")

    print(f"\nDone. Icons: {ICONS}\nSteam: {STEAM}")
    return 0


def _write_icns(path: Path, square: Image.Image) -> None:
    """Build macOS .icns for Steam Mac Icon / desktop shortcuts."""
    iconset_sizes = [16, 32, 64, 128, 256, 512]
    with tempfile.TemporaryDirectory() as tmp:
        iconset = Path(tmp) / "Conscript.iconset"
        iconset.mkdir()
        for dim in iconset_sizes:
            img = square.resize((dim, dim), Image.Resampling.LANCZOS)
            img.save(iconset / f"icon_{dim}x{dim}.png")
            if dim <= 512:
                double = dim * 2
                img2 = square.resize((double, double), Image.Resampling.LANCZOS)
                img2.save(iconset / f"icon_{dim}x{dim}@2x.png")
        subprocess.run(
            ["iconutil", "-c", "icns", str(iconset), "-o", str(path)],
            check=True,
        )


if __name__ == "__main__":
    raise SystemExit(main())
