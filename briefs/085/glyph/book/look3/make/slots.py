"""The slots: every page is drawn by these, from data, with no hand placing. The rules that make it extend live here. Glyph, brief 085."""
import sys
sys.path.insert(0, '/Users/sky/Projects/suborned/briefs/085/glyph/book/make')
sys.path.insert(0, '/Users/sky/Projects/suborned/briefs/085/glyph/people-panel/make')
from page import *
import numpy as np, cv2
R = '/Users/sky/Projects/suborned/'; OUT = R + 'briefs/085/glyph/book/look3/'
ORANGE = (0xe8, 0x62, 0x2c, 255)  # the café parasols, sampled from the frame
DEEP, SEA2, PALE = (0x1f, 0x6c, 0x67, 255), (0x72, 0xb5, 0xa7, 255), (0x9b, 0xcf, 0xc2, 255)
L, T, U = 'learned', 'told', 'unk'
HI = {'waitress': 'inez_A', 'driver': 'driver_A', 'councillor': 'vale_C'}  # the paintings that exist; anyone else takes the bust pictogram


def street(ramp, dx=0):
    """The real frozen frame, HUD removed by its own mask, softened and screen-printed down to a few flat tones, so it stays calm."""
    p = R + 'out/batch/shots-prev2/cafe-front-day'; im = np.array(Image.open(p + '.png').convert('RGB'))[:, :, ::-1].copy()
    m = (np.array(Image.open(p + '.hud.png').convert('RGBA'))[:, :, 3] > 24).astype(np.uint8) * 255; m = cv2.dilate(m, np.ones((9, 9), np.uint8))
    im = cv2.inpaint(im, m, 7, cv2.INPAINT_TELEA); im = cv2.medianBlur(im, 5); im = np.roll(im, dx, axis=1); g = cv2.GaussianBlur(cv2.cvtColor(im, cv2.COLOR_BGR2GRAY), (0, 0), 1.0).astype(float) / 255; g = np.clip((g - .12) / .8, 0, .999)
    lv = np.clip((g * len(ramp)).astype(int), 0, len(ramp) - 1); return Image.fromarray(np.array(ramp, np.uint8)[lv])


def face_big(P, who, x, y, d, ring=None, rw=0, fill=PALE, shadow=None):
    """A person's real painting (or the kit's pictogram if none exists), in a disc. It may run off the screen. `shadow`: a flat offset disc behind it, printed out of register."""
    k = 3; n = round(d * P.s); m = Image.new('L', (n * k, n * k), 0); ImageDraw.Draw(m).ellipse((0, 0, n * k - 1, n * k - 1), fill=255); m = m.resize((n, n), Image.LANCZOS)
    g = ImageDraw.Draw(P.img); sc = P.s
    if shadow: g.ellipse(((x + d * .05) * sc, (y + d * .05) * sc, (x + d * 1.05) * sc, (y + d * 1.05) * sc), fill=shadow)
    if who is None:
        bust(P, x, y, d, fill)
    else:
        if who in HI:
            s = Image.open(R + f'briefs/063/cameos/{HI[who]}.png').convert('RGB'); i = s.width * 8 // 100; s = s.crop((i, i, s.width - i, s.height - i))
        else:
            s = Image.open(R + f'briefs/033/portraits/{who}_states.png').convert('RGB').crop((100, 90, 640, 630))
        s = s.resize((n, n), Image.LANCZOS).convert('RGBA'); s.putalpha(m); lay = Image.new('RGBA', P.img.size, (0, 0, 0, 0)); lay.paste(s, (round(x * sc), round(y * sc))); P.img.alpha_composite(lay)
    if ring: P.disc(x, y, d, fill=None, ring=ring, rw=rw)


