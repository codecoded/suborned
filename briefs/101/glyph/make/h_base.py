"""The HUD sweep's shared base (brief 101): the three real frames with today's HUD lifted off, the painted faces, the kit's icons and
type, and the real data for the six moments. Built on the book's mock primitives (briefs/085 page.py and kit.py). Glyph. Mock only."""
import sys, os, math
R = '/Users/sky/Projects/suborned/'
sys.path.insert(0, R + 'briefs/085/glyph/book/make'); sys.path.insert(0, R + 'briefs/085/glyph/people-panel/make')
from page import *  # Page (put, head, wrap), kit's Layer: nine, icon, disc, rrect, line, ground_ring; INK, CREAM, DIM, TURQ, BODY, SOFT
from PIL import Image, ImageDraw, ImageFilter, ImageChops, ImageOps
import numpy as np, cv2

OUT = R + 'briefs/101/glyph/'; SCR = R + 'out/scratch/glyph/101/'; KIT = R + 'unity/Assets/Suborned/UI/Resources/Kit/'
PAGE = (0xf9, 0xf7, 0xee, 255); HAIR = (0xd8, 0xd3, 0xc4, 255); FOLD = (0xf0, 0xed, 0xe2, 255)
ORANGE = (0xe8, 0x62, 0x2c, 255); DEEP = (0x1f, 0x6c, 0x67, 255); PALE = (0x9b, 0xcf, 0xc2, 255); AMBER = (0xf0, 0xa0, 0x20, 255)
BODY_ = (0x0c, 0x34, 0x34, 255); SHADOW = (0x0b, 0x16, 0x15, 70); VIOLET = (0x8a, 0x5c, 0xf0, 255)
NAMES = {'scout': 'Larthi', 'fixer': 'Hasti', 'tech': 'Sethre', 'enforcer': 'Arruns', 'councillor': 'Popovic'}
JOBS = {'scout': 'Scout', 'fixer': 'Fixer', 'tech': 'Tech', 'enforcer': 'Enforcer'}
FACES = {'councillor': KIT + 'portraits/hero_councillor@2x.png', 'scout': KIT + 'portraits/hero_scout@2x.png', 'fixer': KIT + 'portraits/hero_fixer@2x.png',
         'tech': KIT + 'portraits/hero_tech@2x.png', 'enforcer': KIT + 'portraits/hero_enforcer@2x.png', 'waitress': R + 'briefs/063/cameos/inez_A.png',
         'driver': R + 'briefs/063/cameos/driver_A.png', 'guard': R + 'briefs/063/cameos/model_security.png'}
INSET = {'councillor': .035, 'scout': .07, 'fixer': .07, 'tech': .07, 'enforcer': .07, 'waitress': .08, 'driver': .08, 'guard': .06}

# The three real frames: the capture, today's HUD layer (.hud.png) lifted by inpainting; where a card covered open ground, the same view a
# moment earlier (the brief-100 clip, frame 0 at 2:32.2, same camera, no card yet) fills it instead of a smear.
FRAMES = {'loud': (R + 'briefs/100/latch/shots/why-loud-police', R + 'briefs/100/latch/shots/why-loud-clip/f0000.jpg', [(744, 60, 1176, 176), (1514, 926, 1912, 1024)]),
          'quiet': (R + 'briefs/100/latch/shots/why-quiet-radio', None, []),
          'aim': (R + 'out/batch/shots/fight-loud-checkpoint', None, [])}


