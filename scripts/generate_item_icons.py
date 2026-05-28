#!/usr/bin/env python3
from __future__ import annotations

import math
import random
from dataclasses import dataclass

from PIL import Image, ImageChops, ImageDraw, ImageEnhance, ImageFilter


W, H = 1536, 1024


def _clamp(x: float, lo: float = 0.0, hi: float = 1.0) -> float:
    return lo if x < lo else hi if x > hi else x


def _lerp(a: float, b: float, t: float) -> float:
    return a + (b - a) * t


def _add_vignette(img: Image.Image, strength: float = 0.85) -> Image.Image:
    mask = Image.new("L", (W, H), 0)
    d = ImageDraw.Draw(mask)
    d.ellipse((-W * 0.15, -H * 0.25, W * 1.15, H * 1.25), fill=255)
    mask = mask.filter(ImageFilter.GaussianBlur(120))
    mask = ImageEnhance.Contrast(mask).enhance(1.4)
    dark = Image.new("RGB", (W, H), (8, 8, 10))
    out = Image.composite(img, dark, ImageChops.invert(mask.point(lambda p: int(p * strength))))
    return out


def _noise_layer(seed: int, amount: int = 18) -> Image.Image:
    rng = random.Random(seed)
    n = Image.new("L", (W, H))
    px = n.load()
    for y in range(H):
        for x in range(W):
            px[x, y] = rng.randrange(0, 256)
    n = n.filter(ImageFilter.GaussianBlur(0.9))
    n = ImageEnhance.Contrast(n).enhance(1.35)
    n = ImageEnhance.Brightness(n).enhance(0.55)
    return n.point(lambda p: int(p * amount / 255))


def _textured_bg(seed: int) -> Image.Image:
    base = Image.new("RGB", (W, H), (18, 18, 20))
    grad = Image.new("L", (W, H))
    gp = grad.load()
    for y in range(H):
        for x in range(W):
            dx = (x - W * 0.52) / (W * 0.55)
            dy = (y - H * 0.48) / (H * 0.65)
            r = math.sqrt(dx * dx + dy * dy)
            v = int(_clamp(1.0 - r) * 255)
            gp[x, y] = v
    grad = grad.filter(ImageFilter.GaussianBlur(28))
    bg = ImageEnhance.Brightness(base).enhance(0.95)
    bg = Image.composite(bg, Image.new("RGB", (W, H), (12, 12, 14)), ImageChops.invert(grad))

    n = _noise_layer(seed, amount=35)
    bg = ImageChops.add(bg, Image.merge("RGB", (n, n, n)))
    bg = _add_vignette(bg, strength=0.78)
    return bg


def _drop_shadow(mask: Image.Image, dx: int, dy: int, blur: int, alpha: int) -> Image.Image:
    sh = ImageChops.offset(mask, dx, dy)
    sh = sh.filter(ImageFilter.GaussianBlur(blur))
    sh = ImageEnhance.Brightness(sh).enhance(alpha / 255.0)
    return sh


def _screen_blend(base: Image.Image, top: Image.Image, opacity: float) -> Image.Image:
    # screen: 1 - (1-a)(1-b)
    a = base.convert("RGB")
    b = top.convert("RGB")
    inv = ImageChops.invert(ImageChops.multiply(ImageChops.invert(a), ImageChops.invert(b)))
    return Image.blend(a, inv, _clamp(opacity))


def _metal_fill(seed: int, tint=(165, 170, 176)) -> Image.Image:
    rng = random.Random(seed)
    tex = Image.new("RGB", (W, H), tint)
    # add directional scratch noise
    n = Image.new("L", (W, H))
    px = n.load()
    for y in range(H):
        for x in range(W):
            v = rng.randrange(0, 256)
            if (x + y) % 19 == 0:
                v = 255
            px[x, y] = v
    n = n.filter(ImageFilter.MotionBlur(18, 25)) if hasattr(ImageFilter, "MotionBlur") else n.filter(ImageFilter.GaussianBlur(1.3))
    n = ImageEnhance.Contrast(n).enhance(1.6)
    n = ImageEnhance.Brightness(n).enhance(0.45)
    n_rgb = Image.merge("RGB", (n, n, n))
    tex = ImageChops.subtract(tex, n_rgb)
    tex = ImageEnhance.Color(tex).enhance(0.35)
    return tex


