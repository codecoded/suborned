"""The round 4 board for Sky's phone (brief 101): 1000 px wide, the book's light page. Sky's pick first (the wrap, A over B at the same scale), then
item 9 (the docked line), item 10 (the last chrome), and the coverage of every frame from tools/hud-budget. Run after r4_budget.py (which renders the
frames and reads them). Glyph."""
import os, sys, re
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from r4_base import *
from r4_board_text import *

W, M = 1000, 40; CW = W - 2 * M
COV = json.load(open(OUT4 + 'make/coverage.json')); BX = json.load(open(OUT4 + 'make/boxes.json'))


def sheet(h):
    P = Page(2, W, h); ImageDraw.Draw(P.img).rectangle((0, 0, W * 2, h * 2), fill=PAGE); return P


def done(P, y): return P.img.crop((0, 0, W * 2, round(y * 2))).resize((W, round(y)), Image.LANCZOS).convert('RGB')


def pic(P, im, x, y, w, r=8):
    h = round(im.height * w / im.width); s = P.s; big = im.convert('RGBA').resize((round(w * s), round(h * s)), Image.LANCZOS)
    m = Image.new('L', big.size, 0); ImageDraw.Draw(m).rounded_rectangle((0, 0, big.width - 1, big.height - 1), r * s, fill=255); big.putalpha(m)
    P.img.alpha_composite(big, (round(x * s), round(y * s))); P.rrect(x, y, w, h, r, line=HAIR, lw=1.2); return y + h


def para(P, x, y, t, w, size=19, col=INK, face_='Jost-Medium', lead=1.38):
    """Wrapped text, its first line's top at y; a figure keeps its unit. Returns the bottom."""
    t = re.sub(r'(\d) (%|s\b|px)', lambda m_: m_.group(1) + ' ' + m_.group(2), t)
    L = size * lead; line, n = '', 0; y0 = y + L / 2
    for word in t.split(' '):
        trial = (line + ' ' + word).strip(' ')
        if P.tw(trial, size, face_) > w and line: P.text(x, y0 + n * L, line, size, col=col, face=face_); n += 1; line = word
        else: line = trial
    P.text(x, y0 + n * L, line, size, col=col, face=face_); return y0 + (n + 1) * L - L / 2


def head(P, y, cap, title, size=32):
    caps(P, M, y, cap, size=14, col=DEEP, gap=3); return para(P, M, y + 22, title, CW, size=size, face_='Jost-Bold', lead=1.18)


def bullets(P, y, items, size=16, col=INK):
    for t in items: P.disc(M + 2, y + size * .55, 7, fill=DEEP, ring=None); y = para(P, M + 18, y, t, CW - 18, size=size, col=col) + 5
    return y


def two_x(name): return Image.open(SCR4 + f'{name}-2x.png').convert('RGB')


def real_2x(key): return Image.open(SHOTS[key][0] + '.png').convert('RGB').resize((3840, 2160), Image.LANCZOS)


def crop2(im, box): x0, y0, x1, y1 = box; return im.crop((x0 * 2, y0 * 2, x1 * 2, y1 * 2))


def labelled(P, y, im, label, figs, col=INK, w=CW):
    """A crop with its label row over it: the option's name (bold) and its figures (soft), right-aligned."""
    P.text(M, y + 11, label, 17, col=col, face='Jost-Bold'); P.text(M + w, y + 11, figs, 14.5, col=SOFT, face='Jost-SemiBold', anchor='rm')
    return pic(P, im, M, y + 24, w) + 12


def pct(name, side='mock'): return COV[side][name]['pct']


def wh(name, piece='bubble'): x0, y0, x1, y1 = BX[name][piece]; return f'{round(x1 - x0)} × {round(y1 - y0)} px'


def header():
    P = sheet(400); y = 40; caps(P, M, y, CAP, size=14, col=DEEP, gap=3); P.put(M, y + 50, TITLE, 44, col=INK, face='Jost-Bold'); y += 84
    return done(P, para(P, M, y, SUB, CW, size=16, col=SOFT) + 16)


