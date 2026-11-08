"""Direction 1 · Street labels (C refined). No plate anywhere: the corners hold only what has no place in the street (the clock, the
seal's count, money and Trail, the roster, the scope, the Book), as bold type with a solid cream keyline that reads on paving, asphalt and
palms alike; everything else is labelled where it happens. News with a place docks at the edge it is past; the rest sits by the Book.
Glyph, brief 101 round 2. Mock only."""
import sys, os
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from r2_base import *


def clock(P, D, seal=None):
    """Top right, right-aligned: the time; the seal's count under it in orange; Alert lamps, Trail and money; the next key event."""
    X = 1898; w = tag(P, X, 38, D['clock'], 42, anchor='rm'); tab(P, X - w - 44, 21, 34, 'pause')
    y = 80
    if seal:
        w2 = tag(P, X, y + 6, seal['left'], 42, fill=ORANGE, key=INK, kw=2.2, anchor='rm'); kicon(P, 'lock', X - w2 - 38, y - 9, 30, fill=ORANGE, key=INK, kw=2)
        y += 52
    x = X - tag(P, X, y, D['cash'], 17, face_='Jost-SemiBold', anchor='rm'); kicon(P, 'p_cash', x - 25, y - 10, 20); x -= 25 + 16
    x -= tag(P, x, y, str(D['trail']), 18, anchor='rm'); kicon(P, 'p_trail', x - 24, y - 10, 20); x -= 24 + 16
    lw = 3 * 13 + 2 * 4; lamps(P, x - lw, y, D['alert']); kicon(P, 'p_alert', x - lw - 25, y - 10, 20, fill=ORANGE if D['alert'] else INK)
    ic, word, t = D['next']; y += 28; x = X - tag(P, X, y, t, 17, anchor='rm'); x -= 6 + tag(P, x - 6, y, word, 15, face_='Jost-SemiBold', anchor='rm')
    kicon(P, ic, x - 24, y - 9, 18, fill=DEEP)
    y += 20
    for i, (who, k) in enumerate(KNOWN):  # the key people, grey until known; a click finds them
        kx = X - 26 - (3 - i) * 30; disc_shadow(P, kx, y, 26, a=55); face(P, who, kx, y, 26, ring=PAGE, rw=2, gray=not k)


def roster(P, D):
    """Faces only: the live portraits, no tray; the selected one ringed turquoise; health as pips; what an agent is doing as its icon on
    the face's corner (never its seconds)."""
    for i, who in enumerate(CELL):
        x, y, d = 22, 22 + i * 86, 60; sel = who == D['sel']; disc_shadow(P, x, y, d, a=75)
        if sel: P.disc(x - 5, y - 5, d + 10, fill=None, ring=TURQ, rw=3.5)
        st = D['live'].get(who, 'calm'); live_face(P, who, st, x, y, d); cue(P, st, x, y, d)
        hp, mx = D['hp'][who]
        for k in range(mx): P.disc(x + d + 5 - 1, y + 5 + k * 7 - 1, 7, fill=INK, ring=None); P.disc(x + d + 5, y + 5 + k * 7, 5, fill=TURQ if k < hp else PAGE, ring=None)
        b = D['busy'].get(who)
        if b: P.disc(x + d - 20, y + d - 22, 26, fill=BODY_, ring=PAGE, rw=2); P.icon(b, x + d - 14, y + d - 16, 14, col=CREAM)
        if st == 'firing' and not b: P.disc(x + d - 20, y + d - 22, 26, fill=ORANGE, ring=PAGE, rw=2); P.icon('fire', x + d - 14, y + d - 16, 14, col=CREAM)
    tab(P, 34, 22 + 4 * 86 + 2, 36, 'units')


def scope(P, key):
    """The plotting scope as a small round lens of dark glass: the real map's light parts in cream and turquoise, a cream rim, three round tabs."""
    src = FRAMES[key][0] + '.png' if key in FRAMES else SRC + 'fight-loud-body.png'; lens(P, src, 18, 922, 146, glass=INK[:3] + (222,), rim=PAGE)
    for ang, ic in ((-145, 'turn_view'), (-90, 'eye_open'), (-35, 'turn_view')):
        a = math.radians(ang); cx, cy = 18 + 73 + math.cos(a) * 80, 922 + 73 + math.sin(a) * 80; tab(P, cx - 16, cy - 16, 32, ic)


def book(P, unread=True):
    tab(P, 1844, 996, 54, 'papers')
    if unread: P.disc(1885, 994, 15, fill=TURQ, ring=PAGE, rw=2)
    tag(P, 1871, 1062, 'Book', 13, face_='Jost-SemiBold', anchor='mm', kw=2)


BADGE = {'police': lambda P, x, y, d: badge(P, 'signal_police', x + d * .16, y + d * .16, d * .68),
         'car': lambda P, x, y, d: P.icon('car', x + d * .22, y + d * .22, d * .56, col=CREAM)}


