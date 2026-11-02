"""Shared for round four's three layouts: the real close shots, the same page data as look3 (content.py), no hand placing. Glyph, brief 085."""
import sys, math
sys.path.insert(0, '/Users/sky/Projects/suborned/briefs/085/glyph/book/look3/make')
from slots import *
import content as C
from content import PAGES, SECTIONS, WITH_CASH, INEZ, INEZ12, GUARD, AGENT, JOB, CASH, LIST8, LIST200
OUT4 = R + 'briefs/085/glyph/book/look4/'; LAT = R + 'briefs/085/latch/'; SC = R + 'out/scratch/glyph/look4/'
JOB['track'] = [('car', 'Rivals in', '2:35', 1), ('nextevent', 'Chime', '4:30', 0), ('turn_person', 'Last chance', '4:40', 0), ('s_moving', 'Vale out', '4:50', 0), ('car', 'Vale at car', '5:10', 0)]  # text.json briefing.timeline[].short


def _bgr(p): return np.array(Image.open(p).convert('RGB'))[:, :, ::-1].copy()
def _in(a, boxes, r=7):
    m = np.zeros(a.shape[:2], np.uint8)
    for x0, y0, x1, y1 in boxes: m[y0:y1, x0:x1] = 255
    return cv2.inpaint(a, m, r, cv2.INPAINT_TELEA)
def _rgb(a): return Image.fromarray(a[:, :, ::-1])


def vale_shot():
    """Vale has no close shot on disk. His model render (brief 063, on grey) cut out and stood before the real Office frame. Flagged in the notes."""
    f = _in(_bgr(R + 'out/batch/shots-prev1/office-front-yaw210.png'), [(10, 10, 110, 480), (1420, 8, 1910, 110), (10, 930, 230, 1070), (880, 1020, 1040, 1070), (1860, 1020, 1910, 1070)])
    f = cv2.resize(f, (0, 0), fx=1.2, fy=1.2)[100:1180, 300:2220].copy(); bg = Image.fromarray(cv2.GaussianBlur(f, (0, 0), 5)[:, :, ::-1]).convert('RGBA')
    v = Image.open(R + 'briefs/063/cameos/vale_C_card_model_720x592.png').convert('RGB'); a = np.array(v)[:, :, ::-1].copy()
    m = np.full(a.shape[:2], cv2.GC_PR_FGD, np.uint8); m[:40, :] = 0; m[:300, :60] = 0; m[:300, -60:] = 0; m[300:, 300:420] = 1
    b, g = np.zeros((1, 65)), np.zeros((1, 65)); cv2.grabCut(a, m, None, b, g, 5, cv2.GC_INIT_WITH_MASK); al = cv2.GaussianBlur(np.where((m == 1) | (m == 3), 255, 0).astype(np.uint8), (0, 0), 1.2)
    o = Image.fromarray(np.dstack([a[:, :, ::-1], al])).resize((1100, 904), Image.LANCZOS); bg.alpha_composite(o, (410, 176)); return bg.convert('RGB')


def shots():
    """Clean 1920 frames, real as captured, with subtitles and Skip painted out."""
    s = {}
    s['waitress'] = _rgb(_in(_bgr(LAT + 'lookc/r085-hold-after.png'), [(1800, 1000, 1900, 1050)]))
    s['scout'] = _rgb(_in(_bgr(LAT + 'listen/r085-scout-alone.png'), [(40, 840, 1240, 1060), (1800, 990, 1910, 1060)]))
    s['guard'] = _rgb(_in(_bgr(LAT + 'r085-call-guard.png'), [(740, 130, 1180, 330), (1680, 0, 1920, 62), (1840, 980, 1920, 1030), (800, 1030, 1050, 1080)]))
    s['councillor'] = vale_shot()
    return s


SHOT = None
def shot(k):
    global SHOT
    if SHOT is None: SHOT = shots()
    return SHOT[k]
ANCHOR = {'waitress': (995, 390), 'scout': (1020, 330), 'guard': (960, 300), 'councillor': (960, 240)}  # the head in the frame


def framed(k, cx, top, zoom, W=1920, H=1080):
    """The close shot scaled so the head lands at (cx, top), the rest filled from the frame's own edge."""
    im = shot(k); ax, ay = ANCHOR[k]; z = zoom; nw, nh = round(1920 * z), round(1080 * z); big = im.resize((nw, nh), Image.LANCZOS)
    x0, y0 = round(ax * z - cx), round(ay * z - top); a = np.array(big)
    xs = np.clip(np.arange(W) + x0, 0, nw - 1); ys = np.clip(np.arange(H) + y0, 0, nh - 1); return Image.fromarray(a[ys][:, xs])


def _clean(a):
    """The play HUD that lands in view, painted out; the figures are untouched."""
    return _in(a, [(1420, 8, 1910, 110), (880, 1016, 1040, 1072), (1860, 1020, 1910, 1072), (10, 10, 280, 480), (10, 930, 230, 1070)])


def street_real(dx=0, cell=False):
    """The real play frame in its own colours: people-panel's frame with its open ring lifted out (a real 0:59). Without `cell`, the Scout and Fixer are painted out."""
    from base import frame_cafe_stopped, frame_learned
    if cell: a = _in(np.array(frame_learned())[:, :, ::-1].copy(), [(885, 376, 1034, 396), (952, 440, 1150, 462)])
    else: a = _in(np.array(frame_cafe_stopped())[:, :, ::-1].copy(), [(940, 405, 1050, 460)])
    return Image.fromarray(np.roll(_clean(a), dx, axis=1)[:, :, ::-1])


