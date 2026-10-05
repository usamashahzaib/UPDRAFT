"""Stack the SAME normalised region of reference and capture vertically, at full width.

`parity.py crop` puts the two regions side by side, which is right for tall narrow regions
(a card, a nav bar). For a short wide region - a HUD top strip, a bottom control row - side by
side makes each half too small to read. This stacks them instead, reference on top.

  python ProjectHotdrop/UIEvidence/compare/stack.py 04 0.0 0.0 1.0 0.16 [--scale 2]
      -> compare/04_stack.png
"""
from __future__ import annotations

import sys
from pathlib import Path

from PIL import Image, ImageDraw

OUT = Path(__file__).resolve().parent


def main() -> None:
    num = sys.argv[1]
    x0, y0, x1, y1 = (float(v) for v in sys.argv[2:6])
    scale = 2.0
    if "--scale" in sys.argv:
        scale = float(sys.argv[sys.argv.index("--scale") + 1])

    ref = Image.open(OUT / f"{num}_ref.png").convert("RGB")
    cur = Image.open(OUT / f"{num}_cur.png").convert("RGB")

    tiles = []
    for im in (ref, cur):
        box = (round(x0 * im.width), round(y0 * im.height),
               round(x1 * im.width), round(y1 * im.height))
        t = im.crop(box := (max(0, box[0]), max(0, box[1]),
                            min(im.width, box[2]), min(im.height, box[3])))
        if scale != 1.0:
            t = t.resize((round(t.width * scale), round(t.height * scale)), Image.LANCZOS)
        tiles.append((f"{im.width}x{im.height}", t))

    bar = 22
    width = max(t.width for _, t in tiles)
    height = sum(t.height for _, t in tiles) + bar * 2
    canvas = Image.new("RGB", (width, height), (8, 8, 8))
    d = ImageDraw.Draw(canvas)
    y = 0
    for i, (size, t) in enumerate(tiles):
        label = "REFERENCE" if i == 0 else "CURRENT (Unity)"
        colour = (120, 255, 120) if i == 0 else (255, 220, 90)
        d.text((6, y + 5), f"{label}  {size}  region x {x0}..{x1}  y {y0}..{y1}", fill=colour)
        canvas.paste(t, (0, y + bar))
        y += bar + t.height
    path = OUT / f"{num}_stack.png"
    canvas.save(path)
    print(path)


if __name__ == "__main__":
    main()