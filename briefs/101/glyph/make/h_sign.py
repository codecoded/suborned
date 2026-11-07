"""B · Enamel signs. The HUD is the city's own signage: flat vitreous enamel in the book's deep teal with a cream inline (the start page's
Front door and Pickup plates, the book's round tabs), die-cut as Googie roundels and lozenges; numbers sit on cream price tickets in ink;
orange enamel means now. No chrome, screws or gradients; a crisp shadow stands each sign off the street. Glyph, brief 101. Mock only."""
import sys, os
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from h_base import *

DIMC = (0xc9, 0xe2, 0xdc, 255)  # cream-teal for second lines on enamel (the kit's --text-dim, lifted for the brighter enamel)


KNOWN = [('waitress', 1), ('guard', 1), ('driver', 0), ('councillor', 1)]  # the mission's key people; 0 = not yet known (grey). Illustrative states


def sign(P, x, y, w, h, fill=DEEP, r=None, inline=CREAM):
    r = h / 2 if r is None else r; shadow(P, x, y, w, h, r, dy=3, a=80, blur=2.5); P.rrect(x, y, w, h, r, fill=fill)
    P.rrect(x + 4.5, y + 4.5, w - 9, h - 9, max(1, r - 4.5), line=inline, lw=1.8)


def roundel(P, x, y, d, fill=DEEP, inline=CREAM, iw=1.8):
    disc_shadow(P, x, y, d, dy=3, a=85, blur=2.5); P.disc(x, y, d, fill=fill, ring=None); P.disc(x + 4.5, y + 4.5, d - 9, fill=None, ring=inline, rw=iw)


def ticket(P, x, y, w, h, r=7):
    """A cream price ticket tucked into a sign: ink numbers."""
    P.rrect(x, y, w, h, r, fill=PAGE); P.rrect(x + 2.5, y + 2.5, w - 5, h - 5, r - 2, line=HAIR, lw=1)


def lamps(P, x, y, n, d=14, gap=5, off=CREAM):
    for i in range(3):
        if i < n: P.disc(x + i * (d + gap), y - d / 2, d, fill=ORANGE, ring=CREAM, rw=1.5)
        else: P.disc(x + i * (d + gap), y - d / 2, d, fill=None, ring=off, rw=1.6)
    return 3 * d + 2 * gap


def clock(P, D, seal=None):
    """The station clock: a roundel with the time, its lozenge tail carrying the numbers right to left (Alert next to the time), the pause
    its own small roundel at the tail's end."""
    ty, th, xr = 38, 50, 1786; items = []
    x = xr - 14; x -= lamps(P, 0, -999, 0) if False else 0
    # measure right to left, then draw
    aw = 3 * 14 + 2 * 5; x -= aw; ax = x; x -= 30; aix = x; x -= 22
    tw_ = P.tw(str(D['trail']), 20); x -= tw_; trx = x; x -= 28; tix = x; x -= 22
    cw = P.tw(D['cash'], 20, 'Jost-SemiBold'); x -= cw; cx_ = x; x -= 27; cix = x; x -= 22
    ic, word, t = D['next']; nw = P.tw(t, 20); x -= nw; ntx = x; x -= 6 + P.tw(word, 15, 'Jost-Medium'); nwx = x; x -= 28; nix = x; x -= 56
    tx0 = x; sign(P, tx0, ty, 1846 - tx0, th)
    roundel(P, tx0 + 10, ty + 3, 44, fill=BODY_); P.icon('pause', tx0 + 22, ty + 15, 20, col=CREAM)
    P.icon(ic, nix, ty + 14, 22, col=CREAM); P.put(nwx, ty + 26, word, 15, col=DIMC); P.put(ntx, ty + 25, t, 20, col=CREAM, face='Jost-Bold')
    for sx in (cix - 11, tix - 11, aix - 11): P.line([(sx, ty + 13), (sx, ty + 37)], col=DIMC, lw=1.2)
    P.icon('p_cash', cix, ty + 14, 22, col=CREAM); P.put(cx_, ty + 25, D['cash'], 20, col=CREAM, face='Jost-SemiBold')
    P.icon('p_trail', tix, ty + 13, 23, col=CREAM); P.put(trx, ty + 25, str(D['trail']), 20, col=CREAM, face='Jost-Bold')
    P.icon('p_alert', aix, ty + 13, 23, col=ORANGE if D['alert'] else CREAM); lamps(P, ax, ty + 25, D['alert'])
    for i, (who, k) in enumerate(KNOWN):  # the key people as small roundels hung under the tail, grey until known
        kx = 1612 + i * 42; P.line([(kx + 17, ty + th - 2), (kx + 17, ty + th + 8)], col=BODY_, lw=2); roundel(P, kx, ty + th + 6, 34, fill=DEEP if k else BODY_, iw=1); face(P, who, kx + 4, ty + th + 10, 26, ring=None, gray=not k)
    roundel(P, 1786, 8, 112, fill=DEEP, iw=2.2); P.disc(1797, 19, 90, fill=None, ring=CREAM, rw=1); P.put(1842, 66, D['clock'], 34, col=CREAM, face='Jost-Bold', anchor='mm')
    if not seal: return
    # the seal: an orange roundel hung under the clock on two short bars; the cause and the job beside it on a small lozenge
    for hx in (1820, 1864): P.line([(hx, 116), (hx, 134)], col=BODY_, lw=3)
    sx, sy, sd = 1774, 130, 136; roundel(P, sx, sy, sd, fill=ORANGE, iw=2.2); P.icon('lock', sx + sd / 2 - 12, sy + 16, 24, col=CREAM)
    P.put(sx + sd / 2, sy + 68, seal['left'], 42, col=CREAM, face='Jost-Bold', anchor='mm'); caps(P, sx + sd / 2, sy + 99, 'seals ' + seal['at'], size=10.5, col=CREAM, gap=1.8, anchor='m')
    lx, ly, lw_, lh = 1548, sy + 42, 236, 54; sign(P, lx, ly, lw_, lh, fill=BODY_)
    P.disc(lx + 10, ly + 9, 36, fill=ORANGE, ring=None); P.icon(seal['cause'][0], lx + 18, ly + 17, 20, col=CREAM)
    face(P, 'councillor', lx + 62, ly + 7, 40, ring=CREAM, rw=2)
    for k in range(4): P.disc(lx + 112 + k * 12, ly + 25, 5, fill=CREAM, ring=None)
    P.disc(lx + 166, ly + 7, 40, fill=None, ring=CREAM, rw=2); P.icon('car', lx + 175, ly + 16, 22, col=CREAM)


