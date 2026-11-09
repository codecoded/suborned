"""The hover outline board for Sky's phone (brief 101): 1000 px wide on the book's light page. The pick; the three looks on Sky's case and where
two of them trip; the four cases in the pick (the guard in the door, one of the cell among the others, partly hidden, not a person) at play
size and close; what gets it and what it replaces; Latch's spec, route and cost; Sky's questions; how it was made.
Glyph. Run after ol_frames.py (it reads out/scratch/glyph/outline/frames/ and make/coverage.json)."""
import os, sys, re, json
from PIL import Image, ImageDraw
sys.path.insert(0, '/Users/sky/Projects/suborned/briefs/101/glyph/r3/make')
from r3_base import Page, caps, PAGE, INK, DEEP, SOFT, HAIR, ORANGE  # the book's page, Jost, the kit's ink
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from ol_board_text import *

R = '/Users/sky/Projects/suborned/'; OUT = R + 'briefs/101/glyph/outline/'; FR = R + 'out/scratch/glyph/outline/frames/'
W, M = 1000, 40; CW = W - 2 * M


def sheet(h): P = Page(2, W, h); ImageDraw.Draw(P.img).rectangle((0, 0, W * 2, h * 2), fill=PAGE); return P


def done(P, y): return P.img.crop((0, 0, W * 2, round(y * 2))).resize((W, round(y)), Image.LANCZOS).convert('RGB')


def img(name): return Image.open(FR + name + '.png').convert('RGB')


def pic(P, im, x, y, w, r=8):
    """A picture at width w (px on the board), rounded corners and a hairline. Returns its bottom."""
    h = round(im.height * w / im.width); s = P.s; big = im.convert('RGBA').resize((round(w * s), round(h * s)), Image.LANCZOS)
    m = Image.new('L', big.size, 0); ImageDraw.Draw(m).rounded_rectangle((0, 0, big.width - 1, big.height - 1), r * s, fill=255); big.putalpha(m)
    P.img.alpha_composite(big, (round(x * s), round(y * s))); P.rrect(x, y, w, h, r, line=HAIR, lw=1.2); return y + h


def para(P, x, y, t, w, size=16, col=INK, face_='Jost-Medium', lead=1.38):
    """Wrapped text, its first line's top at y; a figure keeps its unit. Returns the bottom."""
    t = re.sub(r'(\d) (%|s\b|px|m\b|ms\b)', lambda m_: m_.group(1) + ' ' + m_.group(2), t)
    L = size * lead; line, n = '', 0; y0 = y + L / 2
    for word in t.split(' '):
        trial = (line + ' ' + word).strip(' ')
        if P.tw(trial, size, face_) > w and line: P.text(x, y0 + n * L, line, size, col=col, face=face_); n += 1; line = word
        else: line = trial
    P.text(x, y0 + n * L, line, size, col=col, face=face_); return y0 + (n + 1) * L - L / 2


def head(P, y, cap, title, size=28):
    caps(P, M, y, cap, size=13, col=DEEP, gap=3); return para(P, M, y + 20, title, CW, size=size, face_='Jost-Bold', lead=1.18)


def bullets(P, y, items, size=15.5, col=INK, x=M, w=CW):
    for t in items: P.disc(x + 1, y + size * .55, 8, fill=DEEP, ring=None); y = para(P, x + 18, y, t, w - 18, size=size, col=col) + 5
    return y


def cap(P, x, y, t, w, col=SOFT): return para(P, x, y + 2, t, w, size=13.5, col=col, face_='Jost-SemiBold', lead=1.3) + 2


def row(P, y, items, gap=20, x0=M):
    """Pictures side by side, each (name, width, caption); captions under them. Returns the bottom."""
    x = x0; bottoms = []
    for name, w, c in items:
        b = pic(P, img(name), x, y, w); bottoms.append(cap(P, x, b + 4, c, w, DEEP) if c else b); x += w + gap
    return max(bottoms)


