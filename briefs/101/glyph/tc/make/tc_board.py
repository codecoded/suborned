"""The take-control board for Sky's phone (brief 101): 1000 px wide on the book's light page. The call; the two pictograms; the three states
(rest, hover, in control) and the way back over real frames at 2x; what it takes; what the lab view lacks; the coverage.
Glyph. Run after tc_frames.py (or tc_budget.py, which runs it): it reads the frames, crops and make/coverage.json."""
import os, sys, re, json
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from tc_frames import *
from tc_board_text import *

W, M = 1000, 40; CW = W - 2 * M


def sheet(h):
    P = Page(2, W, h); ImageDraw.Draw(P.img).rectangle((0, 0, W * 2, h * 2), fill=PAGE); return P


def done(P, y): return P.img.crop((0, 0, W * 2, round(y * 2))).resize((W, round(y)), Image.LANCZOS).convert('RGB')


def pic(P, im, x, y, w, r=8):
    h = round(im.height * w / im.width); s = P.s; big = im.convert('RGBA').resize((round(w * s), round(h * s)), Image.LANCZOS)
    m = Image.new('L', big.size, 0); ImageDraw.Draw(m).rounded_rectangle((0, 0, big.width - 1, big.height - 1), r * s, fill=255); big.putalpha(m)
    P.img.alpha_composite(big, (round(x * s), round(y * s))); P.rrect(x, y, w, h, r, line=HAIR, lw=1.2); return y + h


def para(P, x, y, t, w, size=17, col=INK, face_='Jost-Medium', lead=1.38):
    """Wrapped text, its first line's top at y; a figure keeps its unit. Returns the bottom."""
    t = re.sub(r'(\d) (%|s\b|px|m\b|fps)', lambda m_: m_.group(1) + ' ' + m_.group(2), t)
    L = size * lead; line, n = '', 0; y0 = y + L / 2
    for word in t.split(' '):
        trial = (line + ' ' + word).strip(' ')
        if P.tw(trial, size, face_) > w and line: P.text(x, y0 + n * L, line, size, col=col, face=face_); n += 1; line = word
        else: line = trial
    P.text(x, y0 + n * L, line, size, col=col, face=face_); return y0 + (n + 1) * L - L / 2


def head(P, y, cap, title, size=30):
    caps(P, M, y, cap, size=13, col=DEEP, gap=3); return para(P, M, y + 20, title, CW, size=size, face_='Jost-Bold', lead=1.18)


def bullets(P, y, items, size=16, col=INK, x=M, w=CW):
    for t in items: P.disc(x + 1, y + size * .55, 8, fill=DEEP, ring=None); y = para(P, x + 18, y, t, w - 18, size=size, col=col) + 5
    return y


def crop2x(name, box):
    """A 1x box of a mocked frame, from its 2x copy."""
    x0, y0, x1, y1 = box; return Image.open(SCRT + f'{name}-2x.png').convert('RGB').crop((x0 * 2, y0 * 2, x1 * 2, y1 * 2))


def caption(P, x, y, t, col=SOFT): P.text(x, y + 9, t, 13.5, col=col, face='Jost-SemiBold'); return y + 22


def header(cov):
    P = sheet(1400); y = 36; caps(P, M, y, CAP, size=13, col=DEEP, gap=3); P.text(M, y + 44, TITLE, 44, col=INK, face='Jost-Bold'); y += 78
    y = para(P, M, y, SUB, CW, size=15, col=SOFT) + 18
    box_y = y; y += 18; P.text(M + 22, y + 14, CALL, 26, col=DEEP, face='Jost-Bold'); y += 40
    d = {k: f"{cov[k]['after_pct'] - cov[k]['before_pct']:.2f}" for k in ('rest', 'hover', 'control')}
    y = bullets(P, y, [t.format(rest_d=d['rest'], hover_d=d['hover'], ctrl_d=d['control']) for t in WHY], size=16.5, x=M + 22, w=CW - 44)
    y = para(P, M + 22, y + 4, WHY_AFTER, CW - 44, size=15.5, col=SOFT) + 16
    P.rrect(M, box_y, CW, y - box_y, 14, line=DEEP, lw=2)
    return done(P, y + 14)


def icons():
    P = sheet(600); y = head(P, 16, 'the pictograms', ICONS_HEAD) + 12
    for k, (name, word, what) in enumerate(ICONS):
        x = M + k * (CW / 2)
        for d, dx in ((64, 0), (30, 76)):  # at 2x the built tab's size, and at 1x as the HUD draws it
            disc_shadow(P, x + dx, y + (64 - d) / 2, d, dy=2, a=60, blur=2.5); P.disc(x + dx, y + (64 - d) / 2, d, fill=BODY3, ring=PAGE, rw=2 * d / 30)
            s_ = d * .52; ticon(P, name, x + dx + (d - s_) / 2, y + (64 - d) / 2 + (d - s_) / 2, s_)
        P.text(x + 122, y + 16, word, 18, col=INK, face='Jost-Bold'); para(P, x + 122, y + 28, what, CW / 2 - 140, size=14, col=SOFT)
    y += 92; y = para(P, M, y, ICONS_NOTE, CW, size=14, col=SOFT) + 8
    return done(P, y)


