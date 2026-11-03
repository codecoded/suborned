# Glyph, brief 039 (2026-10-26): the four pictograms the fight proposal adds to the kit's icons/singles, drawn at the kit's weight (solid, round-ended, stroke
# about 8 % of a 64 px square, @2x file) and one overlay promoted to an icon (the crosshair). Proposal-grade: if Sky picks, they are redrawn as vector art for
# icons/singles. `install()` makes them resolvable by name in the board libraries (kitdraw.icon and glib.icon), so pills and rings use them like any kit icon.
import sys, math, os
sys.path.insert(0, "/Users/sky/Projects/suborned/briefs/058"); sys.path.insert(0, "/Users/sky/Projects/suborned/briefs/057")
import kitdraw as kd
import glib
from PIL import Image, ImageDraw

R = "/Users/sky/Projects/suborned/"
TMPI = R + "out/scratch/glyph/039/icons/"
NEW = ("shield", "shield_half", "steam", "burst", "mark", "megaphone", "crosshair")
Q = 4; N = 64 * Q * 2          # draw at 512, save 64

def _bez(p0, p1, p2, n=28): return [((1 - t) ** 2 * p0[0] + 2 * (1 - t) * t * p1[0] + t * t * p2[0], (1 - t) ** 2 * p0[1] + 2 * (1 - t) * t * p1[1] + t * t * p2[1]) for t in [i / n for i in range(n + 1)]]

def _shield_pts(s=2.0):
    """a heater shield with round top corners, on a 128 grid scaled by s"""
    r = 14; pts = []
    pts += [(20 + r, 10)]; pts += [(108 - r, 10)]
    pts += _bez((108 - r, 10), (108, 10), (108, 10 + r), 8)
    pts += [(108, 66)]
    pts += _bez((108, 66), (108, 96), (64, 118), 24)
    pts += _bez((64, 118), (20, 96), (20, 66), 24)
    pts += [(20, 10 + r)]; pts += _bez((20, 10 + r), (20, 10), (20 + r, 10), 8)
    return [(x * s * 2, y * s * 2) for x, y in pts]

def _canvas(): return Image.new("RGBA", (N, N), (0, 0, 0, 0))
def _save(im, name):
    os.makedirs(TMPI, exist_ok=True); im.resize((64, 64), Image.LANCZOS).save(TMPI + "%s@2x.png" % name)

