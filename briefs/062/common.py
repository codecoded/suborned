# Glyph, brief 062: shared drawing for the briefing and start boards. Everything is read from the game's own kit
# (unity/Assets/Suborned/UI/Resources/Kit), the game's real 2160 frames, and renders of the game's own figures; no colour here lacks a source.
import sys, os, math
sys.path.insert(0, "/Users/sky/Projects/suborned/briefs/057")
import kitdraw as kd
from kitdraw import *            # R, K, CREAM, INKDEEP, SLOTINK, TURQL, DIM, CHROME, INK, load, tint, icon, sprite
import numpy as np, cv2
from PIL import Image, ImageDraw, ImageFont, ImageFilter, ImageChops
OUT = R + "briefs/062/"; SC = R + "out/scratch/glyph/062/"; os.makedirs(SC, exist_ok=True)
kd.S = 2.0                       # 3840 x 2160 canvas = 2 px per 1080p game px
BRASS = (153, 120, 77); IVORY = (238, 233, 218); CARDIV = (252, 228, 204); DEEPTEAL = (31, 108, 103); SEAFOAM = (114, 181, 167)
TEXTCARD = (44, 58, 56); TURQ = (28, 185, 198); AMBER = (240, 160, 32)
def S(): return kd.S
def P(v): return int(round(v * kd.S))
def jost(name, px): return ImageFont.truetype(K + "fonts/" + name, int(round(px * kd.S)))
def txt(im, xy, s, name="Jost-Bold.ttf", px=15, fill=CREAM, anchor="ls", track=0.0, shadow=None):
    """text at game-px xy; track = letter spacing in em; shadow=(alpha, dy)"""
    f = jost(name, px); d = ImageDraw.Draw(im, "RGBA"); x, y = P(xy[0]), P(xy[1])
    if track == 0:
        if shadow: d.text((x, y + P(shadow[1])), s, font=f, fill=INKDEEP + (int(255 * shadow[0]),), anchor=anchor)
        d.text((x, y), s, font=f, fill=fill if len(fill) == 4 else fill + (255,), anchor=anchor); return f.getlength(s)
    tw = sum(f.getlength(c) + track * f.size for c in s) - track * f.size
    if anchor[0] == "r": x -= tw
    elif anchor[0] == "m": x -= tw / 2
    a = "l" + anchor[1]; cx = x
    for c in s:
        if shadow: d.text((cx, y + P(shadow[1])), c, font=f, fill=INKDEEP + (int(255 * shadow[0]),), anchor=a)
        d.text((cx, y), c, font=f, fill=fill if len(fill) == 4 else fill + (255,), anchor=a); cx += f.getlength(c) + track * f.size
    return tw
def tw_of(s, name="Jost-Bold.ttf", px=15, track=0.0):
    f = jost(name, px); return (sum(f.getlength(c) + track * f.size for c in s) - track * f.size) / kd.S if track else f.getlength(s) / kd.S
def frame(rel, w, h, slice2x, scale):
    """kit nine-slice at game-px size w x h; scale = the sprite's display scale (1 = @1x)"""
    return nine(rel, P(w), P(h), slice2x, kd.S / 2 * scale)
def put(im, sp, x, y, anchor="lt"):
    w, h = sp.size
    ox = {"l": 0, "m": w / 2, "r": w}[anchor[0]]; oy = {"t": 0, "m": h / 2, "b": h}[anchor[1]]
    im.alpha_composite(sp, (int(round(P(x) - ox)), int(round(P(y) - oy))))
def ico(im, name, x, y, px=16, rgb=CREAM, anchor="mm"):
    sp = icon(name, px, rgb); put(im, sp, x, y, anchor)
def disc(im, x, y, px, rel="markers/disc_dark_24"):
    sp = sprite(rel, game_h=px); put(im, sp, x, y, "mm")
