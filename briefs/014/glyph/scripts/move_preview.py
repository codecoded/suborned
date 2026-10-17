"""Glyph: briefs/014/glyph/move-preview.png. Two move-preview variants on the real 014 play shot (cafe-after.png), five states each.
A = "the ring": one colour-coded ground ring at the cursor, a symbol inside, the walk time as a clock fill on the rim, the exposed stretch of the route glowing.
B = "the ground answers": a plain clock ring; the cone pulses and the route glows when seen; a "?" pip and a pulse on the unwatched watcher when unknown; a lamp pip for Alert.
Everything over the street is drawn by this script (the build's own cursor chip is what Alt shows). Run from the repo root: python3 -I briefs/014/glyph/scripts/move_preview.py"""
import sys, os, math
sys.path.insert(0, '/Users/sky/Projects/suborned/out/glyph013'); os.chdir('/Users/sky/Projects/suborned/out/glyph013')
import lib
from lib import *
R = '/Users/sky/Projects/suborned'
SHOT = Image.open(R + '/briefs/014/play/cafe-after.png').convert('RGBA')
G14 = R + '/briefs/014/glyph/glyphs/'; KS = R + '/briefs/011/ui/kit/icons/singles/'
def glyph(name):
    for d in (G14, KS):
        p = d + name + '@2x.png'
        if os.path.exists(p): return Image.open(p).convert('RGBA')
    raise FileNotFoundError(name)
# colours (signals are reserved: only the watcher's own colour, never a new meaning)
CAM = (224, 58, 47); GUARD = (240, 160, 32); RIVAL = (138, 79, 208)
TQL = (96, 224, 234); CREAM = (247, 244, 236); INK = (18, 48, 46)
SS = 3                                   # supersample
YAW = math.radians(30); PITCH = 0.616    # ground ellipse ratio at 38 deg
S = 38.0                                 # px per metre at play zoom in this shot
CROP = (330, 585, 1030, 1015)            # x0,y0,x1,y1 of the shot every panel shows
PW, PH = CROP[2] - CROP[0], CROP[3] - CROP[1]
AGENT = (500, 948)
def g2s(o, X, Z): return (o[0] + S * (X * math.cos(YAW) - Z * math.sin(YAW)), o[1] + S * PITCH * (X * math.sin(YAW) + Z * math.cos(YAW)))
def s2g(dx, dy):
    u, v = dx / S, dy / (PITCH * S); return (u * math.cos(YAW) + v * math.sin(YAW), -u * math.sin(YAW) + v * math.cos(YAW))
def cone(o, toward, half=32, rng=9.5, n=18):
    X, Z = s2g(toward[0] - o[0], toward[1] - o[1]); a0 = math.atan2(Z, X)
    pts = [o]
    for i in range(n + 1):
        a = a0 - math.radians(half) + math.radians(2 * half) * i / n
        pts.append(g2s(o, rng * math.cos(a), rng * math.sin(a)))
    return pts
def inside(p, poly):
    x, y = p; c = False; j = len(poly) - 1
    for i in range(len(poly)):
        xi, yi = poly[i]; xj, yj = poly[j]
        if (yi > y) != (yj > y) and x < (xj - xi) * (y - yi) / (yj - yi + 1e-9) + xi: c = not c
        j = i
    return c
def resample(pts, step):
    out = []; carry = 0.0
    for (x0, y0), (x1, y1) in zip(pts, pts[1:]):
        L = math.hypot(x1 - x0, y1 - y0)
        if L == 0: continue
        d = carry
        while d <= L: out.append((x0 + (x1 - x0) * d / L, y0 + (y1 - y0) * d / L)); d += step
        carry = d - L
    return out
