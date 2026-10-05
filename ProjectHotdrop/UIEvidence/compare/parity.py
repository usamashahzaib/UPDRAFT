"""HOTDROP UI parity tool — measure the Unity capture against the UIUX reference.

Subcommands (run from the repo root):
  python ProjectHotdrop/UIEvidence/compare/parity.py montage 03
      -> ProjectHotdrop/UIEvidence/compare/03_montage.png : reference above current, same width
  python ProjectHotdrop/UIEvidence/compare/parity.py crop 03 x0 y0 x1 y1 [--pad 20]
      -> crops the SAME normalised region out of both and stacks them, for reading detail
  python ProjectHotdrop/UIEvidence/compare/parity.py rows 03 [--luma 90]
      -> prints the ink row-bands (text/element bands) of both images, so layout can be compared
      -> numbers, not a picture
  python ProjectHotdrop/UIEvidence/compare/parity.py colors 03
      -> prints mean colour of a 5x3 grid of both images
"""
from __future__ import annotations

import sys
from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw, ImageStat

ROOT = Path(__file__).resolve().parents[3]
OUT = Path(__file__).resolve().parent


def load(num: str) -> tuple[Image.Image, Image.Image]:
    ref = Image.open(OUT / f"{num}_ref.png").convert("RGB")
    cur = Image.open(OUT / f"{num}_cur.png").convert("RGB")
    return ref, cur


def _fit(im: Image.Image, width: int) -> Image.Image:
    h = round(im.height * width / im.width)
    return im.resize((width, h), Image.LANCZOS)


def montage(num: str, width: int = 1440) -> Path:
    ref, cur = load(num)
    a, b = _fit(ref, width), _fit(cur, width)
    bar = 26
    canvas = Image.new("RGB", (width, a.height + b.height + bar * 3), (8, 8, 8))
    d = ImageDraw.Draw(canvas)
    d.text((6, 6), f"REFERENCE  {num}  {ref.width}x{ref.height}", fill=(120, 255, 120))
    canvas.paste(a, (0, bar))
    d.text((6, bar + a.height + 6), f"CURRENT (Unity)  {cur.width}x{cur.height}", fill=(255, 220, 90))
    canvas.paste(b, (0, bar * 2 + a.height))
    path = OUT / f"{num}_montage.png"
    canvas.save(path)
    return path


def crop(num: str, box: tuple[float, float, float, float], pad: int = 12,
         height: int = 720) -> Path:
    """box is normalised (x0, y0, x1, y1) so the same region is taken from both.

    Both sides are scaled to the same HEIGHT (not width) before stacking: the references are
    1600x1280 comps and the captures are 1080-tall gameplay frames, so equal height is the only
    scaling under which "this text is the same size" or "this bar has the same weight" is a fair
    statement. Scaling to equal width would make the 5:4 reference look smaller than it is.
    """
    ref, cur = load(num)
    tiles = []
    for im in (ref, cur):
        x0, y0 = round(box[0] * im.width), round(box[1] * im.height)
        x1, y1 = round(box[2] * im.width), round(box[3] * im.height)
        t = im.crop((max(0, x0 - pad), max(0, y0 - pad),
                     min(im.width, x1 + pad), min(im.height, y1 + pad)))
        t = t.resize((max(1, round(t.width * height / t.height)), height), Image.LANCZOS)
        tiles.append(t)
    gap = 18
    width = sum(t.width for t in tiles) + gap
    bar = 26
    canvas = Image.new("RGB", (width, height + bar * 3), (8, 8, 8))
    d = ImageDraw.Draw(canvas)
    d.text((6, 6), "REFERENCE (5:4 comp, scaled to capture height)", fill=(120, 255, 120))
    canvas.paste(tiles[0], (0, bar))
    d.text((tiles[0].width + gap, bar + 6), "CURRENT", fill=(255, 220, 90))
    canvas.paste(tiles[1], (tiles[0].width + gap, bar))
    path = OUT / f"{num}_crop.png"
    canvas.save(path)
    return path


