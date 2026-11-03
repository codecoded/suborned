"""The guided opening on the recommended panel (C): the clock stopped at 0:00, one short step per agent. Glyph, brief 085. Words are draft for Fable."""
import math, sys
sys.path.insert(0, '.')
from base import *
from options import *
PORT_Y = {'scout': 65, 'fixer': 161, 'tech': 258, 'enforcer': 354}
STEPS = ['scout', 'fixer', 'tech', 'enforcer', 'councillor']


def roster(L, who):
    """The speaker's portrait lit, the other three dimmed."""
    for r, y in PORT_Y.items():
        if r == who: L.disc(57 - 40, y - 40, 80, fill=None, ring=TURQ, rw=3.5)
        else: L.disc(57 - 36, y - 36, 72, fill=(0x0b, 0x16, 0x15, 130), ring=None)


def say(L, who, lines, size=18):
    y = PORT_Y[who]; w, h = bubble(L, 112, y - 25, lines, size=size)
    g = ImageDraw.Draw(L.img); s = L.s
    g.polygon([(113 * s, (y - 7) * s), (113 * s, (y + 7) * s), (103 * s, y * s)], fill=(0xf4, 0xf1, 0xe8, 240))
    L.note(112, y - 25, w, h, 'speech', who.capitalize() + ': ' + ' '.join(lines))


def mouse(L, x, y, button):
    L.rrect(x, y, 20, 30, 9, fill=INK_DEEP, line=CREAM, lw=1.6)
    k = 3; n = L.s * k; big = Image.new('RGBA', (20 * n, 30 * n), (0, 0, 0, 0)); g = ImageDraw.Draw(big)
    g.rounded_rectangle((n, n, 19 * n - 1, 29 * n - 1), 8 * n, fill=TURQ); g.rectangle((0, 12 * n, 20 * n, 30 * n), fill=(0, 0, 0, 0))
    g.rectangle((0 if button == 'right' else 10 * n, 0, 10 * n if button == 'right' else 20 * n, 12 * n), fill=(0, 0, 0, 0))
    L.paste(big.resize((20 * L.s, 30 * L.s), Image.LANCZOS), x, y)
    L.line([(x + 10, y + 1), (x + 10, y + 12)], col=CREAM, lw=1.2); L.line([(x + 1, y + 12), (x + 19, y + 12)], col=CREAM, lw=1.2)


def goal(L, parts, button=None):
    """The goal line's own place: one imperative, at most six words, its nouns in the kit's turquoise, as they are on the goal line."""
    size = 20; tw = sum(L.tw(t, size) for t, _ in parts); w = tw + 52 + (32 if button else 0); x = 960 - w / 2; h = 48; y = 1020
    L.nine('panel_strip', x, y, w, h, scale=.5)
    cx = x + 26
    if button: mouse(L, cx, y + 9, button); cx += 32
    for t, hot in parts:
        L.text(cx, y + h / 2 - 1, t, size, col=TURQ if hot else CREAM); cx += L.tw(t, size)
    L.note(x, y, w, h, 'line', ''.join(t for t, _ in parts))


def steps(L, now, done=(), start=False):
    """The step plate, where the feed stands (nothing to report while the clock is stopped): five faces, Next, Skip."""
    x, y, w, h = 1524, 958, 382, 62
    L.nine('panel_dark', x, y, w, h)
    for i, r in enumerate(STEPS):
        fx = x + 16 + i * 35
        L.face(r, fx, y + 16, 30, ring=TURQ if r == now else CHROME, rw=2.4 if r == now else 1.2)
        if r in done: L.art('badges/mark_ok', fx + 18, y + 12, 15)
        elif r != now: L.disc(fx, y + 16, 30, fill=(0x0b, 0x16, 0x15, 120), ring=None)
    bx = x + 16 + 5 * 35 + 6
    if start:
        L.nine('plate_dark', bx, y + 14, 112, 34); L.rrect(bx + 2, y + 16, 108, 30, 7, line=TURQ, lw=2); L.icon('nextevent', bx + 9, y + 23, 15, col=TURQ); L.text(bx + 29, y + 30, 'Start', 16, col=TURQ)
        L.note(x, y, w, h, 'image', 'guide steps')
        return
    L.nine('plate_dark', bx, y + 14, 80, 34); L.text(bx + 12, y + 30, 'Next', 15.5); L.icon('chev_down', bx + 54, y + 23, 15) if False else L.text(bx + 58, y + 30, '›', 18)
    L.nine('plate_dark', bx + 84, y + 14, 60, 34, alpha=.8); L.text(bx + 96, y + 30, 'Skip', 14.5, col=DIM)
    L.note(x, y, w, h, 'image', 'guide steps')


