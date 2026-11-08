"""The round 2 board for Sky's phone (brief 101): 1000 px wide, in the book's light page. It leads with the busiest moment (today, then the two
directions, each with its room on screen and two close-ups at 2x); then the coverage table; what a shot shows instead of a percentage; each
direction's four moments; the pick, what it costs to build and what to capture next. Glyph. Run after r2_cover.py (which renders everything)."""
import sys, os, json
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from r2_base import *
from r2_board_text import *

W, M = 1000, 40; CW = W - 2 * M
COV = json.load(open(OUT2 + 'make/coverage.json'))['rows']
TEAL = (0x00, 0x96, 0x88, 255); GREY = (0x8f, 0x9b, 0x98, 255)  # the chart's series: validated (dataviz validate_palette, light, on #f9f7ee); today is the de-emphasis grey
SERIES = [('today', 'Today', GREY), ('street', '1 · Street labels', TEAL), ('tokens', '2 · Corner tokens', ORANGE)]
FILE = {'today': 'today', 'S': 'S', 'T': 'T'}; KEY = {'today': 'today', 'S': 'street', 'T': 'tokens'}
CLOSE = {'today': [('the seal card', (740, 56, 1180, 180)), ('the aim and the fight', (870, 560, 1290, 800))],
         'S': [('the clock and the seal', (1540, 0, 1920, 210)), ('the aim and the fight', (980, 620, 1230, 810))],
         'T': [('the clock and the seal', (1480, 0, 1920, 230)), ('the aim and the fight', (980, 620, 1230, 810))]}


def sheet(h):
    P = Page(2, W, h); ImageDraw.Draw(P.img).rectangle((0, 0, W * 2, h * 2), fill=PAGE); return P


def done(P, y): return P.img.crop((0, 0, W * 2, round(y * 2))).resize((W, round(y)), Image.LANCZOS).convert('RGB')


def pic(P, im, x, y, w, r=10):
    h = round(im.height * w / im.width); s = P.s; big = im.convert('RGBA').resize((round(w * s), round(h * s)), Image.LANCZOS)
    m = Image.new('L', big.size, 0); ImageDraw.Draw(m).rounded_rectangle((0, 0, big.width - 1, big.height - 1), r * s, fill=255); big.putalpha(m)
    P.img.alpha_composite(big, (round(x * s), round(y * s))); P.rrect(x, y, w, h, r, line=HAIR, lw=1.2); return y + h


def para(P, x, y, t, w, size=20, col=INK, face_='Jost-Medium', lead=1.38):
    """Wrapped text whose first line's top is y; a figure keeps its unit ('15 %', '4 s', '2880 × 1864' break only at plain spaces). Returns the bottom."""
    import re
    t = re.sub(r'(\d) (%|s\b|×)', lambda m_: m_.group(1) + '\u00a0' + m_.group(2), t); t = re.sub(r'× (\d)', '×\u00a0\\1', t)
    L = size * lead; line, n = '', 0; y0 = y + L / 2
    for word in t.split(' '):
        trial = (line + ' ' + word).strip(' ')
        if P.tw(trial, size, face_) > w and line: P.text(x, y0 + n * L, line, size, col=col, face=face_); n += 1; line = word
        else: line = trial
    P.text(x, y0 + n * L, line, size, col=col, face=face_); P.words_add(t); return y0 + (n + 1) * L - L / 2


def head(P, y, cap, title, size=40):
    caps(P, M, y, cap, size=15, col=DEEP, gap=3); P.put(M, y + 46, title, size, col=INK, face='Jost-Bold'); return y + 46 + size * .75


def pill(P, x, y, t, fill=ORANGE, size=15):
    w = P.tw(t, size) + 26; P.rrect(x, y - 15, w, 30, 15, fill=fill); P.put(x + 13, y, t, size, col=CREAM, face='Jost-Bold'); return w


def header():
    P = sheet(1400); y = 46; caps(P, M, y, 'brief 101 · the hud sweep · round 2', size=15, col=DEEP, gap=3)
    P.put(M, y + 54, 'Lean first', 48, col=INK, face='Jost-Bold'); y += 98
    for q in SKY: y = para(P, M, y, q, CW, size=18, col=SOFT, lead=1.34) + 8
    P.put(M, y + 12, '(Sky, on round 1)', 14, col=SOFT, face='Jost-SemiBold'); y += 40
    y = para(P, M, y, RULE, CW, size=18, lead=1.38) + 14
    y = para(P, M, y, 'Glyph · 8 November 2026 · mock only: real game frames with today\'s HUD lifted off, real type, the runs\' own numbers.', CW, size=15, col=SOFT) + 30
    return done(P, y)


