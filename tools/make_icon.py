#!/usr/bin/env python3
"""Generates Assets/ActiveRest.ico and Assets/ActiveRest.png (Liquid Glass activity-ring icon).

Usage: python3 tools/make_icon.py   (needs Pillow and numpy)
"""
import math
from pathlib import Path

import numpy as np
from PIL import Image, ImageChops, ImageDraw, ImageFilter

S = 2048                      # supersampled canvas
MARGIN = 120
SIZE = S - 2 * MARGIN
RADIUS = 420
OUT = Path(__file__).resolve().parent.parent / "Assets"


def lerp(a, b, t):
    return tuple(int(round(a[i] + (b[i] - a[i]) * t)) for i in range(len(a)))


def gradient(size, c0, c1, angle_deg):
    """Linear gradient image (RGBA) between two colours along the given angle."""
    w, h = size
    ys, xs = np.mgrid[0:h, 0:w].astype(np.float32)
    a = math.radians(angle_deg)
    proj = (xs - w / 2) * math.cos(a) + (ys - h / 2) * math.sin(a)
    t = (proj - proj.min()) / (proj.max() - proj.min())
    out = np.zeros((h, w, 4), dtype=np.uint8)
    for i in range(4):
        out[..., i] = (c0[i] + (c1[i] - c0[i]) * t).astype(np.uint8)
    return Image.fromarray(out, "RGBA")


def squircle_mask():
    m = Image.new("L", (S, S), 0)
    ImageDraw.Draw(m).rounded_rectangle((MARGIN, MARGIN, S - MARGIN, S - MARGIN), RADIUS, fill=255)
    return m


def ring(center, radius, width, start, sweep, c0, c1):
    """Arc with round caps and a gradient along its length, as an RGBA layer."""
    mask = Image.new("L", (S, S), 0)
    d = ImageDraw.Draw(mask)
    # PIL draws the stroke inward from the bounding box, so grow the box to centre the band on `radius`.
    outer = radius + width / 2
    box = (center[0] - outer, center[1] - outer, center[0] + outer, center[1] + outer)
    d.arc(box, start, start + sweep, fill=255, width=width)
    for ang in (start, start + sweep):
        r = math.radians(ang)
        cx = center[0] + radius * math.cos(r)
        cy = center[1] + radius * math.sin(r)
        d.ellipse((cx - width / 2, cy - width / 2, cx + width / 2, cy + width / 2), fill=255)

    grad = gradient((S, S), c0, c1, 45)
    layer = Image.new("RGBA", (S, S), (0, 0, 0, 0))
    layer.paste(grad, (0, 0), mask)
    return layer, mask


def build():
    mask = squircle_mask()

    # Base: deep navy glass with a soft blue light from the upper left.
    base = gradient((S, S), (34, 38, 74, 255), (8, 10, 24, 255), 70)
    glow = Image.new("RGBA", (S, S), (0, 0, 0, 0))
    ImageDraw.Draw(glow).ellipse((MARGIN - 300, MARGIN - 420, S * 0.62, S * 0.5), fill=(10, 132, 255, 70))
    glow = glow.filter(ImageFilter.GaussianBlur(260))
    base = Image.alpha_composite(base, glow)

    canvas = Image.new("RGBA", (S, S), (0, 0, 0, 0))
    canvas.paste(base, (0, 0), mask)

    c = (S // 2, S // 2)
    # Outer ring: focus (green), inner ring: relaxed (blue).
    outer, outer_mask = ring(c, 560, 190, -90, 262, (48, 209, 88, 255), (120, 255, 190, 255))
    inner, inner_mask = ring(c, 330, 150, -90, 190, (10, 132, 255, 255), (100, 210, 255, 255))
    dot = Image.new("RGBA", (S, S), (0, 0, 0, 0))
    ImageDraw.Draw(dot).ellipse((c[0] - 82, c[1] - 82, c[0] + 82, c[1] + 82), fill=(255, 159, 10, 255))

    # Tracks behind the rings.
    for radius, width in ((560, 190), (330, 150)):
        track = Image.new("RGBA", (S, S), (0, 0, 0, 0))
        o = radius + width / 2
        ImageDraw.Draw(track).ellipse((c[0] - o, c[1] - o, c[0] + o, c[1] + o),
                                      outline=(255, 255, 255, 26), width=width)
        canvas = Image.alpha_composite(canvas, track)

    for layer, m in ((outer, outer_mask), (inner, inner_mask)):
        halo = layer.filter(ImageFilter.GaussianBlur(46))
        halo.putalpha(ImageChops.multiply(halo.getchannel("A"), Image.new("L", (S, S), 150)))
        canvas = Image.alpha_composite(canvas, halo)
        canvas = Image.alpha_composite(canvas, layer)
    canvas = Image.alpha_composite(canvas, dot)

    # Glass: specular sheen on the upper half and a thin lit rim.
    sheen = Image.new("RGBA", (S, S), (0, 0, 0, 0))
    ImageDraw.Draw(sheen).ellipse((MARGIN - 300, MARGIN - 1500, S - MARGIN + 300, MARGIN + 520), fill=(255, 255, 255, 30))
    sheen_mask = ImageChops.multiply(mask, sheen.getchannel("A"))
    sheen.putalpha(sheen_mask)
    canvas = Image.alpha_composite(canvas, sheen)

    rim = Image.new("RGBA", (S, S), (0, 0, 0, 0))
    ImageDraw.Draw(rim).rounded_rectangle((MARGIN, MARGIN, S - MARGIN, S - MARGIN), RADIUS, outline=(255, 255, 255, 70), width=10)
    canvas = Image.alpha_composite(canvas, rim)

    # Soft drop shadow under the squircle.
    shadow = Image.new("RGBA", (S, S), (0, 0, 0, 0))
    ImageDraw.Draw(shadow).rounded_rectangle((MARGIN, MARGIN + 36, S - MARGIN, S - MARGIN + 36), RADIUS, fill=(0, 0, 0, 120))
    shadow = shadow.filter(ImageFilter.GaussianBlur(40))
    return Image.alpha_composite(shadow, canvas)


def main():
    OUT.mkdir(exist_ok=True)
    img = build()
    png = img.resize((512, 512), Image.LANCZOS)
    png.save(OUT / "ActiveRest.png")
    ico_sizes = [16, 24, 32, 48, 64, 128, 256]
    img.resize((256, 256), Image.LANCZOS).save(OUT / "ActiveRest.ico", format="ICO", sizes=[(s, s) for s in ico_sizes])
    print("wrote", OUT / "ActiveRest.ico", OUT / "ActiveRest.png")


if __name__ == "__main__":
    main()
