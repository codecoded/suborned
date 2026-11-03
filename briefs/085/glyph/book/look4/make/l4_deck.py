"""B · The deck, taken seriously: a printed playing card, not a modal. Index corners, the real close shot as the picture, a second card dealt under it, piles for sections and for crowds."""
from l4_common import *
PRINT_B = [(0x06, 0x24, 0x24), (0x0c, 0x34, 0x34), (0x12, 0x40, 0x3f), (0x18, 0x4c, 0x4a)]


def card(P, x, y, w, h, deg, draw, fill=CREAM):
    """A card is its own sheet: flat, one enamel inline, laid at a small angle."""
    s = Page(2, w, h); ImageDraw.Draw(s.img).rounded_rectangle((0, 0, w * 2 - 1, h * 2 - 1), 44, fill=fill); s.rrect(10, 10, w - 20, h - 20, 16, line=DEEP, lw=2.5); draw(s); P.words += s.words
    im = s.img.rotate(deg, resample=Image.BICUBIC, expand=True); P.img.alpha_composite(im, (round(x * 2 - (im.width - w * 2) / 2), round(y * 2 - (im.height - h * 2) / 2)))


def back(P, x, y, w, h, icon, word, on=False, thick=4):
    """A pile: its thickness is its size, the top card a flat atomic back with one pictogram."""
    for i in range(thick, 0, -1): P.rrect(x + i * 3, y - i * 3, w, h, 14, fill=(0xd8, 0xd3, 0xc4, 255) if i % 2 else CREAM)
    P.rrect(x, y, w, h, 14, fill=ORANGE if on else DEEP); P.rrect(x + 6, y + 6, w - 12, h - 12, 9, line=CREAM, lw=2)
    P.icon(icon, x + w / 2 - w * .22, y + h * .16, w * .44, col=CREAM); P.put(x + w / 2, y + h * .76, word, 25, col=CREAM, face='Jost-Bold', anchor='mm')


def piles(P, c, secs):
    n = len(secs); cols = 3; w, h = 134, 172; X0, Y0 = 1400, 590
    for i, (key, ic, wd) in enumerate(secs): back(P, X0 + (i % cols) * (w + 22), Y0 + (i // cols) * (h + 28), w, h, ic, wd, on=key == c['section'])


def small_card(P, x, y, who, nm, w=190, h=240, deg=0):
    def d(s):
        face_big(s, who, (w - 130) / 2, 26, 130, fill=PALE); s.put(w / 2, 188, nm, 25, col=INK, face='Jost-SemiBold', anchor='mm')
    card(P, x, y, w, h, deg, d)


def deck(c, secs):
    P = Page(2); lay(P, street(PRINT_B)); icon = dict((k, i) for k, i, _ in secs).get(c['section'])
    if 'people' in c or 'groups' in c:
        P.put(70, 96, c['title'], 110, col=CREAM, face='Jost-Bold'); P.put(70 + P.tw(c['title'], 110) + 28, 110, c['count'], 58, col=TURQ, face='Jost-Medium')
        if 'people' in c:
            for i, (who, nm) in enumerate(c['people']): small_card(P, 70 + (i % 4) * 306, 200 + (i // 4) * 400, who, nm, w=280, h=370) if False else small_card(P, 70 + (i % 4) * 306, 210 + (i // 4) * 300, who, nm, w=280, h=280)
        else:
            for i, (ic, wd, n) in enumerate(c['groups']):
                x, y = 80 + (i % 3) * 420, 240 + (i // 3) * 370; back(P, x, y, 190, 250, ic, wd, thick=max(2, round(n / 7))); P.put(x + 240, y + 130, str(n), 92, col=CREAM, face='Jost-Bold')
    else:
        def second(s):
            X, W = 80, 550; y = 56
            y = flow(s, c['facts'], X, y, W, INK, SOFT) + 4
            y = ways(s, c, X, y, W, h=92, shutc=SOFT)
            if c.get('meters'):
                for i, (w, v, n) in enumerate(c['meters']):
                    if i and i % 2 == 0: pass
                meters_line(s, c['meters'][:2], X, y + 14, INK, SOFT, DEEP, size=25); meters_line(s, c['meters'][2:], X, y + 54, INK, SOFT, DEEP, size=25); y += 92
                s.put(X, y + 10, 'She knows', 23, col=SOFT, face='Jost-Medium'); y += 28
                for ic, w in c['knows']:
                    if ic in ('fixer', 'scout', 'tech', 'enforcer'): face_big(s, ic, X, y, 34)
                    else: s.icon(ic, X + 3, y + 3, 28, col=INK)
                    s.put(X + 44, y + 17, w, 25, col=INK, face='Jost-SemiBold'); y += 40
                y += 8
            if c.get('track'): track2(s, c['track'], X + 2, max(y, 712), W, INK, SOFT, DEEP, d=44)
        card(P, 640, 112, 710, 880, 1.0, second)
        def first(s):
            pic = c.get('pic') or ('guard' if c.get('bust') else None)
            ph = 690; px, py, pw = 28, 28, 594
            if pic in ANCHOR:
                f = framed(pic, 297, 90, {'waitress': 1.25, 'scout': 1.3, 'guard': 1.3, 'councillor': .78}[pic], W=594, H=ph); s.paste(f.resize((pw * 2, ph * 2), Image.LANCZOS).convert('RGBA'), px, py)
            elif pic: face_big(s, pic, 110, 90, 430, fill=PALE)
            else: s.rrect(px, py, pw, ph, 10, fill=DEEP); s.icon(c['glyph'], px + 150, py + 150, 300, col=CREAM)
            # the index corner, as on a playing card
            s.disc(px + 12, py + 12, 74, fill=CREAM, ring=None); s.icon(icon, px + 26, py + 26, 46, col=DEEP)
            n = name_size(c['name'], 590, s, top=112); s.put(32, 738 + n * .5, c['name'], n, col=INK, face='Jost-Bold')
            yy = 738 + n * 1.02 + 14; s.put(34, yy, c['sub'], 32, col=SOFT, face='Jost-Medium')
            if c.get('state'):
                tw = s.tw(c['state'], 34); ax = 650 - tw - 76; s.rrect(ax, yy - 28, tw + 44, 56, 28, fill=ORANGE); s.put(ax + 22, yy, c['state'], 34, col=CREAM, face='Jost-Bold')
        card(P, 56, 66, 650, 950, -1.2, first)
        for i, (who, nm) in enumerate(c.get('others') or []): small_card(P, 1390 + i * 165, 96 + (i % 2) * 18, who, nm, w=190, h=240, deg=4 - i * 4)
    piles(P, c, secs)
    P.plate(1748, 14, c['clock'], 'pause', h=44, size=24)
    return P
