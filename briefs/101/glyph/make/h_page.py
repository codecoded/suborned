"""A · The page's edge. The book's cream page peeks into play at the right-hand corners, cut by one big curve with the book's turquoise halo:
the clock and the numbers at the top, the one crucial card at the bottom with its face breaking the curve. When something matters the
corner grows (the seal turns its halo orange). Ink type, the book's round deep-teal tabs, painted faces, orange for now. Glyph, brief 101."""
import sys, os
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from h_base import *

def corner(P, c, rx, ry, halo=TURQ):
    """A page corner: a quarter of an ellipse centred on the screen's corner, cream, the book's turquoise halo on its curve."""
    cx, cy = c
    return blob(P, lambda g, s: g.ellipse(((cx - rx) * s, (cy - ry) * s, (cx + rx) * s, (cy + ry) * s), fill=255), PAGE[:3] + (250,), halo=halo, halo_a=175, halo_blur=12, sh=(3, 40, 6))


KNOWN_ = [('waitress', 1), ('guard', 1), ('driver', 0), ('councillor', 1)]  # the mission's key people; 0 = not yet known (grey). Illustrative states


def tab(P, x, y, d, icon, word=None, fill=BODY_, size=None):
    """The book's round tab: a deep-teal disc, a cream pictogram, its word under the icon inside the disc."""
    disc_shadow(P, x, y, d, a=60); P.disc(x, y, d, fill=fill, ring=None)
    if word: P.icon(icon, x + d * .27, y + d * .14, d * .46, col=CREAM); P.put(x + d / 2, y + d * .76, word, size or d * .2, col=CREAM, face='Jost-Bold', anchor='mm')
    else: P.icon(icon, x + d * .24, y + d * .24, d * .52, col=CREAM)


def lamps(P, x, y, n, d=15, gap=5):
    """Alert as three lamps on a short line (the book's track): lit orange, unlit hollow. Returns the width."""
    P.line([(x + d / 2, y), (x + 2 * (d + gap) + d / 2, y)], col=HAIR, lw=2)
    for i in range(3):
        if i < n: P.disc(x + i * (d + gap), y - d / 2, d, fill=ORANGE, ring=None)
        else: P.disc(x + i * (d + gap), y - d / 2, d, fill=PAGE, ring=SOFT, rw=1.6)
    return 3 * d + 2 * gap