def _rowbands(im: Image.Image, luma: int, axis: str = "y") -> list[tuple[int, int, float]]:
    """Bands of rows/columns whose brightest pixel exceeds `luma` — i.e. where anything draws."""
    g = im.convert("L")
    px = g.load()
    w, h = g.size
    hits = []
    if axis == "y":
        for y in range(h):
            best = 0
            for x in range(0, w, 2):
                v = px[x, y]
                if v > best:
                    best = v
                    if best >= luma:
                        break
            hits.append(best)
    else:
        for x in range(w):
            best = 0
            for y in range(0, h, 2):
                v = px[x, y]
                if v > best:
                    best = v
                    if best >= luma:
                        break
            hits.append(best)
    bands, start = [], None
    for i, v in enumerate(hits + [0]):
        if v >= luma and start is None:
            start = i
        elif v < luma and start is not None:
            bands.append((start, i - 1, round(100 * start / len(hits), 1)))
            start = None
    return bands


def rows(num: str, luma: int = 90) -> None:
    ref, cur = load(num)
    for name, im in (("REF", ref), ("CUR", cur)):
        bands = _rowbands(im, luma)
        print(f"--- {name} {im.width}x{im.height} luma>{luma}: {len(bands)} bands")
        for a, b, pct in bands:
            print(f"    y {a:5d}..{b:5d}  ({pct:5.1f}%)  h={b - a + 1}")


