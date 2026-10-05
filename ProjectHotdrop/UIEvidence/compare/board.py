"""Build compare/board.html: every reference screen next to its current Unity capture.

Each row is [reference | current] scaled to the same height, with the capture's filename and a
same-height/width note. Images are inlined as data URIs because the preview serves a single file.

  python ProjectHotdrop/UIEvidence/compare/board.py
"""
from __future__ import annotations

import base64
import io
from pathlib import Path

from PIL import Image

OUT = Path(__file__).resolve().parent
TILE_W = 340          # tile width on the page; caption + two tiles must fit one screen width
GAP = 10

PAGE = """<!doctype html><html><head><meta charset="utf-8"><style>
html,body{{margin:0;background:#05070c;color:#dde;font:12px/1.4 monospace}}
h1{{font-size:15px;margin:10px;color:#7CFC00}}
h1 small{{color:#889;font-weight:400}}
.row{{display:flex;gap:{gap}px;align-items:flex-start;margin:0 0 14px 10px}}
.cap{{width:150px;flex:0 0 150px;color:#9fb0c7}}
.cap b{{display:block;color:#fff;font-size:13px;margin-bottom:4px}}
img{{display:block;border:1px solid #1b2940}}
.tag{{font-size:10px;color:#66768c;margin-top:3px}}
.miss{{height:190px;display:flex;align-items:center;justify-content:center;border:1px dashed #445;
      color:#e06377;padding:0 20px;font-size:11px}}
</style></head><body>
<h1>HOTDROP — UIUX reference vs Unity runtime capture
<small>(reference is a 5:4 Stitch comp; captures are 16:9 device frames — both shown at the same height)</small></h1>
{rows}
</body></html>
"""


def tile(im: Image.Image) -> tuple[str, int, int]:
    """Downscale before inlining: the board carries every screen at once, so full-resolution
    frames would make a page too heavy to open. Size is emitted as attributes so the layout never
    depends on the CSS height rule resolving."""
    width = TILE_W
    height = max(1, round(im.height * width / im.width))
    small = im.resize((width, height), Image.LANCZOS)
    buf = io.BytesIO()
    small.save(buf, format="PNG", optimize=True)
    return "data:image/png;base64," + base64.b64encode(buf.getvalue()).decode("ascii"), width, height


def main() -> None:
    rows = []
    for html in sorted(OUT.glob("[0-9][0-9].html"), key=lambda p: p.name):
        num = html.stem
        ref_p, cur_p = OUT / f"{num}_ref.png", OUT / f"{num}_cur.png"
        if not ref_p.exists():
            continue
        ref = Image.open(ref_p).convert("RGB")
        title = html.read_text(encoding="utf-8").split("SCREEN ")[1].split("<")[0]
        ref_uri, w, h = tile(ref)
        tiles = [f'<img src="{ref_uri}" width="{w}" height="{h}">']
        tag = f"<div class='tag'>ref {ref.width}×{ref.height}</div>"
        if cur_p.exists():
            cur = Image.open(cur_p).convert("RGB")
            cur_uri, cw, ch = tile(cur)
            tiles.append(f'<img src="{cur_uri}" width="{cw}" height="{ch}">')
            tag += f"<div class='tag'>cur {cur.width}×{cur.height} — {num}_cur.png</div>"
        else:
            tiles.append(f"<div class='miss'>no runtime capture yet</div>")
            tag += "<div class='tag'>not captured</div>"
        rows.append(
            f'<div class="row"><div class="cap"><b>{title}</b>{tag}</div>{"".join(tiles)}</div>')
    (OUT / "board.html").write_text(PAGE.format(rows="\n".join(rows), gap=GAP), encoding="utf-8")
    print(OUT / "board.html")


if __name__ == "__main__":
    main()
