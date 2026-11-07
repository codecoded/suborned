"""Pieces the concepts share, drawn in the light page's look: the head, the start page's conditions answered, the next-time box, number
tiles, the strip along the foot, the city's rows and the Trail fold. Glyph, brief 099. Mock only."""
from d_base import *
from d_reasons import stats, trail_by_what, trail_by_who, cell_line, city_lines, signed, usd
from d_data import clock

X0, X1 = 1010, 1745   # the book's text column (look4 L, as built)


def head(P, M, x=X0, w=X1 - X0, sizes=(72, 64, 56, 50), line_size=27, y0=48):
    caps(P, x, y0, NEW['caps.' + M['kind']].replace('{end}', M['end']), 19, col=ORANGE if M['kind'] == 'fail' else DEEP)
    s = fit(P, M['head'], w, sizes); P.put(x, y0 + 14 + s * .56, M['head'], s, col=INK, face='Jost-Bold'); y = y0 + 14 + s * 1.12 + 14
    return put_wrap(P, x, y + line_size * .5, M['line'], w, line_size, col=SOFT, maxl=2) + 4


def conds(P, M, x, y, w, size=22, row=40, head_=True):
    """The start page's own rows for the way taken, answered: when each came true and how; the one that lost it in orange."""
    way = M['way']; sign_word = NEW['way.front'] if way == 'front-door' else NEW['way.pickup']
    if head_:
        P.disc(x, y - 17, 34, fill=DEEP, ring=None); P.icon('enter' if way == 'front-door' else 'car', x + 6, y - 11, 22, col=CREAM)
        P.put(x + 46, y, sign_word, 28, col=INK, face='Jost-Bold'); P.put(x + 52 + P.tw(sign_word, 28), y + 2, NEW['way.yours'], 20, col=SOFT); y += 44
    hot_done = False
    for cid, ok, t, how, words in M['conds']:
        hot = not ok and not hot_done and M['kind'] == 'fail' and cid in ('has-vale', 'getaway'); hot_done |= hot
        check(P, x + 2, y - 14, ok, d=28, hot=hot)
        if hot: how, t = M['head'].lower().replace('their', 'their'), M['end']
        note = ' · '.join(z for z in (how, t if (ok or hot or how) else NEW['how.never']) if z)
        nw = P.tw(note, size - 2, 'Jost-Medium'); ww = w - 44 - nw - 18
        sz = fit(P, words, ww, (size, size - 2, size - 3.5), 'Jost-SemiBold')
        P.put(x + 42, y, words, sz, col=INK if ok or hot else SOFT, face='Jost-SemiBold')
        P.text(x + w, y, note, size - 2, col=ORANGE if hot else SOFT, face='Jost-SemiBold' if hot else 'Jost-Medium', anchor='rm'); P.words_add(note)
        y += row
    return y


def next_box(P, M, x, y, w):
    """A fail's one way on in plain words: what to do differently, and the cell's own ways that do it (faces, never orders)."""
    caps(P, x, y, NEW['next.head'], 17, col=ORANGE); y += 30
    y = put_wrap(P, x, y + 4, M['next'], w, 25, col=INK, face_='Jost-SemiBold', maxl=2) + 8; cx = x
    for ic, word, op in NEW['next.ways.lookout']:
        cw = 56 + P.tw(word, 21, 'Jost-SemiBold') + 22 + P.tw(NAME[op], 17) + 18
        P.rrect(cx, y, cw, 44, 22, fill=PAGE, line=DEEP, lw=1.8); face(P, op, cx + 5, y + 5, 34, ring=None)
        P.icon(ic, cx + 44, y + 11, 22, col=DEEP); P.put(cx + 72, y + 22, word, 21, col=DEEP, face='Jost-SemiBold')
        P.put(cx + 80 + P.tw(word, 21, 'Jost-SemiBold'), y + 23, NAME[op], 17, col=SOFT); cx += cw + 12
    return y + 44


def tiles(P, M, x, y, w, sel=None, widths=None, val=48, why_size=17.5):
    """The run in numbers: icon and word, the number, its one line. Each opens its explainer (the chevron says so)."""
    S = stats(M); widths = widths or [w / len(S)] * len(S); cx = x; boxes = {}
    for s, tw_ in zip(S, widths):
        on = s['id'] == sel; hot = s['id'] == 'standing' and s['value'].startswith('Menace') or s['id'] == 'alert' and s['value'] not in ('0',)
        if on: P.rrect(cx - 12, y - 14, tw_ - 6, 176, 18, fill=PAGE, line=DEEP, lw=2.5)
        P.icon(s['icon'], cx, y - 2, 26, col=ORANGE if hot else DEEP); P.put(cx + 34, y + 11, s['label'], 19, col=SOFT, face='Jost-SemiBold')
        P.text(cx + 40 + P.tw(s['label'], 19, 'Jost-SemiBold'), y + 10, '›', 22, col=DEEP if on else SOFT, face='Jost-Bold', anchor='lm')
        vs = fit(P, s['value'], tw_ - 20, (val, val * .86, val * .74)); P.put(cx, y + 64, s['value'], vs, col=ORANGE if hot else INK, face='Jost-Bold')
        put_wrap(P, cx, y + 112, s['why'], tw_ - 22, why_size, col=SOFT, maxl=3)
        boxes[s['id']] = (cx - 12, y - 14, cx + tw_ - 18, y + 162); cx += tw_
    return y + 165, boxes


