"""Brief 096 sheet (Vela): hover_camera_review.py's renders on one page, with the play-camera crops round each camera at 1:1 and x4 (nearest, so the pixels are the play camera's).

python3 -I tools/decokit/hover_camera_sheet.py <renders_dir> [<out.png>]     (default <renders_dir>/sheet.png). Pillow only. Reads review.json for the cameras' pixels."""

import json
import sys
from pathlib import Path

from PIL import Image, ImageDraw, ImageFont

F = Path(sys.argv[1])
OUT = Path(sys.argv[2]) if len(sys.argv) > 2 else F / "sheet.png"
BG, FG, DIM = (19, 44, 43), (236, 240, 232), (176, 205, 199)
R = json.loads((F / "review.json").read_text())


def font(px):
    for p in ("/System/Library/Fonts/Supplemental/Arial Bold.ttf", "/System/Library/Fonts/Helvetica.ttc"):
        if Path(p).exists():
            return ImageFont.truetype(p, px)
    return ImageFont.load_default()


def tile(im, w, label):
    im = im.resize((w, int(im.height * w / im.width)), Image.LANCZOS) if im.width != w else im
    d = ImageDraw.Draw(im)
    tw = d.textlength(label, font=font(18))
    d.rectangle([0, 0, tw + 16, 28], fill=(11, 22, 21))
    d.text((8, 4), label, font=font(18), fill=(255, 255, 255))
    return im


def crop(shot, key, half=56, k=4):
    im = Image.open(F / f"{shot}.png").convert("RGB")
    x, y = R["pixels"][shot][key]
    box = (int(x - half), int(y - half), int(x + half), int(y + half))
    c = im.crop(box)
    return c, c.resize((c.width * k, c.height * k), Image.NEAREST)


rows = [[tile(Image.open(F / "threeq.png").convert("RGB"), 600, "three-quarter (live)"), tile(Image.open(F / "threeq-cradle.png").convert("RGB"), 600, "three-quarter (dead, docked)")],
        [tile(Image.open(F / "side.png").convert("RGB"), 600, "side (live, 16.7 deg nose-down)"), tile(Image.open(F / "side-cradle.png").convert("RGB"), 600, "side (cradle, dead)")]]
play = [tile(Image.open(F / f"play-{y}.png").convert("RGB"), 1212, f"play camera: orthographic size 10, 38 deg, yaw {y}, 1920 x 1080 (downscaled)" + (": the same view turned, the sun behind the wall" if y == 210 else "")) for y in (30, 210)]
crops = []
for y in (30, 210):
    for key in ("live", "docked"):
        one, four = crop(f"play-{y}", key)
        crops.append(tile(four, 448, f"yaw {y} {key}: 112 px at 1:1, x4"))
tot = R["totals"]
lines = [f"HoverCamera {tot['HoverCamera']} triangles (body, lens, tally, ring), HoverCamera_Cradle {tot['HoverCamera_Cradle']}: together under the CCTVPole's 6,882 it replaces.",
         "Blender EEVEE, Standard view, no URP grade: colours and the emissive levels are Glint's to set in game light."]
W = 1212 + 48
H = 24 + sum(r[0].height + 12 for r in rows) + sum(p.height + 12 for p in play) + 2 * (448 + 12) + 30 * len(lines) + 40
sheet = Image.new("RGB", (W, H), BG)
d = ImageDraw.Draw(sheet)
yy = 24
for r in rows:
    sheet.paste(r[0], (24, yy))
    sheet.paste(r[1], (24 + 612, yy))
    yy += r[0].height + 12
for p in play:
    sheet.paste(p, (24, yy))
    yy += p.height + 12
for i, c in enumerate(crops):
    sheet.paste(c, (24 + (i % 2) * 460, yy + (i // 2) * 460))
yy += 2 * 460 + 8
for ln in lines:
    d.text((24, yy), ln, font=font(20), fill=FG)
    yy += 30
sheet.save(OUT)
print(f"wrote {OUT} ({W} x {H})")
