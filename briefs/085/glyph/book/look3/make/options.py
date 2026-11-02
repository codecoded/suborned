"""Three skins over the same slots. Glyph, brief 085."""
from slots import *
from content import *


def star(P, cx, cy, ro, ri, n, fill):
    """The one starburst: the sign you are on."""
    k = 4; R0 = round(ro * P.s * k) + 8; big = Image.new('RGBA', (2 * R0, 2 * R0), (0, 0, 0, 0)); pts = []
    for i in range(2 * n):
        r = (ro if i % 2 == 0 else ri) * P.s * k; a = math.pi * i / n - math.pi / 2; pts.append((R0 + r * math.cos(a), R0 + r * math.sin(a)))
    ImageDraw.Draw(big).polygon(pts, fill=fill); im = big.resize((2 * R0 // k, 2 * R0 // k), Image.LANCZOS); P.paste(im, cx - R0 / k / P.s, cy - R0 / k / P.s)


def clip_circle(P, cx, cy, r, fill):
    k = 2; S_ = P.s; big = Image.new('L', (P.w * S_ // k, P.h * S_ // k), 0); ImageDraw.Draw(big).ellipse(((cx - r) * S_ / k, (cy - r) * S_ / k, (cx + r) * S_ / k, (cy + r) * S_ / k), fill=255)
    m = big.resize((P.w * S_, P.h * S_), Image.LANCZOS); lay = Image.new('RGBA', P.img.size, fill); lay.putalpha(m); P.img.alpha_composite(lay)


def body(P, c, X, W, y0, ink, dim, on, fill, signs, heroes, label="She knows", shadow=None, bottom=1040, orb=None):
    """The page, the same order in every option: hero, name, state, facts, ways, scales, knows, track, others. Rule: facts take room from the hero. Hero and name step down together as facts grow."""
    if 'people' in c or 'groups' in c:
        P.put(X, y0 + 74, c['title'], 110, col=ink, face='Jost-Bold'); P.put(X + P.tw(c['title'], 110) + 30, y0 + 92, c['count'], 58, col=on, face='Jost-Medium')
        if 'people' in c: people_grid(P, c['people'], X + 10, y0 + 180, W, ink, dim, fill)
        else: groups(P, c['groups'], X + 4, y0 + 220, W, ink, dim, on)
        return
    n = len(c['facts']); hx, hy, hd, top = heroes[0 if n <= 4 else 1 if n <= 8 else 2]
    if orb: orbit(P, hx + hd / 2, hy + hd / 2, hd * .69, hd * .25, -32, orb, dot=ORANGE, at=330)
    if c.get('pic') or c.get('bust'): face_big(P, c.get('pic'), hx, hy, hd, fill=fill, shadow=shadow)
    else:
        if shadow: ImageDraw.Draw(P.img).ellipse(((hx + hd * .05) * 2, (hy + hd * .05) * 2, (hx + hd * 1.05) * 2, (hy + hd * 1.05) * 2), fill=shadow)
        glyph_big(P, c['glyph'], hx, hy, hd, fill=on, col=CREAM if ink == INK else BODY_)
    tx = hx + hd * 1.05 + 44; tw_ = X + W - tx; oth = c.get('others') or []; nm_w = tw_ - (len(oth) * 100 + 10 if oth else 0)
    s = name_size(c['name'], nm_w, P, top=top); P.put(tx, y0 + s * .52, c['name'], s, col=ink, face='Jost-Bold')
    for i, (who, nm) in enumerate(oth):
        ox = X + W - (len(oth) - i) * 100 + 8; face_big(P, who, ox, y0 + 4, 84, fill=fill, ring=dim, rw=1.5); P.put(ox + 42, y0 + 112, nm, 21, col=dim, face='Jost-SemiBold', anchor='mm')
    y = y0 + s * 1.02 + 22; P.put(tx, y, c['sub'], 34, col=dim, face='Jost-Medium'); sx = tx + P.tw(c['sub'], 34, 'Jost-Medium') + 22
    if c.get('state'):
        tw = P.tw(c['state'], 34); P.rrect(sx, y - 26, tw + 40, 52, 26, fill=ORANGE); P.put(sx + 20, y, c['state'], 34, col=CREAM, face='Jost-Bold')
    fb = flow_facts(P, c['facts'], tx, y + 42, tw_, ink, dim)
    yy = fb + 10
    if c.get('does'):
        ws = [112 + max(P.tw(w[1], 40), P.tw(w[2] or '', 25, 'Jost-Medium')) + 34 + 16 for w in c['does']]
        cx = tx if tx + sum(ws) <= X + W else X + 4   # rule: the ways stay beside the facts if they fit, else they go under the hero
        if cx != tx: yy = max(yy, hy + hd + 30)
        for wy in c['does']: cx += sign(P, cx, yy, wy, **signs) + 16
        yy += 112
    yy = max(yy + 26, hy + hd + 36)
    if c.get('meters'):
        meters_line(P, c['meters'], X + 4, yy + 12, ink, dim, on); knows_line(P, c['knows'], X + 4, yy + 60, label, ink, dim); yy += 96
    if c.get('track'):
        tr_y = max(yy + 6, bottom - 118); track(P, c['track'], X + 6, tr_y, W - 40, ink, dim, on)


PRINT_A = [(0x4f, 0x98, 0x8e), (0x5f, 0xa6, 0x9b), (0x72, 0xb5, 0xa7), (0x83, 0xc1, 0xb4)]
PRINT_B = [(0x06, 0x24, 0x24), (0x0c, 0x34, 0x34), (0x15, 0x48, 0x46), (0x1f, 0x5c, 0x58)]
PRINT_C = [(0x9b, 0xcf, 0xc2), (0xac, 0xd8, 0xcc), (0xbc, 0xe0, 0xd6), (0xca, 0xe7, 0xde)]


def sign_opt(c, secs):
    """A · The pole: a cream field with one great curve; a thin enamel pole in the street carries the sections as round signs."""
    P = Page(2); P.img = street(PRINT_A, dx=420).resize((3840, 2160), Image.LANCZOS).convert('RGBA'); clip_circle(P, -720, 540, 2230, CREAM)
    SH = SEA2 if STAR else None
    body(P, c, 56, 1350, 60, INK, SOFT, DEEP, PALE, {}, [(-46, -40, 640, 176), (-60, -60, 470, 132), (-50, -50, 330, 96)], shadow=SH)
    x = 1690; P.line([(x, 0), (x, 1080)], col=CREAM, lw=7)
    n = len(secs); P.disc(x - 58, 22, 116, fill=CREAM, ring=None); P.icon('pause', x - 15, 36, 30, col=INK); P.text(x, 96, c['clock'], 30, col=INK, anchor='mm')
    top, bot = 150, 1060; ds = [220 if k == c['section'] else 118 for k, _, _ in secs]; gap = (bot - top - sum(ds)) / (n + 1); y = top + gap
    for (key, ic, w), d in zip(secs, ds):
        on = key == c['section']; cy = y + d / 2
        if on and STAR: star(P, x, cy, d * .62, d * .47, 12, ORANGE)
        else: P.disc(x - d / 2, y, d, fill=ORANGE if on else BODY_, ring=CREAM, rw=4)
        P.icon(ic, x - d * .22, cy - d * .36, d * .44, col=CREAM); P.put(x, cy + d * .2, w, 34 if on else 21, col=CREAM, face='Jost-Bold', anchor='mm'); y += d + gap
    return P


def deck_opt(c, secs):
    """B · The deck: everything is one card; each section is a pile whose card edges show behind it."""
    P = Page(2); P.img = street(PRINT_B).resize((3840, 2160), Image.LANCZOS).convert('RGBA')
    CX, CY, CW, CH = 56, 44, 1480, 992; n = len(secs); ph = min(190, (CH - 40) / n)
    for i, (key, ic, w) in enumerate(secs):
        on = key == c['section']; y = CY + 20 + i * ph; col = ORANGE if on else DEEP; tx = CX + CW - 40; ew = 250 if on else 190
        P.rrect(tx, y, ew, ph - 14, 26, fill=col); P.rrect(tx + 8, y + 8, ew - 16, ph - 30, 20, line=CREAM, lw=2)
        lx = tx + 40 + (ew - 40) / 2 + 4; P.icon(ic, lx - 30, y + 24, 60, col=CREAM); P.put(lx, y + 112, w, 30, col=CREAM, face='Jost-Bold', anchor='mm')
    P.rrect(CX, CY, CW, CH, 34, fill=CREAM)
    body(P, c, CX + 44, CW - 90, CY + 44, INK, SOFT, DEEP, PALE, {}, [(CX + 44, CY + 44, 440, 150), (CX + 44, CY + 44, 340, 120), (CX + 44, CY + 44, 260, 96)], shadow=SEA2, bottom=CY + CH - 30)
    P.plate(1730, 14, c['clock'], 'pause', h=44, size=24)
    return P


def hud_opt(c, secs):
    """C · The HUD, grown: the kit's teal enamel, the signage hotbar, one great flat circle."""
    P = Page(2); P.img = street(PRINT_C).resize((3840, 2160), Image.LANCZOS).convert('RGBA')
    X0, Y0, W0, H0 = 40, 34, 1600, 896
    P.rrect(X0, Y0, W0, H0, 30, fill=BODY_)
    cir = Image.new('RGBA', P.img.size, (0, 0, 0, 0)); ImageDraw.Draw(cir).ellipse((1040 * 2, -240 * 2, 2240 * 2, 960 * 2), fill=(0x15, 0x4c, 0x4a, 255)); mk = Image.new('L', P.img.size, 0); ImageDraw.Draw(mk).rounded_rectangle((X0 * 2, Y0 * 2, (X0 + W0) * 2, (Y0 + H0) * 2), 60, fill=255)
    cir.putalpha(ImageChops.multiply(cir.getchannel('A'), mk)); P.img.alpha_composite(cir); P.rrect(X0 + 8, Y0 + 8, W0 - 16, H0 - 16, 24, line=CREAM, lw=2.5)
    body(P, c, X0 + 40, W0 - 100, Y0 + 40, CREAM, DIM, TURQ, DEEP, dict(fill=DEEP, ink=CREAM, line=CREAM, sub=DIM, shutc=DIM), [(X0 + 40, Y0 + 36, 400, 160), (X0 + 40, Y0 + 40, 330, 124), (X0 + 40, Y0 + 40, 240, 92)], shadow=None, bottom=Y0 + H0 - 38)
    n = len(secs); sw = 142; x = X0 + W0 / 2 - (n * sw + (n - 1) * 12) / 2
    for key, ic, w in secs:
        on = key == c['section']; yy = 930 if on else 944; hh = 136 if on else 122
        P.rrect(x, yy, sw, hh, 20, fill=DEEP if not on else ORANGE); P.rrect(x + 6, yy + 6, sw - 12, hh - 12, 15, line=CREAM, lw=2.5); P.icon(ic, x + sw / 2 - 28, yy + 14, 56, col=CREAM); P.put(x + sw / 2, yy + 94, w, 25, col=CREAM, face='Jost-Bold', anchor='mm'); x += sw + 12
    P.plate(1730, 14, c['clock'], 'pause', h=44, size=24)
    return P


STAR = False
OPTS = dict(A=('pole', sign_opt), B=('deck', deck_opt), C=('hud', hud_opt))