def ring(L, cx, cy, wedges, r=88, hole=28):
    """The ring as built, drawn on the layer: five wedges with the likely way at 12 o'clock, a hole on the person."""
    k = 3 * L.s; R = int((r + 4) * k); im = Image.new('RGBA', (2 * R, 2 * R), (0, 0, 0, 0)); g = ImageDraw.Draw(im)
    g.ellipse((4 * k, 4 * k, 2 * R - 4 * k, 2 * R - 4 * k), fill=(0x0c, 0x34, 0x34, 235), outline=CHROME, width=int(3 * k))
    n = len(wedges); step = 360 / n
    for i, (icon, st) in enumerate(wedges):
        a0 = -90 - step / 2 + i * step
        if st == 'lit': g.pieslice((7 * k, 7 * k, 2 * R - 7 * k, 2 * R - 7 * k), a0, a0 + step, fill=(0x1a, 0x70, 0x74, 255))
    for i in range(n):
        a = math.radians(-90 - step / 2 + i * step); g.line([(R, R), (R + math.cos(a) * r * k, R + math.sin(a) * r * k)], fill=(0xcb, 0xc6, 0xc3, 200), width=int(1.4 * k))
    g.ellipse((R - hole * k, R - hole * k, R + hole * k, R + hole * k), fill=(0, 0, 0, 0)); 
    mask = Image.new('L', im.size, 255); ImageDraw.Draw(mask).ellipse((R - hole * k, R - hole * k, R + hole * k, R + hole * k), fill=0)
    im.putalpha(ImageChops.multiply(im.getchannel('A'), mask))
    L.paste(im.resize((2 * R // 3, 2 * R // 3), Image.LANCZOS), cx - (r + 4), cy - (r + 4))
    L.disc(cx - hole - 2, cy - hole - 2, 2 * hole + 4, fill=None, ring=CHROME, rw=2)
    for i, (icon, st) in enumerate(wedges):
        a = math.radians(-90 + i * step); ix, iy = cx + math.cos(a) * 58 - 11, cy + math.sin(a) * 58 - 11
        L.icon(icon, ix, iy, 22, alpha=.45 if st == 'shut' else 1)
        if st == 'shut': L.icon('lock', ix + 13, iy + 13, 12)
    L.note(cx - r, cy - r, 2 * r, 2 * r, 'image', 'ring')


def price(L, x, y, word, bits, shut=None, mark=None, right=True):
    """A wedge's pill: its word, then its one-line price; a shut way, its lock's short reason."""
    size = 15; w = 16 + L.tw(word, size) + 10
    for ic, t in bits: w += 18 + L.tw(t, size - 1) + 9
    if shut: w += 18 + L.tw(shut, size - 1) + 6
    if mark: w += 20
    x0 = x if right else x - w
    L.nine('plate_dark', x0, y, w, 26, alpha=.75 if shut else 1); cx = x0 + 10
    L.text(cx, y + 12.5, word, size, col=DIM if shut else CREAM); cx += L.tw(word, size) + 9
    for ic, t in bits:
        L.icon(ic, cx, y + 5.5, 15); cx += 18; L.text(cx, y + 12.5, t, size - 1); cx += L.tw(t, size - 1) + 9
    if shut: L.icon('lock', cx, y + 5.5, 15, col=DIM); cx += 18; L.text(cx, y + 12.5, shut, size - 1, col=DIM)
    if mark: L.art(f'badges/{mark}', cx, y + 5, 16)
    L.note(x0, y, w, 26, 'image', 'ring price')


def queued(L, who):
    """The roster's chip for an order held till the clock runs, as built (the plan: HostHoldStatus, HudToolkit.Hold)."""
    L.nine('plate_dark', 110, PORT_Y[who] - 13, 26, 26); L.icon('nextevent', 115, PORT_Y[who] - 8, 16, col=TURQ)


def ghost(L, who, x, y, label):
    """The plan's ghost plate, as built: the doer's face and the order's words, at the end of a dashed path."""
    w = 12 + 22 + 6 + L.tw(label, 14.5) + 13
    L.nine('plate_dark', x - w / 2, y, w, 32); L.face(who, x - w / 2 + 10, y + 5, 22, rw=1.4); L.text(x - w / 2 + 38, y + 15.5, label, 14.5)
    L.note(x - w / 2, y, w, 32, 'image', 'ghost plate')


def path(L, a, b):
    L.line([a, b], col=INK, lw=2.2, dashed=True); L.ground_ring(b[0], b[1], 18, col=INK, lw=2.4, glow=False)


fo = clock(frame_open(), '0:00')
fc = clock(frame_cafe_stopped(), '0:00')
fd = clock(frame_door_stopped(), '0:00', x=1455, cash='$100,000')
INEZ = (1296, 136)
shots = {}
def out(n, world, L):
    shots[n] = (world, L); save(n, world, L, 'open' if world is fo else 'learned', state='guide')

# 1 Scout: the first input
L = Layer(); roster(L, 'scout'); say(L, 'scout', ["I watch and listen. Hold on someone,", "I'll learn what they hide."])
c_rail(L, OPENING, lit=('waitress', 'guard', 'driver')); L.ground_ring(*INEZ, 28); goal(L, [('Hold left on ', 0), ('someone', 1), ('.', 0)], 'left'); steps(L, 'scout'); out('guide-1-scout', fo, L)

# 2 Scout: the same, once taken. The order is real; everyone goes when the clock starts
L = Layer(); roster(L, 'scout'); bubble(L, 146, 40, 'Eyes on.', size=18); L.note(146, 40, 86, 30, 'speech', 'Scout: Eyes on.'); queued(L, 'scout')
c_rail(L, OPENING, 'waitress'); c_card(L, OPENING, 'waitress'); path(L, (806, 615), (1252, 176)); L.ground_ring(*INEZ, 28); ghost(L, 'scout', 1210, 142, 'Watch Inez · 34 s')
goal(L, [('Taken. It starts with the clock.', 0)]); steps(L, 'scout'); out('guide-2-scout-taken', fo, L)

# 3 Fixer: the ring
L = Layer(); roster(L, 'fixer'); say(L, 'fixer', ["I turn people: Ask, Pay, Help or Pressure.", "Each has a price."]); queued(L, 'scout')
ring(L, 962, 490, [('ask', 'lit'), ('pay', 'shut'), ('help', 'shut'), ('pressure', 'shut'), ('dot', 'open')])
price(L, 846, 372, 'Ask', [('p_time', '39 s'), ('p_trail', '+1')], mark='mark_unk'); price(L, 1056, 438, 'Pay', [], shut='Watch first'); price(L, 1030, 556, 'Help', [], shut='Listen first'); price(L, 894, 556, 'Pressure', [], shut='Listen first', right=False)
c_rail(L, OPENING, lit=('waitress', 'driver')); goal(L, [('Click ', 0), ('someone', 1), ('. See every way.', 0)], 'left'); steps(L, 'fixer', ('scout',)); out('guide-3-fixer', fc, L)

# 4 Tech: walking; he points at his cabinet and his camera, and it's only a walk
L = Layer(); roster(L, 'tech'); say(L, 'tech', ["I loop cameras and move the patrol,", "from that cabinet."]); queued(L, 'scout')
L.ground_ring(1318, 662, 48); L.ground_ring(1408, 800, 30); c_rail(L, OPENING)
goal(L, [('Click the ', 0), ('ground', 1), ('. Tech walks.', 0)], 'left'); steps(L, 'tech', ('scout', 'fixer')); out('guide-4-tech', fd, L)

# 5 Enforcer: both places he can take Vale; the right hold shown, never asked for
L = Layer(); roster(L, 'enforcer'); say(L, 'enforcer', ["I take Vale, inside or at his car.", "Only I carry him."]); queued(L, 'scout')
L.ground_ring(960, 486, 34); L.ground_ring(190, 288, 54); c_rail(L, OPENING, lit=('councillor',))
goal(L, [('Hold right to fire. Not yet.', 0)], 'right'); steps(L, 'enforcer', ('scout', 'fixer', 'tech')); out('guide-5-enforcer', fd, L)

# 6 Vale and the goal: the view back on the cell, the clock's start the player's
L = Layer(); roster(L, 'scout'); bubble(L, 146, 40, ["Vale's inside until the chime."], size=18); L.note(146, 40, 250, 30, 'speech', "Scout: Vale's inside until the chime."); queued(L, 'scout')
c_rail(L, OPENING, 'councillor'); c_card(L, OPENING, 'councillor'); L.ground_ring(905, 985, 92); path(L, (806, 615), (1252, 176)); ghost(L, 'scout', 1210, 142, 'Watch Inez · 34 s')
goal(L, [('Take ', 0), ('Vale', 1), (' alive to the ', 0), ('car', 1), ('.', 0)]); steps(L, 'councillor', ('scout', 'fixer', 'tech', 'enforcer'), start=True); out('guide-6-goal', fo, L)