def wrap():
    t = json.load(open(R + 'contracts/missions/handover/text.json')); Q = layer()
    conv = [b['line'] for c in t['scenes']['conversations']['items'] for b in c['beats']] + [b['line'] for c in t['scenes']['conversations']['items'] for st in c.get('street', []) or [] for b in st.get('beats', [])]
    barks = [l for e in t.get('dialogue', []) for l in e.get('lines', [])]
    two = sum(1 for s in conv if len(rows_for(Q, s, WRAP)[0]) == 2); bark2 = sum(1 for s in barks if len(rows_for(Q, s, WRAP)[0]) == 2)
    off = lambda n: round(abs((BX[n]['bubble'][0] + BX[n]['bubble'][2]) / 2 - 1825))
    P = sheet(3000); y = head(P, 24, 'for sky · a design change', WRAP_HEAD) + 8; y = para(P, M, y, WRAP_WHY, CW, size=16.5) + 14
    P.text(M, y + 10, "r123's card frame, 4:27: the Fixer's 21-word line of the Help", 15.5, col=DEEP, face='Jost-Bold'); y += 26
    box = (440, 395, 1220, 560)
    y = labelled(P, y, crop2(two_x('line-one'), box), WRAP_A, f"{wh('line-one')} · HUD {pct('line-one')} % of the screen")
    y = labelled(P, y, crop2(two_x('line-wrap'), box), WRAP_B, f"{wh('line-wrap')} · HUD {pct('line-wrap')} %", col=DEEP) + 10
    P.text(M, y + 10, "Route A's call, 2:59: the Fixer's 17-word line", 15.5, col=DEEP, face='Jost-Bold'); y += 22; y = para(P, M, y, CALL_NOTE, CW, size=14.5, col=SOFT) + 6
    box = (1240, 900, 1920, 1080)
    y = labelled(P, y, crop2(two_x('call-one'), box), WRAP_A_CLAMPED, f"{wh('call-one')} · HUD {pct('call-one')} %")
    y = labelled(P, y, crop2(two_x('call-wrap'), box), WRAP_B, f"{wh('call-wrap')} · HUD {pct('call-wrap')} %", col=DEEP) + 6
    pts = [p.format(a_off=off('call-one'), b_off=off('call-wrap'), two=two, all=len(conv), bark2=bark2, barks=len(barks)) for p in WRAP_POINTS]
    y = bullets(P, y, pts, size=16) + 6
    P.rrect(M, y, CW, 50, 12, fill=(0xe4, 0xef, 0xec, 255)); y = para(P, M + 16, y + 12, WRAP_PICK, CW - 32, size=17, face_='Jost-Bold', col=DEEP) + 18
    P.text(M, y + 10, 'The whole frames, B under A', 14, col=SOFT, face='Jost-SemiBold'); y += 24
    pic(P, two_x('line-one').resize((1920, 1080), Image.LANCZOS), M, y, 455); y2 = pic(P, two_x('call-one').resize((1920, 1080), Image.LANCZOS), M + 465, y, 455) + 8
    pic(P, two_x('line-wrap').resize((1920, 1080), Image.LANCZOS), M, y2, 455); y = pic(P, two_x('call-wrap').resize((1920, 1080), Image.LANCZOS), M + 465, y2, 455) + 14
    return done(P, y)


def docked():
    P = sheet(2600); y = head(P, 24, 'item 9 · for latch and splice', DOCK_HEAD) + 8; y = para(P, M, y, DOCK_WHY, CW, size=16.5) + 12
    box = (600, 0, 1200, 96)
    y = labelled(P, y, crop2(real_2x('fight'), box), 'today', f"three lines on screen · HUD {COV['today']['fight']['pct']} %", col=SOFT)
    y = labelled(P, y, crop2(two_x('fight'), box), 'round 4', f"two lines · HUD {pct('fight')} %", col=DEEP) + 4
    P.text(M, y + 10, "The whole frame at 4:15: today | round 4, where the Fixer's older line has given way", 14, col=SOFT, face='Jost-SemiBold'); y += 24
    pic(P, real_2x('fight').resize((1920, 1080), Image.LANCZOS), M, y, 455); y = pic(P, two_x('fight').resize((1920, 1080), Image.LANCZOS), M + 465, y, 455) + 14
    for name, t in DOCK_RULES:
        P.text(M, y + 11, name, 17, col=DEEP, face='Jost-Bold'); y = para(P, M + 170, y, t, CW - 170, size=16) + 8
    y = para(P, M, y + 2, DOCK_ORDER, CW, size=14.5, col=SOFT) + 14
    y = para(P, M, y, INSET_NOTE, CW, size=15.5) + 6; box = (0, 830, 500, 940)
    a = crop2(real_2x('inset'), box); b = crop2(two_x('inset'), box)
    P.text(M, y + 10, f"today · HUD {COV['today']['inset']['pct']} %", 14, col=SOFT, face='Jost-SemiBold'); P.text(M + 465, y + 10, f"round 4 · HUD {pct('inset')} %", 14, col=DEEP, face='Jost-SemiBold'); y += 22
    pic(P, a, M, y, 455); y = pic(P, b, M + 465, y, 455) + 14
    return done(P, y)