def news(P, N, dock=None):
    """The crucial card as labels: its badge on an orange disc, the title, where and when as a cause icon and a time, '1 of n ›'.
    With `dock` (x, y) it docks at the edge its thing is past, the number big beside the badge; else it sits left of the Book."""
    if dock:
        x, y = dock; d = 74; disc_shadow(P, x, y, d, a=70); P.disc(x, y, d, fill=ORANGE, ring=PAGE, rw=3); BADGE[N['kind']](P, x, y, d)
        g = ImageDraw.Draw(P.img); s = P.s; cy = y + d / 2; g.polygon([((x + d + 3) * s, (cy - 9) * s), ((x + d + 14) * s, cy * s), ((x + d + 3) * s, (cy + 9) * s)], fill=ORANGE)
        rx = x - 12
        if N['in']: tag(P, rx, cy - 12, N['in'], 38, fill=ORANGE, key=INK, kw=2.0, anchor='rm'); tag(P, rx, cy + 22, N['title'], 17, anchor='rm')
        else: tag(P, rx, cy, N['title'], 19, anchor='rm')
        n, of = N['of']; tag(P, x + d / 2, y + d + 16, f'{n} of {of} ›', 13, face_='Jost-SemiBold', anchor='mm', kw=2); return
    x, y, d = 1782, 1000, 46; disc_shadow(P, x, y, d, a=65); P.disc(x, y, d, fill=ORANGE, ring=PAGE, rw=2.5); BADGE[N['kind']](P, x, y, d)
    rx = x - 10; tag(P, rx, y + 13, N['title'], 18, anchor='rm')
    w = tag(P, rx, y + 37, N['when'], 14, face_='Jost-SemiBold', fill=SOFT, anchor='rm'); w2 = tag(P, rx - w - 6, y + 37, N['where'], 14, face_='Jost-Medium', anchor='rm')
    kicon(P, N['cause'], rx - w - w2 - 30, y + 27, 18, fill=ORANGE, key=PAGE)
    if N['in']: tag(P, rx - P.tw(N['title'], 18) - 14, y + 14, N['in'], 30, fill=ORANGE, key=INK, kw=2, anchor='rm')
    n, of = N['of']; tag(P, x + d / 2, y + d + 14, f'{n} of {of} ›', 12.5, face_='Jost-SemiBold', anchor='mm', kw=2)


def price_row(P, S, x, y):
    """What the first shot sets off, as one row of marks under the target: Goes loud, Alert to 2, police come, the district seals, Trail.
    No odds and no seconds (Sky, 2026-11-08)."""
    t = 'Goes loud'; tw_ = P.tw(t, 15, 'Jost-Bold') + 22; shadow(P, x, y - 13, tw_, 26, 13, dy=2, a=50, blur=2); P.rrect(x, y - 13, tw_, 26, 13, fill=ORANGE)
    P.put(x + 11, y, t, 15, col=CREAM, face='Jost-Bold'); x += tw_ + 12
    kicon(P, 'p_alert', x, y - 10, 20, fill=ORANGE); x += 25; x += lamps(P, x, y, S['alert'], d=12, gap=3) + 14
    P.disc(x - 1.5, y - 13.5, 27, fill=INK, ring=None); P.disc(x, y - 12, 24, fill=PAGE, ring=None); badge(P, 'signal_police', x + 1.5, y - 10.5, 21); x += 36
    kicon(P, 'lock', x, y - 10, 20); x += 32
    kicon(P, 'p_trail', x, y - 10, 20); tag(P, x + 24, y, S['trail'], 17)


def shot(P, S, hold=.6):
    """The aim: the target's ring in his signal colour (whole: in range; in arcs: a long shot), the crosshair with the commit's hold ring
    filling turquoise, the word Fire, and the price row. His face over him in amber names him."""
    (hx, hy), (fx, fy) = S['head'], S['feet']; ground_arc(P, fx, fy - 2, 34, 1, AMBER, 4.0, gaps=3 if S['long'] else 0)
    d = 34; disc_shadow(P, hx - d / 2, hy - 54 - d, d, a=60); face(P, S['at'], hx - d / 2, hy - 54 - d, d, ring=AMBER, rw=3); P.line([(hx, hy - 54), (hx, hy - 16)], col=AMBER, lw=2)
    cx, cy = hx + 30, hy + 34; crosshair(P, cx, cy); arc(P, cx, cy, 21, 0, 360 * hold, TURQ, 4); tag(P, cx + 34, cy + 1, 'Fire', 18)
    price_row(P, S, cx - 24, cy + 62)


def busy_bits(P, D):
    for who, t, (x, y) in D['speech']: bubble(P, who, t, x, y, stem=24, live=D['live'].get(who) if who in CELL else None)


def render(today_only=False):
    out = {}
    P = base2('quiet'); roster(P, QUIET2); scope(P, 'quiet'); clock(P, QUIET2); book(P, unread=False); out['rest'] = (save2(P, 'S-rest'), cover(P))
    P = base2('quiet'); busy_bits(P, QUIET2); roster(P, QUIET2); scope(P, 'quiet'); clock(P, QUIET2); book(P)
    news(P, QUIET2['news']); reporter(P, dict(QUIET2['report2'], dir=None)); out['quiet'] = (save2(P, 'S-quiet'), cover(P))
    P = base2('loud'); busy_bits(P, LOUD2); roster(P, LOUD2); scope(P, 'loud'); clock(P, LOUD2, seal=LOUD2['seal']); book(P)
    news(P, LOUD2['news'], dock=(1820, 520)); out['loud'] = (save2(P, 'S-loud'), cover(P))
    P = base2('aim'); roster(P, AIM2); scope(P, 'aim'); clock(P, AIM2); book(P, unread=False); shot(P, AIM2['shot']); out['aim'] = (save2(P, 'S-aim'), cover(P))
    P = base2('busy'); B = BUSY; busy_bits(P, B); roster(P, B); scope(P, 'busy'); clock(P, B, seal=B['seal']); book(P); news(P, B['news'])
    car_marks(P, *B['car']['at'], B['car']['held'], B['car']['aboard']); edge_face(P, B['edge'][0], *B['edge'][1])
    f = B['focus']; focus(P, f['head'], f['feet'], f['hp'], cover_=f['cover'], long_=f['long'], aim=True, holds=f['holds']); reporter(P, B['report'])
    out['busy'] = (save2(P, 'S-busy'), cover(P))
    return out


if __name__ == '__main__':
    for k, (im, cov) in render().items(): print(k, f'HUD {cov:.1%}')
