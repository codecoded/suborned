"""The harbour liner berth (brief 007 round 2, Sky: "LinerDock reads as a lattice rocket gantry, not a liner berth").
Rockets stay at the horizon pads; the berth is a harbour liner terminal (Horizon/pano/pano_harbour, AtomicAge
monorail_terminal and grand_hotel for the deco language).

  LinerDock@liner-berth  the deco liner terminal on the caisson (plan landmarks.liner-berth: disc r 22, top 104 on a
                         1.2 m caisson deck, so the piece fits inside r 21.8 and rises <= 102.8 above the deck):
                         a two-storey streamline terminal hall, a fluted harbour tower with an observation gallery, a
                         glazed lantern and a needle mast with a gold starburst and a rose beacon; on the water side two
                         lattice boarding gantries with enclosed streamline gangways at the liner's promenade-deck level
                         (10 m above the water) and the upper deck (17 m), bollards and capstans on the caisson edge.
                         Pivot = caisson centre on its deck (z 0). The water side is local -X (plan +x, east, once the
                         FBX axes are baked: kit x = -Blender x).
  OceanLiner             a 96 m streamline ocean liner (the berth's ship, x 0.5 of a 1930s express liner): navy hull
                         with an oxblood boot-top and a gold sheer line, four stepped ivory decks with ribbon windows and
                         turquoise speed lines, a rounded bridge, three raked turquoise funnels, masts, lifeboats,
                         portholes. Pivot = waterline amidships (z 0 = water), bow to -Y. It needs a water site in the
                         plan beside the caisson (handed to Datum); until then it is a candidate.
"""

import math

from geo import circle, rrect
from kit import asset
from mats import accent, pal, register
from mega_parts import strut, truss
from skyport_parts import ball
from structures import sec, tube

register("DK_Hull_Navy", "enamel", "#24406A")        # dE2000 23.8 from rivals
register("DK_Hull_Ivory", "enamel", "#F4EFE3")
register("DK_Steel_Ivory", "enamel", "#E4DFD2")
register("DK_Window_Teal", "enamel", "#2E6D70")
register("DK_Window_Tower", "enamel", "#34434B")
register("DK_Enamel_Turquoise", "enamel", accent("turquoise"))
register("DK_Enamel_Flamingo", "enamel", accent("flamingo"))
register("DK_Enamel_Sunshine", "enamel", accent("sunshine"))
register("DK_Enamel_Coral17", "enamel", accent("coral"))
register("DK_Lamp_Deco", "emit", "#FFF1C4", 5.0)


# ----------------------------------------------------------------- the terminal

def _streamline_block(s, w, d, z0, z1, mat, bands=(), glass=(), r=None):
    """Rounded-end block w (X) x d (Y): ribbon glass bands (z pairs) and turquoise speed lines, both slightly proud."""
    r = r if r is not None else min(w, d) / 2 - 0.05
    s.extrude(rrect(w, d, r, n=8), z0, z1, mat, smooth_sides=True)
    for za, zb in glass:
        s.extrude(rrect(w + 0.08, d + 0.08, r + 0.04, n=8), za, zb, pal.Window_Teal, smooth_sides=True, top=False, bottom=False)
        s.extrude(rrect(w + 0.16, d + 0.16, r + 0.08, n=8), zb, zb + 0.18, pal.Chrome, smooth_sides=True, top=False, bottom=False)
    for za, zb in bands:
        s.extrude(rrect(w + 0.14, d + 0.14, r + 0.07, n=8), za, zb, pal.Enamel_Turquoise, smooth_sides=True, top=False, bottom=False)


