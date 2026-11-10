"""Brief 104 (Vela): PatrolBot104's six LED faces from patrol-robot-details.png, as render-only textures and the atlas the expression proposal uses.
python3 -I briefs/104/vela/patrolbot/make/faces.py <out_dir>

A 34 x 18 dot grid over the screen's UVs (u to the robot's left as you face it, v up), amber dots on dark glass with the faint cell grid of the sheets.
Writes calm, curious, alert, cross, dazed, off .png (680 x 360) and atlas.png (3 x 2 cells, the order above)."""

import math
import sys
from pathlib import Path

from PIL import Image, ImageDraw

COLS, ROWS, PX = 34, 18, 20
GLASS, CELL, LIT, HALO = (22, 18, 14), (40, 34, 27), (255, 178, 70), (120, 62, 18)
EYES = ((10, 7), (23, 7))  # cell centres (x right, y down)


def disc(c, r):
    return {(x, y) for x in range(COLS) for y in range(ROWS) if (x - c[0]) ** 2 + (y - c[1]) ** 2 <= r * r + 0.3}


def ring(c, r):
    return {(x, y) for x in range(COLS) for y in range(ROWS) if abs(math.hypot(x - c[0], y - c[1]) - r) < 0.55}


def line(p, q):
    n = int(round(max(abs(q[0] - p[0]), abs(q[1] - p[1])))) + 1
    return {(round(p[0] + (q[0] - p[0]) * i / max(n - 1, 1)), round(p[1] + (q[1] - p[1]) * i / max(n - 1, 1))) for i in range(n)}


def cross(c, r):
    return line((c[0] - r, c[1] - r), (c[0] + r, c[1] + r)) | line((c[0] - r, c[1] + r), (c[0] + r, c[1] - r))


FACES = {
    "calm": disc(EYES[0], 2.2) | disc(EYES[1], 2.2) | {(14, 12), (15, 13), (16, 13), (17, 13), (18, 13), (19, 12)},
    "curious": disc(EYES[0], 3.2) | disc(EYES[1], 1.2) | ring((16.5, 13.0), 1.0),
    "alert": disc(EYES[0], 2.2) | disc(EYES[1], 2.2),
    "cross": line((7, 6), (12, 8)) | line((7, 7), (12, 9)) | line((21, 8), (26, 6)) | line((21, 9), (26, 7)) | line((13, 13), (20, 13)),
    "dazed": cross(EYES[0], 2) | cross(EYES[1], 2) | {(13 + i, 13 + (i % 2)) for i in range(8)},
    "off": set(),
}


def draw(dots):
    im = Image.new("RGB", (COLS * PX, ROWS * PX), GLASS)
    d = ImageDraw.Draw(im)
    for x in range(COLS):
        for y in range(ROWS):
            cx, cy = x * PX + PX / 2, y * PX + PX / 2
            if (x, y) in dots:
                d.ellipse((cx - 8, cy - 8, cx + 8, cy + 8), fill=HALO)
                d.ellipse((cx - 6, cy - 6, cx + 6, cy + 6), fill=LIT)
            else:
                d.rectangle((cx - 7, cy - 7, cx + 7, cy + 7), outline=CELL)
    return im


def main():
    out = Path(sys.argv[1])
    out.mkdir(parents=True, exist_ok=True)
    atlas = Image.new("RGB", (3 * COLS * PX, 2 * ROWS * PX))
    for i, (name, dots) in enumerate(FACES.items()):
        im = draw(dots)
        im.save(out / f"{name}.png")
        atlas.paste(im, ((i % 3) * COLS * PX, (i // 3) * ROWS * PX))
    atlas.save(out / "atlas.png")
    print(f"[faces] {len(FACES)} faces + atlas -> {out}")


main()