def door_real(dx=0):
    from base import frame_door_stopped
    return Image.fromarray(np.roll(_clean(np.array(frame_door_stopped())[:, :, ::-1].copy()), dx, axis=1)[:, :, ::-1])


SIZES = {1: [(36, 60, 46), (31, 54, 42)], 2: [(30, 52, 40), (27, 46, 36), (24, 40, 32), (22, 37, 30)]}
def flow(P, facts, x, y, w, ink, dim):
    """The facts rule: 1-4 one column, 5-12 two; the size steps down until the longest row fits its column. The cap stays 12."""
    facts = facts[:12]; n = len(facts); cols = 1 if n <= 4 else 2; cw = w / cols
    for size, step, d in SIZES[cols]:
        lim = max(P.tw(f[1] + (' ?' if f[3] == U else ''), size, 'Jost-SemiBold') + (P.tw(f[2], size * .76, 'Jost-Medium') + 10 if f[2] and f[2] != '?' else 0) for f in facts) + d + 12 + 14
        if lim <= cw: break
    rows = -(-n // cols)
    for i, (ic, word, note, st) in enumerate(facts):
        fx, fy = x + (i // rows) * cw, y + (i % rows) * step
        if st == U: P.rim(fx, fy, d, d, d / 2, U, col=dim, lw=1.8); P.text(fx + d / 2, fy + d / 2, '?', d * .56, col=dim, anchor='mm')
        else: P.icon(ic, fx + 2, fy + 2, d - 4, col=ink, alpha=.8 if st == T else 1)
        P.put(fx + d + 12, fy + d / 2, word + (' ?' if st == U and note == '?' else ''), size, col=dim if st == U else ink, face='Jost-SemiBold')
        if note and note != '?': P.put(fx + d + 12 + P.tw(word, size, 'Jost-SemiBold') + 10, fy + d / 2 + 1, note, size * .76, col=dim)
    return y + rows * step


def ways(P, c, x, y, W, h=96, **signs):
    """The ways rule: signs flow left to right and wrap. Returns the bottom."""
    if not c.get('does'): return y
    cx = x
    for wy in c['does']:
        w = 100 + max(P.tw(wy[1], 40), P.tw(wy[2] or '', 25, 'Jost-Medium')) + 34
        if cx + w > x + W and cx > x: cx = x; y += h + 12
        sign(P, cx, y, wy, w=w, h=h, **signs); cx += w + 14
    return y + h + 22


def track2(P, items, x, y, w, ink, dim, on, hot=ORANGE, d=46, paper=(0xf7, 0xf4, 0xec, 255)):
    """The track rule: one evenly spaced line, the latest six. The label size steps down until the longest fits its share."""
    items = items[-6:]; n = len(items); step = w / max(n, 1)
    for s in (24, 22, 20, 18):
        if max(P.tw(i[1], s, 'Jost-SemiBold') for i in items) <= step - 8: break
    P.line([(x + d / 2, y + d / 2), (x + (n - 1) * step + d / 2, y + d / 2)], col=dim, lw=2.5)
    for i, (ic, word, t, past) in enumerate(items):
        cx = x + i * step; fill = hot if past == 2 else on if past else None
        if fill: P.disc(cx, y, d, fill=fill, ring=None); P.icon(ic, cx + d * .2, y + d * .2, d * .6, col=CREAM if ink == INK else BODY_)
        else: P.disc(cx, y, d, fill=paper, ring=dim, rw=2); P.icon(ic, cx + d * .2, y + d * .2, d * .6, col=ink)
        P.put(cx, y + d + 26, word, s, col=ink, face='Jost-SemiBold'); P.put(cx, y + d + 56, t, 22, col=dim)


def lay(P, im): P.img = im.resize((3840, 2160), Image.LANCZOS).convert('RGBA')
def circle_fill(P, cx, cy, r, fill, clip=None):
    S_ = P.s; big = Image.new('L', (P.w, P.h), 0); ImageDraw.Draw(big).ellipse(((cx - r), (cy - r), (cx + r), (cy + r)), fill=255); m = big.resize((P.w * S_, P.h * S_), Image.BICUBIC)
    if clip: m = ImageChops.multiply(m, clip)
    a = fill[3] if len(fill) > 3 else 255
    if a < 255: m = m.point(lambda v: v * a // 255)
    lay_ = Image.new('RGBA', P.img.size, fill[:3] + (0,)); lay_.putalpha(m); P.img.alpha_composite(lay_)


def text_block(P, c, X, W, y, ink, dim, on, signs, name_top=124, state_pill=True, knowlabel='She knows', tracksize=None):
    """Name, sub, state, facts, ways, scales, knows, track, in that order, top down. Empty slots take no room."""
    s = name_size(c['name'], W, P, top=name_top) if len(c.get('facts', [])) <= 8 else name_size(c['name'], W, P, top=name_top * .72)
    P.put(X, y + s * .5, c['name'], s, col=ink, face='Jost-Bold'); y += s * 1.0 + 16
    P.put(X, y, c['sub'], 30, col=dim, face='Jost-Medium')
    if c.get('state') and state_pill:
        sx = X + P.tw(c['sub'], 30, 'Jost-Medium') + 18; tw = P.tw(c['state'], 30); P.rrect(sx, y - 23, tw + 34, 46, 23, fill=ORANGE); P.put(sx + 17, y, c['state'], 30, col=CREAM, face='Jost-Bold')
    y = flow(P, c['facts'], X, y + 36, W, ink, dim) + 6
    y = ways(P, c, X, y, W, **signs)
    if c.get('meters'):
        meters_line(P, c['meters'], X + 2, y + 12, ink, dim, on, size=23); knows_line(P, c['knows'], X + 2, y + 54, knowlabel, ink, dim, size=23); y += 86
    return y
