#!/usr/bin/env python3
"""Key grey background from Boris cutout and place on 1536×1024 RGBA canvas."""

from __future__ import annotations

import argparse
import sys
from pathlib import Path

try:
    import numpy as np
    from PIL import Image
except ImportError:
    print("Requires Pillow and numpy: pip install Pillow numpy", file=sys.stderr)
    sys.exit(1)

CANVAS_SIZE = (1536, 1024)
BG_RGB = (43, 43, 43)
DEFAULT_TOLERANCE = 28


def hard_key_rgba(img: Image.Image, bg_rgb: tuple[int, int, int], tolerance: int) -> Image.Image:
    rgb = img.convert("RGB")
    arr = np.array(rgb, dtype=np.float32)
    r, g, b = arr[..., 0], arr[..., 1], arr[..., 2]
    bg_r, bg_g, bg_b = bg_rgb
    dist = np.sqrt((r - bg_r) ** 2 + (g - bg_g) ** 2 + (b - bg_b) ** 2)
    alpha = np.where(dist <= tolerance, 0, 255).astype(np.uint8)
    rgba = np.dstack([arr.astype(np.uint8), alpha])
    return Image.fromarray(rgba, "RGBA")


def opaque_bbox(img: Image.Image) -> tuple[int, int, int, int]:
    alpha = np.array(img.split()[-1])
    ys, xs = np.where(alpha > 0)
    if len(xs) == 0:
        raise ValueError("No opaque pixels after background key")
    return int(xs.min()), int(ys.min()), int(xs.max()) + 1, int(ys.max()) + 1


def place_on_canvas(
    cutout: Image.Image,
    x1: float,
    y1: float,
    x2: float,
    y2: float,
    canvas_size: tuple[int, int] = CANVAS_SIZE,
    hide_below_y: float | None = None,
) -> Image.Image:
    canvas_w, canvas_h = canvas_size
    target_x1 = int(round(x1 * canvas_w))
    target_y1 = int(round(y1 * canvas_h))
    target_x2 = int(round(x2 * canvas_w))
    target_y2 = int(round(y2 * canvas_h))
    target_w = max(1, target_x2 - target_x1)
    target_h = max(1, target_y2 - target_y1)

    bbox = opaque_bbox(cutout)
    subject = cutout.crop(bbox)
    sw, sh = subject.size
    scale = min(target_w / sw, target_h / sh)
    new_w = max(1, int(round(sw * scale)))
    new_h = max(1, int(round(sh * scale)))
    subject = subject.resize((new_w, new_h), Image.Resampling.LANCZOS)

    # Re-hard-key after resize so resized edge pixels stay opaque, not grey
    subject_arr = np.array(subject)
    subject_arr[..., 3] = np.where(subject_arr[..., 3] > 127, 255, 0).astype(np.uint8)
    subject = Image.fromarray(subject_arr, "RGBA")

    paste_x = target_x1 + (target_w - new_w) // 2
    paste_y = target_y2 - new_h

    canvas = Image.new("RGBA", canvas_size, (0, 0, 0, 0))
    canvas.paste(subject, (paste_x, paste_y), subject)

    if hide_below_y is not None:
        clip_y = int(round(hide_below_y * canvas_h))
        alpha = np.array(canvas.split()[-1])
        alpha[clip_y:, :] = 0
        canvas.putalpha(Image.fromarray(alpha))

    return canvas


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("source", type=Path, help="Generated Boris PNG on #2B2B2B background")
    parser.add_argument("dest", type=Path, help="Output path, e.g. Conscript/img/cafe-boris.png")
    parser.add_argument("--tolerance", type=int, default=DEFAULT_TOLERANCE)
    parser.add_argument("--x1", type=float, default=0.154)
    parser.add_argument("--y1", type=float, default=0.20)
    parser.add_argument("--x2", type=float, default=0.393)
    parser.add_argument("--y2", type=float, default=0.92)
    parser.add_argument(
        "--hide-below-y",
        type=float,
        default=None,
        help="Zero alpha at and below this normalized canvas Y (hides legs behind counter)",
    )
    args = parser.parse_args()

    if not args.source.is_file():
        print(f"Source not found: {args.source}", file=sys.stderr)
        sys.exit(1)

    keyed = hard_key_rgba(Image.open(args.source), BG_RGB, args.tolerance)
    out = place_on_canvas(
        keyed, args.x1, args.y1, args.x2, args.y2, hide_below_y=args.hide_below_y
    )
    args.dest.parent.mkdir(parents=True, exist_ok=True)
    out.save(args.dest, optimize=True)

    alpha = np.array(out.split()[-1])
    semi = int(np.sum((alpha > 0) & (alpha < 255)))
    print(f"Wrote {args.dest} ({out.size[0]}x{out.size[1]}), semi-transparent pixels: {semi}")


if __name__ == "__main__":
    main()
