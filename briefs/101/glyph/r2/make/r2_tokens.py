"""Direction 2 · Corner tokens (a minimal B, mixed with C). Small enamel tokens only where something must sit in a corner: the clock as a
station roundel with the Alert lamps on its face, the seal as an orange roundel hung under it, one slim tail for the next key event, Trail
and money, the news as a lozenge by the Book, the roster's faces in enamel rings, the scope as a round enamel instrument. Flat vitreous
enamel (the book's deep teal, a cream inline, orange for now), a crisp shadow, no chrome or screws. Everything on a person or a place is the
street layer of direction 1. Glyph, brief 101 round 2. Mock only."""
import sys, os
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from r2_base import *
import r2_street as S

DIMC = (0xc9, 0xe2, 0xdc, 255)  # cream-teal for second lines on enamel (round 1's B)


def roundel(P, x, y, d, fill=DEEP, inline=CREAM, iw=1.6, sh=85):
    disc_shadow(P, x, y, d, dy=2.5, a=sh, blur=2.2); P.disc(x, y, d, fill=fill, ring=None); P.disc(x + 4, y + 4, d - 8, fill=None, ring=inline, rw=iw)


def lozenge(P, x, y, w, h, fill=DEEP, inline=CREAM):
    shadow(P, x, y, w, h, h / 2, dy=2.5, a=80, blur=2.2); P.rrect(x, y, w, h, h / 2, fill=fill); P.rrect(x + 3.5, y + 3.5, w - 7, h - 7, h / 2 - 3.5, line=inline, lw=1.4)


def clock(P, D, seal=None):
    """The station clock: a roundel with the time and the Alert lamps; a slim tail to its left with the next key event, Trail and money;
    the pause its own small roundel; the seal an orange roundel hung under the clock; the key people hung under the tail."""
    cx, cy, d = 1826, 14, 78
    # the tail, measured right to left
    ic, word, t = D['next']; parts = [('p_cash', D['cash'], 'Jost-SemiBold'), ('p_trail', str(D['trail']), 'Jost-Bold'), (ic, word + ' ' + t, 'Jost-Bold')]
    ws = [P.tw(v, 15.5, f) + 22 for _, v, f in parts]; tw_ = sum(ws) + 16 * (len(parts) - 1) + 34; tx0 = cx + 8 - tw_; ty, th = 34, 32
    lozenge(P, tx0, ty, tw_ + 26, th); x = tx0 + 16
    for (icn, v, f), w in zip(reversed(parts), reversed(ws)):
        P.icon(icn, x, ty + 8, 16, col=CREAM if icn != 'nextevent' else TURQ); P.text(x + 21, ty + 17, v, 15.5, col=CREAM, face=f); P.words_add(v); x += w + 16
    roundel(P, cx, cy, d, fill=DEEP, iw=1.8); P.text(cx + d / 2, cy + 32, D['clock'], 24, col=CREAM, face='Jost-Bold', anchor='mm'); P.words_add(D['clock'])
    for i in range(3):
        lx = cx + d / 2 - 21 + i * 15; P.disc(lx - 5, cy + 50, 11, fill=ORANGE if i < D['alert'] else None, ring=CREAM if i >= D['alert'] else None, rw=1.4)
    roundel(P, tx0 - 34, ty, 32, fill=BODY_, iw=1.2); P.icon('pause', tx0 - 26, ty + 8, 16, col=CREAM)
    ky = ty + th + 8
    for i, (who, k) in enumerate(KNOWN):
        kx = tx0 + 14 + i * 30; P.line([(kx + 12, ty + th - 2), (kx + 12, ky + 2)], col=BODY_, lw=2); disc_shadow(P, kx, ky, 24, a=60); face(P, who, kx, ky, 24, ring=CREAM, rw=1.6, gray=not k)
    if not seal: return
    for hx in (cx + 25, cx + 53): P.line([(hx, cy + d - 2), (hx, cy + d + 10)], col=BODY_, lw=3)
    sx, sy, sd = cx + 1, cy + d + 6, 76; roundel(P, sx, sy, sd, fill=ORANGE, iw=1.8); P.icon('lock', sx + sd / 2 - 9, sy + 11, 18, col=CREAM)
    P.text(sx + sd / 2, sy + 46, seal['left'], 24, col=CREAM, face='Jost-Bold', anchor='mm'); P.words_add(seal['left'])


def roster(P, D):
    """The faces in enamel rings (deep teal with a cream inline): the live portraits, the selection ring turquoise outside; health as cream
    pips on a slim enamel strip; what an agent is doing as a small enamel roundel on the face (never its seconds)."""
    for i, who in enumerate(CELL):
        x, y, d = 22, 22 + i * 86, 60; sel = who == D['sel']; disc_shadow(P, x - 3, y - 3, d + 6, a=80, dy=2.5, blur=2.2)
        P.disc(x - 4, y - 4, d + 8, fill=DEEP, ring=None)
        if sel: P.disc(x - 8, y - 8, d + 16, fill=None, ring=TURQ, rw=3.5)
        st = D['live'].get(who, 'calm'); live_face(P, who, st, x, y, d, ring=CREAM, rw=1.4); cue(P, st, x - 2, y - 2, d + 4)
        hp, mx = D['hp'][who]; sh_ = mx * 7 + 6; P.rrect(x + d + 4, y + 2, 11, sh_, 5.5, fill=DEEP)
        for k in range(mx): P.disc(x + d + 6.5, y + 6 + k * 7, 6, fill=CREAM if k < hp else BODY_, ring=None)
        b = D['busy'].get(who) or ('fire' if st == 'firing' else None)
        if b: roundel(P, x + d - 22, y + d - 24, 28, fill=ORANGE if b == 'fire' else BODY_, iw=1, sh=60); P.icon(b, x + d - 15, y + d - 17, 14, col=CREAM)
    roundel(P, 32, 22 + 4 * 86 + 2, 40, fill=DEEP); P.icon('units', 42, 22 + 4 * 86 + 12, 20, col=CREAM)


