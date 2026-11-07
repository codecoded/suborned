"""C · In the street. The corners keep almost nothing; the city is annotated where things happen, like a plan: ink labels with a cream
halo straight on the street, painted faces on stems, thin lines from a face or a cause to its place, rings on the ground. The seal is the
district closing in: an orange edge round the whole view with its count in the top edge. Off-screen news docks at the edge it comes
from. Glyph, brief 101. Mock only."""
import sys, os
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from h_base import *


KNOWN = [('waitress', 1), ('guard', 1), ('driver', 0), ('councillor', 1)]  # the mission's key people; 0 = not yet known (grey). Illustrative states


def label(P, x, y, t, size, col=INK, face_='Jost-Bold', anchor='lm', halo=PAGE, hr=3.6, ha=240):
    """A map label: ink type with a soft cream halo, readable on the paving and the road alike."""
    s = P.s; f = font(face_, round(size * s)); l, t0, r, b = f.getbbox(t, anchor=anchor); pad = int(hr * s * 3); W, H = r - l + 2 * pad, b - t0 + 2 * pad
    m = Image.new('L', (W, H), 0); ImageDraw.Draw(m).text((pad - l, pad - t0), t, font=f, fill=255, anchor=anchor)
    k = int(hr * s * 2) | 1; g = m.filter(ImageFilter.MaxFilter(k)).filter(ImageFilter.GaussianBlur(hr * s * .55))
    ox, oy = round(x * s) + l - pad, round(y * s) + t0 - pad
    lay_ = Image.new('RGBA', (W, H), halo[:3] + (0,)); lay_.putalpha(g.point(lambda v: v * ha // 255)); P.img.alpha_composite(lay_, (ox, oy))
    tx = Image.new('RGBA', (W, H), col[:3] + (0,)); tx.putalpha(m); P.img.alpha_composite(tx, (ox, oy)); P.words_add(t); return (r - l) / s


def ink_icon(P, name, x, y, d, col=INK, halo=PAGE):
    s = P.s; n = round(d * s); a = sprite('icons/singles/' + name).getchannel('A').resize((n, n), Image.LANCZOS); pad = round(4 * s)
    m = Image.new('L', (n + 2 * pad, n + 2 * pad), 0); m.paste(a, (pad, pad)); g = m.filter(ImageFilter.MaxFilter(round(5 * s) | 1)).filter(ImageFilter.GaussianBlur(2 * s))
    lay_ = Image.new('RGBA', m.size, halo[:3] + (0,)); lay_.putalpha(g.point(lambda v: v * 235 // 255)); P.img.alpha_composite(lay_, (round(x * s) - pad, round(y * s) - pad))
    P.icon(name, x, y, d, col=col)


def lamps(P, x, y, n, d=15, gap=5):
    for i in range(3):
        if i < n: P.disc(x + i * (d + gap), y - d / 2, d, fill=ORANGE, ring=PAGE, rw=2)
        else: P.disc(x + i * (d + gap), y - d / 2, d, fill=PAGE, ring=INK, rw=1.6)
    return 3 * d + 2 * gap


def tab(P, x, y, d, icon, fill=BODY_):
    disc_shadow(P, x, y, d, a=60); P.disc(x, y, d, fill=fill, ring=PAGE, rw=2); P.icon(icon, x + d * .24, y + d * .24, d * .52, col=CREAM)


def clock(P, D):
    X = 1896; w = label(P, X, 44, D['clock'], 46, anchor='rm'); tab(P, X - w - 54, 24, 40, 'pause')
    y = 94; x = X; t = str(D['trail']); x -= label(P, x, y, t, 20, anchor='rm'); ink_icon(P, 'p_trail', x - 27, y - 11, 22); x -= 27 + 18
    x -= lamps(P, x - 55, y, D['alert']); ink_icon(P, 'p_alert', x - 30, y - 11, 22, col=ORANGE if D['alert'] else INK)
    y = 128; x = X - label(P, X, y, D['cash'], 19, face_='Jost-SemiBold', anchor='rm'); ink_icon(P, 'p_cash', x - 28, y - 11, 22)
    ic, word, t = D['next']; y = 162; x = X - label(P, X, y, t, 19, anchor='rm'); x -= 8 + label(P, x - 8, y, word, 15, face_='Jost-Medium', col=SOFT, anchor='rm')
    P.disc(x - 36, y - 14, 28, fill=DEEP, ring=PAGE, rw=2); P.icon(ic, x - 30, y - 8, 16, col=CREAM)
    for i, (who, k) in enumerate(KNOWN):  # the key people as small faces under the clock, grey until known
        kx = 1896 - 32 - (3 - i) * 36; disc_shadow(P, kx, 184, 30, a=60); face(P, who, kx, 184, 30, ring=PAGE, rw=2, gray=not k)


def seal_edge(P, S):
    """The district closing in: an orange edge round the view, a soft glow inward; its count in a tab in the top edge, the job under it."""
    W, H = 1920, 1080; s = P.s; m = Image.new('L', (W * s // 2, H * s // 2), 0); g = ImageDraw.Draw(m); k = s / 2
    g.rectangle((0, 0, W * k, H * k), fill=255); g.rounded_rectangle((7 * k, 7 * k, (W - 7) * k, (H - 7) * k), 22 * k, fill=0); m = m.resize((W * s, H * s), Image.BICUBIC)
    glow = Image.new('RGBA', P.img.size, ORANGE[:3] + (0,)); glow.putalpha(m.filter(ImageFilter.GaussianBlur(16 * s)).point(lambda v: v * 120 // 255)); P.img.alpha_composite(glow)
    edge = Image.new('RGBA', P.img.size, ORANGE[:3] + (0,)); edge.putalpha(m); P.img.alpha_composite(edge)
    tx, tw_, th = 960 - 128, 256, 70; shadow(P, tx, -20, tw_, th + 20, 24, a=50); P.rrect(tx, -20, tw_, th + 20, 24, fill=ORANGE)
    P.icon('lock', tx + 26, 18, 30, col=CREAM); P.put(tx + 66, 34, S['left'], 44, col=CREAM, face='Jost-Bold'); caps(P, tx + 66 + P.tw(S['left'], 44) + 10, 22, 'seals', size=12, col=CREAM)
    P.put(tx + 66 + P.tw(S['left'], 44) + 10, 44, S['at'], 17, col=CREAM, face='Jost-Bold')
    P.disc(tx - 20, 34, 34, fill=BODY_, ring=PAGE, rw=2); P.icon(S['cause'][0], tx - 12, 42, 18, col=CREAM)
    gx, gy = 960, 104; face(P, 'councillor', gx - 74, gy - 20, 40, ring=PAGE, rw=2.5)
    for k_ in range(4): P.disc(gx - 26 + k_ * 12, gy - 3, 6, fill=ORANGE, ring=PAGE, rw=1)
    P.disc(gx + 26, gy - 20, 40, fill=PAGE, ring=DEEP, rw=2.5); P.icon('car', gx + 35, gy - 11, 22, col=DEEP)


def dock(P, x, y, d, badge_fn, words, num=None, side='r'):
    """News from off screen: its badge docked at the edge it comes from, a chevron, the words as labels; the number big."""
    disc_shadow(P, x, y, d, a=70); badge_fn(P, x, y, d)
    cx, cy = x + d / 2, y + d / 2; ch = [(x + d + 4, cy - 10), (x + d + 16, cy), (x + d + 4, cy + 10)] if side == 'r' else [(cx - 10, y + d + 4), (cx, y + d + 16), (cx + 10, y + d + 4)]
    g = ImageDraw.Draw(P.img); g.polygon([(a * P.s, b * P.s) for a, b in ch], fill=ORANGE)
    lx = x - 12 if side == 'r' else x + d + 12; anc = 'rm' if side == 'r' else 'lm'
    if num: label(P, lx, cy - 10, num, 40, col=ORANGE, anchor=anc); label(P, lx, cy + 24, words, 18, anchor=anc)
    else: label(P, lx, cy, words, 22, anchor=anc)


def why_tag(P, x, y, icon, t, when):
    """Where it happened: the cause's icon in an orange disc on the spot, its short line and time as labels."""
    P.disc(x - 16, y - 16, 32, fill=ORANGE, ring=PAGE, rw=2.5); P.icon(icon, x - 9, y - 9, 18, col=CREAM)
    w = label(P, x + 24, y - 2, t, 19); label(P, x + 24 + w + 10, y - 1, when, 16, col=SOFT, face_='Jost-SemiBold')


def dotted(P, pts, col=ORANGE, step=13, d=5):
    for (ax, ay), (bx, by) in zip(pts, pts[1:]):
        n = int(math.dist((ax, ay), (bx, by)) // step)
        for i in range(n + 1): t = i / max(n, 1); P.disc(ax + (bx - ax) * t - d / 2, ay + (by - ay) * t - d / 2, d, fill=col, ring=None)


def ground_arc(P, cx, cy, rx, frac, col, lw, ratio=.42, full=None):
    """A ring on the ground at the play camera's tilt; `frac` of it drawn from the back, clockwise; `full` draws the whole ring faintly first."""
    s = P.s; k = 3; ry = rx * ratio; pad = lw * 2 + 4; W, H = round((rx + pad) * 2 * s * k), round((ry + pad) * 2 * s * k)
    big = Image.new('RGBA', (W, H), (0, 0, 0, 0)); g = ImageDraw.Draw(big); bb = (pad * s * k, pad * s * k, W - pad * s * k, H - pad * s * k)
    if full: g.ellipse(bb, outline=full, width=round(lw * .6 * s * k))
    g.arc(bb, -90, -90 + 360 * frac, fill=col, width=round(lw * s * k)); out = big.resize((W // k, H // k), Image.LANCZOS)
    P.img.alpha_composite(out, (round((cx - rx - pad) * s), round((cy - ry - pad) * s)))


def roster(P, D):
    for i, who in enumerate(CELL):
        x, y, d = 24, 22 + i * 102, 64; sel = who == D['sel']; disc_shadow(P, x, y, d, a=80)
        if sel: P.disc(x - 5, y - 5, d + 10, fill=None, ring=TURQ, rw=3.5)
        face(P, who, x, y, d, ring=PAGE, rw=2.5); label(P, x + d / 2, y + d + 12, NAMES[who], 14, face_='Jost-SemiBold', anchor='mm')
        hp, mx = D['hp'][who]
        for k in range(mx): P.disc(x + d + 6, y + 6 + k * 7, 5, fill=DEEP if k < hp else PAGE, ring=PAGE, rw=1)
        b = D['busy'].get(who)
        if b:
            ic, t, left = b; arc(P, x + d / 2, y + d / 2, d / 2 + 3, 0, 360 * left, ORANGE, 4.5)
            P.disc(x + d - 18, y + d - 24, 28, fill=BODY_, ring=PAGE, rw=2); P.icon(ic, x + d - 12, y + d - 18, 16, col=CREAM); label(P, x + d + 16, y + d - 10, t, 16)
    tab(P, 34, 442, 44, 'units'); label(P, 56, 500, 'Squad', 13, face_='Jost-SemiBold', anchor='mm')


def scope(P, key):
    """The plotting scope as lines on the street: the real map's light parts printed in ink with a cream halo, inside four corner marks."""
    a = np.array(Image.open(FRAMES[key][0] + '.png').convert('RGB').crop((24, 944, 216, 1058))).astype(float); L = a @ [0.3, 0.59, 0.11]
    al = Image.fromarray(np.clip((L - 46) / 110 * 255, 0, 255).astype(np.uint8)).resize((round(192 * P.s), round(114 * P.s)), Image.LANCZOS)
    x, y = 24, 940; g = al.filter(ImageFilter.MaxFilter(7)).filter(ImageFilter.GaussianBlur(4))
    h = Image.new('RGBA', al.size, PAGE[:3] + (0,)); h.putalpha(g.point(lambda v: min(255, v * 2))); P.paste(h, x, y)
    t = Image.new('RGBA', al.size, DEEP[:3] + (0,)); t.putalpha(al); P.paste(t, x, y)
    for (cx, cy, sx, sy) in ((16, 932, 1, 1), (224, 932, -1, 1), (16, 1062, 1, -1), (224, 1062, -1, -1)):
        P.line([(cx, cy), (cx + 18 * sx, cy)], col=PAGE, lw=5); P.line([(cx, cy), (cx, cy + 18 * sy)], col=PAGE, lw=5)
        P.line([(cx, cy), (cx + 18 * sx, cy)], col=INK, lw=2.5); P.line([(cx, cy), (cx, cy + 18 * sy)], col=INK, lw=2.5)
    for xx, ic in ((4, 'turn_view'), (103, 'eye_open'), (202, 'turn_view')): tab(P, xx, 896, 34, ic)


def speech(P, sp):
    who, t, (x, y) = sp; w = P.tw(t, 16, 'Barlow-Medium') + 46; x0 = x - w / 2
    shadow(P, x0, y - 14, w, 30, 15, a=40, blur=3); P.rrect(x0, y - 14, w, 30, 15, fill=PAGE); face(P, who, x0 + 3, y - 11, 24, ring=None)
    P.text(x0 + 34, y + 1, t, 16, col=INK, face='Barlow-Medium'); P.words_add(t); P.line([(x, y + 16), (x, y + 40)], col=PAGE, lw=2)


def reporter(P, Rp, hover=True):
    """The report on its way, on the reporter: a ring on the ground at his feet drains orange to the landing; his face on a stem with the
    way's icon and the seconds; the turquoise act ring while he can be stopped; on hover, what he's doing and what it costs, as labels."""
    hx, hy = Rp['head']; fx, fy = Rp['feet']; ground_arc(P, fx, fy, 34, Rp['left'] / Rp['of'], ORANGE, 5, full=PAGE[:3] + (200,))
    d = 54; x, y = hx - d / 2, hy - 58 - d; P.line([(hx, y + d), (hx, hy - 6)], col=PAGE, lw=2.5)
    P.disc(x - 8, y - 8, d + 16, fill=None, ring=TURQ, rw=2.5); disc_shadow(P, x, y, d, a=70); face(P, Rp['who'], x, y, d, ring=PAGE, rw=2.5)
    P.disc(x + d - 14, y - 6, 28, fill=BODY_, ring=PAGE, rw=2); P.icon(Rp['way'], x + d - 8, y, 16, col=CREAM)
    label(P, x - 10, y + d / 2, f"{Rp['left']} s", 30, col=ORANGE, anchor='rm')
    if not hover: return
    lx = x + d + 22; label(P, lx, y + 10, Rp['plate'], 20); label(P, lx, y + 36, Rp['fix'], 16, col=DEEP, face_='Jost-SemiBold')
    yy = y + 64; w = label(P, lx, yy, 'then', 14, col=SOFT, face_='Jost-Medium'); ink_icon(P, 'p_alert', lx + w + 10, yy - 10, 20, col=ORANGE)
    lamps(P, lx + w + 36, yy, Rp['alert'], d=13, gap=4); P.disc(lx + w + 104, yy - 13, 26, fill=PAGE, ring=None); badge(P, 'signal_police', lx + w + 105, yy - 12, 24)
    label(P, lx + w + 136, yy, 'police', 16, face_='Jost-SemiBold')


def aim(P, A):
    """The chance on the ground: the target's ring becomes a gauge filled to the odds, the number beside it; his face over his head in his
    signal's amber; what going loud costs as one row of labels by the shooter."""
    hx, hy = A['head']; fx, fy = A['feet']; ground_arc(P, fx, fy - 2, 46, A['pct'] / 100, AMBER, 6, full=PAGE[:3] + (190,))
    d = 46; disc_shadow(P, hx - d / 2, hy - 62 - d, d, a=70); face(P, A['at'], hx - d / 2, hy - 62 - d, d, ring=AMBER, rw=3.5)
    P.line([(hx, hy - 62), (hx, hy - 10)], col=AMBER, lw=2)
    lx = fx + 64; label(P, lx, fy - 34, f"{A['pct']}%", 50); label(P, lx + 2, fy + 4, 'down in about ' + A['down'], 17, face_='Jost-SemiBold')
    label(P, lx + 2, fy + 28, 'Fire · ' + A['m'], 15, col=SOFT, face_='Jost-Medium')
    sx, sy = A['from']; y = sy + 70; x = sx - 120
    t = 'Goes loud'; tw_ = P.tw(t, 15, 'Jost-Bold') + 22; P.rrect(x, y - 13, tw_, 26, 13, fill=ORANGE); P.put(x + 11, y, t, 15, col=CREAM, face='Jost-Bold'); x += tw_ + 14
    ink_icon(P, 'p_alert', x, y - 11, 21, col=ORANGE); x += 26 + lamps(P, x + 26, y, A['alert'], d=13, gap=4) + 16
    P.disc(x, y - 14, 28, fill=PAGE, ring=None); badge(P, 'signal_police', x + 1, y - 13, 26); x += 30 + label(P, x + 30, y, A['police'], 18) + 16
    ink_icon(P, 'lock', x, y - 10, 19); x += 23 + label(P, x + 23, y, A['seal'], 18) + 16
    ink_icon(P, 'p_trail', x, y - 11, 21); label(P, x + 25, y, A['trail'], 18)


def police_badge(P, x, y, d):
    P.disc(x, y, d, fill=ORANGE, ring=PAGE, rw=3); badge(P, 'signal_police', x + d * .14, y + d * .14, d * .72)


def car_badge(P, x, y, d):
    P.disc(x, y, d, fill=ORANGE, ring=PAGE, rw=3); P.icon('car', x + d * .22, y + d * .22, d * .56, col=CREAM)


def book(P):
    tab(P, 1834, 994, 70, 'papers'); label(P, 1869, 1072, 'Book', 13, face_='Jost-SemiBold', anchor='mm'); P.disc(1890, 994, 16, fill=TURQ, ring=PAGE, rw=2)


def render():
    out = {}
    P0 = base('loud'); P = base('loud'); speech(P, LOUD['speech']); roster(P, LOUD); scope(P, 'loud'); clock(P, LOUD); book(P)
    pc = LOUD['police']; dotted(P, [(1790, 600), (1300, 480), (560, 258)], col=ORANGE[:3] + (210,)); why_tag(P, 540, 250, 'fire', pc['short'], pc['when'])
    dock(P, 1806, 556, 88, police_badge, pc['title'], num=pc['in']); seal_edge(P, LOUD['seal'])
    out['loud'] = (save(P, 'C-street-loud'), hud_cover(P0, P))
    P0 = base('quiet'); P = base('quiet'); speech(P, QUIET['speech']); roster(P, QUIET); scope(P, 'quiet'); clock(P, QUIET); book(P)
    dock(P, 1806, 820, 80, car_badge, QUIET['feed']['title']); reporter(P, QUIET['report']); out['quiet'] = (save(P, 'C-street-quiet'), hud_cover(P0, P))
    P0 = base('aim'); P = base('aim'); roster(P, AIM); scope(P, 'aim'); clock(P, AIM); book(P); aim(P, AIM['aim'])
    out['aim'] = (save(P, 'C-street-aim'), hud_cover(P0, P))
    return out


if __name__ == '__main__':
    for k, (im, cov) in render().items(): print(k, f'HUD {cov:.1%}')