def card(P, C, badge_fn, big=None):
    """The crucial card as a sign: the badge roundel overlapping its left end, the title, one line; the number on its ticket; 1 of n."""
    x, y, w, h = 1404, 944, 402, 80; sign(P, x, y, w, h, r=40)
    badge_fn(P, x - 36, y - 12, 104)
    tw_ = w - 84 - (96 if big else 16); size = next(z for z in (24, 22, 20, 18) if P.tw(C['title'], z) <= tw_)
    P.put(x + 78, y + 30, C['title'], size, col=CREAM, face='Jost-Bold'); P.put(x + 78, y + 56, C['line'], 15, col=DIMC)
    if big:
        cap, num = big; ticket(P, x + w - 98, y + 12, 84, 56); caps(P, x + w - 56, y + 27, cap, size=10, col=SOFT, anchor='m'); P.put(x + w - 56, y + 50, num, 24, col=ORANGE, face='Jost-Bold', anchor='mm')
    i, n = C['of']; t = f"{C['when']} · {i} of {n} ›"; tw2 = P.tw(t, 13, 'Jost-SemiBold') + 22; sign(P, x + w - tw2 - 30, y + h - 6, tw2, 24, fill=BODY_, r=12); P.put(x + w - tw2 - 19, y + h + 6, t, 13, col=CREAM, face='Jost-SemiBold')
    book(P)


def book(P):
    roundel(P, 1822, 958, 86, fill=DEEP); P.icon('papers', 1847, 974, 36, col=CREAM); sign(P, 1818, 1032, 94, 26, fill=BODY_, r=13); caps(P, 1865, 1045, 'book', size=12, col=CREAM, anchor='m')
    P.disc(1888, 960, 18, fill=TURQ, ring=CREAM, rw=2)


def roster(P, D):
    for i, who in enumerate(CELL):
        x, y, d = 22, 18 + i * 106, 74; sel = who == D['sel']
        if sel: P.disc(x - 6, y - 6, d + 12, fill=None, ring=TURQ, rw=3.5)
        roundel(P, x, y, d, fill=DEEP); face(P, who, x + 6, y + 6, d - 12, ring=CREAM, rw=1.5)
        nm = NAMES[who].upper(); tw_ = sum(P.tw(c, 12, 'Jost-SemiBold') + 2 for c in nm) + 18; tx = x + d / 2 - tw_ / 2
        sign(P, tx, y + d - 8, tw_, 22, fill=BODY_, r=11, inline=BODY_); caps(P, x + d / 2, y + d + 3, nm, size=12, col=CREAM, gap=2, anchor='m')
        hp, mx = D['hp'][who]
        for k in range(mx): P.disc(x + d + 6, y + 8 + k * 7, 5, fill=CREAM if k < hp else None, ring=None if k < hp else CREAM, rw=1)
        b = D['busy'].get(who)
        if b:
            ic, t, left = b; arc(P, x + d / 2, y + d / 2, d / 2 - 2, 0, 360 * left, ORANGE, 5)
            roundel(P, x + d - 20, y + d - 30, 32, fill=BODY_); P.icon(ic, x + d - 12, y + d - 22, 16, col=CREAM)
            ticket(P, x + d + 16, y + d - 28, P.tw(t, 15) + 16, 24); P.put(x + d + 24, y + d - 16, t, 15, col=INK, face='Jost-Bold')
    roundel(P, 30, 440, 54, fill=DEEP); P.icon('units', 44, 453, 26, col=CREAM); sign(P, 22, 488, 70, 22, fill=BODY_, r=11, inline=BODY_); caps(P, 57, 499, 'squad', size=11, col=CREAM, anchor='m')