def clock_corner(P, D, seal=None):
    rx, ry = (406, 398) if seal else (338, 252); corner(P, (1920, 0), rx, ry, halo=ORANGE if seal else TURQ); X = 1892
    for i, (who, k) in enumerate(KNOWN_):  # the key people sit on the curve, their faces breaking its edge; grey until known
        f = math.radians((72 if seal else 62) + i * (5 if seal else 8)); cx, cy = 1920 - rx * math.cos(f), ry * math.sin(f); disc_shadow(P, cx - 18, cy - 18, 36, a=60)
        face(P, who, cx - 18, cy - 18, 36, ring=PAGE if k else HAIR, rw=2.5, gray=not k)
    P.put(X, 46, D['clock'], 50, col=INK, face='Jost-Bold', anchor='rm'); cw = P.tw(D['clock'], 50)
    tab(P, X - cw - 62, 24, 44, 'pause')
    # Alert lamps and Trail on one row; cash; the next stop as a disc and its time
    w = P.tw(str(D['trail']), 21, 'Jost-Bold'); P.put(X, 101, str(D['trail']), 21, col=INK, face='Jost-Bold', anchor='rm'); P.icon('p_trail', X - w - 26, 90, 22, col=INK)
    lx = X - w - 26 - 22 - 70; lamps(P, lx, 101, D['alert']); P.icon('p_alert', lx - 30, 90, 22, col=ORANGE if D['alert'] else INK)
    P.put(X, 140, D['cash'], 21, col=INK, face='Jost-SemiBold', anchor='rm'); P.icon('p_cash', X - P.tw(D['cash'], 21, 'Jost-SemiBold') - 29, 129, 22, col=INK)
    ic, word, t = D['next']; tw_ = P.tw(t, 21, 'Jost-Bold'); P.put(X, 177, t, 21, col=INK, face='Jost-Bold', anchor='rm')
    ww = P.tw(word, 16, 'Jost-Medium'); P.put(X - tw_ - 8, 178, word, 16, col=SOFT, anchor='rm'); dx = X - tw_ - 8 - ww - 38
    P.disc(dx, 163, 30, fill=DEEP, ring=None); P.icon(ic, dx + 6, 169, 18, col=CREAM)
    if not seal: return
    # the seal: the corner grows, its halo goes orange; lock disc, the m:ss huge, when it seals; the job as a picture (Popovic -> our car)
    y = 262; P.put(X, y, seal['left'], 70, col=ORANGE, face='Jost-Bold', anchor='rm'); sw = P.tw(seal['left'], 70)
    lx = X - sw - 86; disc_shadow(P, lx, y - 38, 74, a=50); P.disc(lx, y - 38, 74, fill=ORANGE, ring=None); P.icon('lock', lx + 19, y - 19, 36, col=CREAM)
    P.disc(lx - 14, y + 4, 34, fill=BODY_, ring=PAGE, rw=2.5); P.icon(seal['cause'][0], lx - 6, y + 12, 18, col=CREAM)  # its cause, one icon (hover: "shots fired")
    caps(P, X, y + 44, 'seals ' + seal['at'], size=15, col=ORANGE, anchor='r')
    gy = 340; car = X - 44; face(P, 'councillor', car - 128, gy - 24, 48, ring=PAGE, rw=2.5)
    for k in range(4): P.disc(car - 70 + k * 14, gy - 2, 5, fill=SOFT, ring=None)
    P.disc(car, gy - 22, 44, fill=PAGE, ring=DEEP, rw=2.5); P.icon('car', car + 10, gy - 12, 24, col=DEEP)


def cx_at(rx, ry, y, c=(1920, 1080)):
    """Where a corner's curve is on row y."""
    t = (c[1] - y) / ry; return c[0] - rx * math.sqrt(max(0.0, 1 - t * t))


def card_corner(P, C, badge_fn, big=None):
    """The one crucial card: the bottom corner grows round it; its badge sits on the curve, breaking it; the title beside the badge; one
    line with when along the curve; its number big when it has one; 1 of n; the Book tab."""
    rx, ry = (446, 240) if big else (430, 196); corner(P, (1920, 1080), rx, ry); top = 1080 - ry
    bd = 84; byc = top + (64 if big else 56); bxc = cx_at(rx, ry, byc) + 14; disc_shadow(P, bxc - bd / 2, byc - bd / 2, bd, a=70); badge_fn(P, bxc - bd / 2, byc - bd / 2, bd)
    tx = bxc + bd / 2 + 12; size = next((z for z in (30, 28, 26, 24, 22) if P.tw(C['title'], z) <= 1906 - tx), 22); P.put(tx, byc - 4, C['title'], size, col=INK, face='Jost-Bold')
    ly = byc + (54 if big else 44); lx = cx_at(rx, ry, ly - 12) + 18; right = 1910 if ly + 10 < 976 else 1810  # clear of the Book tab
    line = C['line'] + ' · ' + C['when']; ls = next((z for z in (19, 18, 17) if P.tw(line, z, 'Jost-Medium') <= right - lx), None)
    if ls: P.put(lx, ly, line, ls, col=SOFT, face='Jost-Medium')
    else: P.put(lx, ly, C['line'], 18, col=SOFT, face='Jost-Medium'); P.put(cx_at(rx, ry, ly + 18) + 18, ly + 30, C['when'], 17, col=SOFT, face='Jost-SemiBold')
    if big:
        cap, num = big; nx = cx_at(rx, ry, ly + 24) + 22; caps(P, nx + 2, ly + 36, cap, size=13, col=ORANGE); P.put(nx, ly + 74, num, 48, col=ORANGE, face='Jost-Bold')
    i, n = C['of']; P.put(1792, 1058, f'{i} of {n}', 16, col=SOFT, face='Jost-SemiBold', anchor='rm'); P.text(1803, 1057, '›', 22, col=DEEP, face='Jost-Bold', anchor='mm')
    tab(P, 1822, 978, 84, 'papers', 'Book', size=15); P.disc(1890, 982, 16, fill=TURQ, ring=PAGE, rw=2)


