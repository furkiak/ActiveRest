#!/usr/bin/env python3
"""Composes the README images from raw 2x window renders.

  dotnet run --project tools/Screenshots -- /tmp/shots --scale 2 --matte
  python3 tools/make_readme_images.py /tmp/shots docs/screenshots
"""
import sys
from pathlib import Path

import numpy as np
from PIL import Image, ImageFilter

src = Path(sys.argv[1])
out = Path(sys.argv[2])
out.mkdir(parents=True, exist_ok=True)


def wallpaper(w, h, seed=3):
    """Colourful abstract backdrop so the glass surfaces have something to refract."""
    rng = np.random.default_rng(seed)
    ys, xs = np.mgrid[0:h, 0:w].astype(np.float32)
    t = (xs / w * 0.6 + ys / h * 0.4)
    base = np.zeros((h, w, 3), np.float32)
    stops = [(0.0, (22, 28, 72)), (0.5, (74, 46, 130)), (1.0, (20, 90, 150))]
    for i in range(3):
        c0 = np.interp(t, [s[0] for s in stops], [s[1][i] for s in stops])
        base[..., i] = c0
    img = Image.fromarray(base.astype(np.uint8), "RGB").convert("RGBA")
    blobs = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    from PIL import ImageDraw
    d = ImageDraw.Draw(blobs)
    palette = [(255, 105, 180, 150), (64, 224, 208, 130), (255, 170, 60, 110), (120, 120, 255, 150), (90, 220, 120, 100)]
    for color in palette:
        r = int(min(w, h) * rng.uniform(0.18, 0.32))
        cx, cy = int(rng.uniform(0, w)), int(rng.uniform(0, h))
        d.ellipse((cx - r, cy - r, cx + r, cy + r), fill=color)
    blobs = blobs.filter(ImageFilter.GaussianBlur(min(w, h) // 7))
    return Image.alpha_composite(img, blobs)


def load_matte(name):
    """Recovers an RGBA image from renders over black and over white (difference matte)."""
    black = np.asarray(Image.open(src / f"{name}.black.png").convert("RGB"), np.float32)
    white = np.asarray(Image.open(src / f"{name}.white.png").convert("RGB"), np.float32)
    alpha = np.clip(1.0 - (white - black).mean(axis=2) / 255.0, 0.0, 1.0)
    color = np.where(alpha[..., None] > 0.004, black / np.maximum(alpha[..., None], 0.004), 0.0)
    rgba = np.dstack([np.clip(color, 0, 255), alpha * 255.0]).astype(np.uint8)
    return Image.fromarray(rgba, "RGBA")


def load(name, scale=1.0):
    im = load_matte(name)
    if scale != 1.0:
        im = im.resize((round(im.width * scale), round(im.height * scale)), Image.LANCZOS)
    return im


def compose(name, items, size, seed):
    canvas = wallpaper(*size, seed=seed)
    for im, (x, y) in items:
        canvas.alpha_composite(im, (x, y))
    canvas.convert("RGB").save(out / name, optimize=True)
    print("wrote", out / name, canvas.size)


# Hero: dark main, light main, dark details
a, b, c = load("main-day-en-dark", 0.86), load("main-day-en-light", 0.86), load("details-en-dark", 0.86)
gap = 20
W = a.width + b.width + c.width + gap * 4
H = max(a.height, b.height, c.height) + 80
compose("overview.png", [(a, (gap, 40)), (b, (a.width + gap * 2, 40)), (c, (a.width + b.width + gap * 3, 40))], (W, H), seed=5)

# Gallery: periods, report, settings, Turkish, toast
tiles = [load("main-week-en-dark", 0.62), load("main-year-en-light", 0.62), load("details-en-light", 0.62),
         load("settings-en-dark", 0.62), load("main-day-tr-dark", 0.62)]
toast = load("toast-en-dark", 0.62)
g = 16
W = sum(t.width for t in tiles) + g * (len(tiles) + 1)
H = max(t.height for t in tiles) + toast.height + g * 4
x, items = g, []
for t in tiles:
    items.append((t, (x, g)))
    x += t.width + g
items.append((toast, (W // 2 - toast.width // 2, max(t.height for t in tiles) + g * 2)))
compose("gallery.png", items, (W, H), seed=9)
