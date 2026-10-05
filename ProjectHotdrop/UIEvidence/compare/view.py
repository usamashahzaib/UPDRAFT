"""Inline one or more PNGs into compare/viewer.html so the Preview tab can show them.

The preview serves a single registered HTML file, so images must be data URIs.

  python ProjectHotdrop/UIEvidence/compare/view.py 01_crop_top.png 01_crop_mid.png
  python ProjectHotdrop/UIEvidence/compare/view.py --title "Screen 01" a.png b.png
"""
from __future__ import annotations

import base64
import sys
from pathlib import Path

OUT = Path(__file__).resolve().parent

PAGE = """<!doctype html><html><head><meta charset="utf-8"><style>
html,body{{margin:0;background:#050505;color:#ddd;font:12px monospace}}
.wrap{{width:99vw;margin:0 auto}}
img{{display:block;width:100%;height:auto;margin:6px 0;border:1px solid #222}}
h1{{font-size:13px;margin:6px;color:#7CFC00}}
</style></head><body><div class="wrap"><h1>{title}</h1>{imgs}</div></body></html>
"""


def main() -> None:
    args = sys.argv[1:]
    title = "viewer"
    if "--title" in args:
        i = args.index("--title")
        title = args[i + 1]
        del args[i:i + 2]
    parts = []
    for name in args:
        p = (OUT / name) if not Path(name).is_absolute() else Path(name)
        if not p.exists():
            print(f"!! missing {p}")
            continue
        uri = "data:image/png;base64," + base64.b64encode(p.read_bytes()).decode("ascii")
        parts.append(f'<img src="{uri}" alt="{p.name}">')
    (OUT / "viewer.html").write_text(PAGE.format(title=title, imgs="\n".join(parts)), encoding="utf-8")
    print(OUT / "viewer.html")


if __name__ == "__main__":
    main()
