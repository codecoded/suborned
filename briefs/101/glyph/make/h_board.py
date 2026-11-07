"""The board for Sky's phone (brief 101): 1000 px wide, in the book's light page. Today's six moments and the rest of the HUD with a verdict
each; the three directions, each with its whole frame, its six moments at 2x and what it costs on screen; the pick, the build order, what
stays, the Air, and the captures I didn't have. Glyph. Run after h_rest.py and h_crops.py."""
import sys, os, json
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from h_base import *
from h_board_text import *

W, M = 1000, 40; CW = W - 2 * M
COV = json.load(open(OUT + 'make/coverage.json'))


def sheet(h=3400):
    P = Page(2, W, h); ImageDraw.Draw(P.img).rectangle((0, 0, W * 2, h * 2), fill=PAGE); return P


def done(P, y): return P.img.crop((0, 0, W * 2, round(y * 2))).resize((W, round(y)), Image.LANCZOS).convert('RGB')


def pic(P, im, x, y, w, r=10):
    """An image at width w, rounded, with a hairline. Returns its bottom."""
    h = round(im.height * w / im.width); s = P.s; big = im.convert('RGBA').resize((w * s, h * s), Image.LANCZOS)
    m = Image.new('L', big.size, 0); ImageDraw.Draw(m).rounded_rectangle((0, 0, big.width - 1, big.height - 1), r * s, fill=255); big.putalpha(m)
    P.img.alpha_composite(big, (round(x * s), round(y * s))); P.rrect(x, y, w, h, r, line=HAIR, lw=1.2); return y + h


def para(P, x, y, t, w, size=20, col=INK, face_='Jost-Medium', lead=1.38):
    """Wrapped text whose first line's top is y. Returns the bottom."""
    L = size * lead; return P.wrap(x, y + L / 2, t, w, size, col=col, face=face_, lead=L) - L / 2


def head(P, y, cap, title, size=40):
    caps(P, M, y, cap, size=15, col=DEEP, gap=3); P.put(M, y + 46, title, size, col=INK, face='Jost-Bold'); return y + 46 + size * .75


def pill(P, x, y, t, fill=ORANGE, size=15):
    w = P.tw(t, size) + 26; P.rrect(x, y - 15, w, 30, 15, fill=fill); P.put(x + 13, y, t, size, col=CREAM, face='Jost-Bold'); return w


def header():
    P = sheet(1100); y = 46; caps(P, M, y, 'brief 101 · the hud sweep', size=15, col=DEEP, gap=3)
    P.put(M, y + 54, "From lazy to the book's level", 46, col=INK, face='Jost-Bold'); y += 96
    y = para(P, M, y, QUOTE, CW, size=19, col=SOFT) + 10
    y = para(P, M, y, 'Glyph · 7 November 2026 · mock only: real game frames with today\'s HUD lifted off, real type, the runs\' own numbers.', CW, size=16, col=SOFT) + 26
    tw = (CW - 2 * 20) // 3
    for i, d in enumerate('ABC'):
        x = M + i * (tw + 20); yb = pic(P, Image.open(OUT + DIRS[d]['file'] + '-loud.png'), x, y, tw, r=8)
        P.disc(x, yb + 14, 34, fill=ORANGE if d == 'A' else DEEP, ring=None); P.put(x + 17, yb + 31, d, 19, col=CREAM, face='Jost-Bold', anchor='mm')
        P.put(x + 44, yb + 31, DIRS[d]['name'], 19, col=INK, face='Jost-Bold')
        if d == 'A': pill(P, x + 44 + P.tw(DIRS[d]['name'], 19) + 10, yb + 31, 'my pick', size=13)
    y = yb + 66; return done(P, y)


def today():
    P = sheet(3600); y = head(P, 40, 'today', "What's lazy, and why") + 18
    y = para(P, M, y, ROOT, CW, size=19) + 26; cw = (CW - 2 * 24) // 3
    for i in range(0, 6, 3):
        bot = y
        for j, (k, title, verdict) in enumerate(TODAY[i:i + 3]):
            x = M + j * (cw + 24); yb = pic(P, Image.open(OUT + f'crops/today-{k}.png'), x, y, cw, r=8)
            P.put(x, yb + 20, title, 17, col=INK, face='Jost-Bold'); yb = para(P, x, yb + 32, verdict, cw, size=14.5, col=SOFT, lead=1.32); bot = max(bot, yb)
        y = bot + 26
    caps(P, M, y + 6, 'the rest of the hud in play', size=14, col=DEEP, gap=2.6); y += 30
    for piece, verdict in INVENTORY:
        P.put(M, y + 12, piece, 17, col=INK, face='Jost-Bold'); yb = para(P, M + 200, y, verdict, CW - 200, size=16, col=INK, lead=1.32)
        y = yb + 12; P.line([(M, y - 6), (W - M, y - 6)], col=HAIR, lw=1)
    return done(P, y + 26)


