#!/usr/bin/env python3
"""Brief 020 C: a labelled contact sheet of render020kit.py's frames. python3 -I tools/decokit/sheet020.py --dir out/work020/kit1 --ids A,B,C --out sheet.png [--cols 3] [--view play|true|eye] [--tris]
Each tile: the frame, the piece id and (with --tris) its triangle count, status and size from contracts/world/kit.json."""
import json
import sys
from pathlib import Path

from PIL import Image, ImageDraw, ImageFont

REPO = Path(__file__).resolve().parents[2]
arg = lambda k, d: sys.argv[sys.argv.index(k) + 1] if k in sys.argv else d  # noqa: E731


def main():
    d, ids, out, cols, view = Path(arg("--dir", ".")), [i for i in arg("--ids", "").split(",") if i], arg("--out", "sheet.png"), int(arg("--cols", 3)), arg("--view", "play")
    kit = {a["id"]: a for a in json.loads((REPO / "contracts/world/kit.json").read_text())["assets"]}
    tiles = []
    for n in ids:
        p = d / f"{n}.{view}.png"
        if p.exists():
            tiles.append((n, Image.open(p).convert("RGB")))
    if not tiles:
        raise SystemExit("no frames")
    w, h = tiles[0][1].size
    rows = -(-len(tiles) // cols)
    sheet = Image.new("RGB", (cols * w, rows * (h + 34)), (251, 248, 236))
    dr = ImageDraw.Draw(sheet)
    try:
        font = ImageFont.truetype("/System/Library/Fonts/Helvetica.ttc", 20)
    except OSError:
        font = ImageFont.load_default()
    for i, (n, im) in enumerate(tiles):
        x, y = (i % cols) * w, (i // cols) * (h + 34)
        sheet.paste(im.resize((w, h)), (x, y + 34))
        k = kit.get(n)
        label = n + (f"   {k['tris']} tris  {k['status']}  {k['size'][0]:.1f} x {k['size'][2]:.1f} x {k['size'][1]:.1f} m" if k and "--tris" in sys.argv else "")
        dr.text((x + 8, y + 6), label, fill=(30, 40, 40), font=font)
    sheet.save(out)
    print(out, sheet.size)


if __name__ == "__main__":
    main()
