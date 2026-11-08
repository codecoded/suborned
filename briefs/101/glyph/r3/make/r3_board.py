"""The round 3 board for Sky's phone (brief 101): 1000 px wide, the book's light page. The five rules; what's still in the old kit; three moments
before and after at 2x; every other piece; Inez's card; the next event's limit (for Splice); the news line; the order for Latch; the coverage.
Glyph. Run after r3_frames.py and r3_kit.py (which render the pictures and the measures)."""
import os, sys, re
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from r3_base import *
from r3_board_text import *

W, M = 1000, 40; CW = W - 2 * M
COV = json.load(open(OUT3 + 'make/coverage.json')); KITC = json.load(open(OUT3 + 'make/coverage-kit.json'))
SRC = {k: SHOTS[k][0] for k in ('quiet', 'window', 'card', 'conv', 'ring', 'next')}


def sheet(h):
    P = Page(2, W, h); ImageDraw.Draw(P.img).rectangle((0, 0, W * 2, h * 2), fill=PAGE); return P


def done(P, y): return P.img.crop((0, 0, W * 2, round(y * 2))).resize((W, round(y)), Image.LANCZOS).convert('RGB')


def pic(P, im, x, y, w, r=8):
    h = round(im.height * w / im.width); s = P.s; big = im.convert('RGBA').resize((round(w * s), round(h * s)), Image.LANCZOS)
    m = Image.new('L', big.size, 0); ImageDraw.Draw(m).rounded_rectangle((0, 0, big.width - 1, big.height - 1), r * s, fill=255); big.putalpha(m)
    P.img.alpha_composite(big, (round(x * s), round(y * s))); P.rrect(x, y, w, h, r, line=HAIR, lw=1.2); return y + h


def para(P, x, y, t, w, size=19, col=INK, face_='Jost-Medium', lead=1.38):
    """Wrapped text, its first line's top at y; a figure keeps its unit. Returns the bottom."""
    t = re.sub(r'(\d) (%|s\b|px|ch)', lambda m_: m_.group(1) + ' ' + m_.group(2), t)
    L = size * lead; line, n = '', 0; y0 = y + L / 2
    for word in t.split(' '):
        trial = (line + ' ' + word).strip(' ')
        if P.tw(trial, size, face_) > w and line: P.text(x, y0 + n * L, line, size, col=col, face=face_); n += 1; line = word
        else: line = trial
    P.text(x, y0 + n * L, line, size, col=col, face=face_); return y0 + (n + 1) * L - L / 2


def head(P, y, cap, title, size=34):
    caps(P, M, y, cap, size=14, col=DEEP, gap=3); return para(P, M, y + 22, title, CW, size=size, face_='Jost-Bold', lead=1.18)


def frame2x(key, after):
    return Image.open(SCR3 + f'{key}-2x.png').convert('RGB') if after else Image.open(SRC[key] + '.png').convert('RGB').resize((3840, 2160), Image.LANCZOS)


def pair(P, y, key, box, label=True):
    """Before | after of one crop at 2x, side by side, each 460 wide. Returns the bottom."""
    x0, y0, x1, y1 = box; a = frame2x(key, False).crop((x0 * 2, y0 * 2, x1 * 2, y1 * 2)); b = frame2x(key, True).crop((x0 * 2, y0 * 2, x1 * 2, y1 * 2))
    if label: P.text(M, y + 10, 'today', 14, col=SOFT, face='Jost-SemiBold'); P.text(M + 480, y + 10, 'in the built kit', 14, col=DEEP, face='Jost-SemiBold'); y += 24
    h1 = pic(P, a, M, y, 460); pic(P, b, M + 480, y, 460); return h1


def cov_line(P, y, key):
    f = COV['frames'][key]; return para(P, M, y + 6, f"{f['moment']} · build {f['build']} · HUD {f['before_pct']} % of the screen today, {f['after_pct']} % in the kit", CW, size=14.5, col=SOFT) + 4


