"""Compose <dir>/*.png (order.txt) from SourceArt/DecoKit or Scratch/Captures/DecoKit into a labelled sheet in Scratch/Captures/DecoKit.
python3 Tools/Blender/decokit/compose_sheet.py [--cols 6] [--dir icons] [--out sheet.png] [--pair icons_unity]
--pair puts each icon next to its twin from another dir (Cycles | Unity parity check), resized to match."""
import math
import sys
from pathlib import Path

from PIL import Image, ImageDraw

ROOT = Path(__file__).resolve().parents[3]
ART, OUT = ROOT / "SourceArt/DecoKit", ROOT / "Scratch/Captures/DecoKit"  # kept icon sets live in SourceArt; test sets in Scratch
BG = (251, 248, 236)


def arg(key, default):
    return sys.argv[sys.argv.index(key) + 1] if key in sys.argv else default


def tile(path, px):
    im = Image.open(path).convert("RGBA").resize((px, px), Image.LANCZOS)
    t = Image.new("RGBA", im.size, (*BG, 255))
    t.alpha_composite(im)
    return t.convert("RGB")


cols = int(arg("--cols", 6))
def find(d):
    return ART / d if (ART / d).exists() else OUT / d


src, pair = find(arg("--dir", "icons")), arg("--pair", None)
names = [n for n in (src / "order.txt").read_text().split() if not pair or (find(pair) / f"{n}.png").exists()]
px = Image.open(src / f"{names[0]}.png").width
w = px * (2 if pair else 1)
rows = math.ceil(len(names) / cols)
sheet = Image.new("RGB", (cols * w, rows * (px + 22)), BG)
dr = ImageDraw.Draw(sheet)
for i, n in enumerate(names):
    x, y = (i % cols) * w, (i // cols) * (px + 22)
    sheet.paste(tile(src / f"{n}.png", px), (x, y))
    if pair:
        sheet.paste(tile(find(pair) / f"{n}.png", px), (x + px, y))
    dr.text((x + 8, y + px + 4), n + ("  (Cycles | Unity)" if pair else ""), fill=(90, 84, 70))
out = OUT / arg("--out", "sheet_pair.png" if pair else "sheet.png")
out.parent.mkdir(parents=True, exist_ok=True)
sheet.save(out)
print(out, sheet.size)
