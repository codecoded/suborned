# Glyph, brief 057: the lovers icon, three candidates, drawn solid white on transparent at 64 px @2x like the kit's singles
# (bbox about 3..59, white, alpha only; the game tints it cream). Supersampled 8x and reduced with LANCZOS.
# Run: python3 -I briefs/057/lovers/draw.py   -> briefs/057/lovers/lovers_{a,b,c}@2x.png
import math, os
from PIL import Image, ImageDraw, ImageChops
SS = 8; N = 64 * SS
OUT = os.path.dirname(os.path.abspath(__file__))

def heart(cx, cy, w, rot=0.0, n=360):
    """parametric heart, width w (canvas px), centred on its bbox; rotated rot degrees (clockwise on screen)"""
    pts = []
    for i in range(n):
        t = 2 * math.pi * i / n
        x = 16 * math.sin(t) ** 3
        y = 13 * math.cos(t) - 5 * math.cos(2 * t) - 2 * math.cos(3 * t) - math.cos(4 * t)
        pts.append((x, -y))
    xs = [p[0] for p in pts]; ys = [p[1] for p in pts]
    s = w / (max(xs) - min(xs)); mx = (max(xs) + min(xs)) / 2; my = (max(ys) + min(ys)) / 2
    r = math.radians(rot); c, sn = math.cos(r), math.sin(r)
    out = []
    for x, y in pts:
        x = (x - mx) * s; y = (y - my) * s
        out.append(((cx + x * c - y * sn) * SS, (cy + x * sn + y * c) * SS))
    return out

def layer():
    return Image.new("L", (N, N), 0)
def poly(l, pts, fill=255): ImageDraw.Draw(l).polygon(pts, fill=fill)
def ell(l, cx, cy, rx, ry=None, fill=255):
    ry = ry or rx; ImageDraw.Draw(l).ellipse(((cx - rx) * SS, (cy - ry) * SS, (cx + rx) * SS, (cy + ry) * SS), fill=fill)
def rrect(l, x0, y0, x1, y1, r, fill=255):
    ImageDraw.Draw(l).rounded_rectangle((x0 * SS, y0 * SS, x1 * SS, y1 * SS), radius=r * SS, fill=fill)
def grow(l, px):
    """dilate a layer by px canvas px (round), for the knock-out gaps"""
    from PIL import ImageFilter
    k = int(px * SS) * 2 + 1
    return l.filter(ImageFilter.MaxFilter(k if k % 2 else k + 1))
def save(l, name):
    im = l.resize((64, 64), Image.LANCZOS)
    out = Image.new("RGBA", (64, 64), (255, 255, 255, 0)); out.putalpha(im)
    out.save(os.path.join(OUT, name))
    return out

def cand_a():
    """A, two hearts: a large heart in front, a second heart behind and up-right, a gap cut round the front heart so they read as two, not a blob"""
    back = layer(); poly(back, heart(40, 24, 31, 14))
    front = layer(); poly(front, heart(25, 38, 38, -8))
    gap = grow(front, 3.2)
    back = ImageChops.subtract(back, gap)
    return ImageChops.lighter(back, front)

def bust(l, x0, x1, headx, heady=29.5, head=7.6, top=41, r=11.5):
    ell(l, headx, heady, head)
    rrect(l, x0, top, x1, 59 + r, r)          # rounded shoulders; the lower part is cut flat below
    return l

def cand_b():
    """B, couple and heart: two busts side by side, heads leaning in, a heart floating above the gap between them"""
    l = layer()
    bust(l, 3, 30.6, 20.5)
    bust(l, 33.4, 61, 43.5)
    ImageDraw.Draw(l).rectangle((0, 59 * SS, N, N), fill=0)
    h = layer(); poly(h, heart(32, 13.5, 22))
    return ImageChops.lighter(l, h)

def cand_c():
    """C, heart with the couple knocked out: a solid heart, two heads and shared shoulders cut from it"""
    l = layer(); poly(l, heart(32, 31.5, 57))
    k = layer()
    ell(k, 24.6, 22.5, 4.9); ell(k, 39.4, 22.5, 4.9)
    rrect(k, 16.4, 30, 31.3, 40.5, 5.5); rrect(k, 32.7, 30, 47.6, 40.5, 5.5)
    return ImageChops.subtract(l, k)

if __name__ == "__main__":
    for k, f in (("a", cand_a), ("b", cand_b), ("c", cand_c)):
        save(f(), "lovers_%s@2x.png" % k)
    print("ok")