def cameo(im, x, y, px, src, ring=True):
    """a face in the kit's chrome cameo ring (2 px #cbc6c3 over 1 px ink-deep): src = PIL image (square-ish) or a kit portrait name"""
    n = P(px); Q = 3; base = Image.new("RGBA", (n * Q, n * Q), (0, 0, 0, 0)); d = ImageDraw.Draw(base)
    d.ellipse((0, 0, n * Q - 1, n * Q - 1), fill=INKDEEP + (255,))
    r1 = int(round(1 * kd.S * Q)); r2 = r1 + int(round(2 * kd.S * Q))
    d.ellipse((r1, r1, n * Q - 1 - r1, n * Q - 1 - r1), fill=CHROME + (255,))
    d.ellipse((r2, r2, n * Q - 1 - r2, n * Q - 1 - r2), fill=INKDEEP + (255,))
    base = base.resize((n, n), Image.LANCZOS)
    if isinstance(src, str): src = load(K + "portraits/%s@2x.png" % src)
    inner = n - 2 * int(round(2.4 * kd.S)); b = src.convert("RGBA")
    s = max(inner / b.width, inner / b.height); b = b.resize((int(b.width * s) + 1, int(b.height * s) + 1), Image.LANCZOS)
    cx, cy = (b.width - inner) // 2, (b.height - inner) // 2; b = b.crop((cx, cy, cx + inner, cy + inner))
    m = Image.new("L", (inner, inner), 0); ImageDraw.Draw(m).ellipse((0, 0, inner - 1, inner - 1), fill=255)
    b.putalpha(ImageChops.multiply(b.split()[3], m)); base.alpha_composite(b, ((n - inner) // 2, (n - inner) // 2))
    put(im, base, x, y, "mm"); return base
def hairline(im, x0, y0, x1, y1, rgb=BRASS, w=1.0, a=255):
    d = ImageDraw.Draw(im, "RGBA"); d.line((P(x0), P(y0), P(x1), P(y1)), fill=rgb + (a,), width=max(1, int(round(w * kd.S))))
def stroke(im, pts, w=2.9, dash=None, a=1.0):
    """the cone/route language of brief 054: cream over ink-deep (0.17 under 0.075 at alpha 0.6 in the world; here in game px)"""
    Q = 2; ov = Image.new("RGBA", (im.width * Q, im.height * Q), (0, 0, 0, 0)); d = ImageDraw.Draw(ov)
    PP = [(P(x) * Q, P(y) * Q) for x, y in pts]
    for rgb, ww, al in ((INKDEEP, w * 2.3, 0.55), (CREAM, w, 1.0)):
        ww = int(round(ww * kd.S * Q))
        if not dash:
            d.line(PP, fill=rgb + (int(255 * al * a),), width=ww, joint="curve")
        else:
            on, off = dash[0] * kd.S * Q, dash[1] * kd.S * Q; run = 0.0; pen = True
            for (x0, y0), (x1, y1) in zip(PP, PP[1:]):
                L = math.hypot(x1 - x0, y1 - y0); t = 0.0
                while t < L:
                    seg = min((on if pen else off) - run, L - t)
                    if pen: d.line((x0 + (x1 - x0) * t / L, y0 + (y1 - y0) * t / L, x0 + (x1 - x0) * (t + seg) / L, y0 + (y1 - y0) * (t + seg) / L), fill=rgb + (int(255 * al * a),), width=ww)
                    t += seg; run += seg
                    if run >= (on if pen else off) - 1e-6: pen = not pen; run = 0.0
    ov = ov.resize(im.size, Image.LANCZOS); im.alpha_composite(ov)
def ring_ground(im, x, y, rx, ry, dash=None, w=2.4):
    Q = 2; ov = Image.new("RGBA", (im.width * Q, im.height * Q), (0, 0, 0, 0)); d = ImageDraw.Draw(ov)
    for rgb, ww in ((INKDEEP, w * 2.1), (CREAM, w)):
        ww = int(round(ww * kd.S * Q)); box = (P(x - rx) * Q, P(y - ry) * Q, P(x + rx) * Q, P(y + ry) * Q)
        if not dash: d.ellipse(box, outline=rgb + (255,), width=ww)
        else:
            n = 28
            for i in range(n):
                a0 = 360 * i / n; d.arc(box, a0, a0 + 360 / n * 0.55, fill=rgb + (255,), width=ww)
    ov = ov.resize(im.size, Image.LANCZOS); im.alpha_composite(ov)
def shadowed(im, sp, x, y, anchor="lt", blur=14, alpha=0.5, dy=6):
    w, h = sp.size; ox = {"l": 0, "m": w / 2, "r": w}[anchor[0]]; oy = {"t": 0, "m": h / 2, "b": h}[anchor[1]]
    X, Y = int(round(P(x) - ox)), int(round(P(y) - oy))
    sh = Image.new("RGBA", (w + 4 * blur * 2, h + 4 * blur * 2), (0, 0, 0, 0)); a = sp.split()[3].point(lambda v: int(v * alpha))
    sh.paste(INKDEEP + (255,), (blur * 4, blur * 4), a); sh = sh.filter(ImageFilter.GaussianBlur(blur))
    im.alpha_composite(sh, (X - blur * 4, Y - blur * 4 + P(dy) // 2)) if X - blur * 4 >= 0 and Y - blur * 4 >= 0 else None
    im.alpha_composite(sp, (X, Y))
def vignette(im, strength=0.5, inner=0.55, color=INKDEEP):
    w, h = im.size; yy, xx = np.mgrid[0:h, 0:w]; r = np.sqrt(((xx - w / 2) / (w / 2)) ** 2 + ((yy - h / 2) / (h / 2)) ** 2) / math.sqrt(2)
    a = np.clip((r - inner) / (1 - inner), 0, 1) ** 1.6 * strength * 255
    ov = Image.new("RGBA", im.size, color + (0,)); ov.putalpha(Image.fromarray(a.astype("uint8"))); im.alpha_composite(ov)
def gradient(im, box, rgb, a0, a1, vertical=True):
    x0, y0, x1, y1 = [P(v) for v in box]; w, h = x1 - x0, y1 - y0
    t = np.linspace(a0, a1, h if vertical else w); arr = np.tile(t[:, None], (1, w)) if vertical else np.tile(t[None, :], (h, 1))
    ov = Image.new("RGBA", (w, h), rgb + (0,)); ov.putalpha(Image.fromarray((arr * 255).clip(0, 255).astype("uint8"))); im.alpha_composite(ov, (x0, y0))
def radial(im, cx, cy, rx, ry, rgb, a0):
    x0, y0 = P(cx - rx), P(cy - ry); w, h = P(2 * rx), P(2 * ry); yy, xx = np.mgrid[0:h, 0:w]
    r = np.sqrt(((xx - w / 2) / (w / 2)) ** 2 + ((yy - h / 2) / (h / 2)) ** 2); a = np.clip(1 - r, 0, 1) ** 1.3 * a0 * 255
    ov = Image.new("RGBA", (w, h), rgb + (0,)); ov.putalpha(Image.fromarray(a.astype("uint8")))
    im.alpha_composite(ov, (x0, y0))

# ---------------------------------------------------------------- the world, HUD taken out
def world(name="overhead-pickup", folder="briefs/049/build/2160-0299/"):
    base = Image.open(R + folder + name + ".png").convert("RGBA")
    hud = Image.open(R + folder + name + ".hud.png").convert("RGBA").split()[3]
    m = np.array(hud); m = (m > 8).astype("uint8") * 255; m = cv2.dilate(m, np.ones((9, 9), np.uint8))
    a = np.array(base.convert("RGB")); small = cv2.resize(a, (1920, 1080), interpolation=cv2.INTER_AREA); ms = cv2.resize(m, (1920, 1080), interpolation=cv2.INTER_NEAREST)
    filled = cv2.inpaint(small, ms, 7, cv2.INPAINT_TELEA); up = cv2.resize(filled, (3840, 2160), interpolation=cv2.INTER_CUBIC)
    mm = cv2.GaussianBlur(m, (15, 15), 0)[..., None] / 255.0
    out = (a * (1 - mm) + up * mm).astype("uint8"); return Image.fromarray(out).convert("RGBA")

# ---------------------------------------------------------------- the file photograph: a game figure's bust, as a stamped record print
def file_photo(src, size, crop=None, silver_hair=False, warm=(247, 233, 205), cold=(14, 30, 29), grain=0.05, seed=3):
    im = Image.open(src).convert("RGB")
    if crop: im = im.crop(crop)
    im = im.resize(size, Image.LANCZOS); a = np.array(im).astype(float) / 255
    if silver_hair:   # the stand-in's blond hair to silver: warm light pixels in the top third
        h, w = a.shape[:2]; hsv = cv2.cvtColor((a * 255).astype("uint8"), cv2.COLOR_RGB2HSV).astype(float)
        mask = ((hsv[..., 0] > 8) & (hsv[..., 0] < 32) & (hsv[..., 1] > 40)) ; mask[int(h * 0.50):] = False
        g = a.mean(axis=2, keepdims=True); gray = np.clip(g * 0.88 + 0.05, 0, 1); a = np.where(mask[..., None], np.repeat(gray, 3, axis=2) * np.array([0.93, 0.96, 1.0]), a)
    L = (0.30 * a[..., 0] + 0.59 * a[..., 1] + 0.11 * a[..., 2]); L = np.clip((L - 0.30) / 0.62, 0, 1) ** 0.95
    rng = np.random.default_rng(seed); L = np.clip(L + rng.normal(0, grain, L.shape), 0, 1)
    c0 = np.array(cold) / 255.0; c1 = np.array(warm) / 255.0
    out = c0[None, None, :] * (1 - L[..., None]) + c1[None, None, :] * L[..., None]
    h, w = L.shape; yy, xx = np.mgrid[0:h, 0:w]; r = np.sqrt(((xx - w / 2) / (w / 2)) ** 2 + ((yy - h * 0.45) / (h / 2)) ** 2)
    out *= (1 - 0.35 * np.clip(r - 0.6, 0, 1))[..., None]
    return Image.fromarray((out * 255).clip(0, 255).astype("uint8")).convert("RGBA")
def rounded(im, r):
    m = Image.new("L", im.size, 0); ImageDraw.Draw(m).rounded_rectangle((0, 0, im.width - 1, im.height - 1), radius=r, fill=255)
    im = im.copy(); im.putalpha(ImageChops.multiply(im.split()[3], m)); return im