def busiest():
    P = sheet(5600); y = head(P, 40, 'the busiest moment', 'The final fight, three ways') + 20
    y = para(P, M, y, BUSY_CAP, CW, size=17, lead=1.38) + 22
    for d in ('today', 'S', 'T'):
        cov = COV['busy'][KEY[d]]; name = DIRS[d]['name']
        P.put(M, y + 22, name, 30, col=INK, face='Jost-Bold'); nx = M + P.tw(name, 30) + 14
        if d == 'T': pill(P, nx, y + 22, 'my pick', size=13)
        P.put(W - M, y + 20, f'{cov:.1f} %', 40, col=INK if d != 'today' else SOFT, face='Jost-Bold', anchor='rm')
        caps(P, W - M - P.tw(f'{cov:.1f} %', 40) - 12, y + 26, 'of the screen', size=11.5, col=SOFT, gap=1.6, anchor='r'); y += 50
        y = para(P, M, y, DIRS[d]['line'], CW, size=15.5, col=SOFT, lead=1.34) + 12
        y = pic(P, Image.open(OUT2 + f'{FILE[d]}-busy.png'), M, y, CW) + 14
        cw = (CW - 20) / 2; x = M; bot = y
        for lab, box in CLOSE[d]:
            im = Image.open(OUT2 + f'{FILE[d]}-busy.png').crop(box); im = im.resize((im.width * 2, im.height * 2), Image.LANCZOS)
            caps(P, x, y + 6, lab + ' · 2x', size=11.5, col=DEEP, gap=1.6); bot = max(bot, pic(P, im, x, y + 20, cw, r=8)); x += cw + 20
        y = bot + 44
    y = para(P, M, y - 16, BUSY_REAL, CW, size=14, col=SOFT, lead=1.32) + 30
    return done(P, y)


def room():
    """The coverage table: one row per moment, the three numbers as text (ink) with a thin bar each on one shared scale (0 to 11 %)."""
    P = sheet(1400); y = head(P, 40, 'room on screen', 'Half of today, or less') + 24
    lx, nx, bx = M, M + 250, M + 330; bw = CW - 330; sc = bw / 11
    x = bx
    for key, lab, col in SERIES:  # the legend
        P.rrect(x, y - 6, 14, 12, 3, fill=col); P.put(x + 20, y, lab, 14.5, col=INK, face='Jost-SemiBold'); x += P.tw(lab, 14.5, 'Jost-SemiBold') + 44
    y += 30
    names = {'rest': 'At rest', 'quiet': 'Quiet: a report and a card', 'loud': 'Loud: the seal and police', 'aim': 'Aiming the first shot', 'busy': 'The busiest moment'}
    for k in ('rest', 'quiet', 'loud', 'aim', 'busy'):
        P.line([(M, y - 4), (W - M, y - 4)], col=HAIR, lw=1); P.put(lx, y + 33, names[k], 17, col=INK, face='Jost-Bold' if k == 'busy' else 'Jost-SemiBold')
        for i, (key, lab, col) in enumerate(SERIES):
            v = COV[k][key]; yy = y + 12 + i * 22; P.put(nx + 52, yy + 6, f'{v:.1f} %', 15, col=INK, face='Jost-Bold' if key != 'today' else 'Jost-Medium', anchor='rm')
            P.rrect(bx, yy, max(4, v * sc), 12, 4, fill=col)
        y += 12 + 3 * 22 + 12
    for t in (0, 5, 10, 15):
        if t * sc <= bw: P.put(bx + t * sc, y + 10, f'{t} %', 12, col=SOFT, face='Jost-Medium', anchor='mm')
    y += 34; y = para(P, M, y, ROOM, CW, size=14.5, col=SOFT, lead=1.34) + 30
    return done(P, y)


def cue_key(P, x, y):
    """The shot's cues, drawn as on the target: his ring whole or in arcs, the shield whole or half."""
    items = [('in range', 0, None), ('a long shot', 3, None), ('his cover counts', 0, 'whole'), ('in part', 0, 'part')]; cw = CW / 4
    for i, (lab, gaps, cov) in enumerate(items):
        cx = x + cw * i + cw / 2; ground_arc(P, cx, y + 30, 40, 1, AMBER, 4.4, gaps=gaps)
        if cov: shield(P, cx - 9, y + 36, 18, part=cov == 'part')
        P.put(cx, y + 86, lab, 15, col=INK, face='Jost-SemiBold', anchor='mm')
    return y + 104


