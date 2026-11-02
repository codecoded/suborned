"""C · The close-up: the frozen scene camera on the real person, full frame, as the conversation scenes already do. One enamel plate with one curve; the sections ride that curve."""
from l4_common import *
def arc_x(y, cx=2300, cy=540, r=1430): return cx - math.sqrt(r * r - (y - cy) ** 2)


def closeup(c, secs):
    P = Page(2); person = 'pic' in c and c.get('pic') or ('bust' in c and 'guard')
    wide = person not in ANCHOR
    if not wide: lay(P, framed(person, 450, 190, {'waitress': 1.2, 'scout': 1.45, 'guard': 1.25, 'councillor': .95}[person]))
    else: lay(P, street_real(-420))
    RAD = 1760 if wide else 1430
    circle_fill(P, 2300, 540, RAD, BODY_)
    X = 720 if wide else 1010; W = 1120 if wide else 840
    if 'people' in c or 'groups' in c:
        P.put(X, 86, c['title'], 100, col=CREAM, face='Jost-Bold'); P.put(X + P.tw(c['title'], 100) + 24, 100, c['count'], 54, col=TURQ, face='Jost-Medium')
        if 'people' in c: people_grid(P, c['people'], X + 6, 190, W + 20, CREAM, DIM, PALE, d=160)
        else: groups(P, c['groups'], X + 2, 200, W, CREAM, DIM, TURQ)
    else:
        if c.get('glyph'): glyph_big(P, c['glyph'], X, 150, 240, fill=(0xf7, 0xf4, 0xec, 255), col=BODY_); X += 280; W -= 280
        y = text_block(P, c, X, W, 58, CREAM, DIM, TURQ, dict(fill=DEEP, ink=CREAM, line=CREAM, sub=DIM, shutc=DIM), state_pill=False)
        if c.get('track'): track2(P, c['track'], X + 2, max(y + 6, 880), W - 10, CREAM, DIM, TURQ, paper=BODY_)
    # the state, across the figure, as big as the person
    if c.get('state'):
        t = c['state'].upper(); tw = P.tw(t, 86); P.rrect(440 - tw / 2 - 40, 920, tw + 80, 112, 18, fill=ORANGE); P.put(440, 974, t, 86, col=CREAM, face='Jost-Bold', anchor='mm')
    # the others: faces top left, a click away
    for i, (who, nm) in enumerate(c.get('others') or []):
        fx = 30 + i * 112; face_big(P, who, fx, 26, 96, fill=PALE, ring=CREAM, rw=3); P.rrect(fx + 2, 116, 92, 32, 8, fill=BODY_); P.put(fx + 48, 131, nm, 19, col=CREAM, face='Jost-SemiBold', anchor='mm')
    # the sections ride the curve
    n = len(secs); ys = [150 + (i + .5) * (800 / n) for i in range(n)]
    for (key, ic, w), y in zip(secs, ys):
        on = key == c['section']; d = 118 if on else 88; x = arc_x(y, r=RAD)
        P.disc(x - d / 2, y - d / 2, d, fill=ORANGE if on else DEEP, ring=CREAM, rw=3.5); P.icon(ic, x - d * .25, y - d * .36, d * .5, col=CREAM); P.put(x, y + d * .25, w, 24 if on else 17, col=CREAM, face='Jost-Bold', anchor='mm')
    P.plate(1742, 16, c['clock'], 'pause', h=44, size=24)
    return P
PRINT_S = [(0x4f, 0x98, 0x8e), (0x5f, 0xa6, 0x9b), (0x72, 0xb5, 0xa7), (0x83, 0xc1, 0xb4)]
