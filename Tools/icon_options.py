"""Draws the Golden Mend app icon concepts (1024x1024 PNG) with Pillow.

Each concept is drawn at 4x and downsampled for smooth edges. Run:
    python Tools/icon_options.py <output folder>
"""
import math
import random
import sys
from pathlib import Path

from PIL import Image, ImageDraw, ImageFilter

S = 4096  # working size, downsampled to 1024
GOLD = (247, 196, 84)
GOLD_LIGHT = (255, 232, 160)
GOLD_DARK = (176, 124, 36)
INDIGO = (40, 52, 98)
INDIGO_DEEP = (22, 28, 58)
INK = (28, 23, 21)
CREAM = (244, 234, 214)
CELADON = (150, 196, 178)
CELADON_DARK = (92, 140, 126)
TERRACOTTA = (204, 110, 74)
CLAY_DARK = (128, 56, 38)


def radial(size, inner, outer, center=(0.5, 0.42)):
    """A soft radial gradient background."""
    img = Image.new("RGB", (size, size), outer)
    px = img.load()
    cx, cy = center[0] * size, center[1] * size
    max_d = size * 0.75
    small = 256
    grad = Image.new("RGB", (small, small))
    gp = grad.load()
    for y in range(small):
        for x in range(small):
            d = math.hypot(x / small * size - cx, y / small * size - cy) / max_d
            t = min(1.0, d)
            gp[x, y] = tuple(int(inner[i] + (outer[i] - inner[i]) * t) for i in range(3))
    return grad.resize((size, size), Image.BICUBIC)


def crack(rng, start, end, jag, steps):
    """A jagged crack polyline from start to end."""
    pts = [start]
    for i in range(1, steps):
        t = i / steps
        x = start[0] + (end[0] - start[0]) * t + rng.uniform(-jag, jag)
        y = start[1] + (end[1] - start[1]) * t + rng.uniform(-jag, jag)
        pts.append((x, y))
    pts.append(end)
    return pts


def gold_seam(draw, pts, width):
    """A kintsugi seam: dark gold base, bright gold body, light highlight."""
    draw.line(pts, fill=GOLD_DARK, width=int(width * 1.35), joint="curve")
    draw.line(pts, fill=GOLD, width=int(width), joint="curve")
    shifted = [(x - width * 0.15, y - width * 0.15) for x, y in pts]
    draw.line(shifted, fill=GOLD_LIGHT, width=max(2, int(width * 0.3)), joint="curve")
    r = width * 0.5
    for x, y in (pts[0], pts[-1]):
        draw.ellipse((x - r, y - r, x + r, y + r), fill=GOLD)


def clip_to(mask, layer):
    out = Image.new("RGBA", layer.size, (0, 0, 0, 0))
    out.paste(layer, (0, 0), mask)
    return out


def finish(img, path):
    img = img.convert("RGB").resize((1024, 1024), Image.LANCZOS)
    img.save(path)
    print("wrote", path)


def bowl_mask(size, top_y, bottom_y, half_width, foot_w):
    """A tea bowl seen from the side: wide rim, rounded body, small foot."""
    m = Image.new("L", (size, size), 0)
    d = ImageDraw.Draw(m)
    cx = size / 2
    body_h = bottom_y - top_y
    d.pieslice((cx - half_width, top_y - body_h, cx + half_width, bottom_y), 0, 180, fill=255)
    d.rectangle((cx - foot_w / 2, bottom_y - body_h * 0.06, cx + foot_w / 2, bottom_y + body_h * 0.12), fill=255)
    return m