def bust(P, x, y, d, fill):
    """No painting: the signage hotbar's own flat head and shoulders, in a disc. Every person in the game can have this."""
    k = 3; n = round(d * P.s) * k; m = Image.new('L', (n, n), 0); g = ImageDraw.Draw(m); g.ellipse((n * .34, n * .2, n * .66, n * .52), fill=255); g.ellipse((n * .16, n * .58, n * .84, n * 1.3), fill=255)
    c = Image.new('L', (n, n), 0); ImageDraw.Draw(c).ellipse((0, 0, n - 1, n - 1), fill=255); m = ImageChops.multiply(m, c).resize((n // k, n // k), Image.LANCZOS)
    g2 = ImageDraw.Draw(P.img); sc = P.s; g2.ellipse((x * sc, y * sc, (x + d) * sc, (y + d) * sc), fill=DEEP)
    lay = Image.new('RGBA', P.img.size, (0, 0, 0, 0)); o = Image.new('RGBA', m.size, CREAM[:3] + (255,)); o.putalpha(m); lay.paste(o, (round(x * sc), round(y * sc))); P.img.alpha_composite(lay)


def orbit(P, cx, cy, rx, ry, deg, col, lw=5, dot=None, at=0.0):
    """One tilted atomic orbit, flat, behind the hero, with one electron."""
    k = 2; W, H = P.img.size; big = Image.new('L', (W // k, H // k), 0); g = ImageDraw.Draw(big); sc = P.s / k
    pts = []
    t = math.radians(deg)
    for i in range(361):
        a = math.radians(i); ex, ey = rx * math.cos(a), ry * math.sin(a); pts.append(((cx + ex * math.cos(t) - ey * math.sin(t)) * sc, (cy + ex * math.sin(t) + ey * math.cos(t)) * sc))
    g.line(pts, fill=255, width=round(lw * sc), joint='curve'); m = big.resize((W, H), Image.LANCZOS); lay = Image.new('RGBA', (W, H), col[:3] + (0,)); lay.putalpha(m); P.img.alpha_composite(lay)
    if dot:
        a = math.radians(at); ex, ey = rx * math.cos(a), ry * math.sin(a); dx, dy = cx + ex * math.cos(t) - ey * math.sin(t), cy + ex * math.sin(t) + ey * math.cos(t); ImageDraw.Draw(P.img).ellipse(((dx - 15) * P.s, (dy - 15) * P.s, (dx + 15) * P.s, (dy + 15) * P.s), fill=dot)


def glyph_big(P, icon, x, y, d, fill=DEEP, col=CREAM):
    x0, y0 = max(x, 0), max(y, 0); g = ImageDraw.Draw(P.img); g.ellipse((x * P.s, y * P.s, (x + d) * P.s, (y + d) * P.s), fill=fill)
    n = round(d * .5 * P.s); a = sprite(f'icons/singles/{icon}').getchannel('A').resize((n, n), Image.BICUBIC).filter(ImageFilter.GaussianBlur(n / 90)).point(lambda v: 0 if v < 110 else 255 if v > 145 else (v - 110) * 7)
    o = Image.new('RGBA', (n, n), col[:3] + (0,)); o.putalpha(a); P.paste(o, x + d * .25, y + d * .25)


def name_size(name, w, P, top=150):
    """Rule: the name takes the biggest of three sizes that fits."""
    for s in (top, top * .72, top * .52):
        if P.tw(name, s) <= w: return s
    return top * .4


def flow_facts(P, facts, x, y, w, ink=INK, dim=SOFT, slot_fill=None, h=None):
    """Rule: 1 to 4 facts, one column, large; 5 to 8, two columns; 9 to 12, three. Above 12 the rest wait behind a '+n' disc. A fact is a pictogram and at most 3 words."""
    n = len(facts); cols = 1 if n <= 4 else 2 if n <= 8 else 3; size, step, d = {1: (37, 62, 48), 2: (30, 52, 40), 3: (25, 44, 34)}[cols]
    cw = w / cols; rows = -(-min(n, 12) // cols)
    for i, (ic, word, note, st) in enumerate(facts[:12]):
        c, r = i // rows, i % rows; fx, fy = x + c * cw, y + r * step
        if st == U:
            P.rim(fx, fy, d, d, d / 2, U, col=dim, lw=1.8); P.text(fx + d / 2, fy + d / 2, '?', d * .56, col=dim, anchor='mm')
        else:
            P.icon(ic, fx + 2, fy + 2, d - 4, col=ink, alpha=.8 if st == T else 1)
        P.put(fx + d + 14, fy + d / 2, word + (' ?' if st == U and note == '?' else ''), size, col=dim if st == U else ink, face='Jost-SemiBold')
        if note and note != '?': P.put(fx + d + 14 + P.tw(word, size, 'Jost-SemiBold') + 12, fy + d / 2 + 1, note, size * .78, col=dim)
    return y + rows * step


def meters_line(P, meters, x, y, ink=INK, dim=SOFT, on=DEEP, size=25):
    """Rule: any scale reads as a word and its dots, on one line. A person's scales share the line."""
    cx = x
    for w, v, n in meters:
        P.put(cx, y, w, size, col=ink, face='Jost-SemiBold'); cx += P.tw(w, size, 'Jost-SemiBold') + 10
        for k in range(n):
            if k < v: P.disc(cx, y - 8, 16, fill=on, ring=None)
            else: P.disc(cx, y - 8, 16, fill=None, ring=dim, rw=1.5)
            cx += 20
        cx += 22
    return cx


def knows_line(P, knows, x, y, label, ink=INK, dim=SOFT, size=25):
    P.put(x, y, label, size, col=dim, face='Jost-Medium'); cx = x + P.tw(label, size, 'Jost-Medium') + 14
    for ic, w in knows:
        if ic in ('fixer', 'scout', 'tech', 'enforcer'): face_big(P, ic, cx, y - 16, 32)
        else: P.icon(ic, cx + 2, y - 14, 28, col=ink)
        cx += 40; P.put(cx, y, w, size, col=ink, face='Jost-SemiBold'); cx += P.tw(w, size, 'Jost-SemiBold') + 24
    return cx


def track(P, items, x, y, w, ink=INK, dim=SOFT, on=DEEP, hot=ORANGE, d=46):
    """Rule: one line, spaced evenly, whatever it holds: events or the clock. It shows the latest six; the journal holds them all."""
    items = items[-6:]; n = len(items); step = w / max(n, 1)
    P.line([(x + d / 2, y + d / 2), (x + (n - 1) * step + d / 2, y + d / 2)], col=dim, lw=2.5)
    for i, (ic, word, t, past) in enumerate(items):
        cx = x + i * step; fill = hot if past == 2 else on if past else None
        if fill: P.disc(cx, y, d, fill=fill, ring=None); P.icon(ic, cx + d * .2, y + d * .2, d * .6, col=CREAM)
        else:
            P.disc(cx, y, d, fill=(0xf7, 0xf4, 0xec, 255) if ink == INK else BODY_, ring=dim, rw=2); P.icon(ic, cx + d * .2, y + d * .2, d * .6, col=ink)
        P.put(cx, y + d + 26, word, 24, col=ink, face='Jost-SemiBold'); P.put(cx, y + d + 56, t, 22, col=dim)


BODY_ = (0x0c, 0x34, 0x34, 255)


def sign(P, x, y, way, w=None, h=112, fill=DEEP, ink=CREAM, line=CREAM, sub=DIM, shutc=SOFT):
    """A way: the HUD's signage plate, flat: enamel, one cream inline, a pictogram, a word, its price. Shut ways go hollow."""
    ic, word, price, st = way; shut = st == 'shut'; size = 40
    w = w or 112 + max(P.tw(word, size), P.tw(price or '', 25, 'Jost-Medium')) + 34
    if shut:
        P.rrect(x, y, w, h, 18, fill=None, line=shutc, lw=2); inkc = shutc
    else:
        P.rrect(x, y, w, h, 18, fill=fill); P.rrect(x + 6, y + 6, w - 12, h - 12, 13, line=line, lw=2); inkc = ink
    P.icon('lock' if shut else ic, x + 26, y + h / 2 - 28, 56, col=inkc)
    P.put(x + 100, y + h / 2 - 16, word, size, col=inkc, face='Jost-Bold')
    if price: P.put(x + 100, y + h / 2 + 24, price, 25, col=inkc if shut else sub, face='Jost-Medium')
    return w


def people_grid(P, people, x, y, w, ink=INK, dim=SOFT, fill=PALE, d=150):
    """Rule: up to 8, big discs; to 24, smaller; above that, the groups below."""
    cols = 4; step = w / cols
    for i, (who, nm) in enumerate(people):
        cx = x + (i % cols) * step; cy = y + (i // cols) * (d + 92); face_big(P, who, cx, cy, d, fill=fill); P.put(cx + d / 2, cy + d + 32, nm, 32, col=ink, face='Jost-SemiBold', anchor='mm')


def groups(P, gs, x, y, w, ink=INK, dim=SOFT, on=DEEP, cols=3, rowh=210):
    """Rule: above 24 people, one big number per group. A click opens that group as the grid."""
    step = w / cols
    for i, (ic, word, n) in enumerate(gs):
        cx = x + (i % cols) * step; cy = y + (i // cols) * rowh
        P.put(cx, cy + 56, str(n), 104, col=on, face='Jost-Bold'); P.icon(ic, cx + 2, cy + 118, 36, col=ink); P.put(cx + 48, cy + 136, word, 32, col=ink, face='Jost-SemiBold')
