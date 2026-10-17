"""Glyph, brief 016: `turn_view`, the glyph for hint H13 (Turn the view: Q / E). White monoline on the kit's 32 box, like the other 014 glyphs.
A flattened ground ring (the ground at the camera's pitch) with an arrowhead on each end of two opposite arcs, so it turns either way, and a dot at the centre (the point the view turns about).
Run from the repo root: python3 -I briefs/014/glyph/scripts/turn_view.py -> briefs/014/glyph/glyphs/turn_view@1x/@2x.png"""
import math
from PIL import Image, ImageDraw
N = 512; K = N / 32.0; W = (255, 255, 255, 255)
im = Image.new("RGBA", (N, N), (0, 0, 0, 0)); d = ImageDraw.Draw(im)
CX, CY, RX, RY = 13.4, 16, 11.0, 8.0
def pt(a): return (CX + RX * math.cos(math.radians(a)), CY + RY * math.sin(math.radians(a)))
def line(P, w):
    P = [(x * K, y * K) for x, y in P]; d.line(P, fill=W, width=int(w * K), joint="curve")
    for x, y in P: d.ellipse((x - w * K / 2, y - w * K / 2, x + w * K / 2, y + w * K / 2), fill=W)
def arc(a0, a1, w=3.0, n=30): line([pt(a0 + (a1 - a0) * i / n) for i in range(n + 1)], w)
def head(a, direction):
    # arrowhead at angle a pointing along the tangent (direction +1 = increasing angle)
    x, y = pt(a); tx, ty = -RX * math.sin(math.radians(a)) * direction, RY * math.cos(math.radians(a)) * direction
    L = math.hypot(tx, ty); tx, ty = tx / L, ty / L; nx, ny = -ty, tx
    s = 4.2
    tip = (x + tx * 1.6, y + ty * 1.6)
    line([(tip[0] - tx * s + nx * s * 0.95, tip[1] - ty * s + ny * s * 0.95), tip, (tip[0] - tx * s - nx * s * 0.95, tip[1] - ty * s - ny * s * 0.95)], 3.0)
# one open ring (an orbit, the camera turning about its target), one arrowhead; a small ground diamond in the middle, not a dot, so it never reads as an eye or a refresh
arc(128, 386, 3.0, 48); head(386, +1)
line([(CX, CY - 2.8), (CX + 4.4, CY), (CX, CY + 2.8), (CX - 4.4, CY), (CX, CY - 2.8)], 2.4)
im.resize((64, 64), Image.LANCZOS).save("briefs/014/glyph/glyphs/turn_view@2x.png"); im.resize((32, 32), Image.LANCZOS).save("briefs/014/glyph/glyphs/turn_view@1x.png")
print("ok")