def _gangway(s, a, b, h=2.6, w=2.6):
    """Enclosed streamline gangway tube from a to b (3D points): ivory shell, window band, turquoise keel stripe."""
    tube(s, [a, b], sec(-w / 2, w / 2, -h / 2, h / 2, 1.0), pal.Hull_Ivory)
    tube(s, [a, b], sec(-w / 2 - 0.03, w / 2 + 0.03, 0.0, 0.7, 0.3), pal.Window_Teal, caps=False)
    tube(s, [a, b], sec(-0.5, 0.5, -h / 2 - 0.03, -h / 2 + 0.2), pal.Enamel_Turquoise, caps=False)


def _harbour_tower(s, cx, cy, z0):
    """Fluted harbour tower: limestone piers, teal slots, gold ribs, a gallery, a glazed lantern, dome, mast, starburst."""
    from spacehq import fluted_tier, setback
    with s.at((cx, cy, 0.0)):
        fluted_tier(s, 5.8, z0, 46.0, 16)
        setback(s, 5.8, 46.0, 4.4)
        fluted_tier(s, 4.4, 46.0, 62.0, 12)
        setback(s, 4.4, 62.0, 0.0)
        from mega_parts import lens
        lens(s, 62.4, 3.0, 9.0, 2.2, pal.Chrome, bead=pal.Gold, seg=48)       # observation gallery
        oct_ = circle(3.6, n=8)
        s.extrude(oct_, 64.6, 71.0, pal.Window_Teal)                           # lantern
        for x, y in oct_:
            s.box((x - 0.12, y - 0.12, 64.6), (x + 0.12, y + 0.12, 71.0), pal.Chrome)
        s.lathe([(4.1, 71.0), (4.1, 71.6), (3.2, 72.4)] + [(3.2 * math.cos(math.pi / 2 * k / 6), 72.4 + 3.2 * math.sin(math.pi / 2 * k / 6)) for k in range(1, 7)],
                pal.Hull_Ivory, seg=32)
        s.lathe([(4.15, 71.1), (4.15, 71.5)], pal.Gold, seg=32, cap_bottom=False, cap_top=False)
        s.lathe([(0.6, 75.4), (0.42, 86.0), (0.22, 96.0), (0.08, 101.6), (0.0, 102.4)], pal.Chrome, seg=10, cap_bottom=False)
        for zc, r in ((80.0, 0.5), (88.0, 0.36)):
            s.lathe([(r, zc - 0.3), (r + 0.35, zc), (r, zc + 0.3)], pal.Gold, seg=10, cap_bottom=False, cap_top=False)
        with s.at((0, 0, 88.0)):                                               # Grand Hotel starburst finial
            ball(s, (0, 0, 0), 0.9, pal.Gold, seg=16, n=8)
            for tilt, n, L in ((90, 8, 4.2), (45, 6, 3.0), (135, 6, 3.0)):
                for i in range(n):
                    with s.at((0, 0, 0), (tilt, 0, 360.0 * i / n)):
                        s.lathe([(0.22, 0.0), (0.1, L * 0.55), (0.0, L)], pal.Gold, seg=6)
        ball(s, (0.0, 0.0, 99.0), 0.45, pal.Beacon_Rose, seg=12, n=6)