def states(cov):
    P = sheet(3200); y = head(P, 16, 'the three states', REST_HEAD) + 8; y = para(P, M, y, REST, CW, size=16) + 10
    y = caption(P, M, y, f"route G, 3:28.6, the Scout selected · build {cov['rest']['build']}"); y = pic(P, Image.open(OUTT + 'rest.png'), M, y, CW) + 12
    caption(P, M, y, 'at rest, 2x', DEEP); caption(P, M + CW / 2 + 10, y, 'hovered, 2x', DEEP); y += 22
    pic(P, crop2x('rest', (0, 0, 330, 140)), M, y, CW / 2 - 10); y = pic(P, crop2x('hover', (0, 0, 330, 140)), M + CW / 2 + 10, y, CW / 2 - 10) + 14
    y = para(P, M, y, HOVER_HEAD, CW, size=24, face_='Jost-Bold') + 4; y = para(P, M, y, HOVER, CW, size=16) + 10
    y = caption(P, M, y, 'the pointer on the Tech: his own tab, his sidearm mark as built, 2x', DEEP)
    y = pic(P, Image.open(SCRT + 'hover-other-roster-2x.png').convert('RGB').crop((0, 60, 600, 260)), M, y, CW / 2 - 10) + 16
    return done(P, y)


def control(cov):
    P = sheet(3000); y = para(P, M, 16, CTRL_HEAD, CW, size=30, face_='Jost-Bold', lead=1.18) + 8; y = bullets(P, y, CTRL, size=16) + 6
    y = caption(P, M, y, f"third person on the Scout, walking the plaza to the Office, 0:01 · build {cov['control']['build']}"); y = pic(P, Image.open(OUTT + 'control.png'), M, y, CW) + 12
    caption(P, M, y, 'the way back beside her face, 2x', DEEP); caption(P, M + CW / 2 + 10, y, 'the eye over her ("?" here), 2x', DEEP); y += 22
    pic(P, crop2x('control', (0, 0, 330, 140)), M, y, CW / 2 - 10); y = pic(P, crop2x('control', (850, 572, 1070, 665)), M + CW / 2 + 10, y, CW / 2 - 10) + 12
    y = caption(P, M, y, "the eye's three states, keyed on the plaza, 2x", DEEP); y0 = y; y = pic(P, Image.open(SCRT + 'eyes-2x.png'), M, y, CW / 2 - 10)
    for k, t in enumerate(EYES): P.text(M + (CW / 2 - 10) * (50 + k * 100) / 300, y + 12, t, 13, col=SOFT, face='Jost-SemiBold', anchor='mm')
    y += 30
    y = para(P, M, y + 6, BACK_HEAD, CW, size=24, face_='Jost-Bold') + 4; y = para(P, M, y, BACK, CW, size=16) + 10
    caption(P, M, y, 'the capsule hovered, 2x', DEEP); caption(P, M + CW / 2 + 10, y, 'after the glide: the map, her tab again', DEEP); y += 22
    pic(P, crop2x('back', (0, 0, 330, 140)), M, y, CW / 2 - 10); y = pic(P, crop2x('rest', (0, 0, 330, 140)), M + CW / 2 + 10, y, CW / 2 - 10) + 8
    y = para(P, M, y, 'The right-hand picture is the map frame above: the glide lands centred on her, so the street behind will differ.', CW, size=13.5, col=SOFT) + 10
    return done(P, y)


def cost():
    P = sheet(1800); y = head(P, 16, 'cost', COST_HEAD) + 10
    for i, (what, files, size) in enumerate(COST):
        P.text(M, y + 12, f'{i + 1}', 19, col=DEEP, face='Jost-Bold'); P.text(W - M, y + 12, size, 17, col=ORANGE, face='Jost-Bold', anchor='rm')
        y = para(P, M + 28, y, what, CW - 70, size=15.5); y = para(P, M + 28, y, files, CW - 70, size=13, col=SOFT) + 8
    y = para(P, M, y + 2, 'Words: Fable\'s "Take control" and "Back to map" are already in text.json; the "Take control: T" pop retires. All under one day, with its proofs.', CW, size=14, col=SOFT) + 16
    y = para(P, M, y, LACKS_HEAD, CW, size=24, face_='Jost-Bold') + 6; y = bullets(P, y, LACKS, size=15)
    return done(P, y + 6)


def coverage(cov):
    P = sheet(600); y = head(P, 16, 'coverage', COV_HEAD) + 6; y = para(P, M, y, COV_NOTE, CW, size=14, col=SOFT) + 8
    cols = [(M, 'frame', 'l'), (M + 380, 'build', 'l'), (M + 600, 'today', 'r'), (M + 740, 'mocked', 'r'), (W - M, 'budget', 'r')]
    for x, t, a in cols: P.text(x, y + 10, t, 13.5, col=SOFT, face='Jost-SemiBold', anchor='lm' if a == 'l' else 'rm')
    y += 24; names = {'rest': 'at rest, on the map', 'hover': 'hovered (active)', 'control': 'in control (third person)', 'back': 'the way back hovered (active)'}
    for k in ('rest', 'hover', 'control', 'back'):
        v = cov[k]; row = [names[k], v['build'], f"{v['before_pct']:.2f}", f"{v['after_pct']:.2f}", 'pass']
        for (x, _, a), t in zip(cols, row): P.text(x, y + 11, t, 15, col=DEEP if t == 'pass' else INK, face='Jost-Medium' if a == 'l' else 'Jost-SemiBold', anchor='lm' if a == 'l' else 'rm')
        y += 24
    return done(P, y + 14)


def main():
    cov = json.load(open(OUTT + 'make/coverage.json'))['frames']
    parts = [header(cov), icons(), states(cov), control(cov), cost(), coverage(cov)]
    H = sum(p.height for p in parts) + 8 * len(parts); board = Image.new('RGB', (W, H), PAGE[:3]); y = 0; d = ImageDraw.Draw(board)
    for p in parts:
        board.paste(p, (0, y)); y += p.height; d.line([(M, y + 4), (W - M, y + 4)], fill=HAIR[:3], width=1); y += 8
    board.save(OUTT + 'board.png'); print('board', board.size)


if __name__ == '__main__':
    main()
