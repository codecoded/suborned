"""The light page (brief 088, look4 L) for the debrief: the real frozen street at the end, one big curve, the face over its edge, the
book's round tabs, and the pieces every concept shares. Built on look4's primitives (make/l4_common.py). Glyph, brief 099. Mock only."""
import sys, os, math
sys.path.insert(0, '/Users/sky/Projects/suborned/briefs/085/glyph/book/look4/make')
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from l4_common import *  # Page, INK, SOFT, DEEP, PALE, TURQ, ORANGE, CREAM, BODY_, sign, track2, lay, numpy as np, cv2
from l4_light import lens_mask
from d_words import NEW, f, NAME
from PIL import ImageOps

OUT = R + 'briefs/099/glyph/'; SCR = R + 'out/scratch/glyph/debrief/frames/'; KIT = R + 'unity/Assets/Suborned/UI/Resources/Kit/'
PAGE = (0xf9, 0xf7, 0xee, 255); FOLD = (0xf0, 0xed, 0xe2, 255); HAIR = (0xd8, 0xd3, 0xc4, 255); MINT = (0xd6, 0xec, 0xe6, 255)
ORANGE_T = (0xe8, 0x62, 0x2c, 70); DEEP_T = (0x1f, 0x6c, 0x67, 60)
FACES = {'councillor': KIT + 'portraits/hero_councillor@2x.png', 'scout': KIT + 'portraits/hero_scout@2x.png', 'fixer': KIT + 'portraits/hero_fixer@2x.png',
         'tech': KIT + 'portraits/hero_tech@2x.png', 'enforcer': KIT + 'portraits/hero_enforcer@2x.png', 'waitress': R + 'briefs/063/cameos/inez_A.png',
         'driver': R + 'briefs/063/cameos/driver_A.png', 'guard': R + 'briefs/063/cameos/model_security.png', 'lookout': R + 'paid/099-debrief/codex/lookout.png'}
INSET = {'councillor': .035, 'scout': .07, 'fixer': .07, 'tech': .07, 'enforcer': .07, 'waitress': .08, 'driver': .08, 'guard': .06}
# the real frames: (source, shift so the subject sits left of the curve, where the subject is after the shift, the ground ring's radius)
FRAMES = {'win': (R + 'out/batch/shots/getaway-car-yaw210', -545, (430, 470), 92),
          'fail': (R + 'briefs/088/latch/job', 0, (300, 545), 120),
          'loud': (R + 'out/batch/shots/fight-loud-body', -520, (440, 505), 96)}


def frame(key):
    """A real 1920 frame with its own HUD layer lifted (the shot's .hud.png, dilated, inpainted), then shifted. Cached under out/scratch."""
    os.makedirs(SCR, exist_ok=True); p = SCR + key + '.png'
    if os.path.exists(p): return Image.open(p).convert('RGB')
    src, dx, _, _ = FRAMES[key]
    if key == 'win':  # today's end card sits over the road: lifted by d_lift.py (road and kerb copied along the road), not smeared
        import subprocess; subprocess.run([sys.executable, '-I', os.path.join(os.path.dirname(os.path.abspath(__file__)), 'd_lift.py'), src, SCR + 'win-lifted.png'], check=True)
        a = np.array(Image.open(SCR + 'win-lifted.png').convert('RGB'))[:, :, ::-1].copy(); im = Image.fromarray(np.roll(a, dx, axis=1)[:, :, ::-1]); im.save(p); return im
    a = np.array(Image.open(src + '.png').convert('RGB'))[:, :, ::-1].copy()
    if os.path.exists(src + '.hud.png'):
        m = (np.array(Image.open(src + '.hud.png').convert('RGBA'))[:, :, 3] > 12).astype(np.uint8) * 255
        if key == 'loud':  # the seal card lies on plain plaza: the clean tiles one card-height below fill it; only its rim is inpainted
            x0, y0, x1, y1 = 752, 66, 1170, 169; a[y0:y1, x0:x1] = a[y0 + 98:y1 + 98, x0:x1]; m[y0 + 6:y1 - 6, x0 + 6:x1 - 6] = 0
        n, lab, st, _ = cv2.connectedComponentsWithStats(m)  # cards get a wide margin (their glow), small marks a tight one (heads under bubbles survive)
        big = np.isin(lab, [i for i in range(1, n) if st[i, cv2.CC_STAT_AREA] >= 6000]).astype(np.uint8) * 255
        m = cv2.dilate(big, np.ones((31, 31), np.uint8)) | cv2.dilate(m & ~big, np.ones((7, 7), np.uint8))
        a = cv2.inpaint(a, m, 9, cv2.INPAINT_TELEA)
    if key == 'fail': a[:, 860:] = PAGE[2::-1]  # job.png is the built book's own capture: its old page must not ghost through the new one
    im = Image.fromarray(np.roll(a, dx, axis=1)[:, :, ::-1]); im.save(p); return im