def frame(key):
    os.makedirs(SCR + 'frames', exist_ok=True); p = SCR + 'frames/' + key + '.png'
    if os.path.exists(p): return Image.open(p).convert('RGB')
    src, patch, boxes = FRAMES[key]
    a = np.array(Image.open(src + '.png').convert('RGB'))[:, :, ::-1].copy()
    m = (np.array(Image.open(src + '.hud.png').convert('RGBA'))[:, :, 3] > 12).astype(np.uint8) * 255
    if patch:
        b = np.array(Image.open(patch).convert('RGB'))[:, :, ::-1]
        for x0, y0, x1, y1 in boxes:
            soft = np.zeros(m.shape, np.float32); soft[y0 + 8:y1 - 8, x0 + 8:x1 - 8] = 1; soft = cv2.GaussianBlur(soft, (0, 0), 4)[:, :, None]
            a = (a * (1 - soft) + b * soft).astype(np.uint8); m[y0 + 14:y1 - 14, x0 + 14:x1 - 14] = 0
    for box, p0, dv, L, rs in KERB.get(key, []): a = kerb_fill(a, box, p0, dv, L, rs); m[box[1]:box[3], box[0]:box[2]] = 0
    avoid = np.zeros(m.shape, np.uint8)
    for x0, y0, x1, y1 in AVOID.get(key, []): avoid[y0:y1, x0:x1] = 255  # props a patch must not be copied from (a crossing signal twice reads as a bug)
    m = cv2.dilate(m, np.ones((9, 9), np.uint8)); a = patch_fill(a, m, avoid=avoid, near=NEAR.get(key, ()))
    for (x0, y0, x1, y1), (dx, dy) in COPY.get(key, []):  # plain plaza laid over the scope's corner, feathered on its open edges
        f = np.zeros(a.shape[:2], np.float32); f[y0:y1, x0:x1] = 1; f = cv2.GaussianBlur(f, (0, 0), 3)
        src = np.roll(np.roll(a, -dy, axis=0), -dx, axis=1); f = f[:, :, None]; a = (a * (1 - f) + src * f).astype(np.uint8)
    im = Image.fromarray(a[:, :, ::-1]); im.save(p); return im


KERB = {k: [((1396, 0, 1914, 116), (1590, 60), (0.723, 0.690), 170, 122)] for k in ('loud', 'quiet')}  # the clock bar sat over a kerb


def kerb_fill(a, box, p0, d, L, road_shift):
    """Under a piece lying across a straight kerb: the road side from the road below it, the pavement side from further along the
    kerb (so the edge and its furniture carry on), mirrored at the screen edge; feathered into the frame on the open sides."""
    H, W = a.shape[:2]; x0, y0, x1, y1 = box; ys, xs = np.mgrid[y0:y1, x0:x1]; n = (xs - p0[0]) * d[1] - (ys - p0[1]) * d[0]
    sx = np.where(n < -6, xs, xs + round(L * d[0])); sy = np.where(n < -6, ys + road_shift, ys + round(L * d[1]))
    sx = np.where(sx > W - 1, 2 * (W - 1) - sx, sx); out = a.copy(); fill = a[np.clip(sy, 0, H - 1), np.clip(sx, 0, W - 1)]
    f = np.ones((y1 - y0, x1 - x0), np.float32); f[-10:, :] *= np.linspace(1, 0, 10)[:, None]; f[:, :10] *= np.linspace(0, 1, 10)[None, :]
    out[y0:y1, x0:x1] = (a[y0:y1, x0:x1] * (1 - f[:, :, None]) + fill * f[:, :, None]).astype(np.uint8); return out


AVOID = {'loud': [(466, 860, 548, 1034), (600, 1010, 700, 1080)], 'quiet': [(466, 860, 548, 1034), (600, 1010, 700, 1080)]}
COPY = {'loud': [((0, 872, 218, 1080), (240, -14))], 'quiet': [((0, 872, 218, 1080), (240, -14))]}
NEAR = {'loud': [((0, 880, 260, 1080), (222, -16))], 'quiet': [((0, 880, 260, 1080), (222, -16))]}  # where a piece must take plain plaza: search only near this shift