def colors(num: str) -> None:
    ref, cur = load(num)
    for name, im in (("REF", ref), ("CUR", cur)):
        print(f"--- {name} {im.width}x{im.height}")
        for gy in range(3):
            row = []
            for gx in range(5):
                box = (gx * im.width // 5, gy * im.height // 3,
                       (gx + 1) * im.width // 5, (gy + 1) * im.height // 3)
                st = ImageStat.Stat(im.crop(box))
                row.append("#%02x%02x%02x" % tuple(int(v) for v in st.mean))
            print("   ", "  ".join(row))


def blocks(num: str, luma: int = 100, min_area: int = 40, gap: int = 3) -> None:
    """Report the drawn element boxes of both images as numbers, so placement can be compared.

    A row band is a run of scanlines containing ink; within it, column runs of ink give the
    horizontal extent of each separate block. Coordinates are printed as a fraction of the frame
    (x%, y%) because the reference is 5:4 and the capture is 16:9 - the only comparable thing is
    where an element sits in the composition, not which pixel column it occupies.
    """
    for name, im in zip(("REF", "CUR"), load(num)):
        a = np.asarray(im.convert("RGB")).astype(np.int32)
        gray = (a[:, :, 0] * 0.299 + a[:, :, 1] * 0.587 + a[:, :, 2] * 0.114)
        mask = gray > luma
        height, width = mask.shape
        print(f"--- {name} {width}x{height}  ink>{luma}")
        for y0, y1 in runs(mask.any(axis=1), gap=gap):
            band = mask[y0:y1 + 1]
            for x0, x1 in runs(band.any(axis=0), gap=gap):
                cell = gray[y0:y1 + 1, x0:x1 + 1]
                bright = cell > luma
                if bright.sum() < min_area:
                    continue
                mean = a[y0:y1 + 1, x0:x1 + 1][bright].mean(axis=0).astype(int)
                print(f"    x {100 * x0 / width:5.1f}%..{100 * (x1 + 1) / width:5.1f}%"
                      f"  y {100 * y0 / height:5.1f}%..{100 * (y1 + 1) / height:5.1f}%"
                      f"  wh {x1 - x0 + 1:4d}x{y1 - y0 + 1:3d}"
                      f"  colour #{mean[0]:02x}{mean[1]:02x}{mean[2]:02x}"
                      f"  ink={int(bright.sum())}")


def runs(flags: np.ndarray, gap: int = 3) -> list[tuple[int, int]]:
    """Index runs of True in a 1-D boolean array, bridging gaps shorter than `gap`."""
    idx = np.flatnonzero(flags)
    if idx.size == 0:
        return []
    out = []
    start = prev = int(idx[0])
    for i in idx[1:]:
        i = int(i)
        if i - prev <= gap + 1:
            prev = i
            continue
        out.append((start, prev))
        start = prev = i
    out.append((start, prev))
    return out


def probe(num: str, box: tuple[float, float, float, float], luma: int = 100,
          label: str = "") -> None:
    """Ink bounding box of one element, in both images, as a fraction of the frame.

    `probe 01 0.4 0.30 0.62 0.43 --luma 180` measures the brand wordmark: the box narrows the
    search to that element so neighbouring arcs and chips cannot bleed into the measurement.
    """
    print(f"--- {label or 'probe'}  box x {box[0]:.2f}..{box[2]:.2f}  y {box[1]:.2f}..{box[3]:.2f}  ink>{luma}")
    for name, im in zip(("REF", "CUR"), load(num)):
        a = np.asarray(im.convert("RGB")).astype(np.int32)
        gray = (a[:, :, 0] * 0.299 + a[:, :, 1] * 0.587 + a[:, :, 2] * 0.114)
        h, w = gray.shape
        x0, y0 = int(box[0] * w), int(box[1] * h)
        x1, y1 = int(box[2] * w), int(box[3] * h)
        sub = gray[y0:y1, x0:x1]
        bright = sub > luma
        if not bright.any():
            print(f"    {name}: no ink above {luma}")
            continue
        ys, xs = np.nonzero(bright)
        bx0, bx1 = x0 + xs.min(), x0 + xs.max()
        by0, by1 = y0 + ys.min(), y0 + ys.max()
        colour = a[y0:y1, x0:x1][bright].mean(axis=0).astype(int)
        print(f"    {name}: x {100 * bx0 / w:5.2f}%..{100 * (bx1 + 1) / w:5.2f}%"
              f"  y {100 * by0 / h:5.2f}%..{100 * (by1 + 1) / h:5.2f}%"
              f"  w {100 * (bx1 - bx0 + 1) / w:5.2f}%  h {100 * (by1 - by0 + 1) / h:5.2f}%"
              f"  px {bx1 - bx0 + 1}x{by1 - by0 + 1}"
              f"  colour #{colour[0]:02x}{colour[1]:02x}{colour[2]:02x}")


def main() -> None:
    if len(sys.argv) < 3:
        print(__doc__)
        return
    cmd, num = sys.argv[1], sys.argv[2]
    if cmd == "montage":
        print(montage(num))
    elif cmd == "crop":
        vals = [float(v) for v in sys.argv[3:7]]
        pad = 12
        if "--pad" in sys.argv:
            pad = int(sys.argv[sys.argv.index("--pad") + 1])
        height = 720
        if "--h" in sys.argv:
            height = int(sys.argv[sys.argv.index("--h") + 1])
        print(crop(num, tuple(vals), pad, height))
    elif cmd == "rows":
        luma = 90
        if "--luma" in sys.argv:
            luma = int(sys.argv[sys.argv.index("--luma") + 1])
        rows(num, luma)
    elif cmd == "colors":
        colors(num)
    elif cmd == "probe":
        vals = [float(v) for v in sys.argv[3:7]]
        luma = 100
        label = ""
        if "--luma" in sys.argv:
            luma = int(sys.argv[sys.argv.index("--luma") + 1])
        if "--label" in sys.argv:
            label = sys.argv[sys.argv.index("--label") + 1]
        probe(num, tuple(vals), luma, label)
    elif cmd == "blocks":
        luma = 100
        min_area = 40
        if "--luma" in sys.argv:
            luma = int(sys.argv[sys.argv.index("--luma") + 1])
        if "--min" in sys.argv:
            min_area = int(sys.argv[sys.argv.index("--min") + 1])
        blocks(num, luma, min_area)
    else:
        print(__doc__)


if __name__ == "__main__":
    main()