def header(cov):
    P = sheet(1400); y = 36; caps(P, M, y, CAP, size=13, col=DEEP, gap=3); y = para(P, M, y + 20, TITLE, CW, size=40, face_='Jost-Bold', lead=1.12) + 8
    y = para(P, M, y, SUB, CW, size=15, col=SOFT) + 18
    box_y = y; y += 18; P.text(M + 22, y + 14, CALL, 25, col=DEEP, face='Jost-Bold'); y += 40
    y = bullets(P, y, WHY, size=16, x=M + 22, w=CW - 44); y = para(P, M + 22, y + 4, WHY_AFTER, CW - 44, size=15, col=SOFT) + 16
    P.rrect(M, box_y, CW, y - box_y, 14, line=DEEP, lw=2); return done(P, y + 14)


def looks():
    P = sheet(2000); y = head(P, 16, 'three looks', LOOKS_HEAD) + 4; y = para(P, M, y, LOOKS_NOTE, CW, size=14.5, col=SOFT) + 10
    for st, name, lines in LOOKS:
        a, b = img(f'look-{st}-guard'), img(f'look-{st}-door'); w = 260; top = y
        bot = pic(P, a, M, y, w); pic(P, b, M + w + 8, y, w); tx = M + 2 * w + 26; tw = CW - (tx - M)
        P.text(tx, top + 16, st, 30, col=ORANGE if st == 'B' else DEEP, face='Jost-Bold'); ty = para(P, tx + 30, top + 2, name, tw - 30, size=20, face_='Jost-Bold') + 6
        for t in lines: ty = para(P, tx, ty, t, tw, size=14.5, col=INK) + 4
        ty += 8
        for t, c in zip(PROS[st], (DEEP, ORANGE)): ty = para(P, tx, ty, t, tw, size=14.5, col=c, face_='Jost-SemiBold') + 4
        y = bot + 16
    y = para(P, M, y + 4, TRIPS_HEAD, CW, size=22, face_='Jost-Bold') + 8
    wa = 250; top = y; ba = pic(P, img('trip-A-fixer'), M, y, wa); ca = cap(P, M, ba + 4, TRIPS[0], wa)
    wc = CW - wa - 20; bc = pic(P, img('trip-C-cell'), M + wa + 20, top, wc); cc = cap(P, M + wa + 20, bc + 4, TRIPS[1], wc)
    return done(P, max(ca, cc) + 10)


def case1():
    P = sheet(2200); y = head(P, 16, 'case 1 · sky\'s case', C1_HEAD) + 6; y = para(P, M, y, C1, CW, size=16) + 12
    y = row(P, y, [('c1-guard-1x', 450, C1_CAPS[0] + ', play zoom 10, 1x'), ('c1-door-1x', 450, C1_CAPS[1] + ', 1x')]) + 10
    y = row(P, y, [('c1-guard-3x', 450, 'close, 3x'), ('c1-door-3x', 450, 'close, 3x')]) + 8
    y = bullets(P, y, [C1_NOTE, C1_DOOR], size=15); y = para(P, M, y, C1_PILL, CW, size=13.5, col=SOFT) + 6
    return done(P, y + 6)


