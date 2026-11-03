"""The three options, each in its opening and learned states. Glyph, brief 085 note 10."""
from kit import *
from people import *
FILL, FIRST = (0x16, 0x42, 0x40, 255), (0x20, 0x58, 0x5a, 255)
RX, RW = 1906, 344   # the right edge of the clock bar; the width all options share


# ---------------------------------------------------------------- A: the dossier, every person open at once
def a_card(L, x, y, w, p, role):
    h = 112 if p['ways'] else 64
    L.rrect(x, y, w, h, 9, fill=FILL, line=(0x6e, 0xaf, 0xa8, 110), lw=1)
    L.face(role, x + 8, y + 7, 44, rw=1.6)
    L.text(x + 60, y + 18, p['name'], 17.5); nw = L.tw(p['name'], 17.5); L.text(x + 60 + nw + 7, y + 19, '· ' + JOB[role], 13, col=DIM)
    if p.get('new'): L.icon('dot', x + w - 22, y + 10, 12, col=TURQ)
    if not p['ways']:
        L.icon('p_time', x + 60, y + 34, 15); L.text(x + 79, y + 42, 'Inside. Out 4:50', 15); L.note(x, y, w, h, 'chip', facts=['Inside', 'Out 4:50'], verbs=[]); return h
    cx = x + 60; yy = y + 32
    for i, (ic, st, word) in enumerate(p['facts']):
        sw = slot(L, i, cx, yy, 16, ic, st); L_word = word.replace('Told: ', '').capitalize() if st == 'told' else word.replace(' ?', '') if st == 'unk' else word
        L.text(cx + sw + 3, yy + 8, L_word, 12, col=DIM if st == 'unk' else CREAM); cx += sw + 3 + L.tw(L_word, 12) + 7
    ways_row(L, x + 8, y + 74, p['ways'], h=30, size=14)
    L.note(x, y, w, h, 'chip', facts=[f[2] for f in p['facts']], verbs=[wy[2] for wy in p['ways'] if wy[2]])
    return h


def option_a(L, data, hover=None):
    x, y, w = RX - RW, 66, RW; hs = [(112 if data[r]['ways'] else 64) for r in ORDER]; H = sum(hs) + 3 * 6 + 28
    L.nine('panel_dark', x, y, w, H)
    yy = y + 14
    for r in ORDER: yy += a_card(L, x + 12, yy, w - 24, data[r], r) + 6
    return x, y, w, H


# ---------------------------------------------------------------- B: on the street, no panel
def b_tag(L, x, y, p, role, icons=None, arrow=None, face=True):
    """Face, name, what to try: one mark on or towards the person. At most two icons over a person."""
    h = 48; icons = icons if icons is not None else ([p['next']] if p['next'] else [])
    nw = L.tw(p['name'], 17); tm = L.tw(p.get('time', ''), 15) + 22 if p.get('time') else 0
    w = (22 if arrow else 8) + (48 if face else 8) + nw + 10 + len(icons) * 34 + tm + 8
    x0 = x - w if arrow else x
    L.nine('plate_dark', x0, y, w, h)
    cx = x0 + 9
    if arrow:
        g = ImageDraw.Draw(L.img); s = L.s; ax, ay = cx + 6, y + 24
        g.polygon([((ax + 7) * s, (ay + 7) * s), ((ax - 6) * s, (ay + 5) * s), ((ax + 5) * s, (ay - 6) * s)], fill=TURQ); cx += 16
    if face: L.face(role, cx, y + 4, 40, rw=1.6); cx += 48
    else: cx += 6
    L.text(cx, y + h / 2 - .5, p['name'], 17); cx += nw + 8
    if p.get('time'):
        L.icon('p_time', cx, y + 16, 15); L.text(cx + 18, y + h / 2 - .5, p['time'], 15)
    for ic in icons:
        L.art('markers/disc_dark_30', cx, y + 9, 30); L.icon(ic, cx + 6.5, y + 15.5, 17); cx += 34
    L.note(x0, y, w, h, 'image', f"tag {role}")
    return x0, w


def option_b(L, data, inez_at, tag_at, street=('guard', 'driver', 'councillor'), y0=762, inez_icons=None):
    tx, ty = tag_at; ix, iy = inez_at
    x0, w = b_tag(L, tx, ty, data['waitress'], 'waitress', icons=inez_icons)
    stem(L, x0 + w / 2 if abs(ix - (x0 + w / 2)) < 60 else min(max(ix, x0 + 12), x0 + w - 12), ty + 48, ix, iy)
    for i, r in enumerate(street):
        b_tag(L, RX, y0 + i * 56, data[r], r, arrow=135)


# ---------------------------------------------------------------- C: four faces and one open card
FACE = 58; CELL = 80
def c_rail(L, data, selected=None, x=None, y=66, lit=None):
    x = RX - RW if x is None else x
    h = 112
    L.nine('panel_dark', x, y, RW, h)
    for i, r in enumerate(ORDER):
        p = data[r]; fx = x + 13 + i * CELL + (CELL - FACE) / 2; fy = y + 10; sel = r == selected or (lit and r in lit)
        L.face(r, fx, fy, FACE, ring=TURQ if sel else CHROME, rw=2.5 if sel else 2)
        if p.get('next'):
            L.art('markers/disc_dark_24', fx + FACE - 18, fy + FACE - 20, 24); L.icon(p['next'], fx + FACE - 13.5, fy + FACE - 15.5, 15)
        if p.get('new'): L.disc(fx + FACE - 12, fy - 1, 12, fill=TURQ, ring=INK_DEEP, rw=1)
        L.text(fx + FACE / 2, fy + FACE + 13, p['name'], 15.5, anchor='mm')
        if p.get('time'):
            L.text(fx + FACE / 2, fy + FACE + 28, p['time'], 13, col=DIM, anchor='mm')
        else:
            for k, c in enumerate(p['pips']):
                dx = fx + FACE / 2 - 15 + k * 12
                if c == 'l': L.disc(dx - 3, fy + FACE + 26, 7, fill=CREAM, ring=None)
                elif c == 't': L.disc(dx - 3, fy + FACE + 26, 7, fill=None, ring=CREAM, rw=1.6); L.disc(dx - 1, fy + FACE + 28, 3, fill=CREAM, ring=None)
                else: L.disc(dx - 3, fy + FACE + 26, 7, fill=None, ring=DIM, rw=1)
    L.note(x, y, RW, h, 'image', 'people rail')
    return x, y + h


def c_card(L, data, role, x=None, y=182, price=None):
    x = RX - RW if x is None else x; p = data[role]; i = ORDER.index(role)
    H = 16 + 28 + 3 * 28 + 2 + (8 + 30 if p['ways'] else 0) + (34 if price else 0) + 15
    L.nine('panel_dark', x, y, RW, H)
    L.tail(x + 13 + i * CELL + CELL / 2, y + 1)
    b = card_body(L, x + 20, y + 15, RW - 40, p, role)
    if price:
        L.rrect(x + 20, b + 5, RW - 40, 26, 6, fill=INK_DEEP)
        cx = x + 28
        for ic, t in price:
            if ic: L.icon(ic, cx, b + 10, 15); cx += 18
            L.text(cx, b + 18, t, 14.5); cx += L.tw(t, 14.5) + 13
    L.note(x, y, RW, H, 'chip', facts=[f[2] for f in p['facts']], verbs=[w[2] for w in p['ways'] if w[2]])
    return x, y, H