def scope(P, key):
    """The plotting scope as a round enamel instrument: the real map behind round glass, its buttons as roundels on the rim."""
    cx, cy, r = 120, 978, 92; roundel(P, cx - r, cy - r, 2 * r, fill=DEEP, iw=2)
    m = Image.open(FRAMES[key][0] + '.png').convert('RGB').crop((40, 940, 200, 1062)).resize((round(150 * P.s), round(150 * 122 / 160 * P.s)), Image.LANCZOS)
    big = Image.new('RGB', (round(160 * P.s), round(160 * P.s)), (11, 31, 34)); big.paste(m, (round(5 * P.s), round(22 * P.s)))
    mk = Image.new('L', big.size, 0); ImageDraw.Draw(mk).ellipse((0, 0, big.width - 1, big.height - 1), fill=255); big = big.convert('RGBA'); big.putalpha(mk); P.paste(big, cx - 80, cy - 80)
    P.disc(cx - 80, cy - 80, 160, fill=None, ring=CREAM, rw=1.5)
    for (dx, dy), ic in (((-104, -62), 'turn_view'), ((-17, -112), 'eye_open'), ((70, -62), 'turn_view')): roundel(P, cx + dx, cy + dy, 34, fill=BODY_, iw=1.2); P.icon(ic, cx + dx + 8, cy + dy + 8, 18, col=CREAM)


def speech(P, sp):
    who, t, (x, y) = sp; w = P.tw(t, 16, 'Barlow-Medium') + 48; x0 = x - w / 2
    shadow(P, x0, y - 15, w, 30, 15, dy=2, a=60, blur=2); P.rrect(x0, y - 15, w, 30, 15, fill=PAGE, line=DEEP, lw=1.5); face(P, who, x0 + 3, y - 12, 24, ring=DEEP, rw=1.2)
    P.text(x0 + 35, y, t, 16, col=INK, face='Barlow-Medium'); P.words_add(t); P.line([(x, y + 15), (x, y + 40)], col=DEEP, lw=2)


def reporter(P, Rp, hover=True):
    """A small enamel roundel on a stem over the reporter: the way's icon, an orange sweep draining on its rim, the seconds on a ticket;
    the turquoise act ring while he can still be stopped. Hover: a sign with what he's doing, the fix, and the ticket of what it costs."""
    hx, hy = Rp['head']; d = 58; x, y = hx - d / 2, hy - 60 - d
    P.line([(hx, y + d), (hx, hy - 6)], col=BODY_, lw=3)
    P.disc(x - 10, y - 10, d + 20, fill=None, ring=TURQ, rw=2.5); roundel(P, x, y, d, fill=DEEP); arc(P, hx, y + d / 2, d / 2 - 2, 0, 360 * Rp['left'] / Rp['of'], ORANGE, 5)
    P.icon(Rp['way'], x + 16, y + 16, 26, col=CREAM)
    t = f"{Rp['left']} s"; ticket(P, hx - 22, y + d - 6, 44, 26); P.put(hx, y + d + 7, t, 17, col=ORANGE, face='Jost-Bold', anchor='mm')
    if not hover: return
    sx, sy, w, h = hx + 50, y - 16, 316, 96; sign(P, sx, sy, w, h, r=20)
    face(P, Rp['who'], sx + 12, sy + 12, 40, ring=CREAM, rw=1.5); caps(P, sx + 62, sy + 26, Rp['plate'], size=14, col=CREAM); P.put(sx + 62, sy + 48, Rp['fix'], 15, col=DIMC)
    ticket(P, sx + 12, sy + 62, w - 24, 26); caps(P, sx + 22, sy + 76, 'then', size=10, col=SOFT); P.icon('p_alert', sx + 62, sy + 65, 19, col=ORANGE)
    lamps(P, sx + 86, sy + 75, Rp['alert'], d=12, gap=4, off=SOFT); badge(P, 'signal_police', sx + 150, sy + 64, 22); P.put(sx + 176, sy + 76, 'police', 15, col=INK, face='Jost-SemiBold')