def header():
    P = sheet(2200); y = 40; caps(P, M, y, CAP, size=14, col=DEEP, gap=3); P.put(M, y + 50, TITLE, 46, col=INK, face='Jost-Bold'); y += 86
    y = para(P, M, y, SUB, CW, size=16, col=SOFT) + 18
    for i, (r, d) in enumerate(RULES):
        P.disc(M, y + 2, 30, fill=DEEP, ring=None); P.text(M + 15, y + 17, str(i + 1), 16, col=PAGE, face='Jost-Bold', anchor='mm')
        y = para(P, M + 44, y, r, CW - 44, size=19, face_='Jost-Bold'); y = para(P, M + 44, y + 1, d, CW - 44, size=16, col=SOFT) + 12
    y = head(P, y + 14, 'the inventory', "What's still in the old kit") + 6; y = para(P, M, y, INVENTORY, CW, size=16, col=SOFT) + 8
    for name, n, to in FAMILIES:
        P.text(M, y + 13, name, 16, col=INK, face='Jost-Medium'); P.text(M + 520, y + 13, str(n), 16, col=SOFT, face='Jost-SemiBold', anchor='rm')
        P.text(M + 545, y + 13, '→ ' + to, 16, col=DEEP, face='Jost-SemiBold'); y += 27
    y = para(P, M, y + 8, ALREADY, CW, size=15, col=SOFT) + 10
    return done(P, y)


def moments():
    P = sheet(3200); y = head(P, 24, 'three moments', 'The quiet run, a window, the act ring') + 12
    for key, boxes in (('quiet', [(400, 70, 700, 270), (850, 175, 1110, 385)]), ('window', [(820, 205, 1100, 335)]), ('ring', [(850, 360, 1240, 615)])):
        y = cov_line(P, y, key)
        for i, b in enumerate(boxes): y = pair(P, y, key, b, label=i == 0) + 12
        y += 14
    return done(P, y)


