"""Build the geometric ActiveRest Windows icon from local shapes."""

from pathlib import Path

from PIL import Image, ImageDraw


SIZE = 1024
ROOT = Path(__file__).resolve().parents[1]


def render() -> Image.Image:
    image = Image.new("RGBA", (SIZE, SIZE), (0, 0, 0, 0))
    fill = Image.new("RGBA", (SIZE, SIZE), (0, 0, 0, 0))
    pixels = fill.load()
    for y in range(SIZE):
        for x in range(SIZE):
            blend = (x + y) / (2 * (SIZE - 1))
            pixels[x, y] = (
                round(16 + 20 * blend),
                round(41 + 43 * blend),
                round(73 + 62 * blend),
                255,
            )

    mask = Image.new("L", (SIZE, SIZE))
    ImageDraw.Draw(mask).rounded_rectangle((42, 42, 982, 982), radius=230, fill=255)
    image.paste(fill, (0, 0), mask)
    draw = ImageDraw.Draw(image)
    draw.ellipse((216, 216, 808, 808), outline=(238, 248, 255, 255), width=74)
    draw.rounded_rectangle((392, 370, 466, 656), radius=37, fill=(140, 232, 213, 255))
    draw.rounded_rectangle((558, 370, 632, 656), radius=37, fill=(140, 232, 213, 255))
    return image


if __name__ == "__main__":
    destination = ROOT / "Assets" / "activerest.ico"
    icon = render()
    icon.save(destination, format="ICO", sizes=[(16, 16), (24, 24), (32, 32), (48, 48), (64, 64), (128, 128), (256, 256)])
    icon.resize((256, 256), Image.Resampling.LANCZOS).save(ROOT / "Assets" / "activerest.png")
    print(destination)
