#!/usr/bin/env python3
"""Center-crop to 3:2 and resize a generated image to Conscript scene background size."""

from __future__ import annotations

import argparse
import sys
from pathlib import Path

try:
    from PIL import Image
except ImportError:
    print("Pillow required: pip install Pillow", file=sys.stderr)
    sys.exit(1)

TARGET_SIZE = (1536, 1024)
TARGET_ASPECT = TARGET_SIZE[0] / TARGET_SIZE[1]


def fit_scene_image(img: Image.Image) -> Image.Image:
    img = img.convert("RGB")
    w, h = img.size
    current = w / h
    if current > TARGET_ASPECT:
        new_w = int(h * TARGET_ASPECT)
        left = (w - new_w) // 2
        img = img.crop((left, 0, left + new_w, h))
    else:
        new_h = int(w / TARGET_ASPECT)
        top = (h - new_h) // 2
        img = img.crop((0, top, w, top + new_h))
    return img.resize(TARGET_SIZE, Image.Resampling.LANCZOS)


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("source", type=Path, help="Generated PNG (any size)")
    parser.add_argument("dest", type=Path, help="Output path, e.g. Conscript/img/industrial.png")
    args = parser.parse_args()

    if not args.source.is_file():
        print(f"Source not found: {args.source}", file=sys.stderr)
        sys.exit(1)

    args.dest.parent.mkdir(parents=True, exist_ok=True)
    out = fit_scene_image(Image.open(args.source))
    out.save(args.dest, optimize=True)
    print(f"Wrote {args.dest} ({out.size[0]}x{out.size[1]})")


if __name__ == "__main__":
    main()
