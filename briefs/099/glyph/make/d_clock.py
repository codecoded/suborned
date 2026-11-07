"""Concept B · The clock. The run to scale on one strip: the cell's acts above the line as their faces, the city's answers below it, and
each number as a step line on the same clock, so every rise sits under the act that caused it (Trail at 1:34 under Hasti turning Inez).
A fail marks the window that lost it. A number opens by lighting its steps and naming each. Glyph, brief 099. Mock only."""
import sys, os; sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))  # python3 -I drops the script folder
from d_parts import *
from d_data import page
from d_reasons import trail_by_what, is_shot

FRAME = {'quiet-win.route-a': 'win', 'fail-lookout': 'fail', 'loud-win.seed3': 'loud'}
T0, T1, TOT = 1040, 1566, 1590          # the clock's x range; the totals' column


def spread(xs, gap, lo, hi):
    """Positions as close to their times as they can be without overlapping (a leader joins each to its time)."""
    out = []
    for x in xs: out.append(max(x, out[-1] + gap) if out else max(x, lo))
    over = out[-1] - hi if out else 0
    if over > 0:
        out = [o - over for o in out]
        for i in range(len(out) - 2, -1, -1): out[i] = min(out[i], out[i + 1] - gap)
    return out


def place(P, items, rows, size=16.5, pad=10, lo=T0 - 40, hi=X1):
    """Words on the first free row: [(x, text)] -> [(x, y, text)], each kept inside [lo, hi] and clear of the others on its row."""
    taken = {r: [] for r in rows}; out = []
    for x, t in items:
        w = P.tw(t, size, 'Jost-SemiBold'); x = min(max(x, lo + w / 2), hi - w / 2); best = None
        for r in rows:
            if all(x + w / 2 + pad < a or x - w / 2 - pad > b for a, b in taken[r]): best = r; break
        best = rows[-1] if best is None else best; taken[best].append((x - w / 2, x + w / 2)); out.append((x, best, t))
    return out


