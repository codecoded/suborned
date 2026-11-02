"""L · The light page, calm: the real frozen street in its own colours; the world's floating signal eye over the person, throwing one page of light. Flat, no chrome. The sections are signal lights."""
from l4_common import *
ANCH = {'waitress': ('cafe', 962, 500), 'scout': ('cafe', 1046, 575), 'guard': ('door', 960, 470), 'councillor': ('door', 960, 470)}


def lens_mask(P):
    """The page of light: one big soft curve on the left, screen edge on the right."""
    S_ = P.s; b = Image.new('L', (P.w, P.h), 0); ImageDraw.Draw(b).ellipse((740, -900, 3700, 1980), fill=255); return b.resize((P.w * S_, P.h * S_), Image.BICUBIC)


def light(c, secs):
    """L · One piece of apparatus: the signal eye above the person. A thin line runs from them to their face on the page, as Legion's profiler joins a person to their card."""
    P = Page(2); pic = c.get('pic') or ('guard' if c.get('bust') else None); person = pic in ANCH and pic != 'councillor'; hd = 330  # Vale is inside: no eye on a place he isn't
    a = ANCH.get(pic, ('cafe', 962, 500)); dx = 300 - a[1]
    lay(P, street_real(dx, cell=pic == 'scout') if a[0] == 'cafe' else door_real(dx)); ex, ey = 300, a[2]
    m = lens_mask(P); halo = Image.new('RGBA', P.img.size, (0x60, 0xe0, 0xea, 0)); halo.putalpha(m.filter(ImageFilter.GaussianBlur(22)).point(lambda v: v * 170 // 255)); P.img.alpha_composite(halo)
    pg = Image.new('RGBA', P.img.size, (0xf9, 0xf7, 0xee, 0)); pg.putalpha(m.point(lambda v: v * 247 // 255)); P.img.alpha_composite(pg)
    X, W = 1010, 700; ink, dim, on = INK, SOFT, DEEP
    if person:
        P.line([(ex, ey - 124), (660, 250)], col=TURQ, lw=3); P.ground_ring(ex, ey + 44, 42, lw=3.5); P.disc(ex - 24, ey - 150, 48, fill=CREAM, ring=TURQ, rw=6); P.disc(ex - 8, ey - 134, 16, fill=DEEP, ring=None)
    if 'people' in c or 'groups' in c:
        X0 = 880; P.put(X0, 110, c['title'], 96, col=ink, face='Jost-Bold'); P.put(X0 + P.tw(c['title'], 96) + 24, 124, c['count'], 52, col=on, face='Jost-Medium')
        if 'people' in c: people_grid(P, c['people'], X0, 200, 840, ink, dim, PALE, d=150)
        else: groups(P, c['groups'], X0 + 2, 220, 840, ink, dim, on, rowh=230)
    else:
        if c.get('glyph'): glyph_big(P, c['glyph'], 640, 70, hd, fill=DEEP)
        else: face_big(P, c.get('pic'), 640, 70, hd, fill=PALE, ring=TURQ, rw=5)
        y = text_block(P, c, X, W, 60, ink, dim, on, dict(shutc=SOFT), name_top=112, knowlabel='She knows') if False else None
        y = text_block(P, c, X, W, 60, ink, dim, on, dict(shutc=SOFT), name_top=112)
        if c.get('track'): track2(P, c['track'], 880, max(y + 2, 880), 840, ink, dim, on, d=44)
    n = len(secs); gap = 880 / n
    for i, (key, ic, w) in enumerate(secs):
        y = 110 + (i + .5) * gap; cur = key == c['section']; d = 108 if cur else 84; x = 1830
        P.disc(x - d / 2, y - d / 2, d, fill=ORANGE if cur else BODY_, ring=None); P.icon(ic, x - d * .25, y - d * .36, d * .5, col=CREAM); P.put(x, y + d * .24, w, 22 if cur else 16, col=CREAM, face='Jost-Bold', anchor='mm')
    for i, (who, nm) in enumerate(c.get('others') or []):
        fx = 22 + i * 92; face_big(P, who, fx, 26, 80, fill=PALE, ring=CREAM, rw=3); P.rrect(fx + 2, 102, 76, 28, 7, fill=BODY_); P.put(fx + 40, 115, nm, 17, col=CREAM, face='Jost-SemiBold', anchor='mm')
    P.plate(1756, 14, c['clock'], 'pause', h=40, size=22)
    return P