def patch_fill(a, hole, D=380, pad=14, avoid=None, near=()):
    """Fill each lifted piece with the nearby street that best continues its surroundings (one patch per piece, found by template match on
    the ring around it, never from another hole), feathered at the seam; the last specks are inpainted. A smear only where nothing fits."""
    a = a.copy(); H, W = hole.shape; n, lab, st, _ = cv2.connectedComponentsWithStats(hole); left = hole.copy(); k5 = np.ones((5, 5), np.uint8)
    order = sorted(range(1, n), key=lambda i: -st[i, cv2.CC_STAT_AREA])
    for i in order:
        x, y, w, h, area = st[i]; grown = cv2.dilate(left, k5)
        if area < 40: continue
        tx0, ty0, tx1, ty1 = max(0, x - pad), max(0, y - pad), min(W, x + w + pad), min(H, y + h + pad)
        T = a[ty0:ty1, tx0:tx1]; M = (grown[ty0:ty1, tx0:tx1] == 0).astype(np.uint8)
        if M.sum() < 50: continue
        sx0, sy0, sx1, sy1 = max(0, tx0 - D), max(0, ty0 - D), min(W, tx1 + D), min(H, ty1 + D)
        for (rx0, ry0, rx1, ry1), (ndx, ndy) in near:
            if rx0 <= x + w / 2 <= rx1 and ry0 <= y + h / 2 <= ry1: sx0, sy0, sx1, sy1 = max(0, tx0 + ndx - 30), max(0, ty0 + ndy - 30), min(W, tx1 + ndx + 30), min(H, ty1 + ndy + 30)
        S = a[sy0:sy1, sx0:sx1]
        if S.shape[0] < T.shape[0] or S.shape[1] < T.shape[1]: continue
        res = cv2.matchTemplate(S.astype(np.float32), T.astype(np.float32), cv2.TM_SQDIFF, mask=np.dstack([M] * 3).astype(np.float32))
        nogo = grown if avoid is None else (grown | avoid); bad = cv2.integral((nogo[sy0:sy1, sx0:sx1] > 0).astype(np.uint8)); th, tw = T.shape[:2]
        over = bad[th:th + res.shape[0], tw:tw + res.shape[1]] - bad[0:res.shape[0], tw:tw + res.shape[1]] - bad[th:th + res.shape[0], 0:res.shape[1]] + bad[0:res.shape[0], 0:res.shape[1]]
        res = np.where(over > 0, np.inf, res); res = np.nan_to_num(res, nan=np.inf)
        if not np.isfinite(res).any(): continue
        oy, ox = np.unravel_index(np.argmin(res), res.shape); src = a[sy0 + oy:sy0 + oy + th, sx0 + ox:sx0 + ox + tw]
        f = cv2.GaussianBlur((grown[ty0:ty1, tx0:tx1] > 0).astype(np.float32), (0, 0), 3); f = np.clip(f * 1.6, 0, 1)[:, :, None]
        a[ty0:ty1, tx0:tx1] = (T * (1 - f) + src * f).astype(np.uint8); left[lab == i] = 0
    return cv2.inpaint(a, left, 5, cv2.INPAINT_TELEA) if left.any() else a


def scope_map(key, ramp, lo=14, span=200):
    """The real plot scope's map (inside its rim, from the capture), reprinted through a colour ramp low -> high luminance."""
    a = np.array(Image.open(FRAMES[key][0] + '.png').convert('RGB').crop((24, 944, 216, 1058))).astype(float)
    L = (a @ [0.3, 0.59, 0.11]); t = np.clip((L - lo) / span, 0, 1); ramp = [np.array(c[:3], float) for c in ramp]; n = len(ramp) - 1
    i = np.minimum((t * n).astype(int), n - 1); f = (t * n - i)[:, :, None]; lo = np.stack(ramp)[i]; hi = np.stack(ramp)[i + 1]
    return Image.fromarray((lo * (1 - f) + hi * f).astype(np.uint8))


MODE = {'clear': False}  # clear: draw the HUD alone on a transparent layer and measure it, as the budget tool reads a capture's .hud.png


def base(key):
    P = Page(2)
    P.img = Image.new('RGBA', (3840, 2160), (0, 0, 0, 0)) if MODE['clear'] else frame(key).resize((3840, 2160), Image.LANCZOS).convert('RGBA'); return P


def save(P, name):
    if MODE['clear']: return None
    os.makedirs(OUT, exist_ok=True); im = P.img.resize((1920, 1080), Image.LANCZOS).convert('RGB'); im.save(OUT + name + '.png'); return im


def hud_cover(P0, P):
    """The share of the screen the HUD covers. Clear mode: alpha >= 24 on the HUD layer alone (tools/hud-budget HUD_ALPHA, its 'hud' mask);
    otherwise a rough check: pixels that differ from the frame without the HUD."""
    if MODE['clear']: return float((np.array(P.img.resize((1920, 1080), Image.LANCZOS))[:, :, 3] >= 24).mean())
    a = np.array(P0.img.resize((960, 540)).convert('RGB')).astype(int); b = np.array(P.img.resize((960, 540)).convert('RGB')).astype(int)
    return float((np.abs(a - b).sum(axis=2) > 24).mean())