def book_corner(P):
    """At rest: only the Book tab on a small corner of page."""
    corner(P, (1920, 1080), 156, 150); tab(P, 1822, 978, 84, 'papers', 'Book', size=15); P.disc(1890, 982, 16, fill=TURQ, ring=PAGE, rw=2)


def roster(P, D):
    """The cell as the book's faces: painted, ringed, a name tag; busy is an orange sweep round the face with its act and seconds."""
    for i, who in enumerate(CELL):
        x, y, d = 22, 22 + i * 104, 70; sel = who == D['sel']; disc_shadow(P, x, y, d, a=80)
        if sel: P.disc(x - 5, y - 5, d + 10, fill=None, ring=TURQ, rw=3.5)
        face(P, who, x, y, d, ring=PAGE, rw=3)
        hp, mx = D['hp'][who]; tw_ = P.tw(NAMES[who], 14, 'Jost-SemiBold') + 18; tx = x + d / 2 - tw_ / 2
        shadow(P, tx, y + d - 6, tw_, 22, 11, a=40, blur=3); P.rrect(tx, y + d - 6, tw_, 22, 11, fill=PAGE); P.put(x + d / 2, y + d + 5, NAMES[who], 14, col=INK, face='Jost-SemiBold', anchor='mm')
        for k in range(mx): P.disc(x + d + 8, y + 10 + k * 7, 5, fill=DEEP if k < hp else None, ring=None if k < hp else SOFT, rw=1)
        b = D['busy'].get(who)
        if b:
            ic, t, left = b; arc(P, x + d / 2, y + d / 2, d / 2 + 3, 0, 360 * left, ORANGE, 4.5)
            P.disc(x + d - 18, y + d - 26, 30, fill=BODY_, ring=PAGE, rw=2); P.icon(ic, x + d - 12, y + d - 20, 18, col=CREAM)
            tw2 = P.tw(t, 15, 'Jost-Bold') + 16; P.rrect(x + d + 18, y + d - 22, tw2, 24, 12, fill=PAGE); P.put(x + d + 26, y + d - 10, t, 15, col=INK, face='Jost-Bold')
    tab(P, 30, 448, 54, 'units', 'Squad', size=11)


def scope(P, key):
    """The plot scope on a page corner: the real map reprinted in the book's inks; its turn buttons and the eye as round tabs on the rim."""
    blob(P, lambda g, s: g.rounded_rectangle((14 * s, 920 * s, 236 * s, 1066 * s), 26 * s, fill=255), PAGE[:3] + (250,), halo=TURQ, halo_a=120, halo_blur=10, sh=(3, 46, 6))
    m = scope_map(key, [PAGE, (0xe4, 0xdf, 0xcf, 255), PALE, DEEP, INK], lo=24, span=150).resize((round(198 * P.s), round(118 * P.s)), Image.LANCZOS)
    mk = Image.new('L', m.size, 0); ImageDraw.Draw(mk).rounded_rectangle((0, 0, m.width - 1, m.height - 1), 18 * P.s, fill=255); m = m.convert('RGBA'); m.putalpha(mk); P.paste(m, 26, 936)
    for x, ic in ((2, 'turn_view'), (106, 'eye_open'), (208, 'turn_view')): tab(P, x, 900, 34, ic)