def kit():
    """Every other piece: before above after, three to a row, with its screen share."""
    seen = set(); sel = []
    for r in KITC:  # one cell a piece (the second queued walk and the 1:45 window say nothing new)
        if r['piece'] in seen or r['piece'] == '1:45': continue
        seen.add(r['piece']); sel.append(r)
    P = sheet(4000); y = head(P, 24, 'every other piece', 'Pops, windows, attention, chips, edges, ghosts') + 10
    cw, gap = 300, 10; col = 0; rowh = 0; x0 = M
    for r in sel:
        a = Image.open(SCR3 + f"pairs/{r['n']:02d}-before.png"); b = Image.open(SCR3 + f"pairs/{r['n']:02d}-after.png"); x = x0 + col * (cw + gap)
        hmax = round(a.width * .5); t0 = max(0, (a.height - hmax) // 2); a, b = a.crop((0, t0, a.width, min(a.height, t0 + hmax))), b.crop((0, t0, b.width, min(b.height, t0 + hmax)))  # a strip centred on the piece
        name = r['piece'].split(' (')[0]; P.text(x, y + 9, name, 14.5, col=INK, face='Jost-Bold')
        o = f"{r['old_px'] / 20736:.2f}" if r['old_px'] else 'none today'; nn = f"{r['new_px'] / 20736:.2f}"
        P.text(x, y + 28, f'{o} → {nn} % of the screen', 13, col=SOFT, face='Jost-Medium')
        yy = pic(P, a, x, y + 40, cw, r=6) + 4; yy = pic(P, b, x, yy, cw, r=6); rowh = max(rowh, yy - y)
        col += 1
        if col == 3: col = 0; y += rowh + 16; rowh = 0
    if col: y += rowh + 16
    y = para(P, M, y, "Each pair: today above, the built kit below, at 2x. 'Report stopped' and the last 10 s are drawn on real frames where the game showed something else at that moment.", CW, size=14, col=SOFT) + 10
    return done(P, y)


def inez():
    P = sheet(2600); y = head(P, 24, "inez's card", CARD_HEAD) + 8; y = para(P, M, y, CARD_WHY, CW, size=16.5) + 10
    y = cov_line(P, y, 'conv'); y = pair(P, y, 'conv', (700, 170, 1220, 430)) + 16
    y = cov_line(P, y, 'card'); P.text(M, y + 10, 'today', 14, col=SOFT, face='Jost-SemiBold'); P.text(M + 480, y + 10, 'in the built kit', 14, col=DEEP, face='Jost-SemiBold'); y += 24
    pic(P, frame2x('card', False), M, y, 460); y = pic(P, frame2x('card', True), M + 480, y, 460) + 10
    y = pair(P, y, 'card', (1720, 95, 1920, 165), label=False) + 10
    pc = next(r for r in COV['pieces'] if r['piece'].startswith('person card'))
    y = para(P, M, y, CARD_NUMS.format(cb=COV['frames']['card']['before_pct'], ca=COV['frames']['card']['after_pct'], po=f"{pc['old_pct']:.2f}", pn=f"{pc['new_pct']:.2f}"), CW, size=15, col=SOFT) + 12
    return done(P, y)


def next_rows():
    """The seven timeline names as the built row draws them, on the plaza, with their sizes; the real row of 0.2.286 at 2x."""
    tl = json.load(open(R + 'contracts/missions/handover/text.json'))['briefing']['timeline']
    P = sheet(2600); y = head(P, 24, 'for splice', NEXT_HEAD) + 8; y = para(P, M, y, NEXT_WHY, CW, size=16) + 10
    real = Image.open(SRC['next'] + '.png').convert('RGB').crop((1700, 50, 1920, 170)).resize((440, 240), Image.LANCZOS); P.text(M, y + 10, "today (0.2.286, 5:21): over the marker's 2-word cap", 14, col=SOFT, face='Jost-SemiBold')
    y = pic(P, real, M, y + 24, 300) + 12
    ground = lift('card', [(1476, 748, 1912, 954)])[0].crop((1480, 760, 1900, 940)).resize((840, 360), Image.LANCZOS)
    Q = layer(); ry = 18
    for t in tl:
        w = next_row(Q, 400, ry, t['short'], t['at']); ry += 30
    strip = ground.convert('RGBA'); lab = Q.img.crop((0, 0, 840, 2 * (ry + 4))).resize((420, ry + 4), Image.LANCZOS)
    base = ground.resize((420, ry + 4)).convert('RGBA'); base.alpha_composite(lab); y0 = y; pic(P, base.resize((840, 2 * (ry + 4)), Image.LANCZOS), M, y, 400)
    fs = font('Jost-SemiBold', 30)
    for i, t in enumerate(tl):
        nm = t['short']; P.text(M + 420, y0 + 18 * 400 / 420 + i * 30 * 400 / 420, f"{len(nm)} characters · {len(nm.split())} word{'s' if len(nm.split()) > 1 else ''}", 14, col=SOFT, face='Jost-Medium')
    y = y0 + (ry + 4) * 400 / 420 + 14
    for t in NEXT_LIMIT: P.disc(M + 2, y + 8, 8, fill=DEEP, ring=None); y = para(P, M + 18, y, t, CW - 18, size=17, face_='Jost-SemiBold') + 4
    y = para(P, M, y + 6, NEXT_REASON, CW, size=15.5, col=SOFT) + 10
    P.text(M, y + 10, 'Splice implements:', 16, col=DEEP, face='Jost-Bold'); y += 26
    for t in SPLICE: P.disc(M + 2, y + 8, 7, fill=INK, ring=None); y = para(P, M + 18, y, t, CW - 18, size=14.5) + 4
    return done(P, y + 8)


def news_line():
    P = sheet(1600); y = head(P, 24, 'the why line', LINE_HEAD) + 8; y = para(P, M, y, LINE_WHY, CW, size=16) + 10
    y = pair(P, y, 'card', (1400, 990, 1920, 1080)) + 10
    y = para(P, M, y, LINE_FABLE, CW, size=15, col=SOFT) + 10
    return done(P, y)


def order():
    P = sheet(2400); y = head(P, 24, 'for latch', LATCH_HEAD) + 10
    for i, (what, files, size) in enumerate(LATCH):
        P.text(M, y + 12, f'{i + 1}', 20, col=DEEP, face='Jost-Bold'); P.text(W - M, y + 12, size, 18, col=ORANGE, face='Jost-Bold', anchor='rm')
        y = para(P, M + 30, y, what, CW - 70, size=16.5); y = para(P, M + 30, y, files, CW - 70, size=13.5, col=SOFT) + 9
    y = para(P, M, y + 4, LATCH_NOTE, CW, size=15, col=SOFT) + 14
    P.text(M, y + 10, 'Open', 17, col=DEEP, face='Jost-Bold'); y += 26
    for t in OPEN: P.disc(M + 2, y + 8, 7, fill=INK, ring=None); y = para(P, M + 18, y, t, CW - 18, size=15) + 4
    return done(P, y + 10)


def table():
    """The coverage of each piece, one row a piece, in two columns: today's HUD layer inside its box and the new piece alone (alpha >= 24 at 1080,
    as tools/hud-budget reads it). 'Report stopped' has no capture of today's plate: no like-for-like figure."""
    rows, seen = [], set()
    for name, o, n in [(r['piece'], r['old_pct'], r['new_pct']) for r in COV['pieces']] + [(r['piece'].split(' (')[0], (r['old_px'] or 0) / 20736 if r['piece'] != 'Report stopped (on the reporter, illustrative)' else 0, r['new_px'] / 20736) for r in KITC]:
        if name in seen or name in ('the last 10 s', 'a loop the Tech holds'): continue
        seen.add(name); rows.append((name, o, n))
    like = [r for r in rows if r[1] > 0]; less = sum(1 for r in like if r[2] < r[1]); grow = [r[0].replace('door: ', 'the ').replace(' (the driver)', '') for r in like if r[2] >= r[1]]
    gs = ', '.join(grow[:-1]) + ' and ' + grow[-1] if len(grow) > 1 else ''.join(grow); gs = gs[:1].upper() + gs[1:]
    P = sheet(400 + 12 * len(rows)); y = head(P, 24, 'coverage', 'Each piece, today and in the kit') + 6
    y = para(P, M, y, f"% of the 1920 × 1080 screen, read as tools/hud-budget reads a capture: today | in the kit. {less} of the {len(like)} pieces measured like for like come out smaller. "
            + gs + ' grow by a hair: the built tab and ring carry a soft shadow. The cream rim lies where the chrome one was.', CW, size=14.5, col=SOFT) + 8
    half = (len(rows) + 1) // 2; cx = (M, M + CW / 2 + 10); cw = CW / 2 - 10
    for i, (name, o, n) in enumerate(rows):
        x = cx[i // half]; yy = y + (i % half) * 21
        P.text(x, yy + 10, name[:30], 13.5, col=INK, face='Jost-Medium'); P.text(x + cw - 62, yy + 10, f'{o:.2f}' if o else '–', 13.5, col=SOFT, face='Jost-SemiBold', anchor='rm')
        P.text(x + cw - 4, yy + 10, f'{n:.2f}', 13.5, col=DEEP if (not o or n <= o) else ORANGE, face='Jost-Bold', anchor='rm')
    return done(P, y + half * 21 + 14)


def main():
    parts = [header(), moments(), kit(), inez(), next_rows(), news_line(), order(), table()]
    H = sum(p.height for p in parts) + 8 * len(parts); board = Image.new('RGB', (W, H), PAGE[:3]); y = 0; d = ImageDraw.Draw(board)
    for p in parts:
        board.paste(p, (0, y)); y += p.height; d.line([(M, y + 4), (W - M, y + 4)], fill=HAIR[:3], width=1); y += 8
    board.save(OUT3 + 'board.png'); print('board', board.size)


if __name__ == '__main__':
    main()