def chrome():
    P = sheet(3400); y = head(P, 24, 'item 10 · for latch', CHROME_HEAD) + 8; y = para(P, M, y, CHROME_WHY, CW, size=16.5) + 12
    for key, name, box, what in (('plate', 'plate', (0, 255, 420, 400), 'The Action plate on, Knock out armed (r29 proof)'),
                                 ('seed', 'seed', (0, 255, 420, 365), 'The Action plate on, Alt held, nothing armed (seed proof)'),
                                 ('strip', 'strip', (0, 0, 420, 100), "The strip after Shift: the Scout's Watch, Listen and Mislead (hold proof)")):
        P.text(M, y + 10, what, 15.5, col=DEEP, face='Jost-Bold'); y += 22
        P.text(M, y + 10, f"today · HUD {COV['today'][key]['pct']} %", 14, col=SOFT, face='Jost-SemiBold'); P.text(M + 465, y + 10, f"round 4 · HUD {pct(name)} %", 14, col=DEEP, face='Jost-SemiBold'); y += 22
        pic(P, crop2(real_2x(key), box), M, y, 455); y = pic(P, crop2(two_x(name), box), M + 465, y, 455) + 14
    y = para(P, M, y, 'The cursor (r29): the armed verb rides it as its keyed pictogram.', CW, size=15) + 4
    box = (690, 410, 800, 480); pic(P, crop2(real_2x('plate'), box), M, y, 200); y = pic(P, crop2(two_x('plate'), box), M + 220, y, 200) + 14
    P.text(M, y + 10, "The row's states", 17, col=DEEP, face='Jost-Bold'); y += 24
    for i in range(0, 5, 2):
        for j in (0, 1):
            if i + j > 4: continue
            x = M + j * 465; P.text(x, y + 9, STATES[i + j], 13.5, col=SOFT, face='Jost-SemiBold')
            yy = pic(P, Image.open(SCR4 + f'state-{i + j}.png'), x, y + 20, 455)
        y = yy + 12
    y = bullets(P, y + 4, CHROME_POINTS, size=16) + 6
    return done(P, y)


def table():
    rows = [('4:15, three lines to two (item 9)', 'fight', 'fight'), ("2:39, Inez's docked line (item 9)", 'inset', 'inset'),
            ('the Action plate, Knock out armed', 'plate', 'plate'), ('the Action plate, Alt held', 'seed', 'seed'), ('the strip after Shift', 'strip', 'strip'),
            ('4:27, the long line: A one row', 'line', 'line-one'), ('4:27, the long line: B two rows', 'line', 'line-wrap'),
            ("2:59, the call's line: A one row", 'call', 'call-one'), ("2:59, the call's line: B two rows", 'call', 'call-wrap')]
    P = sheet(900); y = head(P, 24, 'coverage', COV_HEAD) + 6; y = para(P, M, y, COV_NOTE, CW, size=14.5, col=SOFT) + 10
    P.text(M + 600, y + 10, 'today', 14, col=SOFT, face='Jost-SemiBold', anchor='rm'); P.text(M + 720, y + 10, 'round 4', 14, col=DEEP, face='Jost-SemiBold', anchor='rm')
    P.text(M + CW, y + 10, 'lines', 14, col=SOFT, face='Jost-SemiBold', anchor='rm'); y += 24
    for label, tk, mk in rows:
        t, m = COV['today'][tk], COV['mock'][mk]; base = COV['mock'][mk.replace('-wrap', '-one')]['pct'] if mk.endswith('-wrap') else t['pct']
        d = None if mk.endswith('-one') else m['pct'] - base  # the wrap's B against its A (today's 2:59 ran off the screen), the rest against today
        P.text(M, y + 11, label, 15.5, col=INK, face='Jost-Medium'); P.text(M + 600, y + 11, f"{t['pct']:.2f} %", 15.5, col=SOFT, face='Jost-SemiBold', anchor='rm')
        P.text(M + 720, y + 11, f"{m['pct']:.2f} %", 15.5, col=INK if d is None else DEEP if d <= 0 else ORANGE, face='Jost-Bold', anchor='rm')
        P.text(M + CW, y + 11, f"{COV['speech_today'][tk]} to {COV['speech'][mk]}", 15.5, col=SOFT, face='Jost-SemiBold', anchor='rm'); y += 27
    y = para(P, M, y + 6, ("Today's 2:59 reads 4.53 % because its line ran off the screen: clamped on screen, A is 4.89 %. Today's 4:27 is the build's own "
                           "line at 4.51 %; my redraw of the same row reads 4.58 %, so the drawing adds about 0.07. B's rows are coloured against A."), CW, size=14, col=SOFT) + 12
    return done(P, y)


def main():
    parts = [header(), wrap(), docked(), chrome(), table()]
    H = sum(p.height for p in parts) + 8 * len(parts); board = Image.new('RGB', (W, H), PAGE[:3]); y = 0; d = ImageDraw.Draw(board)
    for p in parts:
        board.paste(p, (0, y)); y += p.height; d.line([(M, y + 4), (W - M, y + 4)], fill=HAIR[:3], width=1); y += 8
    board.save(OUT4 + 'board.png'); print('board', board.size)


if __name__ == '__main__':
    main()