def speech(P, sp):
    """A bubble: cream, ink Barlow, the speaker's own small face at its start (not a letter)."""
    who, t, (x, y) = sp; w = P.tw(t, 16, 'Barlow-Medium') + 46; x0 = x - w / 2
    shadow(P, x0, y - 14, w, 30, 15, a=40, blur=3); P.rrect(x0, y - 14, w, 30, 15, fill=PAGE); face(P, who, x0 + 3, y - 11, 24, ring=None)
    P.text(x0 + 34, y + 1, t, 16, col=INK, face='Barlow-Medium'); P.words_add(t); P.line([(x, y + 16), (x, y + 40)], col=PAGE, lw=2)


def reporter(P, Rp, hover=True):
    """The reporter's countdown: his face on a stem over his head, an orange sweep draining to the landing, the way's icon, the seconds;
    the turquoise act ring while the cell can still stop him. Hover: what he's doing, the fix, and what it costs when it lands."""
    hx, hy = Rp['head']; d = 62; x, y = hx - d / 2, hy - 64 - d
    P.line([(hx, y + d + 10), (hx, hy - 6)], col=PAGE, lw=2.5)
    P.disc(x - 11, y - 11, d + 22, fill=None, ring=TURQ, rw=2.5); disc_shadow(P, x - 5, y - 5, d + 10, a=60)
    P.disc(x - 5, y - 5, d + 10, fill=PAGE, ring=None); arc(P, hx, y + d / 2, d / 2 + 2.5, 0, 360 * Rp['left'] / Rp['of'], ORANGE, 5)
    face(P, Rp['who'], x, y, d, ring=None)
    P.disc(x + d - 16, y + d - 22, 30, fill=BODY_, ring=PAGE, rw=2); P.icon(Rp['way'], x + d - 10, y + d - 16, 18, col=CREAM)
    t = f"{Rp['left']} s"; tw_ = P.tw(t, 17, 'Jost-Bold') + 18; P.rrect(x - 14, y + d - 20, tw_, 26, 13, fill=ORANGE); P.put(x - 5, y + d - 7, t, 17, col=CREAM, face='Jost-Bold')
    if not hover: return
    sx, sy, w, h = hx + 52, y - 10, 300, 112; shadow(P, sx, sy, w, h, 18, a=50); P.rrect(sx, sy, w, h, 18, fill=PAGE)
    P.put(sx + 18, sy + 26, Rp['plate'], 21, col=INK, face='Jost-Bold'); P.put(sx + 18, sy + 54, Rp['fix'], 16, col=DEEP, face='Jost-SemiBold')
    P.line([(sx + 16, sy + 74), (sx + w - 16, sy + 74)], col=HAIR, lw=1.5); caps(P, sx + 18, sy + 93, 'then', size=12, col=SOFT)
    P.icon('p_alert', sx + 64, sy + 82, 20, col=ORANGE); lamps(P, sx + 90, sy + 93, Rp['alert'], d=13, gap=4)
    badge(P, 'signal_police', sx + 156, sy + 80, 26); P.put(sx + 186, sy + 93, 'police', 16, col=INK, face='Jost-SemiBold')