def pick_beats(M, n=8):
    B = list(M['beats'])
    while len(B) > n:
        drop = next((b for b in B if b[1] == 'city' and not b[5]), None) or next((b for b in B if b[1] == 'you' and b[3] in (NEW['beat.watched'],)), None) or B[1]
        B.remove(drop)
    return B


def strip(P, M, x, y, w, n=8, d=44):
    """The book's track along the foot, now the run's own: evenly spaced (the book's rule), the cell's acts as their faces."""
    B = pick_beats(M, n); step = w / max(1, len(B)); lab = 21
    for s_ in (21, 19.5, 18):
        lab = s_
        if max(P.tw(b[3], s_, 'Jost-SemiBold') for b in B) <= step - 14: break
    P.line([(x + d / 2, y + d / 2), (x + (len(B) - 1) * step + d / 2, y + d / 2)], col=SOFT, lw=2.5)
    for i, (t, lane, ic, word, op, hot) in enumerate(B):
        cx = x + i * step
        if op: face(P, op, cx - 2, y - 2, d + 4, ring=ORANGE if hot == 2 else DEEP, rw=3)
        elif hot == 2: P.disc(cx, y, d, fill=ORANGE, ring=None); P.icon(ic, cx + d * .2, y + d * .2, d * .6, col=CREAM)
        elif hot or lane == 'you': P.disc(cx, y, d, fill=DEEP, ring=None); P.icon(ic, cx + d * .2, y + d * .2, d * .6, col=CREAM)
        else: P.disc(cx, y, d, fill=PAGE, ring=SOFT, rw=2); P.icon(ic, cx + d * .2, y + d * .2, d * .6, col=INK)
        two = P.tw(word, lab, 'Jost-SemiBold') > step - 14 and ' ' in word
        ls = word.split(' ', 1) if two else [word]
        for j, l_ in enumerate(ls): P.put(cx, y + d + 22 + j * lab * 1.05, l_, lab, col=ORANGE if hot == 2 else INK, face='Jost-SemiBold')
        P.put(cx, y + d + 48 + (len(ls) - 1) * lab * 1.05, clock(t), 19, col=SOFT)
    return y + d + 60


def city_rows(P, M, x, y, w, cols=3, size=21, n=3):
    rows = city_lines(M)[:n]; cw = w / cols
    for i, (ic, a, b, _) in enumerate(rows):
        cx = x + (i % cols) * cw; cy = y + (i // cols) * 86
        P.icon(ic, cx, cy - 13, 26, col=DEEP); y2 = put_wrap(P, cx + 36, cy, a, cw - 50, size, col=INK, face_='Jost-SemiBold', lead=1.15, maxl=2)
        put_wrap(P, cx + 36, y2 + 4, b, cw - 50, size - 3.5, col=SOFT, maxl=2 if y2 - cy < 30 else 1)
    return y + 86 * (-(-len(rows) // cols))


def fold_trail(P, M, x, y, w, tail_x, by='what'):
    """The Trail explainer as the book's fold: what Trail is, one bar of every point (the biggest share in orange), then each thing that
    left it, biggest first: faces, +n, words and when; the law under it."""
    rows = trail_by_what(M) if by == 'what' else [(None, k, v, [k] if k in NAME else [], '') for k, v in trail_by_who(M)]
    tot = M['run']['trail']; h = 126 + 40 * len(rows) + 34; panel(P, x, y, w, h, tail=(tail_x, True))
    P.put(x + 30, y + 36, f"Trail {signed(tot)}", 34, col=INK, face='Jost-Bold'); P.put(x + 40 + P.tw(f"Trail {signed(tot)}", 34), y + 38, NEW['trail.by.' + by].lower(), 21, col=SOFT)
    put_wrap(P, x + 30, y + 68, NEW['trail.means'], w - 60, 19, col=SOFT, maxl=1)
    pip = min(16, (w - 60) / max(tot, 1) - 4); cx = x + 30
    for i, r in enumerate(rows):  # one bar of every point: shooting in orange, the rest in teal
        for k in range(r[2]): P.disc(cx, y + 88, pip, fill=ORANGE if r[0] == 'fire' else DEEP, ring=None); cx += pip + 4
        cx += 8
    yy = y + 130
    for ic, words, n, ops, when in rows:
        if ops and ops[0] in NAME: face(P, ops[0], x + 28, yy - 18, 36, ring=DEEP, rw=2)
        else: P.disc(x + 28, yy - 18, 36, fill=DEEP, ring=None); P.icon(ic or 'p_trail', x + 35, yy - 11, 22, col=CREAM)
        if len(ops) > 1: face(P, ops[1], x + 54, yy - 18, 36, ring=DEEP, rw=2)
        P.text(x + 150, yy, signed(n), 26, col=ORANGE if ic == 'fire' else INK, face='Jost-Bold', anchor='rm'); P.words_add(signed(n))
        P.put(x + 166, yy, words, 22, col=INK, face='Jost-SemiBold')
        if when: P.put(x + 178 + P.tw(words, 22, 'Jost-SemiBold'), yy + 1, when, 19, col=SOFT)
        yy += 40
    P.line([(x + 30, yy - 17), (x + w - 30, yy - 17)], col=HAIR, lw=1.5)
    put_wrap(P, x + 30, yy + 3, NEW['trail.law'], w - 60, 18.5, col=SOFT, maxl=1)
    return y + h