def _paint_fill(seed: int, tint=(120, 28, 28)) -> Image.Image:
    rng = random.Random(seed)
    tex = Image.new("RGB", (W, H), tint)
    n = _noise_layer(seed + 31, amount=52)
    tex = ImageChops.add(tex, Image.merge("RGB", (n, n, n)))
    tex = ImageEnhance.Contrast(tex).enhance(1.15)
    # chip mask
    chips = Image.new("L", (W, H), 0)
    d = ImageDraw.Draw(chips)
    for _ in range(1400):
        x = rng.randrange(0, W)
        y = rng.randrange(0, H)
        r = rng.randrange(1, 4)
        d.ellipse((x - r, y - r, x + r, y + r), fill=rng.randrange(160, 255))
    chips = chips.filter(ImageFilter.GaussianBlur(1.0))
    metal = _metal_fill(seed + 91, tint=(150, 154, 160))
    tex = Image.composite(metal, tex, chips)
    return tex


def _paste_with_mask(bg: Image.Image, layer: Image.Image, mask: Image.Image) -> Image.Image:
    out = bg.copy()
    out.paste(layer, (0, 0), mask)
    return out


def render_crowbar(out_path: str) -> None:
    seed = 1007
    bg = _textured_bg(seed)

    mask = Image.new("L", (W, H), 0)
    d = ImageDraw.Draw(mask)

    # crowbar geometry in local coords
    cx, cy = W * 0.58, H * 0.56
    angle = -22 * math.pi / 180.0

    def rot(x: float, y: float) -> tuple[float, float]:
        xr = x * math.cos(angle) - y * math.sin(angle)
        yr = x * math.sin(angle) + y * math.cos(angle)
        return (cx + xr, cy + yr)

    # shaft
    shaft_len = 620
    shaft_w = 74
    p = []
    for (x, y) in [
        (-shaft_len / 2, -shaft_w / 2),
        (shaft_len / 2, -shaft_w / 2),
        (shaft_len / 2, shaft_w / 2),
        (-shaft_len / 2, shaft_w / 2),
    ]:
        p.append(rot(x, y))
    d.polygon(p, fill=255)

    # hooked head
    head_r = 110
    hx, hy = rot(shaft_len / 2 - 20, 0)
    d.ellipse((hx - head_r, hy - head_r, hx + head_r, hy + head_r), fill=255)
    # cut out inner hook
    d.ellipse((hx - 62, hy - 62, hx + 62, hy + 62), fill=0)
    # flatten claw opening
    d.polygon([rot(shaft_len / 2 + 55, -90), rot(shaft_len / 2 + 160, -20), rot(shaft_len / 2 + 55, 60)], fill=0)

    # tapered tail
    tx, ty = rot(-shaft_len / 2 + 10, 0)
    d.ellipse((tx - 46, ty - 46, tx + 46, ty + 46), fill=255)

    mask = mask.filter(ImageFilter.GaussianBlur(0.6))

    shadow = _drop_shadow(mask, dx=22, dy=26, blur=22, alpha=190)
    bg = Image.composite(bg, Image.new("RGB", (W, H), (0, 0, 0)), shadow)

    metal = _metal_fill(seed + 1, tint=(172, 176, 182))
    paint = _paint_fill(seed + 2, tint=(118, 24, 24))

    # paint mostly on the shaft, some bare metal near ends
    paint_mask = Image.new("L", (W, H), 0)
    pd = ImageDraw.Draw(paint_mask)
    # central band
    pd.rounded_rectangle((W * 0.23, H * 0.44, W * 0.86, H * 0.69), radius=90, fill=210)
    paint_mask = ImageChops.multiply(paint_mask, mask)
    paint_mask = paint_mask.filter(ImageFilter.GaussianBlur(6))

    crowbar = Image.composite(paint, metal, paint_mask)
    bg = _paste_with_mask(bg, crowbar, mask)

    # subtle highlight
    highlight = Image.new("RGB", (W, H), (80, 90, 100))
    hl_mask = Image.new("L", (W, H), 0)
    hld = ImageDraw.Draw(hl_mask)
    hld.polygon([rot(-220, -30), rot(310, -30), rot(280, -8), rot(-260, -8)], fill=120)
    hl_mask = ImageChops.multiply(hl_mask, mask).filter(ImageFilter.GaussianBlur(8))
    bg = _screen_blend(bg, _paste_with_mask(Image.new("RGB", (W, H), (0, 0, 0)), highlight, hl_mask), 0.55)

    # global finish
    bg = ImageEnhance.Contrast(bg).enhance(1.06)
    bg = ImageEnhance.Sharpness(bg).enhance(1.2)
    bg.save(out_path, "PNG")