@asset("LinerDock@liner-berth", "Landmarks", bevel=0.0)
def liner_terminal(s):
    # terminal hall: two storeys on the land side, streamline rounded ends, ribbon windows, a roof terrace
    hx = 5.0                                                              # hall centre x (land side)
    with s.at((hx, 0.0, 0.0)):
        _streamline_block(s, 18.0, 30.0, 0.0, 10.4, pal.Limestone_Tex, bands=((4.6, 5.0), (9.6, 10.0)), glass=((1.4, 4.2), (6.0, 9.2)))
        s.extrude(rrect(18.6, 30.6, 9.3, n=8), 10.4, 11.0, pal.Limestone_Tex, smooth_sides=True)          # coping
        s.extrude(rrect(17.4, 29.4, 8.7, n=8), 11.0, 11.03, pal.Terrazzo_Ivory)
        # the land-side entrance: a coral-inlaid arched portal and a gold canopy fin over it
        with s.at((9.0, 0.0, 0.0), (90, 0, 90)):                           # local XY = world (y, z), extrude along +x
            arch = [(3.0 * math.cos(math.pi * k / 12), 4.6 + 3.0 * math.sin(math.pi * k / 12)) for k in range(13)]
            s.extrude([(3.0, 0.0)] + arch + [(-3.0, 0.0)], 0.0, 0.06, pal.Window_Teal)
            ring = [(3.4 * math.cos(math.pi * k / 12), 4.6 + 3.4 * math.sin(math.pi * k / 12)) for k in range(13)]
            for k in range(12):
                q = [arch[k], ring[k], ring[k + 1], arch[k + 1]]
                s.extrude(q, 0.0, 0.14, pal.Enamel_Coral17)
            s.extrude([(-0.35, 8.2), (0.35, 8.2), (0.35, 10.8), (-0.35, 10.8)], 0.0, 0.5, pal.Gold)
        for y in (-11.0, -6.0, 6.0, 11.0):                                   # roof flagpoles with pennants
            with s.at((0.0, y, 0.0)):
                s.lathe([(0.08, 11.0), (0.05, 18.0)], pal.Chrome, seg=6)
                m = (pal.Enamel_Flamingo, pal.Enamel_Turquoise, pal.Enamel_Sunshine, pal.Enamel_Turquoise)[int(abs(y)) % 4]
                s.box((0.0, -0.02, 16.4), (1.8, 0.02, 17.6), m)
    _harbour_tower(s, hx + 2.0, 0.0, 11.0)
    # boarding gantries on the water side (local -X): lattice towers, gangways at the promenade deck (+8.8 = 10 m over the
    # water) and the upper deck (+15.8), gangway heads cantilevered to the caisson edge with hooded bellows
    for sy in (-1, 1):
        gx, gy = -15.0, sy * 9.0
        truss(s, (gx, gy, 0.0), (gx, gy, 24.0), 1.5, 1.5, 8, pal.Steel_Ivory, chord=0.36, lace=0.18)
        s.box((gx - 1.8, gy - 1.8, 24.0), (gx + 1.8, gy + 1.8, 24.4), pal.Steel_Ivory)
        s.box((gx - 1.9, gy - 1.9, 24.4), (gx + 1.9, gy + 1.9, 24.7), pal.Enamel_Turquoise)
        with s.at((gx, gy, 0.0)):
            s.lathe([(0.07, 24.7), (0.04, 29.0)], pal.Chrome, seg=6)
            ball(s, (0.0, 0.0, 29.1), 0.22, pal.Lamp_Deco, seg=10, n=5)
        # heads reach the liner's walls as sited by Datum (OceanLiner centre plan x 402.6 = 30.6 m east of the caisson centre):
        # deck 1 side (half width 6.1) at 10 m over the water, deck 3 side (4.8) at 17 m; the hood's face sits on the wall
        for zg, x_end in ((8.8, -(30.6 - 6.1)), (15.8, -(30.6 - 4.8))):
            _gangway(s, (hx - 9.2, gy, zg), (gx + 1.5, gy, zg))
            _gangway(s, (gx - 1.5, gy, zg), (x_end + 0.9, gy, zg), h=2.4, w=2.4)
            s.box((x_end, gy - 1.5, zg - 1.5), (x_end + 0.9, gy + 1.5, zg + 1.5), pal.Window_Tower)  # bellows hood
            s.box((x_end + 0.05, gy - 1.6, zg + 1.4), (x_end + 1.1, gy + 1.6, zg + 1.65), pal.Enamel_Coral17)
            strut(s, (gx, gy, zg + 6.0), (x_end + 1.4, gy, zg + 1.3), 0.14, pal.Steel)  # stay
    # caisson edge furniture on the water side: bollards and capstans
    for k in range(-4, 5):
        a = math.radians(180 + 13 * k)
        x, y = 20.6 * math.cos(a), 20.6 * math.sin(a)
        with s.at((x, y, 0.0)):
            if k % 2:
                s.lathe([(0.35, 0.0), (0.3, 0.6), (0.42, 0.7), (0.42, 0.85), (0.0, 0.85)], pal.Iron, seg=10)
            else:
                s.lathe([(0.6, 0.0), (0.45, 0.3), (0.45, 0.8), (0.55, 0.9), (0.0, 0.95)], pal.Steel_Ivory, seg=12)
    # lamp standards round the forecourt
    from spacehq import _lamp_standard
    for k in range(6):
        a = math.radians(30 + 60 * k)
        _lamp_standard(s, 19.0 * math.cos(a), 19.0 * math.sin(a), 0.0)