def face(P, who, x, y, d, ring=TURQ, rw=3, fill=PALE, gray=False):
    """A painted face in a disc: the kit's hero paintings (the cell, Popovic) and brief 063's (Inez, the driver, the guard)."""
    im = Image.open(FACES[who]).convert('RGB'); i = round(im.width * INSET.get(who, .05)); im = im.crop((i, i, im.width - i, im.height - i))
    if gray: im = ImageOps.grayscale(im).convert('RGB')
    n = round(d * P.s); k = 3; m = Image.new('L', (n * k, n * k), 0); ImageDraw.Draw(m).ellipse((0, 0, n * k - 1, n * k - 1), fill=255)
    s = im.resize((n, n), Image.LANCZOS).convert('RGBA'); s.putalpha(m.resize((n, n), Image.LANCZOS))
    P.disc(x, y, d, fill=fill, ring=None); lay_ = Image.new('RGBA', P.img.size, (0, 0, 0, 0)); lay_.paste(s, (round(x * P.s), round(y * P.s))); P.img.alpha_composite(lay_)
    if ring: P.disc(x, y, d, fill=None, ring=ring, rw=rw)


def badge(P, name, x, y, d):
    """A kit badge (signal_police, signal_guard...) at size d."""
    P.art('badges/' + name, x, y, d)


def shadow(P, x, y, w, h, r, dy=3, a=60, blur=5):
    """A soft drop shadow under a shape, so a light piece holds on the pale paving."""
    s = P.s; lay_ = Image.new('RGBA', P.img.size, (0, 0, 0, 0)); g = ImageDraw.Draw(lay_)
    g.rounded_rectangle((x * s, (y + dy) * s, (x + w) * s, (y + h + dy) * s), r * s, fill=(0x0b, 0x16, 0x15, a))
    P.img.alpha_composite(lay_.filter(ImageFilter.GaussianBlur(blur * s)))


def disc_shadow(P, x, y, d, dy=3, a=70, blur=4):
    s = P.s; lay_ = Image.new('RGBA', P.img.size, (0, 0, 0, 0)); ImageDraw.Draw(lay_).ellipse((x * s, (y + dy) * s, (x + d) * s, (y + d + dy) * s), fill=(0x0b, 0x16, 0x15, a))
    P.img.alpha_composite(lay_.filter(ImageFilter.GaussianBlur(blur * s)))


