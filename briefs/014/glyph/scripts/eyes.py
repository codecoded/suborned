"""Glyph, brief 014: the three eye states redrawn so the eye shape dominates (Sky, 2026-10-16: the unknown eye read as a help "?" at chip size).
Run from the repo root: python3 -I briefs/014/glyph/scripts/eyes.py   (Pillow only). Writes briefs/014/glyph/glyphs/eye_{open,shut,unk}@1x/@2x.png
(white monoline on a 32 box, like the rest of the kit; Unity tints them). Rules:
  open    = a wide almond, a solid iris, three rays over the lid: "seen" (the code tints it red: a small display pictogram)
  shut    = the lid line only, curved down, with lashes: "nobody sees"
  unknown = the same wide almond, an EMPTY iris ring (nothing known yet) and a small "?" tucked in the top-right corner, outside the almond
All three keep the almond/lid across at least 28 of the 32 box so the eye is what you read at 16 to 18 px."""
import math, os
from PIL import Image, ImageDraw
OUT = "briefs/014/glyph/glyphs/"
N = 512; K = N / 32.0
def new(): im = Image.new("RGBA", (N, N), (0, 0, 0, 0)); return im, ImageDraw.Draw(im)
W = (255, 255, 255, 255)
def line(d, pts, w):
    P = [(x * K, y * K) for x, y in pts]; d.line(P, fill=W, width=int(w * K), joint="curve")
    for x, y in P: d.ellipse((x - w * K / 2, y - w * K / 2, x + w * K / 2, y + w * K / 2), fill=W)
def circ(d, cx, cy, r, w=0):
    if w == 0: d.ellipse(((cx - r) * K, (cy - r) * K, (cx + r) * K, (cy + r) * K), fill=W)
    else: d.ellipse(((cx - r) * K, (cy - r) * K, (cx + r) * K, (cy + r) * K), outline=W, width=int(w * K))
def almond(cy, x0=1.5, x1=30.5, h=8.6, n=48, p=0.78):
    up = [(x0 + (x1 - x0) * i / n, cy - h * math.sin(math.pi * i / n) ** p) for i in range(n + 1)]
    dn = [(x0 + (x1 - x0) * i / n, cy + h * math.sin(math.pi * i / n) ** p) for i in range(n + 1)]
    return up, dn
def eye_open(d):
    up, dn = almond(18.5); line(d, up, 3.0); line(d, dn, 3.0); circ(d, 16, 18.5, 5.6)
    for x, dx in ((7.5, -1.3), (16, 0), (24.5, 1.3)): line(d, [(x, 7.2), (x + dx, 2.6)], 2.6)
def eye_shut(d):
    n = 40; lid = [(1.8 + 28.4 * i / n, 8.5 + 10.5 * math.sin(math.pi * i / n)) for i in range(n + 1)]
    line(d, lid, 3.6)
    for t in (0.16, 0.5, 0.84):
        x = 1.8 + 28.4 * t; y = 8.5 + 10.5 * math.sin(math.pi * t); line(d, [(x, y), (x + (t - 0.5) * 7, y + 6.2)], 3.0)
def eye_unk(d):
    up, dn = almond(22, 1.5, 28.5, 7.6); line(d, up, 3.0); line(d, dn, 3.0)
    circ(d, 15, 22, 4.2, 2.6)                                      # empty iris: nothing known yet
    # a small "?" tucked in the top-right corner, clear of the almond
    hook = [(26.2 + 4.0 * math.cos(math.radians(a)), 5.8 + 3.7 * math.sin(math.radians(a))) for a in range(190, 421, 12)]
    line(d, hook, 2.5); line(d, [hook[-1], (26.2, 11.4)], 2.5); circ(d, 26.2, 15.2, 1.55)
for name, fn in (("eye_open", eye_open), ("eye_shut", eye_shut), ("eye_unk", eye_unk)):
    im, d = new(); fn(d)
    im.resize((64, 64), Image.LANCZOS).save(OUT + name + "@2x.png"); im.resize((32, 32), Image.LANCZOS).save(OUT + name + "@1x.png")
print("ok")
