#!/usr/bin/env python3
"""Center-crop to 3:4 and resize a generated portrait for dialog UI."""

from __future__ import annotations

import argparse
import sys
from pathlib import Path

try:
    from PIL import Image
except ImportError:
    print("Pillow required: pip install Pillow", file=sys.stderr)
    sys.exit(1)

DEFAULT_SIZE = (384, 512)
PORTRAIT_ASPECT = DEFAULT_SIZE[0] / DEFAULT_SIZE[1]


def fit_portrait(img: Image.Image, size: tuple[int, int]) -> Image.Image:
    img = img.convert("RGB")
    w, h = img.size
    target_aspect = size[0] / size[1]
    current = w / h
    if current > target_aspect:
        new_w = int(h * target_aspect)
        left = (w - new_w) // 2
        img = img.crop((left, 0, left + new_w, h))
    else:
        new_h = int(w / target_aspect)
        top = (h - new_h) // 2
        img = img.crop((0, top, w, top + new_h))
    return img.resize(size, Image.Resampling.LANCZOS)


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("source", type=Path)
    parser.add_argument("dest", type=Path)
    parser.add_argument("--width", type=int, default=DEFAULT_SIZE[0])
    parser.add_argument("--height", type=int, default=DEFAULT_SIZE[1])
    args = parser.parse_args()

    if not args.source.is_file():
        print(f"Source not found: {args.source}", file=sys.stderr)
        sys.exit(1)

    size = (args.width, args.height)
    args.dest.parent.mkdir(parents=True, exist_ok=True)
    out = fit_portrait(Image.open(args.source), size)
    out.save(args.dest, optimize=True)
    print(f"Wrote {args.dest} ({out.size[0]}x{out.size[1]})")


if __name__ == "__main__":
    main()