def case2to4():
    P = sheet(3600); y = head(P, 16, 'case 2', C2_HEAD) + 6; y = para(P, M, y, C2, CW, size=16) + 12
    y = row(P, y, [('c2-fixer-1080', 394, C2_CAPS[0] + ', 1080p size'), ('c2-tech-1080', 394, C2_CAPS[1])]) + 8
    y = row(P, y, [('c2-fixer-close', 220, 'on the Fixer, close'), ('c2-tech-close', 220, 'on the Tech, close'), ('c2-today-1080', 394, 'today, the pointer on the Fixer: nothing on her')]) + 18
    y = head(P, y, 'case 3', C3_HEAD) + 6; y = para(P, M, y, C3, CW, size=16) + 12
    y = row(P, y, [('c3-wrap', 300, C3_CAPS[0] + ', close'), ('c3-stop', 300, C3_CAPS[1] + ', close')]) + 4
    y = para(P, M, y, C3_PLAY, CW, size=13.5, col=SOFT) + 18
    y = head(P, y, 'case 4', C4_HEAD) + 6; y = para(P, M, y, C4, CW, size=16) + 12
    y = row(P, y, [('c4-mast-1080', 371, C4_CAPS[0]), ('c4-mast-close', 330, C4_CAPS[1])]) + 8
    y = row(P, y, [('c4-car-1x', 360, C4_CAPS[2] + ', 1x'), ('c4-car-2x', 488, C4_CAPS[3])]) + 6
    return done(P, y + 6)


def gets():
    P = sheet(1600); y = head(P, 16, 'rules', GETS_HEAD) + 10; y = bullets(P, y, GETS, size=15.5) + 8
    P.text(M, y + 10, 'today', 13.5, col=SOFT, face='Jost-SemiBold'); P.text(M + 560, y + 10, 'with the outline', 13.5, col=SOFT, face='Jost-SemiBold'); y += 24
    for what, then in REPL:
        P.line([(M, y), (W - M, y)], col=HAIR, lw=1); a = para(P, M, y + 5, what, 540, size=14.5); b = para(P, M + 560, y + 5, then, CW - 560, size=14.5, col=DEEP, face_='Jost-SemiBold')
        y = max(a, b) + 5
    return done(P, y + 10)


def latch():
    P = sheet(2000); y = head(P, 16, 'build', LATCH_HEAD) + 10
    for k, v in SPEC:
        P.line([(M, y), (W - M, y)], col=HAIR, lw=1); P.text(M, y + 15, k, 15, col=DEEP, face='Jost-Bold'); y = para(P, M + 90, y + 5, v, CW - 90, size=14.5) + 6
    y += 6
    for t in (ROUTE, COST, PROOF): y = para(P, M, y, t, CW, size=15) + 8
    P.text(W - M, y + 10, SIZE, 16, col=ORANGE, face='Jost-Bold', anchor='rm'); return done(P, y + 26)


def sky():
    P = sheet(1200); y = head(P, 16, 'decide', SKY_HEAD) + 10
    for i, (q, t) in enumerate(ASKS):
        P.text(M, y + 12, f'{i + 1}', 19, col=ORANGE, face='Jost-Bold'); y = para(P, M + 28, y, q, CW - 28, size=17, face_='Jost-Bold') + 2
        y = para(P, M + 28, y, t, CW - 28, size=15) + 10
        if i == 1: y = pic(P, img('w-compare'), M + 28, y, img('w-compare').width) + 4; y = cap(P, M + 28, y, W_CAPS, CW - 28, DEEP) + 10
    return done(P, y + 4)


def how(cov):
    v = cov['share']; lo = min(v.values()); hi = max(v.values())
    P = sheet(900); y = head(P, 16, 'evidence', HOW_HEAD, size=22) + 8
    y = bullets(P, y, [t.format(lo=f'{lo:.2f}', hi=f'{hi:.2f}') for t in HOW], size=14, col=SOFT); return done(P, y + 6)


def main():
    cov = json.load(open(OUT + 'make/coverage.json'))
    parts = [header(cov), looks(), case1(), case2to4(), gets(), latch(), sky(), how(cov)]
    H = sum(p.height for p in parts) + 8 * len(parts); board = Image.new('RGB', (W, H), PAGE[:3]); y = 0; d = ImageDraw.Draw(board)
    for p in parts:
        board.paste(p, (0, y)); y += p.height; d.line([(M, y + 4), (W - M, y + 4)], fill=HAIR[:3], width=1); y += 8
    board.save(OUT + 'board.png'); print('board', board.size)


if __name__ == '__main__':
    main()