def aim(P, A):
    hx, hy = A['head']; d = 50; roundel(P, hx - d / 2, hy - 66 - d, d, fill=BODY_, inline=AMBER, iw=2.2); face(P, A['at'], hx - d / 2 + 6, hy - 60 - d, d - 12, ring=None)
    P.line([(hx, hy - 66), (hx, hy - 10)], col=AMBER, lw=2.5)
    sx, sy, w, h = hx + 70, hy - 128, 380, 152; sign(P, sx, sy, w, h, r=26)
    roundel(P, sx - 34, sy + 18, 62, fill=BODY_); face(P, A['who'], sx - 28, sy + 24, 50, ring=None)
    P.disc(sx + 38, sy + 18, 48, fill=ORANGE, ring=CREAM, rw=2); P.icon('fire', sx + 50, sy + 30, 24, col=CREAM); arc(P, sx + 62, sy + 42, 29, 0, 250, TURQ, 4)
    caps(P, sx + 96, sy + 34, 'fire · ' + A['m'], size=14, col=CREAM)
    ticket(P, sx + 96, sy + 50, 128, 58); P.put(sx + 160, sy + 80, f"{A['pct']}%", 40, col=INK, face='Jost-Bold', anchor='mm')
    P.put(sx + 236, sy + 70, 'down in', 15, col=DIMC); P.put(sx + 236, sy + 92, 'about ' + A['down'], 18, col=CREAM, face='Jost-SemiBold')
    y = sy + 130; x = sx + 22; P.line([(sx + 18, sy + 114), (sx + w - 18, sy + 114)], col=DIMC, lw=1)
    P.icon('p_alert', x, y - 11, 21, col=ORANGE); x += lamps(P, x + 26, y, A['alert'], d=12, gap=4) + 40
    badge(P, 'signal_police', x, y - 13, 26); P.put(x + 30, y, A['police'], 17, col=CREAM, face='Jost-Bold'); x += 30 + P.tw(A['police'], 17) + 18
    P.icon('lock', x, y - 10, 19, col=CREAM); P.put(x + 23, y, A['seal'], 17, col=CREAM, face='Jost-Bold'); x += 23 + P.tw(A['seal'], 17) + 18
    P.icon('p_trail', x, y - 11, 21, col=CREAM); P.put(x + 25, y, A['trail'], 17, col=CREAM, face='Jost-Bold')
    t = 'GOES LOUD'; tw_ = sum(P.tw(c, 12, 'Jost-SemiBold') + 2 for c in t) + 22; sign(P, sx + w - tw_ - 16, sy - 14, tw_, 26, fill=ORANGE, r=13); caps(P, sx + w - tw_ / 2 - 16, sy - 1, t, size=12, col=CREAM, gap=2, anchor='m')


def police_badge(P, x, y, d):
    roundel(P, x, y, d, fill=ORANGE, iw=2.2); badge(P, 'signal_police', x + d * .17, y + d * .17, d * .66)


def car_badge(P, x, y, d):
    roundel(P, x, y, d, fill=ORANGE, iw=2.2); P.icon('car', x + d * .24, y + d * .24, d * .52, col=CREAM)


def render():
    out = {}
    P0 = base('loud'); P = base('loud'); speech(P, LOUD['speech']); roster(P, LOUD); scope(P, 'loud'); clock(P, LOUD, seal=LOUD['seal'])
    pc = LOUD['police']; card(P, {'title': pc['title'], 'line': f"{pc['short']} {pc['where']}", 'when': pc['when'], 'of': pc['of']}, police_badge, big=('here in', pc['in']))
    out['loud'] = (save(P, 'B-sign-loud'), hud_cover(P0, P))
    P0 = base('quiet'); P = base('quiet'); speech(P, QUIET['speech']); roster(P, QUIET); scope(P, 'quiet'); clock(P, QUIET)
    card(P, QUIET['feed'], car_badge); reporter(P, QUIET['report']); out['quiet'] = (save(P, 'B-sign-quiet'), hud_cover(P0, P))
    P0 = base('aim'); P = base('aim'); roster(P, AIM); scope(P, 'aim'); clock(P, AIM); book(P); aim(P, AIM['aim'])
    out['aim'] = (save(P, 'B-sign-aim'), hud_cover(P0, P))
    return out


if __name__ == '__main__':
    for k, (im, cov) in render().items(): print(k, f'HUD {cov:.1%}')
