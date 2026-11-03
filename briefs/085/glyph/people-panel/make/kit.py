"""Suborned HUD kit primitives for mocks: the kit's own nine-slice sprites, icons, faces and fonts, drawn on an RGBA layer at S x 1080p.
Glyph, brief 085 note 10. Mock tool only; nothing here ships."""
import math
from functools import lru_cache
from PIL import Image, ImageDraw, ImageFilter, ImageFont, ImageChops

ROOT = '/Users/sky/Projects/suborned/'
K = ROOT + 'unity/Assets/Suborned/UI/Resources/Kit/'
INK, INK_DEEP = (0x17, 0x30, 0x2f, 255), (0x0b, 0x16, 0x15, 255)
CREAM, DIM = (0xf7, 0xf4, 0xec, 255), (0xb0, 0xcd, 0xc7, 255)
CHROME, LINE = (0xcb, 0xc6, 0xc3, 255), (0xd2, 0xe6, 0xe2, 255)
TURQ, BRASS = (0x60, 0xe0, 0xea, 255), (0x99, 0x78, 0x4d, 255)
BODY = (0x0c, 0x34, 0x34, 255)
SLICES = {'panel_dark': ('panels', 35), 'panel_strip': ('panels', 25), 'plate_dark': ('markers', 24), 'panel_slot': ('panels', 26), 'panel_dark_modal': ('panels', 44)}


@lru_cache(maxsize=None)
def sprite(path):
    return Image.open(K + path + '@2x.png').convert('RGBA')


@lru_cache(maxsize=None)
def font(name, px):
    return ImageFont.truetype(K + f'fonts/{name}.ttf', px)