def make():
    W = (255, 255, 255, 255)
    # shield: full cover
    im = _canvas(); ImageDraw.Draw(im).polygon(_shield_pts(), fill=W); _save(im, "shield")
    # shield_half: partial cover, an outline with the left half solid
    im = _canvas(); d = ImageDraw.Draw(im); pts = _shield_pts(); d.line(pts + [pts[0]], fill=W, width=int(N * 0.095), joint="curve")
    half = Image.new("L", im.size, 0); ImageDraw.Draw(half).polygon(pts, fill=255); cut = Image.new("L", im.size, 0); ImageDraw.Draw(cut).rectangle((0, 0, N // 2, N), fill=255)
    solid = Image.new("L", im.size, 0); solid.paste(half, (0, 0), cut)
    a = Image.fromarray(__import__("numpy").maximum(__import__("numpy").array(im.split()[3]), __import__("numpy").array(solid))); im.putalpha(a); _save(im, "shield_half")
    # steam: a grate with three rising wisps
    im = _canvas(); d = ImageDraw.Draw(im); k = N / 256.0
    d.rounded_rectangle((36 * k, 198 * k, 220 * k, 236 * k), radius=16 * k, fill=W)
    for j, x in enumerate((72, 128, 184)):
        pts = []
        for i in range(31):
            t = i / 30.0; y = 178 - t * 150; pts.append(((x + math.sin(t * math.pi * 2 + j * 1.3) * 15) * k, y * k))
        d.line(pts, fill=W, width=int(24 * k), joint="curve")
        for p in (pts[0], pts[-1]): d.ellipse((p[0] - 12 * k, p[1] - 12 * k, p[0] + 12 * k, p[1] + 12 * k), fill=W)
    _save(im, "steam")
    # burst: the kit's pistol, smaller, with three rounds streaking out of it
    gun = Image.open(kd.K + "icons/singles/fire@2x.png").convert("RGBA")
    g = gun.crop(gun.split()[3].getbbox()); gs = 0.62; g = g.resize((int(g.width * gs * N / 64), int(g.height * gs * N / 64)), Image.LANCZOS)
    im = _canvas(); k = N / 256.0; tint = Image.new("RGBA", g.size, W); tint.putalpha(g.split()[3]); im.alpha_composite(tint, (int(8 * k), int(128 * k - g.height / 2 - 6 * k)))
    d = ImageDraw.Draw(im)
    for (y, x0, x1) in ((74, 150, 226), (112, 168, 238), (150, 150, 226)):
        d.line([(x0 * k, y * k), (x1 * k, y * k)], fill=W, width=int(22 * k)); [d.ellipse((p * k - 11 * k, y * k - 11 * k, p * k + 11 * k, y * k + 11 * k), fill=W) for p in (x0, x1)]
    _save(im, "burst")
    # mark: four corner brackets round a dot
    im = _canvas(); d = ImageDraw.Draw(im); k = N / 256.0; L = 62; o = 34; wd = int(24 * k)
    for sx in (-1, 1):
        for sy in (-1, 1):
            cx, cy = (128 + sx * (128 - o)) * k, (128 + sy * (128 - o)) * k
            d.line([(cx, cy - sy * L * k), (cx, cy), (cx - sx * L * k, cy)], fill=W, width=wd, joint="curve")
            for p in ((cx, cy - sy * L * k), (cx - sx * L * k, cy), (cx, cy)): d.ellipse((p[0] - wd / 2, p[1] - wd / 2, p[0] + wd / 2, p[1] + wd / 2), fill=W)
    d.ellipse((128 * k - 26 * k, 128 * k - 26 * k, 128 * k + 26 * k, 128 * k + 26 * k), fill=W); _save(im, "mark")
    # megaphone: a cone, a handle, two waves (Talk down: the Fixer's shout; the kit's `pressure` is the Pressure approach, `radio` is the report)
    im = _canvas(); d = ImageDraw.Draw(im); k = N / 256.0
    d.rounded_rectangle((54 * k, 146 * k, 98 * k, 216 * k), radius=12 * k, fill=W)
    cone = [(22 * k, 104 * k), (78 * k, 104 * k), (158 * k, 52 * k), (158 * k, 204 * k), (78 * k, 152 * k), (22 * k, 152 * k)]
    d.polygon(cone, fill=W); d.line(cone + [cone[0]], fill=W, width=int(16 * k), joint="curve")
    for r in (62,):
        pts = [((172 + r * math.cos(math.radians(an))) * k, (128 + r * math.sin(math.radians(an))) * k) for an in [-40 + 80 * i / 24 for i in range(25)]]
        d.line(pts, fill=W, width=int(22 * k), joint="curve")
        for p_ in (pts[0], pts[-1]): d.ellipse((p_[0] - 11 * k, p_[1] - 11 * k, p_[0] + 11 * k, p_[1] + 11 * k), fill=W)
    _save(im, "megaphone")
    # crosshair: the kit's overlay, as an icon (80 px file, scaled to 64)
    cx = Image.open(kd.K + "overlays/crosshair@2x.png").convert("RGBA"); cx = cx.resize((64, 64), Image.LANCZOS); os.makedirs(TMPI, exist_ok=True); cx.save(TMPI + "crosshair@2x.png")

_orig_kd_icon = kd.icon; _orig_gl_icon = glib.icon
def kd_icon(name, game_px, rgb=kd.CREAM):
    if name in NEW:
        im = kd.load(TMPI + name + "@2x.png"); n = int(round(game_px * kd.S)); return kd.tint(im.resize((n, n), Image.LANCZOS), rgb)
    return _orig_kd_icon(name, game_px, rgb)
def gl_icon(layer, name, cx, cy, size, rgb=kd.CREAM, alpha=255):
    if name in NEW:
        im = kd_icon(name, size, rgb)
        if alpha < 255: im.putalpha(im.split()[3].point(lambda v: int(v * alpha / 255)))
        layer.alpha_composite(im, (int(round(cx * glib.S - im.width / 2)), int(round(cy * glib.S - im.height / 2)))); return
    _orig_gl_icon(layer, name, cx, cy, size, rgb, alpha)

def install():
    if not os.path.exists(TMPI + "megaphone@2x.png"): make()
    kd.icon = kd_icon; glib.icon = gl_icon

if __name__ == "__main__":
    make(); install()
    names = ["shield", "shield_half", "mark", "megaphone", "steam", "burst", "crosshair", "pressure", "radio", "fire", "chev_down", "s_moving"]
    cell = 150; sheet = Image.new("RGBA", (cell * len(names), 330), (11, 22, 21, 255)); d = ImageDraw.Draw(sheet); f = kd.font("Jost-Bold.ttf", 7)
    for i, n in enumerate(names):
        for y, sz in ((50, 64), (140, 26), (200, 18), (250, 14)):
            if n == "crosshair" or n in NEW or True:
                im = kd_icon(n, sz, kd.CREAM); sheet.alpha_composite(im, (int(i * cell + cell / 2 - im.width / 2), int(y - im.height / 2 + (0 if sz == 64 else 0))))
        d.text((i * cell + cell / 2, 305), n, font=f, fill=(176, 205, 199), anchor="mm")
    sheet.convert("RGB").save(R + "out/scratch/glyph/039/icons_sheet2.png"); print("ok")