# ----------------------------------------------------------------- the ship

def _plan_section(y0, y1, B, n=24):
    """Waterplane outline: bow at y0 (pointed, fine entry), stern at y1 (rounded cruiser stern), half beam B; CCW."""
    def w(u):
        bow = min(1.0, (u / 0.32)) ** 0.62
        st = math.sqrt(max(0.0, 1 - max(0.0, (u - 0.86) / 0.14) ** 2))
        return B * min(bow, st)
    ys = [y0 + (y1 - y0) * k / n for k in range(n + 1)]
    us = [k / n for k in range(n + 1)]
    star = [(w(u), y) for u, y in zip(us, ys)]
    port = [(-w(u), y) for u, y in zip(us[1:-1][::-1], ys[1:-1][::-1])]
    return star + port


SECTIONS = ((-1.6, -41.0, 44.5, 5.4), (0.0, -45.0, 46.0, 6.6), (0.8, -45.6, 46.4, 6.8), (3.5, -46.8, 47.2, 7.1), (7.6, -48.0, 47.8, 7.2))


def _halfbeam(z, y):
    """Half beam of the hull at height z and station y (interpolated from SECTIONS)."""
    for (za, a0, a1, Ba), (zb, b0, b1, Bb) in zip(SECTIONS, SECTIONS[1:]):
        if za <= z <= zb:
            f = (z - za) / (zb - za)
            y0, y1, B = a0 + (b0 - a0) * f, a1 + (b1 - a1) * f, Ba + (Bb - Ba) * f
            u = (y - y0) / (y1 - y0)
            if not 0 <= u <= 1:
                return 0.0
            return B * min(min(1.0, u / 0.32) ** 0.62, math.sqrt(max(0.0, 1 - max(0.0, (u - 0.86) / 0.14) ** 2)))
    return 0.0