def face(P, who, x, y, d, ring=TURQ, rw=5, fill=PALE, gray=False):
    """A painted face in a disc: the kit's hero paintings for the cell and Popovic, the brief-063 paintings, the lookout from Codex."""
    im = Image.open(FACES[who]).convert('RGB')
    if who == 'lookout': im = im.crop((150, 40, 1150, 1040))
    else: i = round(im.width * INSET.get(who, .05)); im = im.crop((i, i, im.width - i, im.height - i))
    if gray: im = ImageOps.grayscale(im).convert('RGB')
    n = round(d * P.s); k = 3; m = Image.new('L', (n * k, n * k), 0); ImageDraw.Draw(m).ellipse((0, 0, n * k - 1, n * k - 1), fill=255)
    s = im.resize((n, n), Image.LANCZOS).convert('RGBA'); s.putalpha(m.resize((n, n), Image.LANCZOS))
    P.disc(x, y, d, fill=fill, ring=None); lay_ = Image.new('RGBA', P.img.size, (0, 0, 0, 0)); lay_.paste(s, (round(x * P.s), round(y * P.s))); P.img.alpha_composite(lay_)
    if ring: P.disc(x, y, d, fill=None, ring=ring, rw=rw)


def light_page(P, key, subject, d=330, line=True):
    """The street, its halo, the cream page with one big curve; the subject's face over the edge, joined to where they are."""
    lay(P, frame(key)); m = lens_mask(P)
    halo = Image.new('RGBA', P.img.size, (0x60, 0xe0, 0xea, 0)); halo.putalpha(m.filter(ImageFilter.GaussianBlur(22)).point(lambda v: v * 170 // 255)); P.img.alpha_composite(halo)
    pg = Image.new('RGBA', P.img.size, PAGE[:3] + (0,)); pg.putalpha(m.point(lambda v: v * 247 // 255)); P.img.alpha_composite(pg)
    _, _, (ex, ey), rr = FRAMES[key]
    if line and subject:
        P.line([(ex + 30, ey - 70), (668, 262)], col=TURQ, lw=3); P.ground_ring(ex, ey, rr, lw=3.5)
    if subject: face(P, subject, 640, 70, d)


def caps(P, x, y, t, size=20, col=DEEP, gap=5.5):
    cx = x
    for ch in t.upper():
        P.text(cx, y, ch, size, col=col, face='Jost-SemiBold'); cx += P.tw(ch, size, 'Jost-SemiBold') + gap
    P.words_add(t); return cx


def fit(P, t, w, sizes, face_='Jost-Bold'):
    for s in sizes:
        if P.tw(t, s, face_) <= w: return s
    return sizes[-1]


def tabs(P, cur='job', ys=(200, 375, 550, 725)):
    """The book's round tabs, as built (Latch, brief 088): Cell, People, Job, Journal. At the end the Job tab holds the debrief."""
    for (key, ic, w), y in zip([('cell', 'units', 'Cell'), ('people', 'lovers', 'People'), ('job', 'take', 'Job'), ('journal', 'papers', 'Journal')], ys):
        on = key == cur; dd = 108 if on else 84; x = 1830
        P.disc(x - dd / 2, y - dd / 2, dd, fill=ORANGE if on else BODY_, ring=None); P.icon(ic, x - dd * .25, y - dd * .36, dd * .5, col=CREAM)
        P.text(x, y + dd * .24, w, 22 if on else 16, col=CREAM, face='Jost-Bold', anchor='mm')


def pill(P, x, y, t, w=None, h=62, fill=ORANGE, size=34, chev=True):
    w = w or P.tw(t, size) + (78 if chev else 56); P.rrect(x, y, w, h, h / 2, fill=fill); P.put(x + 30, y + h / 2 - 1, t, size, col=CREAM, face='Jost-Bold')
    if chev: P.text(x + w - 32, y + h / 2 - 2, '›', size, col=CREAM, face='Jost-Bold', anchor='mm')
    return w


def ghost(P, x, y, icon, t, h=52, size=24, col=DEEP):
    """A secondary way: an outlined plate in deep teal, icon and word (clickable, never key-only)."""
    w = 26 + 30 + 10 + P.tw(t, size, 'Jost-SemiBold') + 24; P.rrect(x, y, w, h, h / 2, fill=PAGE, line=col, lw=2.2)
    P.icon(icon, x + 22, y + h / 2 - 15, 30, col=col); P.put(x + 62, y + h / 2 - 1, t, size, col=col, face='Jost-SemiBold'); return w


def ways_on(P, seed, x1=1875, y=1000):
    """Watch the replay · Back to the start · Try again (new start, or the same start: the run's seed, brief 080)."""
    w = P.tw(NEW['way.again'], 34) + 78; x0 = x1 - w; pill(P, x0, y, NEW['way.again'])
    seg_w = P.tw(NEW['way.new'], 17, 'Jost-SemiBold') + P.tw(NEW['way.same'] + f' · {seed}', 17, 'Jost-SemiBold') + 64; sx = x1 - seg_w; sy = y - 40
    P.rrect(sx, sy, seg_w, 30, 15, fill=PAGE, line=HAIR, lw=1.5); a = P.tw(NEW['way.new'], 17, 'Jost-SemiBold') + 32
    P.rrect(sx + 2, sy + 2, a, 26, 13, fill=DEEP); P.put(sx + 18, sy + 15, NEW['way.new'], 17, col=CREAM, face='Jost-SemiBold')
    P.put(sx + a + 14, sy + 15, NEW['way.same'] + f' · {seed}', 17, col=SOFT, face='Jost-SemiBold')
    gx = x0 - 18; w2 = P.tw(NEW['way.start'], 24, 'Jost-SemiBold') + 80; gx -= w2; ghost(P, gx, y + 5, 'papers', NEW['way.start'])
    w3 = P.tw(NEW['way.replay'], 24, 'Jost-SemiBold') + 80; gx -= w3 + 12; ghost(P, gx, y + 5, 'loop', NEW['way.replay'])
    return gx


def check(P, x, y, ok, d=30, hot=False):
    """A start-page condition, answered: a filled tick (true), an orange cross (the one that lost it), a hollow ring (never came true)."""
    if ok: P.disc(x, y, d, fill=DEEP, ring=None); P.icon('check', x + d * .2, y + d * .2, d * .6, col=CREAM)
    elif hot: P.disc(x, y, d, fill=ORANGE, ring=None); P.icon('close_x', x + d * .22, y + d * .22, d * .56, col=CREAM)
    else: P.disc(x, y, d, fill=PAGE, ring=SOFT, rw=2)


def standing_bar(P, x, y, w, st, lo=-5, hi=5):
    """Hero <-> Menace on one track: the bands from the rulebook (Menace at -3, Hero at +3), where the run began (ring), ended (disc) and
    where the next mission starts (word of mouth, between missions: a small arrow on)."""
    X = lambda v: x + (max(lo, min(hi, v)) - lo) / (hi - lo) * w; h = 14
    P.rrect(x, y, w, h, h / 2, fill=(0xe9, 0xe5, 0xd8, 255))
    P.rrect(x, y, X(-3) - x + 4, h, h / 2, fill=(0xf6, 0xc9, 0xb2, 255)); P.rrect(X(3) - 4, y, x + w - X(3) + 4, h, h / 2, fill=(0xb9, 0xdd, 0xd4, 255))
    for v in range(lo, hi + 1): P.line([(X(v), y + h + 3), (X(v), y + h + 8)], col=HAIR, lw=1.5)
    P.put(x, y + h + 24, NEW['band.menace'], 17, col=SOFT, face='Jost-SemiBold'); P.text(x + w, y + h + 24, NEW['band.hero'], 17, col=SOFT, face='Jost-SemiBold', anchor='rm')
    P.text((X(-3) + X(3)) / 2, y + h + 24, NEW['band.neutral'], 17, col=SOFT, face='Jost-Medium', anchor='mm')
    s, e, n = st['start'], st['end'], st['next']; col = ORANGE if e <= -3 else DEEP
    P.disc(X(s) - 11, y + h / 2 - 11, 22, fill=PAGE, ring=SOFT, rw=2.5)
    if n != e: P.line([(X(e), y + h / 2), (X(n), y + h / 2)], col=col, lw=3); P.disc(X(n) - 8, y + h / 2 - 8, 16, fill=PAGE, ring=col, rw=3)
    P.disc(X(e) - 14, y + h / 2 - 14, 28, fill=col, ring=CREAM, rw=2.5)
    return X


def band_of(v): return 'hero' if v >= 3 else 'menace' if v <= -3 else 'neutral'
def signed(v): return f'+{v}' if v > 0 else str(v)
def mood(v): return 'mood_friendly' if v >= 3 else 'mood_scared' if v <= -3 else 'mood_uneasy'


def dim_page(P, keep=(), alpha=150):
    """The rest of the page steps back while one explainer is open (a fold, as the book's)."""
    S_ = P.s; m = lens_mask(P).point(lambda v: v * alpha // 255); hole = Image.new('L', m.size, 0); g = ImageDraw.Draw(hole)
    for x0, y0, x1, y1 in keep: g.rounded_rectangle((x0 * S_, y0 * S_, x1 * S_, y1 * S_), 18 * S_, fill=255)
    disc = Image.new('L', m.size, 0); ImageDraw.Draw(disc).ellipse((636 * S_, 66 * S_, 974 * S_, 404 * S_), fill=alpha)  # the face steps back whole
    m = ImageChops.subtract(ImageChops.lighter(m, disc), hole); lay_ = Image.new('RGBA', P.img.size, PAGE[:3] + (0,)); lay_.putalpha(m); P.img.alpha_composite(lay_)


def panel(P, x, y, w, h, tail=None, fill=FOLD):
    """An explainer's fold: a slightly darker cream sheet with a hairline, and a tail toward what opened it."""
    P.rrect(x + 3, y + 5, w, h, 22, fill=(0, 0, 0, 22)); P.rrect(x, y, w, h, 22, fill=fill, line=HAIR, lw=1.5)
    if tail:
        tx, up = tail; g = ImageDraw.Draw(P.img); s = P.s; ty = y if up else y + h
        g.polygon([((tx - 16) * s, (ty + (1 if up else -1)) * s), ((tx + 16) * s, (ty + (1 if up else -1)) * s), (tx * s, (ty - 16 if up else ty + 16) * s)], fill=fill)
        P.line([(tx - 16, ty), (tx, ty - 16 if up else ty + 16), (tx + 16, ty)], col=HAIR, lw=1.5)


def wrap_lines(P, t, w, size, face_='Jost-Medium'):
    out, line = [], ''
    for word in t.split():
        trial = (line + ' ' + word).strip()
        if P.tw(trial, size, face_) > w and line: out.append(line); line = word
        else: line = trial
    return out + [line] if line else out


def put_wrap(P, x, y, t, w, size, col=SOFT, face_='Jost-Medium', lead=1.25, maxl=3):
    ls = wrap_lines(P, t, w, size, face_)[:maxl]
    for i, l in enumerate(ls): P.put(x, y + i * size * lead, l, size, col=col, face=face_)
    return y + len(ls) * size * lead


def save(P, name):
    os.makedirs(OUT, exist_ok=True); im = P.img.resize((1920, 1080), Image.LANCZOS).convert('RGB'); im.save(OUT + name + '.png'); return im