def axis(P, M, y, tx, end):
    P.line([(T0, y), (T1, y)], col=SOFT, lw=2.5)
    for m in range(0, end // 600 + 1):
        x = tx(m * 600); P.line([(x, y - 6), (x, y + 6)], col=SOFT, lw=2)
        if tx(end) - x > 48: P.text(x + 4, y + 17, f'{m}:00', 15, col=SOFT, face='Jost-Medium', anchor='lm')
    win = M['kind'] == 'win'; x = tx(end); d = 34
    P.disc(x - d / 2, y - d / 2, d, fill=DEEP if win else ORANGE, ring=CREAM, rw=2); P.icon('car' if win else 'close_x', x - d * .3, y - d * .3, d * .6, col=CREAM)


def lane(P, items, y, tx, up=True, d=40, dim=False, lab=17.5):
    """Faces (the cell) or icons (the city and the mission's own stops), each joined to its moment; words on two alternating rows."""
    xs = spread([tx(b[0]) for b in items], d + 8, T0 - 6, T1 + 6)
    for i, ((t, ln, ic, word, op, hot), x) in enumerate(zip(items, xs)):
        xt = tx(t); ay = y + (d / 2 + 34 if up else -d / 2 - 34)
        P.line([(xt, ay), (x, y + (d / 2 if up else -d / 2))], col=HAIR, lw=1.5)
        g = (0xc8, 0xc4, 0xb8, 255)
        if op: face(P, op, x - d / 2, y - d / 2, d, ring=g if dim else ORANGE if hot == 2 else DEEP, rw=2.5, gray=dim)
        else:
            fill = g if dim else ORANGE if hot == 2 else (DEEP if ln == 'you' else PAGE)
            P.disc(x - d / 2, y - d / 2, d, fill=fill, ring=None if hot == 2 or ln == 'you' or dim else SOFT, rw=2)
            P.icon(ic, x - d * .3, y - d * .3, d * .6, col=CREAM if hot == 2 or ln == 'you' or dim else INK)
    rows = [y - d / 2 - 14 - k * 22 for k in range(3)] if up else [y + d / 2 + 16 + k * 22 for k in range(3)]
    for (x, ly, word), b in zip(place(P, [(x, b[3]) for x, b in zip(xs, items)], rows, lab, 8), items):
        P.text(x, ly, word, lab, col=(0xc8, 0xc4, 0xb8, 255) if dim else ORANGE if b[5] == 2 else INK, face='Jost-SemiBold', anchor='mm'); P.words_add(word)
    return xs


def track(P, y, s, steps, lo, hi, tx, h=40, labels=(), dim=False, sub=None, band=False):
    """One number on the clock: its word on the left, a step line where it moved (shooting steps in orange), a short word at each
    step, the total on the right."""
    g = (0xc8, 0xc4, 0xb8, 255); ink = g if dim else INK; soft = g if dim else SOFT; hot = s['value'].startswith('Menace') or s['id'] == 'alert' and s['value'] != '0'
    if band: P.rrect(868, y - h / 2 - 12, X1 + 14 - 868, h + 24, 14, fill=(0xf3, 0xf0, 0xe6, 255))
    P.icon(s['icon'], 880, y - 13, 26, col=soft if dim else (ORANGE if hot else DEEP)); P.put(914, y, s['label'], 20, col=soft, face='Jost-SemiBold')
    Y = lambda v: y + h / 2 - (v - lo) / max(1e-6, hi - lo) * h
    v0 = steps[0][1]; pts = [(T0, Y(v0))]; P.line([(T0, Y(v0)), (T1, Y(v0))], col=HAIR, lw=1.2)
    segs = []
    for (t, v, kind) in steps[1:]:
        x = tx(t); segs.append(((x, pts[-1][1]), (x, Y(v)), kind)); pts.append((x, pts[-1][1])); pts.append((x, Y(v)))
    pts.append((T1, pts[-1][1])); col = g if dim else DEEP
    for a_, b_ in zip(pts, pts[1:]):
        if a_[1] == b_[1]: P.line([a_, b_], col=col, lw=3)
    for a_, b_, kind in segs: P.line([a_, b_], col=g if dim else ORANGE if kind == 'shot' else DEEP, lw=3.5)
    for a_, b_, kind in segs: P.disc(b_[0] - 4.5, b_[1] - 4.5, 9, fill=g if dim else ORANGE if kind == 'shot' else DEEP, ring=None)
    for x, yy, t in labels: P.text(x, yy, t, 16, col=ink, face='Jost-SemiBold', anchor='mm'); P.words_add(t)
    P.text(X1, y - (8 if sub else 0), s['value'], 28, col=soft if dim else (ORANGE if hot else INK), face='Jost-Bold', anchor='rm'); P.words_add(s['value'])
    if sub: P.text(X1, y + 20, sub, 15.5, col=soft, face='Jost-Medium', anchor='rm'); P.words_add(sub)


def steps_of(M, what):
    s = [(0, 0, '')]; v = 0
    if what == 'trail':
        for r in M['trail_rows']: v += r['delta']; s.append((r['tick'], v, 'shot' if is_shot(r) else ''))
    elif what == 'alert':
        for e in M['alert']['rows']: s.append((e['tick'], e['now'], ''))
    elif what == 'standing':
        v = M['standing']['start']; s = [(0, v, '')]
        for t, d, c in M['standing']['moves']: v += d; s.append((t, v, 'shot' if 'security' in c else ''))
    elif what == 'cash':
        for p in M['paid']: s.append((p['tick'], M['usd'], ''))
    return s


def trail_words(P, M, tx, y, h):
    """The explainer, open: each thing that left Trail named at its moment ("+18 Larthi, Hasti: three rivals")."""
    from d_reasons import trail_by_what as tw_
    rows = []
    for icn, words, n, ops, when in tw_(M):
        t0 = min(r['tick'] for r in M['trail_rows'] if (r['who'] in ops or (not ops and r['kind'] == 'car-seen')) and (is_shot(r) == (icn == 'fire')))
        who = ', '.join(NAME[o] for o in ops if o in NAME)
        kinds = list(dict.fromkeys(r['kind'] for r in M['trail_rows'] if not is_shot(r) and (r['who'] in ops if ops else r['kind'] == 'car-seen')))
        what = words.split(' shot ')[-1] if icn == 'fire' else ', '.join(NEW.get('short.' + k, k) for k in kinds)
        rows.append((t0, f"{signed(n)} {who + ': ' if who else ''}{what}"))
    rows.sort()
    return place(P, [(tx(t0), t) for t0, t in rows], [y - h / 2 - 14, y + h / 2 + 16, y - h / 2 - 38, y + h / 2 + 40], 16.5, hi=T1)


def clock_page(M, detail=None):
    P = Page(2); light_page(P, FRAME[M['name']], M['subject']); tabs(P)
    y = head(P, M, sizes=(64, 56, 50), line_size=25)
    end = M['tick']; tx = lambda t: T0 + min(t, end) / end * (T1 - T0)
    you = [b for b in M['beats'] if b[1] == 'you']; city = [b for b in M['beats'] if b[1] in ('city', 'mission') and b[0] != end]
    ly = max(y + 74, 292); lane(P, you, ly, tx, up=True)
    ay = ly + 58; axis(P, M, ay, tx, end)
    cy = ay + 66; lane(P, city, cy, tx, up=False, d=32, dim=bool(detail), lab=16.5)
    ry = cy + 120 + (18 if detail else 0)
    if M['kind'] == 'fail':  # the window that lost it: from the lookout's post to the end
        lk = next((b for b in city if b[3] == NEW['beat.lookout']), None)
        if lk:
            x0, x1 = tx(lk[0]), tx(end); by = cy + 92
            P.line([(x0, by), (x1, by)], col=ORANGE, lw=3); P.line([(x0, by - 8), (x0, by + 8)], col=ORANGE, lw=3); P.line([(x1, by - 8), (x1, by + 8)], col=ORANGE, lw=3)
            P.text((x0 + x1) / 2, by + 22, f('missed', span=clock(end - lk[0])), 17.5, col=ORANGE, face='Jost-SemiBold', anchor='mm'); P.words_add(NEW['missed'])
        ry += 44
    S = {s['id']: s for s in stats(M)}
    tr = steps_of(M, 'trail'); st = steps_of(M, 'standing')
    rows = [('trail', tr, 0, max(3, tr[-1][1])), ('alert', steps_of(M, 'alert'), 0, 3),
            ('standing', st, min(-3, min(v for _, v, _ in st)), max(3, max(v for _, v, _ in st))), ('cash', steps_of(M, 'cash'), 0, max(1, M['usd']))]
    for i, (key, steps, lo, hi) in enumerate(rows):
        on = key == detail; h = 36 if not on else 92
        sub = key == 'standing' and M['standing']['next'] != M['standing']['end'] and f"{signed(M['standing']['end'])} now, {signed(M['standing']['next'])} next" or None
        labels = trail_words(P, M, tx, ry + 28, h) if on else ()  # shut, a step explains itself by lining up under its act; open, each is named
        track(P, ry + (28 if on else 0), S[key], steps, lo, hi, tx, h=h, labels=labels, dim=bool(detail) and not on, sub=sub, band=i % 2 == 0)
        ry += (h + 66) if on else 64
    if not detail: city_rows(P, M, 880, ry + 18, X1 - 880, n=2 if M['kind'] == 'fail' else 3)
    if M['kind'] == 'fail' and not detail:
        y2 = ry + 112; cx = caps(P, 880, y2, NEW['next.head'], 16, col=ORANGE); P.put(cx + 14, y2 + 1, M['next'], 21, col=INK, face='Jost-SemiBold')
    ways_on(P, M['seed'])
    return P


if __name__ == '__main__':
    for n, det, out in (('quiet-win.route-a', None, 'B-clock-win'), ('fail-lookout', None, 'B-clock-fail'), ('loud-win.seed3', 'trail', 'B-clock-detail')):
        if len(sys.argv) > 1 and out not in sys.argv[1:]: continue
        P = clock_page(page(n), det); save(P, out); print(out, P.words, 'words')