class Layer:
    def __init__(self): self.im = Image.new('RGBA', (PW * SS, PH * SS), (0, 0, 0, 0))
    def p(self, pt): return ((pt[0] - CROP[0]) * SS, (pt[1] - CROP[1]) * SS)
    def over(self, other): self.im.alpha_composite(other)
    def draw(self, fn):
        t = Image.new('RGBA', self.im.size, (0, 0, 0, 0)); d = ImageDraw.Draw(t); fn(d, t); self.im.alpha_composite(t)
    def poly(self, pts, fill=None, outline=None, w=2):
        def f(d, t):
            P = [self.p(q) for q in pts]
            if fill: d.polygon(P, fill=fill + (fill_a(fill),) if False else fill)
            if outline: d.line(P + [P[0]], fill=outline, width=int(w * SS), joint='curve')
        self.draw(f)
    def blur(self, pts, col, w, a):
        t = Image.new('RGBA', self.im.size, (0, 0, 0, 0)); d = ImageDraw.Draw(t); P = [self.p(q) for q in pts]
        d.line(P, fill=col + (a,), width=int(w * SS), joint='curve')
        for q in P: d.ellipse((q[0] - w * SS / 2, q[1] - w * SS / 2, q[0] + w * SS / 2, q[1] + w * SS / 2), fill=col + (a,))
        self.im.alpha_composite(t.filter(ImageFilter.GaussianBlur(w * SS * 0.45)))
    def ell(self, c, rx, ry, col, w, a=255, dash=None, arc=None):
        def f(d, t):
            cx, cy = self.p(c); k = SS
            a0, a1 = (0, 360) if arc is None else arc
            steps = max(24, int((a1 - a0) / 3)); pts = []
            for i in range(steps + 1):
                ang = math.radians(a0 + (a1 - a0) * i / steps); pts.append((cx + rx * k * math.sin(ang), cy - ry * k * math.cos(ang)))
            if dash:
                on, off = dash; i = 0
                while i < len(pts) - 1:
                    seg = pts[i:i + on + 1]
                    if len(seg) > 1: d.line(seg, fill=col + (a,), width=int(w * k), joint='curve')
                    i += on + off
            else:
                d.line(pts, fill=col + (a,), width=int(w * k), joint='curve')
                for q in (pts[0], pts[-1]):
                    if arc is not None: d.ellipse((q[0] - w * k / 2, q[1] - w * k / 2, q[0] + w * k / 2, q[1] + w * k / 2), fill=col + (a,))
        self.draw(f)
    def disc(self, c, rx, ry, col, a):
        def f(d, t):
            cx, cy = self.p(c); d.ellipse((cx - rx * SS, cy - ry * SS, cx + rx * SS, cy + ry * SS), fill=col + (a,))
        self.draw(f)
    def circle(self, c, r, col, a, outline=None, ow=2):
        def f(d, t):
            cx, cy = self.p(c); d.ellipse((cx - r * SS, cy - r * SS, cx + r * SS, cy + r * SS), fill=col + (a,), outline=outline, width=int(ow * SS))
        self.draw(f)
    def glyph(self, name, c, size, col, a=255):
        im = glyph(name); al = im.getchannel('A').point(lambda v: v * a // 255)
        o = Image.new('RGBA', im.size, col + (255,)); o.putalpha(al)
        o = o.resize((int(size * SS), int(size * SS)), Image.LANCZOS); cx, cy = self.p(c)
        self.im.alpha_composite(o, (int(cx - o.width / 2), int(cy - o.height / 2)))
    def image(self, im, c, w):
        o = im.resize((int(w * SS), int(im.height * w * SS / im.width)), Image.LANCZOS); cx, cy = self.p(c)
        self.im.alpha_composite(o, (int(cx - o.width / 2), int(cy - o.height / 2)))
    def out(self): return self.im.resize((PW, PH), Image.LANCZOS)
RX, RY = 32.0, 19.7
def ground_dots(L, pts, exposed_poly=None, col=GUARD, dash=False):
    path = pts; dots = resample(path, 15 if not dash else 11)
    for q in dots:
        hit = exposed_poly is not None and inside(q, exposed_poly)
        L.disc(q, 5.0 if hit else 4.4, 3.2 if hit else 2.8, INK, 200)
        L.disc(q, 3.5 if hit else 3.0, 2.2 if hit else 1.9, col if hit else CREAM, 255)
def exposed_stretch(pts, poly):
    seg = []; best = []; 
    for q in resample(pts, 4):
        if inside(q, poly): seg.append(q)
        elif seg: best.append(seg); seg = []
    if seg: best.append(seg)
    return best
def person_pip(L, pos, kind='guard'):
    nm = {'guard': 'signal_guard', 'cam': 'signal_camera'}[kind]
    im = Image.open(R + f'/briefs/011/ui/kit/badges/{nm}@2x.png').convert('RGBA'); L.image(im, (pos[0], pos[1] - 52), 22)
# ------------------------------------------------------------------ the five states
GUARD_AT = (655, 722); UNK_AT = (803, 652)
ROUTES = {
    'safe':    dict(c=(890, 948),  t='14',  r=[AGENT, (690, 962), (890, 948)]),
    'seen':    dict(c=(890, 842),  t='20',  r=[AGENT, (660, 900), (890, 842)]),
    'unknown': dict(c=(905, 770),  t='26',  r=[AGENT, (700, 880), (905, 770)]),
    'alert':   dict(c=(800, 905),  t='9',   r=[AGENT, (700, 930), (800, 905)]),
    'alt':     dict(c=(890, 948),  t='14',  r=[AGENT, (690, 962), (890, 948)]),
}
def ring_A(L, c, kind, secs, sig=GUARD):
    frac = secs / 60.0; arc = (0, 360 * frac)
    col = {'safe': TQL, 'seen': sig, 'unknown': CREAM, 'alert': INK, 'alt': TQL}[kind]
    # under-stroke for contrast on pale stone
    L.disc(c, RX - 3, RY - 1.8, INK, 190)
    L.ell(c, RX, RY, INK, 8.0, 150)
    if kind == 'alert':
        L.ell(c, RX + 6.5, RY + 4.0, INK, 5.0, 170); L.ell(c, RX + 6.5, RY + 4.0, CREAM, 2.6, 255)           # the second rim: a shape cue, not only a colour
        L.ell(c, RX, RY, INK, 5.0, 255)
        L.ell(c, RX, RY, CREAM, 3.0, 255, arc=arc)
    elif kind == 'unknown':
        L.ell(c, RX, RY, CREAM, 3.2, 150, dash=(2, 2))           # dashed track = unknown
        L.ell(c, RX, RY, CREAM, 3.6, 255, arc=arc)
    else:
        L.ell(c, RX, RY, col, 3.2, 110)
        L.ell(c, RX, RY, col, 4.6, 255, arc=arc)
    sym = {'safe': ('s_moving', CREAM), 'seen': ('eye_open', CREAM), 'unknown': ('eye_unk', CREAM), 'alert': ('p_alert', CREAM), 'alt': ('s_moving', CREAM)}[kind]
    L.glyph(sym[0], (c[0], c[1] - 1), 25, sym[1])
def ring_B(L, c, kind, secs):
    frac = secs / 60.0; arc = (0, 360 * frac)
    L.disc(c, RX - 3, RY - 1.8, INK, 70)
    L.ell(c, RX, RY, INK, 8.0, 170)
    L.ell(c, RX, RY, CREAM, 3.4, 255)
    L.ell(c, RX, RY, TQL, 4.6, 255, arc=arc)
    ang = math.radians(300)                                      # pip sits on the rim at about 10 o'clock... upper right is where the cone/route glows start
    pip = (c[0] + RX * math.sin(math.radians(52)), c[1] - RY * math.cos(math.radians(52)))
    if kind == 'unknown':
        L.circle(pip, 9.5, INK, 235, outline=CREAM + (255,), ow=1.6); L.glyph('p_unknown', pip, 13, CREAM)
    if kind == 'alert':
        L.circle(pip, 10.5, INK, 235, outline=CREAM + (255,), ow=1.6); L.glyph('p_alert', pip, 15, CREAM)
def watcher_pulse(L, pos, strong=True):
    for k, a in ((1.0, 150), (1.55, 80)):
        L.ell(pos, 26 * k, 26 * PITCH * k, CREAM, 2.0, a, dash=(3, 2))
    L.circle((pos[0], pos[1] - 52), 11, INK, 200, outline=CREAM + (255,), ow=1.4); L.glyph('p_unknown', (pos[0], pos[1] - 52), 15, CREAM)
def alt_chip():
    cv = Image.new('RGBA', (900, 260), (0, 0, 0, 0))
    lib.icon = (lambda f: (lambda n: glyph(n) if os.path.exists(G14 + n + '@2x.png') or os.path.exists(KS + n + '@2x.png') else f(n)))(lib.icon)
    right = chip(cv, 12, 190, [('s_moving', '14 s'), ('run', '7 s'), ('eye_unk', ''), ('p_alert', '1')])
    bbox = cv.getbbox(); return cv.crop(bbox)
CHIP = alt_chip()
def panel(variant, state):
    r = ROUTES[state]; L = Layer(); c = r['c']; secs = int(r['t']); route = r['r']
    seen = state == 'seen'; sig = GUARD
    gpoly = cone(GUARD_AT, (780, 905), 27, 7.2)
    if seen:
        # the learned cone: static in A, pulsing (brighter, echoed edge) in B
        a_fill = 40 if variant == 'A' else 90
        t = Image.new('RGBA', L.im.size, (0, 0, 0, 0)); d = ImageDraw.Draw(t)
        P = [L.p(q) for q in gpoly]; d.polygon(P, fill=sig + (a_fill,)); L.im.alpha_composite(t)
        L.draw(lambda d, t: d.line([L.p(q) for q in gpoly] + [L.p(gpoly[0])], fill=sig + (200,), width=int((2.0 if variant == 'A' else 3.2) * SS), joint='curve'))
        if variant == 'B':
            big = cone(GUARD_AT, (780, 905), 27, 8.2); L.draw(lambda d, t: d.line([L.p(q) for q in big] + [L.p(big[0])], fill=sig + (110,), width=int(2 * SS), joint='curve'))
        person_pip(L, GUARD_AT, 'guard')
        for stretch in exposed_stretch(route, gpoly):
            L.blur(stretch, sig, 15, 190)
    if state == 'unknown':
        # the unwatched watcher: a guard the Scout has not watched, so nothing is drawn for his cone
        if variant == 'B': watcher_pulse(L, UNK_AT)
        else: L.circle((UNK_AT[0], UNK_AT[1] - 52), 11, INK, 200, outline=CREAM + (255,), ow=1.4); L.glyph('p_unknown', (UNK_AT[0], UNK_AT[1] - 52), 15, CREAM)
    if state == 'seen' or state in ('safe', 'alert', 'alt', 'unknown'):
        pass
    ground_dots(L, route, gpoly if seen else None, sig, dash=(state == 'alert'))
    if variant == 'A': ring_A(L, c, state, secs, sig)
    else: ring_B(L, c, state, secs)
    if state == 'alt':
        L.image(CHIP.resize((int(CHIP.width * 0.40), int(CHIP.height * 0.40)), Image.LANCZOS), (c[0] + 28, c[1] - 70), CHIP.width * 0.40)
    base = SHOT.crop(CROP).convert('RGBA'); base.alpha_composite(L.out())
    return base
LABELS = {'safe': 'Safe walk, 14 s', 'seen': 'Seen walk, 20 s: the guard sees the stretch that glows', 'unknown': 'Unknown: a watcher nobody has watched, 26 s',
          'alert': 'Raises Alert (a run), 9 s', 'alt': 'Alt held: the numbers as today, next to the ring'}
def build():
    states = ['safe', 'seen', 'unknown', 'alert', 'alt']; M = 24; HDR = 96; CAP = 34
    CU_H = 400
    W = M + 2 * (PW + M); H = HDR + len(states) * (PH + CAP + 14) + CU_H + 40
    cv = Image.new('RGBA', (W, H), (36, 52, 50, 255))
    text(cv, M, 22, 'A  The ring (recommended)', 'd', 17, 'Bold', 'cream', 'lm')
    text(cv, M + PW + M, 22, 'B  The ground answers', 'd', 17, 'Bold', 'cream', 'lm')
    text(cv, M, 52, 'The rim fills clockwise with the walk time, a full ring = 60 s. No digits, no words.', 'b', 13, 'Medium', 'cream', 'lm')
    text(cv, M, 74, "A: turquoise safe | cream dashed unknown | the watcher's own colour seen | ink and a second rim raises Alert.", 'b', 13, 'Medium', 'cream', 'lm')
    panels = {}
    for i, st in enumerate(states):
        y = HDR + i * (PH + CAP + 14)
        for j, v in enumerate('AB'):
            x = M + j * (PW + M); im = panel(v, st); panels[(v, st)] = im
            cv.alpha_composite(im, (x, y + CAP))
        text(cv, M, y + 16, LABELS[st], 'b', 14, 'Medium', 'cream', 'lm')
    # the rings close up so the symbols can be judged (A over B)
    y0 = HDR + len(states) * (PH + CAP + 14) + 10
    text(cv, M, y0 + 4, 'The rings close up, 2.5x (A on top, B under)', 'b', 14, 'Medium', 'cream', 'lm')
    CWd, CHd = 275, 175
    for k, st in enumerate(states):
        c = ROUTES[st]['c']; cx, cy = c[0] - CROP[0], c[1] - CROP[1]
        w, h = (110, 70) if st != 'alt' else (176, 112)
        for j, v in enumerate('AB'):
            im = panels[(v, st)].crop((int(cx - w / 2 + (0 if st != 'alt' else 20)), int(cy - h * 0.58 - (0 if st != 'alt' else 18)), int(cx + w / 2 + (0 if st != 'alt' else 20)), int(cy + h * 0.42 - (0 if st != 'alt' else 18))))
            im = im.resize((CWd, CHd), Image.LANCZOS)
            cv.alpha_composite(im, (M + k * (CWd + 8), y0 + 22 + j * (CHd + 8)))
    cv.convert('RGB').save(R + '/briefs/014/glyph/move-preview.png'); print('ok', cv.size)
if __name__ == '__main__': build()