def concept_bowl(path):
    """A: celadon tea bowl on indigo, mended with branching gold seams."""
    rng = random.Random(7)
    img = radial(S, (70, 86, 150), INDIGO_DEEP).convert("RGBA")
    mask = bowl_mask(S, S * 0.36, S * 0.78, S * 0.36, S * 0.22)

    body = Image.new("RGBA", (S, S), CELADON + (255,))
    bd = ImageDraw.Draw(body)
    bd.rectangle((0, S * 0.36, S, S * 0.40), fill=(196, 226, 212, 255))  # rim highlight
    for y in (0.47, 0.50):  # two quiet glaze bands
        bd.rectangle((0, S * y, S, S * (y + 0.012)), fill=CELADON_DARK + (255,))
    bd.ellipse((S * 0.22, S * 0.44, S * 0.42, S * 0.70), fill=(178, 214, 198, 255))  # soft sheen
    body = clip_to(mask, body)
    img.alpha_composite(body)

    seams = Image.new("RGBA", (S, S), (0, 0, 0, 0))
    sd = ImageDraw.Draw(seams)
    main = crack(rng, (S * 0.30, S * 0.36), (S * 0.62, S * 0.78), S * 0.025, 9)
    gold_seam(sd, main, S * 0.022)
    gold_seam(sd, crack(rng, main[4], (S * 0.84, S * 0.48), S * 0.02, 6), S * 0.018)
    gold_seam(sd, crack(rng, main[6], (S * 0.26, S * 0.62), S * 0.02, 5), S * 0.016)
    img.alpha_composite(clip_to(mask, seams))

    glow = Image.new("RGBA", (S, S), (0, 0, 0, 0))
    ImageDraw.Draw(glow).ellipse((S * 0.18, S * 0.80, S * 0.82, S * 0.86), fill=(0, 0, 0, 90))
    img.alpha_composite(glow.filter(ImageFilter.GaussianBlur(S * 0.02)))
    finish(img, path)


def concept_tiles(path):
    """B: the game itself - glazed tiles mended with gold, the anchor tile framed."""
    img = radial(S, (52, 44, 40), INK).convert("RGBA")
    d = ImageDraw.Draw(img)
    grid, gap = 3, S * 0.035
    size = (S * 0.64 - gap * (grid - 1)) / grid
    x0 = y0 = S * 0.18
    glaze = [(206, 112, 78), (226, 136, 92), (188, 96, 66), (214, 124, 84), (234, 150, 104), (196, 104, 72), (220, 130, 88), (182, 92, 62), (228, 142, 98)]
    for r in range(grid):
        for c in range(grid):
            x, y = x0 + c * (size + gap), y0 + r * (size + gap)
            d.rounded_rectangle((x, y, x + size, y + size), radius=size * 0.12, fill=glaze[r * grid + c])
            ring = (r * 2 + c) % 3  # the in-game ring motif, offset per tile so the pieces read as one picture
            ox, oy = x + size * (0.25 + 0.25 * ring), y + size * (0.75 - 0.25 * ring)
            d.ellipse((ox - size * 0.3, oy - size * 0.3, ox + size * 0.3, oy + size * 0.3), outline=(250, 224, 196), width=int(size * 0.055))
            d.ellipse((ox - size * 0.07, oy - size * 0.07, ox + size * 0.07, oy + size * 0.07), fill=(250, 224, 196))
    for i in range(1, grid):  # gold seams between every tile
        p = x0 + i * (size + gap) - gap / 2
        gold_seam(d, [(p, y0 - gap * 0.2), (p, y0 + S * 0.64 + gap * 0.2)], gap * 0.9)
        gold_seam(d, [(x0 - gap * 0.2, p), (x0 + S * 0.64 + gap * 0.2, p)], gap * 0.9)
    cx = x0 + (size + gap)
    d.rounded_rectangle((cx - gap * 0.7, cx - gap * 0.7, cx + size + gap * 0.7, cx + size + gap * 0.7), radius=size * 0.16, outline=GOLD, width=int(gap * 0.9))
    finish(img, path)


def concept_plate(path):
    """C: a full moon of a cream plate, broken into shards and mended in gold, on terracotta."""
    rng = random.Random(21)
    img = radial(S, (226, 132, 92), CLAY_DARK).convert("RGBA")
    mask = Image.new("L", (S, S), 0)
    ImageDraw.Draw(mask).ellipse((S * 0.16, S * 0.16, S * 0.84, S * 0.84), fill=255)

    plate = Image.new("RGBA", (S, S), CREAM + (255,))
    pd = ImageDraw.Draw(plate)
    pd.ellipse((S * 0.27, S * 0.27, S * 0.73, S * 0.73), outline=(220, 200, 170), width=int(S * 0.012))
    pd.ellipse((S * 0.40, S * 0.40, S * 0.60, S * 0.60), fill=(232, 218, 194))
    img.alpha_composite(clip_to(mask, plate))

    seams = Image.new("RGBA", (S, S), (0, 0, 0, 0))
    sd = ImageDraw.Draw(seams)
    hub = (S * 0.47, S * 0.53)
    for angle in (-100, -20, 55, 140, 205):
        a = math.radians(angle)
        end = (S * 0.5 + math.cos(a) * S * 0.4, S * 0.5 + math.sin(a) * S * 0.4)
        gold_seam(sd, crack(rng, hub, end, S * 0.02, 6), S * 0.02)
    img.alpha_composite(clip_to(mask, seams))

    rim = Image.new("RGBA", (S, S), (0, 0, 0, 0))
    ImageDraw.Draw(rim).ellipse((S * 0.16, S * 0.16, S * 0.84, S * 0.84), outline=(255, 248, 232, 255), width=int(S * 0.012))
    img.alpha_composite(rim)
    finish(img, path)