def arc(P, cx, cy, r, a0, a1, col, lw):
    """An arc of a ring (degrees, 0 at the top, clockwise), supersampled."""
    s = P.s; k = 3; n = round((r + lw) * 2 * s * k) + 8; big = Image.new('RGBA', (n, n), (0, 0, 0, 0)); g = ImageDraw.Draw(big); o = n / 2
    rr = r * s * k; g.arc((o - rr, o - rr, o + rr, o + rr), a0 - 90, a1 - 90, fill=col, width=round(lw * s * k))
    sm = big.resize((n // k, n // k), Image.LANCZOS); P.img.alpha_composite(sm, (round(cx * s - sm.width / 2), round(cy * s - sm.height / 2)))


def blob(P, mask_fn, fill, halo=None, halo_a=150, halo_blur=14, sh=None):
    """A free shape from a mask drawn at 1/2 scale (mask_fn(draw, s) with s the pixel scale), with an optional halo on its edge and a shadow."""
    W, H = P.img.size; m = Image.new('L', (W // 2, H // 2), 0); mask_fn(ImageDraw.Draw(m), P.s / 2); m = m.resize((W, H), Image.BICUBIC)
    if sh:
        dy, a, bl = sh; t = Image.new('RGBA', (W, H), (0x0b, 0x16, 0x15, 0)); t.putalpha(ImageChops.offset(m, 0, round(dy * P.s)).filter(ImageFilter.GaussianBlur(bl * P.s)).point(lambda v: v * a // 255)); P.img.alpha_composite(t)
    if halo:
        t = Image.new('RGBA', (W, H), halo[:3] + (0,)); t.putalpha(m.filter(ImageFilter.GaussianBlur(halo_blur * P.s / 2)).point(lambda v: min(255, v * halo_a // 255))); P.img.alpha_composite(t)
    t = Image.new('RGBA', (W, H), fill[:3] + (0,)); t.putalpha(m.point(lambda v: v * fill[3] // 255)); P.img.alpha_composite(t)
    return m


def caps(P, x, y, t, size=14, col=SOFT, gap=2.4, anchor='l'):
    w = sum(P.tw(ch, size, 'Jost-SemiBold') + gap for ch in t.upper()) - gap; cx = x - (w if anchor == 'r' else w / 2 if anchor == 'm' else 0)
    for ch in t.upper(): P.text(cx, y, ch, size, col=col, face='Jost-SemiBold'); cx += P.tw(ch, size, 'Jost-SemiBold') + gap
    P.words_add(t); return w


def crop(im, box, name, z=2):
    """A moment's crop at 2x, for the board and the 2x check."""
    c = im.crop(box); c = c.resize((c.width * z, c.height * z), Image.LANCZOS); os.makedirs(OUT + 'crops', exist_ok=True); c.save(OUT + 'crops/' + name + '.png'); return c


# ---- The real data, by moment. Sources in notes.md; times are the runs' own (10 ticks a second). ----
CELL = ['scout', 'fixer', 'tech', 'enforcer']
LOUD = {  # tools/game/pins/loud.seed3.json at 2:34 (tick 1545): the Enforcer has shot the guard at the Office door; Latch's why-loud-police capture
    'clock': '2:34', 'next': ('car', 'Rivals in', '2:35'), 'cash': '$100,000', 'trail': 6, 'alert': 2,
    'seal': {'left': '3:07', 'at': '5:41', 'cause': ('fire', 'shots fired', 'at the Office door'), 'state': 'empty'},
    'police': {'title': 'Police coming', 'short': 'shots heard', 'where': 'at the Office door', 'when': '2:33', 'in': '0:42', 'at': '3:16', 'of': (1, 2)},
    'busy': {'fixer': ('run', '1 s', .8), 'enforcer': ('fire', '3 s', .4)}, 'hp': {'scout': (5, 5), 'fixer': (5, 5), 'tech': (5, 5), 'enforcer': (8, 8)}, 'sel': 'enforcer',
    'speech': ('tech', 'It took the orders. It always does.', (1136, 640)),
    'meter': [('4:36', 'More police', 'the search goes on'), ('3:45', 'Police coming in', 'shots earlier'), ('2:44', 'Police coming', 'a body was found at the Office door'),
              ('2:33', 'Police coming', 'shots heard at the Office door'), ('2:33', 'Alert 2', 'shots fired at the Office door')]}
QUIET = {  # route-g-radio (Latch, brief 100) at 3:28.6 (tick 2086): another guard radios in the guard lying at the Office door; lands 3:32
    'clock': '3:28', 'next': ('nextevent', 'Chime', '5:10'), 'cash': '$100,000', 'trail': 1, 'alert': 0,
    'report': {'way': 'radio', 'who': 'guard', 'left': 4, 'of': 5, 'plate': 'Radioing it in', 'fix': 'Stop them before it lands', 'why': 'another guard spotted the guard lying',
               'where': 'at the Office door', 'alert': 1, 'then': 'Police come to hold the door', 'head': (867, 452), 'feet': (867, 522)},
    'feed': {'title': 'Our car boxed in', 'line': 'their car is across our way out', 'when': '2:46', 'of': (1, 2)},
    'busy': {}, 'hp': {'scout': (5, 5), 'fixer': (5, 5), 'tech': (5, 5), 'enforcer': (8, 8)}, 'sel': 'tech',
    'speech': ('tech', "The tape's run out.", (1136, 640))}
AIM = {  # loud.seed3 at 2:31 (tick 1510): the Enforcer fires on the guard at 5 m, 750 per mille; hits at 2:33 and 2:35, down 4 s after the order.
    # The street is the batch's fight-loud-checkpoint (the same door and distance); its own clock was 1:43.
    'clock': '2:31', 'next': ('car', 'Rivals in', '2:35'), 'cash': '$100,000', 'trail': 2, 'alert': 0,
    'aim': {'who': 'enforcer', 'at': 'guard', 'pct': 75, 'down': '4 s', 'm': '5 m', 'alert': 2, 'police': '0:45', 'seal': '5:41', 'trail': '+6',
            'head': (912, 262), 'feet': (912, 332), 'from': (960, 492)},
    'busy': {'scout': ('run', '4 s', .1), 'fixer': ('run', '4 s', .1)}, 'hp': {'scout': (5, 5), 'fixer': (5, 5), 'tech': (5, 5), 'enforcer': (8, 8)}, 'sel': 'enforcer'}