@asset("OceanLiner", "Landmarks", bevel=0.0)
def ocean_liner(s):
    secs = [(_plan_section(y0, y1, B), z) for z, y0, y1, B in SECTIONS]
    s.loft(secs[:3], pal.Enamel_Red, cap_top=False)                       # underwater + oxblood boot-top to 0.8
    s.loft(secs[2:], pal.Hull_Navy, cap_bottom=False, cap_top=False)      # navy topsides
    top = SECTIONS[-1]
    s.extrude(_plan_section(top[1] + 0.3, top[2] - 0.3, top[3] - 0.25), 7.4, 7.6, pal.Terrazzo_Ivory)   # weather deck
    s.extrude(_plan_section(top[1], top[2], top[3] + 0.06), 7.25, 7.6, pal.Gold, top=False, bottom=False)  # gold sheer line
    s.extrude(_plan_section(top[1] + 1.0, top[2] - 0.6, top[3] - 0.05), 7.6, 8.6, pal.Hull_Ivory, top=False, bottom=False)  # bulwark
    # portholes: two rows on each side
    for zp in (3.4, 5.6):
        for k in range(36):
            y = -36.0 + 2.1 * k
            for sx in (1, -1):
                x = sx * (_halfbeam(zp, y) + 0.02)
                with s.at((x, y, zp), (0, 90 * sx, 0)):
                    s.extrude(circle(0.26, 8), 0.0, 0.05, pal.Window_Tower)
    # superstructure: stepped streamline decks, rounded fronts, ribbon windows, turquoise speed lines
    decks = ((7.6, 10.6, 12.2, -32.0, 38.0), (10.6, 13.6, 11.0, -26.0, 33.0), (13.6, 16.6, 9.6, -21.0, 24.0), (16.6, 19.0, 8.0, -19.5, -7.5))
    for z0, z1, w, ya, yb in decks:
        with s.at((0.0, (ya + yb) / 2, 0.0)):
            _streamline_block(s, w, yb - ya, z0, z1, pal.Hull_Ivory, bands=((z1 - 0.45, z1 - 0.2),), glass=((z0 + 1.0, z0 + 2.1),), r=w / 2 - 0.05)
            s.extrude(rrect(w + 0.5, yb - ya + 0.5, w / 2 + 0.2, n=8), z1, z1 + 0.15, pal.Hull_Ivory, smooth_sides=True)   # deck overhang
    with s.at((0.0, -13.5, 0.0)):                                          # bridge wings
        s.box((-8.2, -1.2, 17.6), (8.2, 1.2, 19.0), pal.Hull_Ivory)
        s.box((-8.25, -1.25, 18.1), (8.25, 1.25, 18.8), pal.Window_Teal)
    # funnels: raked ellipses, turquoise with an ivory band and a dark cap
    for y in (-8.0, 5.0, 18.0):
        def ell(a, b, dy, n=20):
            return [(a * math.cos(2 * math.pi * k / n), dy + b * math.sin(2 * math.pi * k / n)) for k in range(n)]
        z0, z1, rake = 16.4, 30.0, math.tan(math.radians(12))
        with s.at((0.0, y, 0.0)):
            s.loft([(ell(3.2, 4.8, 0.0), z0), (ell(3.0, 4.5, (z1 - 2.4 - z0) * rake), z1 - 2.4)], pal.Enamel_Turquoise, cap_bottom=False, cap_top=False)
            s.loft([(ell(3.05, 4.55, (z1 - 2.4 - z0) * rake), z1 - 2.4), (ell(3.0, 4.5, (z1 - 1.0 - z0) * rake), z1 - 1.0)], pal.Hull_Ivory, cap_bottom=False, cap_top=False)
            s.loft([(ell(3.0, 4.5, (z1 - 1.0 - z0) * rake), z1 - 1.0), (ell(2.95, 4.45, (z1 - z0) * rake), z1)], pal.Window_Tower, cap_bottom=False)
    # masts and stays
    for y, h in ((-36.0, 32.0), (34.0, 27.0)):
        with s.at((0.0, y, 0.0)):
            s.lathe([(0.32, 7.6), (0.12, h)], pal.Chrome, seg=8, cap_bottom=False)
            s.box((-2.4, -0.1, h - 6.0), (2.4, 0.1, h - 5.7), pal.Chrome)  # yard
            ball(s, (0.0, 0.0, h + 0.2), 0.3, pal.Beacon_Rose, seg=10, n=5)
    for a, b in (((0, -36.0, 32.0), (0, -47.5, 8.0)), ((0, -36.0, 32.0), (0, -8.0, 30.5)), ((0, 18.0, 30.5), (0, 34.0, 27.0)), ((0, 34.0, 27.0), (0, 47.5, 8.0))):
        strut(s, a, b, 0.08, pal.Steel)
    # lifeboats on davits along the second deck
    for sx in (1, -1):
        for k in range(6):
            y = -18.0 + 8.0 * k
            x = sx * 6.4
            s.rbox((x - 0.9, y - 2.8, 12.2), (x + 0.9, y + 2.8, 13.4), 0.5, pal.Hull_Ivory)
            s.box((x - 0.92, y - 2.6, 12.9), (x + 0.92, y + 2.6, 13.05), pal.Enamel_Flamingo)