def aim(P, A):
    """A hover and aim on a target: his face over his head in his signal's amber ring; the price on a cream slip with the shooter's face
    breaking its end: Fire, the chance huge, the time to down; then what going loud costs, as icons and numbers; the right hold sweeps."""
    hx, hy = A['head']; d = 48; disc_shadow(P, hx - d / 2, hy - 64 - d, d, a=70); face(P, A['at'], hx - d / 2, hy - 64 - d, d, ring=AMBER, rw=3.5)
    P.line([(hx, hy - 64 + 2), (hx, hy - 10)], col=AMBER, lw=2)
    sx, sy, w, h = hx + 62, hy - 196, 372, 150; shadow(P, sx, sy, w, h, 24, a=55); P.rrect(sx, sy, w, h, 24, fill=PAGE)
    face(P, A['who'], sx - 30, sy + 18, 60, ring=PAGE, rw=3)
    fx, fy = sx + 46, sy + 22; P.disc(fx, fy, 50, fill=BODY_, ring=None); P.icon('fire', fx + 12, fy + 12, 26, col=CREAM); arc(P, fx + 25, fy + 25, 29, 0, 250, ORANGE, 4)
    P.put(sx + 112, sy + 34, 'Fire', 22, col=INK, face='Jost-Bold'); P.put(sx + 166, sy + 35, '· ' + A['m'], 17, col=SOFT)
    P.put(sx + 110, sy + 82, f"{A['pct']}%", 54, col=INK, face='Jost-Bold'); pw = P.tw(f"{A['pct']}%", 54)
    P.put(sx + 122 + pw, sy + 72, 'down in', 15, col=SOFT); P.put(sx + 122 + pw, sy + 93, 'about ' + A['down'], 17, col=INK, face='Jost-SemiBold')
    P.line([(sx + 20, sy + 112), (sx + w - 20, sy + 112)], col=HAIR, lw=1.5); y = sy + 131; x = sx + 22
    P.icon('p_alert', x, y - 11, 21, col=ORANGE); x += lamps(P, x + 26, y, A['alert'], d=13, gap=4) + 40
    badge(P, 'signal_police', x, y - 13, 26); P.put(x + 30, y, A['police'], 17, col=INK, face='Jost-Bold'); x += 30 + P.tw(A['police'], 17) + 18
    P.icon('lock', x, y - 10, 19, col=INK); P.put(x + 23, y, A['seal'], 17, col=INK, face='Jost-Bold'); x += 23 + P.tw(A['seal'], 17) + 18
    P.icon('p_trail', x, y - 11, 21, col=INK); P.put(x + 25, y, A['trail'], 17, col=INK, face='Jost-Bold')
    t = 'Goes loud'; tw_ = P.tw(t, 15, 'Jost-Bold') + 24; P.rrect(sx + w - tw_ - 14, sy - 13, tw_, 26, 13, fill=ORANGE); P.put(sx + w - tw_ - 2, sy, t, 15, col=CREAM, face='Jost-Bold')


def police_badge(P, x, y, d):
    P.disc(x, y, d, fill=ORANGE, ring=PAGE, rw=3); badge(P, 'signal_police', x + d * .14, y + d * .14, d * .72)


def car_badge(P, x, y, d):
    P.disc(x, y, d, fill=ORANGE, ring=PAGE, rw=3); P.icon('car', x + d * .22, y + d * .22, d * .56, col=CREAM)


def render():
    out = {}
    P0 = base('loud'); P = base('loud'); speech(P, LOUD['speech']); roster(P, LOUD); scope(P, 'loud'); clock_corner(P, LOUD, seal=LOUD['seal'])
    pc = LOUD['police']; card_corner(P, {'title': pc['title'], 'line': f"{pc['short']} {pc['where']}", 'when': pc['when'], 'of': pc['of']}, police_badge, big=('here in', pc['in']))
    out['loud'] = (save(P, 'A-page-loud'), hud_cover(P0, P))
    P0 = base('quiet'); P = base('quiet'); speech(P, QUIET['speech']); roster(P, QUIET); scope(P, 'quiet'); clock_corner(P, QUIET)
    card_corner(P, QUIET['feed'], car_badge); reporter(P, QUIET['report']); out['quiet'] = (save(P, 'A-page-quiet'), hud_cover(P0, P))
    P0 = base('aim'); P = base('aim'); roster(P, AIM); scope(P, 'aim'); clock_corner(P, AIM); book_corner(P); aim(P, AIM['aim'])
    out['aim'] = (save(P, 'A-page-aim'), hud_cover(P0, P))
    return out


if __name__ == '__main__':
    for k, (im, cov) in render().items(): print(k, f'HUD {cov:.1%}')