def render_vodka(out_path: str) -> None:
    seed = 2009
    bg = _textured_bg(seed)

    mask = Image.new("L", (W, H), 0)
    d = ImageDraw.Draw(mask)

    # bottle (centered)
    bx0, by0 = int(W * 0.43), int(H * 0.17)
    bx1, by1 = int(W * 0.57), int(H * 0.84)

    # body
    d.rounded_rectangle((bx0, by0 + 160, bx1, by1), radius=58, fill=255)
    # neck
    nx0, nx1 = int(W * 0.475), int(W * 0.525)
    d.rounded_rectangle((nx0, by0 + 70, nx1, by0 + 200), radius=26, fill=255)
    # cap
    d.rounded_rectangle((nx0 - 10, by0 + 40, nx1 + 10, by0 + 90), radius=22, fill=255)

    mask = mask.filter(ImageFilter.GaussianBlur(0.8))

    shadow = _drop_shadow(mask, dx=18, dy=26, blur=26, alpha=200)
    bg = Image.composite(bg, Image.new("RGB", (W, H), (0, 0, 0)), shadow)

    glass = Image.new("RGB", (W, H), (165, 170, 176))
    # vertical gradient in the bottle
    grad = Image.new("L", (W, H), 0)
    gp = grad.load()
    for y in range(H):
        t = _clamp((y - by0) / (by1 - by0))
        v = int(_lerp(245, 165, t))
        for x in range(W):
            gp[x, y] = v
    grad = grad.filter(ImageFilter.GaussianBlur(18))
    glass = Image.composite(Image.new("RGB", (W, H), (190, 194, 200)), glass, grad)

    # liquid (slight warm tint)
    liquid = Image.new("RGB", (W, H), (190, 180, 165))
    liquid_mask = Image.new("L", (W, H), 0)
    lm = ImageDraw.Draw(liquid_mask)
    fill_y = int(H * 0.73)
    lm.rounded_rectangle((bx0 + 18, by0 + 210, bx1 - 18, fill_y), radius=44, fill=220)
    liquid_mask = ImageChops.multiply(liquid_mask, mask).filter(ImageFilter.GaussianBlur(6))

    bottle = Image.composite(liquid, glass, liquid_mask)

    # label
    label = Image.new("RGB", (W, H), (36, 52, 78))
    label_mask = Image.new("L", (W, H), 0)
    ld = ImageDraw.Draw(label_mask)
    ly0, ly1 = int(H * 0.43), int(H * 0.56)
    ld.rounded_rectangle((bx0 + 10, ly0, bx1 - 10, ly1), radius=28, fill=210)
    label_mask = ImageChops.multiply(label_mask, mask).filter(ImageFilter.GaussianBlur(2))
    bottle = Image.composite(label, bottle, label_mask)

    # cap tint
    cap = Image.new("RGB", (W, H), (24, 28, 34))
    cap_mask = Image.new("L", (W, H), 0)
    cd = ImageDraw.Draw(cap_mask)
    cd.rounded_rectangle((nx0 - 10, by0 + 40, nx1 + 10, by0 + 90), radius=22, fill=210)
    cap_mask = ImageChops.multiply(cap_mask, mask).filter(ImageFilter.GaussianBlur(2))
    bottle = Image.composite(cap, bottle, cap_mask)

    # grime + scratches
    n = _noise_layer(seed + 7, amount=22)
    bottle = ImageChops.subtract(bottle, Image.merge("RGB", (n, n, n)))

    # specular highlights (screen blend)
    spec = Image.new("RGB", (W, H), (150, 160, 170))
    spec_mask = Image.new("L", (W, H), 0)
    sd = ImageDraw.Draw(spec_mask)
    sd.rounded_rectangle((bx0 + 26, by0 + 190, bx0 + 60, by1 - 40), radius=20, fill=120)
    sd.rounded_rectangle((bx1 - 64, by0 + 240, bx1 - 38, by1 - 60), radius=20, fill=70)
    spec_mask = ImageChops.multiply(spec_mask, mask).filter(ImageFilter.GaussianBlur(10))
    spec_layer = _paste_with_mask(Image.new("RGB", (W, H), (0, 0, 0)), spec, spec_mask)
    bottle = _screen_blend(bottle, spec_layer, 0.65)

    bg = _paste_with_mask(bg, bottle, mask)
    bg = ImageEnhance.Contrast(bg).enhance(1.04)
    bg = ImageEnhance.Sharpness(bg).enhance(1.25)
    bg.save(out_path, "PNG")