def scope(P, key):
    """The plotting scope as a round enamel instrument: the real map in cream and turquoise on the deep enamel, three small roundels on its rim."""
    src = FRAMES[key][0] + '.png' if key in FRAMES else SRC + 'fight-loud-body.png'; lens(P, src, 18, 922, 146, glass=DEEP, rim=BODY_, inline=CREAM)
    for ang, ic in ((-145, 'turn_view'), (-90, 'eye_open'), (-35, 'turn_view')):
        a = math.radians(ang); cx, cy = 18 + 73 + math.cos(a) * 80, 922 + 73 + math.sin(a) * 80; roundel(P, cx - 16, cy - 16, 32, fill=BODY_, iw=1); P.icon(ic, cx - 9, cy - 9, 18, col=CREAM)


def book(P, unread=True):
    roundel(P, 1844, 1000, 56, fill=DEEP); P.icon('papers', 1858, 1014, 28, col=CREAM)
    if unread: P.disc(1886, 998, 15, fill=TURQ, ring=CREAM, rw=2)


def news(P, N, dock=None):
    """The crucial card as a lozenge left of the Book: its badge on an orange roundel overlapping the left end, the title, where and when
    on a second line, '1 of n ›'; a key time (police arriving) on a cream ticket at the right end. `dock` is ignored: tokens keep their place."""
    title, where = N['title'], N['where'] + ' · ' + N['when']; w = max(P.tw(title, 17), P.tw(where, 13, 'Jost-Medium') + 22) + 64 + (78 if N['in'] else 0)
    h = 50; x = 1830 - w; y = 1003; lozenge(P, x, y, w, h)
    bd = 58; roundel(P, x - 26, y - 4, bd, fill=ORANGE, iw=1.6); S.BADGE[N['kind']](P, x - 26, y - 4, bd)
    tx = x + 40; P.text(tx, y + 17, title, 17, col=CREAM, face='Jost-Bold'); P.words_add(title)
    P.icon(N['cause'], tx, y + 28, 13, col=ORANGE); P.text(tx + 17, y + 35, where, 13, col=DIMC, face='Jost-Medium'); P.words_add(where)
    if N['in']:
        kx = x + w - 84; P.rrect(kx, y + 7, 72, h - 14, 9, fill=PAGE); P.text(kx + 36, y + h / 2 + 1, N['in'], 22, col=ORANGE, face='Jost-Bold', anchor='mm'); P.words_add(N['in'])
    n, of = N['of']; P.rrect(x + w - 64 - (78 if N['in'] else 0) + 10, y + h - 4, 54, 18, 9, fill=BODY_); P.text(x + w - 37 - (78 if N['in'] else 0) + 10, y + h + 5, f'{n} of {of} ›', 11.5, col=CREAM, face='Jost-SemiBold', anchor='mm')


def render():
    out = {}
    P = base2('quiet'); roster(P, QUIET2); scope(P, 'quiet'); clock(P, QUIET2); book(P, unread=False); out['rest'] = (save2(P, 'T-rest'), cover(P))
    P = base2('quiet'); S.busy_bits(P, QUIET2); roster(P, QUIET2); scope(P, 'quiet'); clock(P, QUIET2); book(P)
    news(P, QUIET2['news']); reporter(P, dict(QUIET2['report2'], dir=None)); out['quiet'] = (save2(P, 'T-quiet'), cover(P))
    P = base2('loud'); S.busy_bits(P, LOUD2); roster(P, LOUD2); scope(P, 'loud'); clock(P, LOUD2, seal=LOUD2['seal']); book(P); news(P, LOUD2['news'])
    out['loud'] = (save2(P, 'T-loud'), cover(P))
    P = base2('aim'); roster(P, AIM2); scope(P, 'aim'); clock(P, AIM2); book(P, unread=False); S.shot(P, AIM2['shot']); out['aim'] = (save2(P, 'T-aim'), cover(P))
    P = base2('busy'); B = BUSY; S.busy_bits(P, B); roster(P, B); scope(P, 'busy'); clock(P, B, seal=B['seal']); book(P); news(P, B['news'])
    car_marks(P, *B['car']['at'], B['car']['held'], B['car']['aboard']); edge_face(P, B['edge'][0], *B['edge'][1])
    f = B['focus']; focus(P, f['head'], f['feet'], f['hp'], cover_=f['cover'], long_=f['long'], aim=True, holds=f['holds']); reporter(P, B['report'])
    out['busy'] = (save2(P, 'T-busy'), cover(P))
    return out


if __name__ == '__main__':
    for k, (im, cov) in render().items(): print(k, f'HUD {cov:.1%}')