def concept_vase(path):
    """D: an indigo vase silhouette on warm cream, one bold gold crack top to bottom."""
    rng = random.Random(3)
    img = radial(S, (252, 244, 228), (226, 210, 182)).convert("RGBA")
    mask = Image.new("L", (S, S), 0)
    md = ImageDraw.Draw(mask)
    cx = S / 2
    # Neck, shoulders, belly and foot built from ellipses and rectangles.
    md.rectangle((cx - S * 0.07, S * 0.14, cx + S * 0.07, S * 0.34), fill=255)
    md.ellipse((cx - S * 0.11, S * 0.11, cx + S * 0.11, S * 0.17), fill=255)
    md.ellipse((cx - S * 0.27, S * 0.30, cx + S * 0.27, S * 0.82), fill=255)
    md.rectangle((cx - S * 0.13, S * 0.76, cx + S * 0.13, S * 0.87), fill=255)

    body = Image.new("RGBA", (S, S), INDIGO + (255,))
    bd = ImageDraw.Draw(body)
    sheen = Image.new("RGBA", (S, S), (0, 0, 0, 0))
    ImageDraw.Draw(sheen).ellipse((cx - S * 0.21, S * 0.36, cx - S * 0.07, S * 0.60), fill=(110, 128, 196, 150))
    body.alpha_composite(sheen.filter(ImageFilter.GaussianBlur(S * 0.03)))
    bd = ImageDraw.Draw(body)
    for y in (0.52, 0.56):
        bd.rectangle((0, S * y, S, S * (y + 0.012)), fill=(214, 200, 168, 255))  # glaze bands
    img.alpha_composite(clip_to(mask, body))

    seams = Image.new("RGBA", (S, S), (0, 0, 0, 0))
    sd = ImageDraw.Draw(seams)
    main = crack(rng, (cx + S * 0.03, S * 0.10), (cx - S * 0.05, S * 0.88), S * 0.03, 11)
    gold_seam(sd, main, S * 0.024)
    gold_seam(sd, crack(rng, main[6], (cx + S * 0.30, S * 0.66), S * 0.02, 5), S * 0.018)
    img.alpha_composite(clip_to(mask, seams))
    finish(img, path)


def contact_sheet(out, names):
    """All options side by side at full size and at home-screen size, on light and dark backgrounds."""
    cell = 360
    sheet = Image.new("RGB", (cell * len(names), cell + 260), (236, 236, 240))
    d = ImageDraw.Draw(sheet)
    d.rectangle((0, cell + 130, sheet.width, sheet.height), fill=(24, 24, 28))
    for i, name in enumerate(names):
        icon = Image.open(out / name).convert("RGBA")
        mask = Image.new("L", (1024, 1024), 0)
        ImageDraw.Draw(mask).rounded_rectangle((0, 0, 1023, 1023), radius=230, fill=255)
        icon.putalpha(mask)
        sheet.paste(icon.resize((320, 320), Image.LANCZOS), (i * cell + 20, 20), icon.resize((320, 320), Image.LANCZOS))
        for j, (size, y) in enumerate(((96, cell + 20), (96, cell + 150))):
            small = icon.resize((size, size), Image.LANCZOS)
            sheet.paste(small, (i * cell + (cell - size) // 2, y), small)
        d.text((i * cell + 20, cell - 8), name.split("-")[0], fill=(60, 60, 70))
    sheet.save(out / "contact-sheet.png")
    print("wrote", out / "contact-sheet.png")


if __name__ == "__main__":
    out = Path(sys.argv[1] if len(sys.argv) > 1 else "icon-options")
    out.mkdir(parents=True, exist_ok=True)
    concept_bowl(out / "A-gold-seam-bowl.png")
    concept_tiles(out / "B-mended-tiles.png")
    concept_plate(out / "C-golden-plate.png")
    concept_vase(out / "D-indigo-vase.png")
    contact_sheet(out, ["A-gold-seam-bowl.png", "B-mended-tiles.png", "C-golden-plate.png", "D-indigo-vase.png"])
