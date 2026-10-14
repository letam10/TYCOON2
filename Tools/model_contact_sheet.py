"""Arrange unmodified Windows Player captures into labelled visual review pages."""
import argparse
from pathlib import Path

from PIL import Image, ImageDraw, ImageFont


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--evidence", type=Path, required=True)
    parser.add_argument("--pattern", required=True)
    parser.add_argument("--name", required=True)
    parser.add_argument("--columns", type=int, default=4)
    parser.add_argument("--per-page", type=int, default=24)
    parser.add_argument("--wide", action="store_true")
    args = parser.parse_args()
    evidence = args.evidence.resolve(strict=True)
    paths = sorted(evidence.glob(args.pattern))
    if not paths:
        raise ValueError("No Player captures match " + args.pattern)
    output = evidence / "review"
    output.mkdir(exist_ok=True)
    font_path = Path("C:/Windows/Fonts/segoeui.ttf")
    font = ImageFont.truetype(str(font_path), 15) if font_path.exists() else ImageFont.load_default()
    width = 400
    height = 330
    for page, start in enumerate(range(0, len(paths), args.per_page), 1):
        selected = paths[start:start + args.per_page]
        rows = (len(selected) + args.columns - 1) // args.columns
        sheet = Image.new("RGB", (args.columns * width, rows * height), "#172C28")
        draw = ImageDraw.Draw(sheet)
        for index, path in enumerate(selected):
            with Image.open(path) as original:
                # Chỉ cắt HUD để xem model; ảnh Player gốc luôn được giữ nguyên.
                x0 = int(original.width * (0.04 if args.wide else 0.30))
                x1 = int(original.width * (0.96 if args.wide else 0.70))
                crop = original.crop((x0, int(original.height * .15), x1, int(original.height * .86)))
                crop.thumbnail((width - 12, height - 40), Image.Resampling.LANCZOS)
                x = index % args.columns * width
                y = index // args.columns * height
                sheet.paste(crop, (x + (width - crop.width) // 2, y + 30))
                draw.text((x + 7, y + 6), path.stem, font=font, fill="#EDF1DE")
        destination = output / f"{args.name}-{page:02}.jpg"
        sheet.save(destination, quality=94)
        print(destination)


if __name__ == "__main__":
    main()