def render_rag(out_path: str) -> None:
    seed = 3013
    bg = _textured_bg(seed)

    mask = Image.new("L", (W, H), 0)
    d = ImageDraw.Draw(mask)

    # Rag: an irregular folded cloth, slightly rotated
    cx, cy = W * 0.52, H * 0.56
    angle = 18 * math.pi / 180.0

    def rot(x: float, y: float) -> tuple[float, float]:
        xr = x * math.cos(angle) - y * math.sin(angle)
        yr = x * math.sin(angle) + y * math.cos(angle)
        return (cx + xr, cy + yr)

    rag_w = 540
    rag_h = 360
    # base cloth shape with wavy edges
    pts = [
        (-rag_w * 0.50, -rag_h * 0.40),
        (-rag_w * 0.10, -rag_h * 0.52),
        (rag_w * 0.42, -rag_h * 0.32),
        (rag_w * 0.52, rag_h * 0.05),
        (rag_w * 0.36, rag_h * 0.48),
        (-rag_w * 0.18, rag_h * 0.54),
        (-rag_w * 0.52, rag_h * 0.22),
    ]
    d.polygon([rot(x, y) for (x, y) in pts], fill=255)

    # fold cutouts
    d.polygon([rot(-160, -30), rot(40, -120), rot(220, 10), rot(20, 70)], fill=0)
    d.polygon([rot(-260, 120), rot(-40, 40), rot(80, 220), rot(-140, 260)], fill=0)

    mask = mask.filter(ImageFilter.GaussianBlur(1.4))

    shadow = _drop_shadow(mask, dx=18, dy=26, blur=26, alpha=200)
    bg = Image.composite(bg, Image.new("RGB", (W, H), (0, 0, 0)), shadow)

    # Cloth texture
    cloth = Image.new("RGB", (W, H), (152, 144, 132))
    n = _noise_layer(seed + 1, amount=70)
    cloth = ImageChops.add(cloth, Image.merge("RGB", (n, n, n)))
    cloth = ImageEnhance.Color(cloth).enhance(0.55)
    cloth = ImageEnhance.Contrast(cloth).enhance(1.12)

    # stains
    stain = Image.new("L", (W, H), 0)
    sd = ImageDraw.Draw(stain)
    rng = random.Random(seed + 2)
    for _ in range(26):
        x = int(cx + rng.uniform(-rag_w * 0.32, rag_w * 0.32))
        y = int(cy + rng.uniform(-rag_h * 0.28, rag_h * 0.28))
        rx = rng.randint(30, 110)
        ry = rng.randint(22, 90)
        sd.ellipse((x - rx, y - ry, x + rx, y + ry), fill=rng.randint(40, 110))
    stain = stain.filter(ImageFilter.GaussianBlur(22))
    stain = ImageChops.multiply(stain, mask)
    stain_rgb = Image.merge("RGB", (stain, stain, stain))
    cloth = ImageChops.subtract(cloth, stain_rgb)

    # subtle weave highlight
    weave = _noise_layer(seed + 3, amount=26).filter(ImageFilter.GaussianBlur(0.6))
    cloth = ImageChops.add(cloth, Image.merge("RGB", (weave, weave, weave)))

    bg = _paste_with_mask(bg, cloth, mask)

    # rim highlight and a soft crease
    rim = Image.new("RGB", (W, H), (170, 170, 175))
    rim_mask = mask.filter(ImageFilter.FIND_EDGES).filter(ImageFilter.GaussianBlur(3))
    bg = _screen_blend(bg, _paste_with_mask(Image.new("RGB", (W, H), (0, 0, 0)), rim, rim_mask), 0.25)

    crease = Image.new("L", (W, H), 0)
    cd = ImageDraw.Draw(crease)
    cd.polygon([rot(-260, -10), rot(-40, -150), rot(210, 40), rot(-10, 160)], fill=90)
    crease = ImageChops.multiply(crease.filter(ImageFilter.GaussianBlur(14)), mask)
    bg = ImageChops.subtract(bg, Image.merge("RGB", (crease, crease, crease)))

    bg = ImageEnhance.Contrast(bg).enhance(1.05)
    bg = ImageEnhance.Sharpness(bg).enhance(1.22)
    bg.save(out_path, "PNG")


