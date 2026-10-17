"""Glyph, brief 016 intel review: two glyphs. White monoline on the kit's 32 box.
 secret_none: the `secret` envelope with a diagonal strike, for "no secret to find" (a learned fact, not an unknown).
 turn_him: a bust and a U-turn arrow, the verb for "turn this person" on the Intel chip (the kit's `turned` check disc reads as "done").
Run from the repo root: python3 -I briefs/014/glyph/scripts/intel_glyphs.py -> briefs/014/glyph/glyphs/"""
import math
from PIL import Image, ImageDraw, ImageChops
G = "briefs/014/glyph/glyphs/"; KIT = "briefs/011/ui/kit/icons/singles/"
N = 512; K = N / 32.0; W = (255, 255, 255, 255)
def new(): im = Image.new("RGBA", (N, N), (0, 0, 0, 0)); return im, ImageDraw.Draw(im)
def line(d, P, w):
    P = [(x * K, y * K) for x, y in P]; d.line(P, fill=W, width=int(w * K), joint="curve")
    for x, y in P: d.ellipse((x - w * K / 2, y - w * K / 2, x + w * K / 2, y + w * K / 2), fill=W)
def save(im, name):
    im.resize((64, 64), Image.LANCZOS).save(G + name + "@2x.png"); im.resize((32, 32), Image.LANCZOS).save(G + name + "@1x.png")
# secret_none: an outlined envelope (rectangle and flap) with a strike through it
im, d = new()
line(d, [(3, 8), (29, 8), (29, 25), (3, 25), (3, 8)], 2.6); line(d, [(3.6, 8.8), (16, 18), (28.4, 8.8)], 2.4)
cut = Image.new("L", (N, N), 255); ImageDraw.Draw(cut).line((6 * K, 29 * K, 26 * K, 4 * K), fill=0, width=int(6.2 * K))
im.putalpha(ImageChops.multiply(im.getchannel("A"), cut)); d = ImageDraw.Draw(im); line(d, [(5, 28), (27, 3.4)], 2.6)
save(im, "secret_none")
# turn_him: a bust and a U-turn arrow
im, d = new()
d.ellipse(((9 - 4.4) * K, (8.4 - 4.4) * K, (9 + 4.4) * K, (8.4 + 4.4) * K), fill=W)
line(d, [(2.4, 27), (2.4, 21), (4.6, 17.4), (9, 16.4), (13.4, 17.4), (15.6, 21), (15.6, 27)], 3.0)
arc = [(23 + 4 * math.cos(math.radians(a)), 12 + 4 * math.sin(math.radians(a))) for a in range(180, 361, 15)]
line(d, [(19, 27)] + arc + [(27, 20)], 3.0); line(d, [(23.6, 19.6), (27, 24), (30.4, 19.6)], 3.0)
save(im, "turn_him")
print("ok")