def shot():
    P = sheet(2400); y = head(P, 40, 'aiming', 'What a shot shows now') + 22
    cw = (CW - 20) / 2
    a = Image.open(OUT2 + 'today-aim.png').crop((690, 130, 1150, 300)); a = a.resize((a.width * 2, a.height * 2), Image.LANCZOS)
    b = Image.open(OUT2 + 'T-aim.png').crop((760, 150, 1250, 420)); b = b.resize((b.width * 2, b.height * 2), Image.LANCZOS)
    caps(P, M, y + 6, 'today · 2x', size=11.5, col=DEEP, gap=1.6); caps(P, M + cw + 20, y + 6, 'both directions · 2x', size=11.5, col=DEEP, gap=1.6)
    yb = max(pic(P, a, M, y + 20, cw, r=8), pic(P, b, M + cw + 20, y + 20, cw, r=8)); y = yb + 22
    for t in SHOT_POINTS:
        P.disc(M + 2, y + 7, 9, fill=ORANGE, ring=None); y = para(P, M + 22, y, t, CW - 22, size=16.5, lead=1.36) + 10
    y = cue_key(P, M, y + 10) + 40
    caps(P, M, y, "today's odds plate", size=14, col=DEEP, gap=2.4); y = para(P, M, y + 16, ODDS, CW, size=16, lead=1.36) + 30
    return done(P, y)


def moments(d):
    P = sheet(2200); name = DIRS[d]['name']; y = head(P, 40, 'direction ' + name[0], name[4:]) + 14
    if d == 'T': pill(P, M + P.tw(name[4:], 40) + 16, y - 30, 'my pick', size=13)
    y = para(P, M, y, DIRS[d]['line'], CW, size=16, lead=1.36) + 18; cw = (CW - 20) / 2
    labels = {'rest': 'At rest', 'quiet': 'Quiet · 3:28', 'loud': 'Loud · 2:34', 'aim': 'Aim · 2:31'}
    for row in (('rest', 'quiet'), ('loud', 'aim')):
        bot = y
        for j, k in enumerate(row):
            x = M + j * (cw + 20); bot = max(bot, pic(P, Image.open(OUT2 + f'{d}-{k}.png'), x, y, cw, r=8))
            P.put(x, bot + 20, labels[k], 16, col=INK, face='Jost-Bold'); P.put(x + cw, bot + 20, f"{COV[k][KEY[d]]:.1f} %  (today {COV[k]['today']:.1f} %)", 14, col=SOFT, face='Jost-SemiBold', anchor='rm')
        y = bot + 48
    return done(P, y + 10)


def pick():
    P = sheet(4000); y = head(P, 40, 'my recommendation', PICK[0], size=36) + 22
    for i, t in enumerate(PICK[1]):
        P.disc(M, y, 28, fill=DEEP, ring=None); P.put(M + 14, y + 14, str(i + 1), 15, col=CREAM, face='Jost-Bold', anchor='mm'); y = para(P, M + 42, y + 1, t, CW - 42, size=17) + 12
    y += 14; caps(P, M, y, 'what it costs to build (latch, piece by piece, each shot on its own)', size=14, col=DEEP, gap=2.2); y += 24
    for n, t, size in BUILD:
        P.put(M + 8, y + 12, n, 18, col=ORANGE, face='Jost-Bold'); P.rrect(W - M - 40, y, 40, 26, 13, fill=DEEP if size == 'S' else ORANGE)
        P.put(W - M - 20, y + 13, size, 14, col=CREAM, face='Jost-Bold', anchor='mm'); y = para(P, M + 40, y, t, CW - 100, size=16, lead=1.32) + 12
    y = para(P, M, y, BUILD_NOTE, CW, size=14.5, col=SOFT, lead=1.32) + 22
    for t in (STAYS, PERF, WORDS): y = para(P, M, y, t, CW, size=16, lead=1.36) + 16
    caps(P, M, y + 8, 'captures i need (for aster)', size=14, col=DEEP, gap=2.4); y += 28
    for t in NEED: P.disc(M + 2, y + 6, 9, fill=ORANGE, ring=None); y = para(P, M + 22, y, t, CW - 22, size=16, lead=1.3) + 6
    return done(P, y + 40)


if __name__ == '__main__':
    parts = [header(), busiest(), room(), shot(), moments('T'), moments('S'), pick()]
    H = sum(p.height for p in parts) + 4 * (len(parts) - 1); board = Image.new('RGB', (W, H), HAIR[:3]); y = 0
    for p in parts: board.paste(p, (0, y)); y += p.height + 4
    board.save(OUT2 + 'board.png'); print('board', board.size)