def direction(d):
    D = DIRS[d]; P = sheet(4400); y = 40
    caps(P, M, y, f'direction {d}', size=15, col=DEEP, gap=3)
    if d == 'A': pill(P, M + 150, y, 'my pick', size=13)
    P.put(M, y + 46, D['name'], 42, col=INK, face='Jost-Bold'); y += 82
    mood = Image.open(R + 'briefs/085/glyph/book/look4/L-light-inez.png') if d == 'A' else Image.open(R + 'paid/101-hud/codex/' + D['mood'])
    mw = 360; mx = W - M - mw; mb = pic(P, mood, mx, y + 4, mw)
    cap = "The book's light page, which A brings into play" if d == 'A' else 'Mood only, a free Codex still. ' + ('Not a layout.' if d == 'B' else 'The figures are the generator\'s, not our people.')
    mb = para(P, mx, mb + 8, cap, mw, size=13, col=SOFT, lead=1.3)
    ty = para(P, M, y, D['idea'], mx - M - 24, size=17, lead=1.38); y = max(ty, mb) + 22
    y = pic(P, Image.open(OUT + D['file'] + '-loud.png'), M, y, CW) + 8
    para(P, M, y, 'The loud run at 2:34: the seal running, police on the way, the Enforcer just fired.', CW, size=14, col=SOFT); y += 34
    cw = (CW - 30) // 2; keys = ['seal', 'police', 'top', 'aim', 'feed', 'report']
    if d == 'C': keys = keys[1:]  # C's seal is the whole frame above
    col = [y, y]  # two columns, each crop goes to the shorter one
    for k in keys:
        j = 0 if col[0] <= col[1] else 1; x = M + j * (cw + 30); caps(P, x, col[j] + 8, MOMENT_NAMES[k], size=12.5, col=DEEP, gap=1.8)
        col[j] = pic(P, Image.open(OUT + f'crops/{d}-{k}.png'), x, col[j] + 24, cw) + 22
    y = max(col)
    if d == 'C': para(P, M, y - 8, 'The seal is the frame above: an orange edge round the whole view, its count in a tab in the top edge.', CW, size=14, col=SOFT); y += 26
    cv = COV[d]; t = (f"Room on screen: {COV['rest'][d]} % at rest, {max(cv.values())} % at its busiest (the budget allows 15 %). "
                      f"Today: {COV['today']['rest (start-overhead-day)']} % at rest, {COV['today']['loud (why-loud-police)']} % in the loud moment.")
    y = para(P, M, y + 4, t, CW, size=16, col=INK, face_='Jost-SemiBold') + 12
    for lab, k, col in (('Good', 'good', DEEP), ('Risk', 'risk', ORANGE)):
        P.put(M, y + 11, lab, 16, col=col, face='Jost-Bold'); y = para(P, M + 60, y, D[k], CW - 60, size=16, col=INK, lead=1.32) + 8
    return done(P, y + 26)


def pick():
    P = sheet(3600); y = head(P, 40, 'my recommendation', PICK[0], size=36) + 20
    for i, t in enumerate(PICK[1]):
        P.disc(M, y, 28, fill=DEEP, ring=None); P.put(M + 14, y + 14, str(i + 1), 15, col=CREAM, face='Jost-Bold', anchor='mm'); y = para(P, M + 42, y + 1, t, CW - 42, size=17) + 12
    y += 14; caps(P, M, y, 'build order (latch, piece by piece, each shot on its own)', size=14, col=DEEP, gap=2.4); y += 22
    for n, t, size in BUILD:
        P.put(M + 8, y + 12, n, 18, col=ORANGE, face='Jost-Bold'); P.rrect(W - M - 40, y, 40, 26, 13, fill=DEEP if size == 'S' else ORANGE)
        P.put(W - M - 20, y + 13, size, 14, col=CREAM, face='Jost-Bold', anchor='mm'); y = para(P, M + 40, y, t, CW - 100, size=16, lead=1.32) + 12
    para(P, M, y, 'Sizes are my estimate for Latch to check: S is small, M is a few S. Once it lands, §15 of the GDD gets its new HUD kit, in a new version after Sky has seen a real capture.', CW, size=14, col=SOFT); y += 46
    for cap, t in (('what stays', STAYS), ('on the air (60 fps at play zoom)', PERF)):
        caps(P, M, y, cap, size=14, col=DEEP, gap=2.4); y = para(P, M, y + 16, t, CW, size=16, lead=1.36) + 22
    caps(P, M, y, "captures i didn't have (for aster)", size=14, col=DEEP, gap=2.4); y += 18
    for t in MISSING:
        P.disc(M + 2, y + 6, 9, fill=ORANGE, ring=None); y = para(P, M + 22, y, t, CW - 22, size=16, lead=1.3) + 4
    return done(P, y + 40)


if __name__ == '__main__':
    parts = [header(), today()] + [direction(d) for d in 'ABC'] + [pick()]
    H = sum(p.height for p in parts) + 4 * (len(parts) - 1); board = Image.new('RGB', (W, H), HAIR[:3]); y = 0
    for p in parts: board.paste(p, (0, y)); y += p.height + 4
    board.save(OUT + 'board.png'); print('board', board.size)