class Layer:
    def __init__(self, s=2, w=1920, h=1080):
        self.s, self.w, self.h = s, w, h
        self.img = Image.new('RGBA', (w * s, h * s), (0, 0, 0, 0))
        self.rects = []  # (x, y, w, h, kind, text, extra): the budget manifest

    def note(self, x, y, w, h, kind, text=None, **extra):
        e = {'kind': kind, 'x': round(x), 'y': round(y), 'w': round(w), 'h': round(h)}
        if text: e['text'] = text
        e.update(extra); self.rects.append(e)

    def paste(self, im, x, y):
        self.img.alpha_composite(im, (round(x * self.s), round(y * self.s)))

    def nine(self, name, x, y, w, h, scale=0.5, alpha=1.0):
        folder, cut = SLICES[name]; sp = sprite(f'{folder}/{name}')
        W, H = round(w * self.s), round(h * self.s); k = scale * self.s; cw = max(1, round(cut * k)); sw, sh = sp.size
        cutx, cuty = cut, min(cut, sh // 2)
        chh = max(1, round(cuty * k))
        out = Image.new('RGBA', (W, H), (0, 0, 0, 0))
        def part(box, size, at):
            if size[0] <= 0 or size[1] <= 0: return
            out.alpha_composite(sp.crop(box).resize(size, Image.LANCZOS), at)
        xs = [(0, cutx, 0, cw), (cutx, sw - cutx, cw, W - 2 * cw), (sw - cutx, sw, W - cw, cw)]
        ys = [(0, cuty, 0, chh), (cuty, sh - cuty, chh, H - 2 * chh), (sh - cuty, sh, H - chh, chh)]
        for sx0, sx1, dx, dw in xs:
            for sy0, sy1, dy, dh in ys:
                part((sx0, sy0, sx1, sy1), (dw, dh), (dx, dy))
        if alpha < 1: out.putalpha(out.getchannel('A').point(lambda v: int(v * alpha)))
        self.img.alpha_composite(out, (round(x * self.s), round(y * self.s)))

    def tw(self, t, size, face='Jost-Bold'):
        return font(face, size * self.s).getlength(t) / self.s

    def text(self, x, y, t, size, col=CREAM, face='Jost-Bold', anchor='lm', alpha=None):
        d = ImageDraw.Draw(self.img); c = col if alpha is None else col[:3] + (int(255 * alpha),)
        d.text((x * self.s, y * self.s), t, font=font(face, size * self.s), fill=c, anchor=anchor)

    def icon(self, name, x, y, size, col=CREAM, folder='icons/singles', alpha=1.0):
        sp = sprite(f'{folder}/{name}'); n = round(size * self.s); a = sp.getchannel('A').resize((n, n), Image.LANCZOS)
        if alpha < 1: a = a.point(lambda v: int(v * alpha))
        out = Image.new('RGBA', (n, n), col[:3] + (0,)); out.putalpha(a); self.img.alpha_composite(out, (round(x * self.s), round(y * self.s)))

    def art(self, path, x, y, w, h=None, alpha=1.0):
        sp = sprite(path).resize((round(w * self.s), round((h or w) * self.s)), Image.LANCZOS)
        if alpha < 1: sp.putalpha(sp.getchannel('A').point(lambda v: int(v * alpha)))
        self.img.alpha_composite(sp, (round(x * self.s), round(y * self.s)))

    def disc(self, x, y, d, fill=INK_DEEP, ring=CHROME, rw=2.0, ring2=None, glow=None):
        """A filled circle with a ring, supersampled."""
        n = round(d * self.s) + 8; k = 4; big = Image.new('RGBA', (n * k, n * k), (0, 0, 0, 0)); g = ImageDraw.Draw(big)
        c0, c1 = 4 * k, (n - 4) * k
        if fill: g.ellipse((c0, c0, c1, c1), fill=fill)
        if ring: g.ellipse((c0, c0, c1, c1), outline=ring, width=round(rw * self.s * k))
        out = big.resize((n, n), Image.LANCZOS)
        self.img.alpha_composite(out, (round(x * self.s) - 4, round(y * self.s) - 4))

    def face(self, role, x, y, d, ring=CHROME, rw=2.0, dim=False):
        """A person's painted face in a chrome ring (the guard: his pictogram, as FaceOf does today)."""
        n = round(d * self.s); k = 3
        hi = {'waitress': 'inez_A', 'driver': 'driver_A', 'councillor': 'vale_C', 'guard': 'model_security'}.get(role)
        self.disc(x, y, d, fill=INK, ring=None)
        if hi:
            sp = Image.open(ROOT + f'briefs/063/cameos/{hi}.png').convert('RGBA'); i = sp.width * 7 // 100; sp = sp.crop((i, i, sp.width - i, sp.height - i))
        else:
            sp = sprite(f'portraits/live/{role}_calm')
        sp = sp.resize((n, n), Image.LANCZOS)
        m = Image.new('L', (n * k, n * k), 0); ImageDraw.Draw(m).ellipse((0, 0, n * k - 1, n * k - 1), fill=255); m = m.resize((n, n), Image.LANCZOS)
        sp.putalpha(ImageChops.multiply(sp.getchannel('A'), m)); self.img.alpha_composite(sp, (round(x * self.s), round(y * self.s)))
        self.disc(x - 1, y - 1, d + 2, fill=None, ring=INK_DEEP, rw=1)
        self.disc(x, y, d, fill=None, ring=ring, rw=rw)

    def rrect(self, x, y, w, h, r, fill=None, line=None, lw=1.0):
        s = self.s; k = 3; W, H = round(w * s * k) + 6, round(h * s * k) + 6
        big = Image.new('RGBA', (W, H), (0, 0, 0, 0)); g = ImageDraw.Draw(big)
        g.rounded_rectangle((3, 3, W - 4, H - 4), r * s * k, fill=fill, outline=line, width=max(1, round(lw * s * k)))
        out = big.resize((max(1, W // k), max(1, H // k)), Image.LANCZOS); self.img.alpha_composite(out, (round(x * s) - 1, round(y * s) - 1))

    def rim(self, x, y, w, h, r, state, col=CREAM, lw=1.3):
        """The Intel rim as built (SlotRim): solid learned, long dashes told, fine dots unknown."""
        if state == 'learned': return self.rrect(x, y, w, h, r, line=col, lw=lw)
        pts = _rr_points(x, y, w, h, r); on, off = (3.0, 2.0) if state == 'told' else (1.0, 2.2)
        s = self.s; k = 3; big = Image.new('RGBA', ((round(w) + 8) * s * k, (round(h) + 8) * s * k), (0, 0, 0, 0)); g = ImageDraw.Draw(big)
        run, draw = 0.0, True
        for a, b in zip(pts, pts[1:]):
            d = math.dist(a, b); run += d
            if draw: g.line([((a[0] - x + 4) * s * k, (a[1] - y + 4) * s * k), ((b[0] - x + 4) * s * k, (b[1] - y + 4) * s * k)], fill=col, width=round(lw * s * k))
            if run >= (on if draw else off): draw, run = not draw, 0.0
        self.img.alpha_composite(big.resize((big.width // k, big.height // k), Image.LANCZOS), (round((x - 4) * s), round((y - 4) * s)))

    def line(self, pts, col=CREAM, lw=1.5, dashed=False):
        s = self.s; g = ImageDraw.Draw(self.img)
        if not dashed: g.line([(a * s, b * s) for a, b in pts], fill=col, width=round(lw * s), joint='curve'); return
        for p, q in zip(pts, pts[1:]):
            d = math.dist(p, q); n = int(d // 7)
            for i in range(n):
                if i % 2 == 0: g.line([((p[0] + (q[0] - p[0]) * i / n) * s, (p[1] + (q[1] - p[1]) * i / n) * s), ((p[0] + (q[0] - p[0]) * (i + 1) / n) * s, (p[1] + (q[1] - p[1]) * (i + 1) / n) * s)], fill=col, width=round(lw * s))

    def ground_ring(self, cx, cy, rx, col=TURQ, lw=3.0, ratio=0.62, glow=True):
        """A ring on the ground at the play camera's tilt, with the kit's turquoise glow."""
        s = self.s; k = 2; ry = rx * ratio; pad = 16; W, H = round((rx + pad) * 2 * s * k), round((ry + pad) * 2 * s * k)
        big = Image.new('RGBA', (W, H), (0, 0, 0, 0)); g = ImageDraw.Draw(big)
        g.ellipse((pad * s * k, pad * s * k, W - pad * s * k, H - pad * s * k), outline=col, width=round(lw * s * k))
        out = big.resize((W // k, H // k), Image.LANCZOS)
        if glow:
            gl = out.filter(ImageFilter.GaussianBlur(4 * s)); gl.putalpha(gl.getchannel('A').point(lambda v: min(255, int(v * 1.6))))
            self.img.alpha_composite(gl, (round((cx - rx - pad) * s), round((cy - ry - pad) * s)))
        self.img.alpha_composite(out, (round((cx - rx - pad) * s), round((cy - ry - pad) * s)))

    def tail(self, cx, y, up=True, w=18, h=11, fill=BODY):
        """The chrome tail: chrome outer triangle, the body's triangle inside."""
        s = self.s; g = ImageDraw.Draw(self.img); sgn = -1 if up else 1
        g.polygon([((cx - w / 2 - 1.5) * s, y * s), ((cx + w / 2 + 1.5) * s, y * s), (cx * s, (y + sgn * (h + 1.5)) * s)], fill=CHROME)
        g.polygon([((cx - w / 2 + .5) * s, (y + 1 * -sgn) * s), ((cx + w / 2 - .5) * s, (y + 1 * -sgn) * s), (cx * s, (y + sgn * (h - 1)) * s)], fill=fill)


def _rr_points(x, y, w, h, r, n=8):
    r = min(r, w / 2, h / 2); pts = []
    def arc(cx, cy, a0):
        for i in range(n + 1):
            a = math.radians(a0 + 90 * i / n); pts.append((cx + math.cos(a) * r, cy + math.sin(a) * r))
    arc(x + w - r, y + r, -90); arc(x + w - r, y + h - r, 0); arc(x + r, y + h - r, 90); arc(x + r, y + r, 180); pts.append(pts[0])
    out = [pts[0]]  # resample to about 0.5 px
    for a, b in zip(pts, pts[1:]):
        d = math.dist(a, b); m = max(1, int(d / .5))
        for i in range(1, m + 1): out.append((a[0] + (b[0] - a[0]) * i / m, a[1] + (b[1] - a[1]) * i / m))
    return out