def render_molotov_unlit(out_path: str) -> None:
    seed = 4019
    bg = _textured_bg(seed)

    # Bottle mask (reuse vodka proportions)
    mask = Image.new("L", (W, H), 0)
    d = ImageDraw.Draw(mask)
    bx0, by0 = int(W * 0.43), int(H * 0.17)
    bx1, by1 = int(W * 0.57), int(H * 0.84)
    d.rounded_rectangle((bx0, by0 + 160, bx1, by1), radius=58, fill=255)  # body
    nx0, nx1 = int(W * 0.475), int(W * 0.525)
    d.rounded_rectangle((nx0, by0 + 70, nx1, by0 + 200), radius=26, fill=255)  # neck
    # NOTE: no cap for molotov; rag is stuffed in the neck.
    mask = mask.filter(ImageFilter.GaussianBlur(0.8))

    shadow = _drop_shadow(mask, dx=18, dy=26, blur=26, alpha=200)
    bg = Image.composite(bg, Image.new("RGB", (W, H), (0, 0, 0)), shadow)

    # Glass + liquid base (like vodka, but a touch dirtier)
    glass = Image.new("RGB", (W, H), (162, 168, 176))
    grad = Image.new("L", (W, H), 0)
    gp = grad.load()
    for y in range(H):
        t = _clamp((y - by0) / (by1 - by0))
        v = int(_lerp(240, 160, t))
        for x in range(W):
            gp[x, y] = v
    grad = grad.filter(ImageFilter.GaussianBlur(18))
    glass = Image.composite(Image.new("RGB", (W, H), (186, 192, 200)), glass, grad)

    liquid = Image.new("RGB", (W, H), (186, 176, 160))
    liquid_mask = Image.new("L", (W, H), 0)
    lm = ImageDraw.Draw(liquid_mask)
    fill_y = int(H * 0.73)
    lm.rounded_rectangle((bx0 + 18, by0 + 210, bx1 - 18, fill_y), radius=44, fill=220)
    liquid_mask = ImageChops.multiply(liquid_mask, mask).filter(ImageFilter.GaussianBlur(6))
    bottle = Image.composite(liquid, glass, liquid_mask)

    # label (torn / muted)
    label = Image.new("RGB", (W, H), (34, 48, 70))
    label_mask = Image.new("L", (W, H), 0)
    ld = ImageDraw.Draw(label_mask)
    ly0, ly1 = int(H * 0.43), int(H * 0.56)
    ld.rounded_rectangle((bx0 + 10, ly0, bx1 - 10, ly1), radius=28, fill=175)
    # tear notch
    ld.polygon([(bx1 - 30, ly0 + 18), (bx1 + 10, ly0 + 44), (bx1 - 30, ly0 + 74)], fill=0)
    label_mask = ImageChops.multiply(label_mask, mask).filter(ImageFilter.GaussianBlur(2))
    bottle = Image.composite(label, bottle, label_mask)

    # dark bottle opening (subtle)
    opening = Image.new("RGB", (W, H), (26, 26, 28))
    opening_mask = Image.new("L", (W, H), 0)
    od = ImageDraw.Draw(opening_mask)
    od.ellipse((nx0 - 10, by0 + 68, nx1 + 10, by0 + 98), fill=190)
    opening_mask = opening_mask.filter(ImageFilter.GaussianBlur(3))
    bottle = Image.composite(opening, bottle, opening_mask)

    # grime + scratches
    n = _noise_layer(seed + 7, amount=24)
    bottle = ImageChops.subtract(bottle, Image.merge("RGB", (n, n, n)))

    # specular highlights
    spec = Image.new("RGB", (W, H), (150, 160, 170))
    spec_mask = Image.new("L", (W, H), 0)
    sd = ImageDraw.Draw(spec_mask)
    sd.rounded_rectangle((bx0 + 26, by0 + 190, bx0 + 60, by1 - 40), radius=20, fill=120)
    sd.rounded_rectangle((bx1 - 64, by0 + 240, bx1 - 38, by1 - 60), radius=20, fill=70)
    spec_mask = ImageChops.multiply(spec_mask, mask).filter(ImageFilter.GaussianBlur(10))
    spec_layer = _paste_with_mask(Image.new("RGB", (W, H), (0, 0, 0)), spec, spec_mask)
    bottle = _screen_blend(bottle, spec_layer, 0.65)

    # Rag stuffed in neck (key distinguishing feature)
    rag_mask = Image.new("L", (W, H), 0)
    rd = ImageDraw.Draw(rag_mask)
    cx = (nx0 + nx1) // 2
    rag_top = by0 - 10
    rag_base = by0 + 110
    # cloth emerging from bottle opening, with jagged/frayed top
    rd.polygon(
        [
            (cx - 55, rag_base),
            (cx + 55, rag_base),
            (cx + 70, rag_base - 42),
            (cx + 34, rag_top + 40),
            (cx + 10, rag_top + 18),
            (cx - 8, rag_top + 42),
            (cx - 26, rag_top + 20),
            (cx - 62, rag_top + 54),
            (cx - 76, rag_base - 34),
        ],
        fill=230,
    )
    # fray spikes
    for i in range(10):
        x = cx - 58 + i * 12
        rd.polygon([(x, rag_top + 38), (x + 6, rag_top + 6), (x + 12, rag_top + 38)], fill=120)
    rag_mask = rag_mask.filter(ImageFilter.GaussianBlur(2.2))

    rag = Image.new("RGB", (W, H), (170, 162, 150))
    rn = _noise_layer(seed + 21, amount=72)
    rag = ImageChops.add(rag, Image.merge("RGB", (rn, rn, rn)))
    rag = ImageEnhance.Color(rag).enhance(0.55)
    rag = ImageEnhance.Contrast(rag).enhance(1.14)
    # soot at tip (unlit, but char-stained)
    soot = Image.new("L", (W, H), 0)
    sdd = ImageDraw.Draw(soot)
    sdd.ellipse((cx - 90, rag_top - 10, cx + 90, rag_top + 90), fill=95)
    soot = soot.filter(ImageFilter.GaussianBlur(26))
    soot = ImageChops.multiply(soot, rag_mask)
    rag = ImageChops.subtract(rag, Image.merge("RGB", (soot, soot, soot)))

    # slight rim shadow where rag enters bottle
    entry_shadow = Image.new("L", (W, H), 0)
    es = ImageDraw.Draw(entry_shadow)
    es.ellipse((cx - 46, by0 + 52, cx + 46, by0 + 92), fill=70)
    entry_shadow = entry_shadow.filter(ImageFilter.GaussianBlur(10))
    bottle = ImageChops.subtract(bottle, Image.merge("RGB", (entry_shadow, entry_shadow, entry_shadow)))

    bg = _paste_with_mask(bg, bottle, mask)

    rag_shadow = _drop_shadow(rag_mask, dx=8, dy=12, blur=14, alpha=160).point(lambda p: int(p * 0.45))
    bg = ImageChops.subtract(bg, Image.merge("RGB", (rag_shadow, rag_shadow, rag_shadow)))
    bg = _paste_with_mask(bg, rag, rag_mask)
    bg = ImageEnhance.Contrast(bg).enhance(1.05)
    bg = ImageEnhance.Sharpness(bg).enhance(1.22)
    bg.save(out_path, "PNG")


@dataclass(frozen=True)
class Output:
    crowbar_path: str
    vodka_path: str
    rag_path: str
    molotov_path: str


def main() -> None:
    out = Output(
        crowbar_path="Conscript/img/items/crowbar.png",
        vodka_path="Conscript/img/items/vodka.png",
        rag_path="Conscript/img/items/rag.png",
        molotov_path="Conscript/img/items/molotov.png",
    )
    render_crowbar(out.crowbar_path)
    render_vodka(out.vodka_path)
    render_rag(out.rag_path)
    render_molotov_unlit(out.molotov_path)
    print(f"Wrote {out.crowbar_path}")
    print(f"Wrote {out.vodka_path}")
    print(f"Wrote {out.rag_path}")
    print(f"Wrote {out.molotov_path}")


if __name__ == "__main__":
    main()

