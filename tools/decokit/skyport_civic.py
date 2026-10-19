"""Skyport civic pieces (registered through skyport.py). Front -Y, no bevel, pivot = footprint centre at ground.
  CircusMonument   centrepiece of a round plaza island (R 14): basin ring (R 13.6) of DK_Water with arcing jets and
                   four causeways, stepped limestone plinth, gilded pedestal, fluted column, chrome armillary orrery
                   with a gilded ringed planet and moons on arms; 28.8 m to the finial.
  SkyRailStation   elevated sky-rail halt. The beam runs along Y at x = 0 (rail top RT = 22 m) and is NOT part of the
                   asset: x in +-1.75 stays clear from the ground to the canopy. Side platforms 4 x 40 m (x 1.8..5.8,
                   top PT = 22.5), streamline canopy with speed ribs and a lit HARBOUR dorsal fin, V-less streamline
                   piers, glazed lift tower with a helical stair down to the ground at +Y and an overbridge to the
                   west platform."""

import math

from geo import ring
from kit import asset
from mats import pal, register
from mega_parts import FRONT, strut
from skyport_parts import ball, band, bars, bead, flight, helix, lift_tube, pol, word
from structures import superell, tube

register("DK_Water", "glass", "#6FB7C4")  # as plaza.py
register("DK_Spray", "glass", "#EEF9FA")  # as plaza.py


def lamp_post(s, x, y, z=0.0, h=3.6):
    """Plaza sphere lamp: chrome fluted post, gold collar, glowing globe."""
    with s.at((x, y, z)):
        s.lathe([(0.2, 0.0), (0.2, 0.25), (0.08, 0.4), (0.06, h - 0.3), (0.12, h - 0.2), (0.0, h - 0.15)], pal.Chrome,
                seg=8, cap_bottom=False)
        s.lathe([(0.13, h - 0.25), (0.13, h - 0.12)], pal.Gold, seg=8, cap_bottom=False, cap_top=False)
    ball(s, (x, y, z + h + 0.15), 0.3, pal.Lamp, seg=10, n=5)


# ----------------------------------------------------------------- CircusMonument

CM_R, CM_WATER = 13.6, 0.52   # basin kerb outer radius, water surface
ORB_Z, ORB_R = 24.7, 3.3      # armillary centre, ring radius (ring top 28.0, finial 28.7)


def basin(s):
    s.lathe([(CM_R, 0.0), (CM_R, 0.5), (CM_R - 0.1, 0.66), (CM_R - 0.3, 0.72), (CM_R - 0.6, 0.72), (CM_R - 0.75, 0.64),
             (CM_R - 0.8, 0.5), (CM_R - 0.8, 0.1)], pal.Limestone_Tex, seg=72, cap_bottom=False, cap_top=False)
    band(s, CM_R + 0.01, 0.28, 0.4, pal.Enamel_Teal, 72)
    s.lathe([(CM_R - 0.8, 0.1), (8.6, 0.1)], pal.Tile_Teal, seg=72, cap_bottom=False, cap_top=False)
    s.lathe([(CM_R - 0.8, CM_WATER), (8.6, CM_WATER)], pal.Water, seg=72, cap_bottom=False, cap_top=False)
    for k in range(12):  # arcing jets from kerb nozzles to the plinth
        a = 90 * (k // 3) + 22.5 * (k % 3 + 1)
        with s.at((0, 0, 0), (0, 0, a)):
            with s.at((CM_R - 0.7, 0.0, 0.6)):
                s.lathe([(0.12, 0.0), (0.12, 0.12), (0.06, 0.2), (0.0, 0.2)], pal.Chrome, seg=8, cap_bottom=False)
            P = [(CM_R - 0.7 - 3.6 * f, 0.0, 0.75 + 3.0 * 4 * f * (1 - f) - 0.25 * f) for f in (j / 8 for j in range(9))]
            s.sweep(P, [0.05 + 0.06 * j / 8 for j in range(9)], pal.Spray, seg=6, caps=False)
            with s.at((CM_R - 4.3, 0.0, CM_WATER)):
                s.lathe([(0.1, 0.0), (0.45, 0.02), (0.25, 0.22), (0.0, 0.3)], pal.Spray, seg=8, cap_bottom=False)
    for k in range(16):  # bubbler plumes ringing the plinth
        with s.at(pol(8.95, 360 * (k + 0.5) / 16, CM_WATER)):
            s.lathe([(0.16, 0.0), (0.1, 0.5), (0.05, 0.9), (0.0, 1.0)], pal.Spray, seg=6, cap_bottom=False)


def plinth(s):
    """Stepped round plinth from the water, four causeways and stairs on the axes, pedestal with frieze and medallions."""
    prof = [(8.6, 0.1), (8.6, 1.0)]
    for r, z in ((7.8, 1.5), (7.0, 2.0), (6.2, 2.5), (5.4, 3.0)):
        prof += [(r, prof[-1][1]), (r, z)]
    s.lathe(prof + [(3.6, 3.0)], pal.Limestone_Tex, seg=64, cap_bottom=False, cap_top=False)
    bead(s, 8.6, 0.85, pal.Gold, 64, 0.04, 0.04)
    for a in (0, 90, 180, 270):
        with s.at((0, 0, 0), (0, 0, a)):
            s.box((8.3, -1.6, 0.0), (CM_R - 0.4, 1.6, 0.74), pal.Limestone_Tex)          # causeway, two steps down
            s.box((CM_R - 0.4, -1.6, 0.0), (CM_R - 0.05, 1.6, 0.5), pal.Limestone_Tex)
            s.box((CM_R - 0.05, -1.6, 0.0), (CM_R + 0.3, 1.6, 0.25), pal.Limestone_Tex)
            s.box((8.3, -1.6, 0.74), (9.1, 1.6, 0.87), pal.Limestone_Tex)
            for i in range(12):                                                         # stair up the tiers
                s.box((5.3, -1.3, 0.0), (8.35 - 0.26 * i, 1.3, 1.01 + 0.167 * (i + 1)), pal.Limestone_Tex)
            for y in (-1.6, 1.3):                                                       # cheek walls, gold copings
                with s.at((0, 0, 0), FRONT):
                    s.extrude([(8.3, 1.0), (8.3, 0.9), (8.7, 0.9), (8.7, 1.35), (5.3, 3.35), (5.3, 3.0)], -y - 0.3, -y,
                              pal.Limestone_Tex)
                    s.extrude([(8.7, 1.35), (8.7, 1.43), (5.3, 3.43), (5.3, 3.35)], -y - 0.32, -y + 0.02, pal.Gold)
        lamp_post(s, *pol(CM_R - 0.3, a + 9.5), 0.74)
        lamp_post(s, *pol(CM_R - 0.3, a - 9.5), 0.74)
    s.lathe([(3.9, 3.0), (3.9, 3.35), (3.5, 3.6), (3.4, 3.6), (3.4, 6.1), (3.8, 6.35), (3.8, 6.75), (2.6, 7.0)],
            pal.Limestone_Tex, seg=48, cap_bottom=False, cap_top=False)
    band(s, 3.42, 5.1, 5.75, pal.Enamel_Teal, 48)
    for z in (5.05, 5.8):
        bead(s, 3.4, z, pal.Gold, 48, 0.04, 0.05)
    for k in range(4):  # gilded medallions: chrome rim, eight-point star
        a = 45 + 90 * k
        with s.at(pol(3.4, a, 4.3), (90, 0, a + 90)):
            s.lathe([(0.85, 0.0), (0.85, 0.08), (0.7, 0.12), (0.0, 0.12)], pal.Gold, seg=24, cap_bottom=False)
            s.lathe(ring(0.85, 0.95, 0.0, 0.14), pal.Chrome, seg=24, cap_bottom=False, cap_top=False)
            bars(s, [[pol(0.6, 22.5 * j), pol(-0.6, 22.5 * j)] for j in range(0, 8, 2)], 0.09, 0.1, 0.18, pal.Chrome)
        lamp_post(s, *pol(4.9, a), 3.0, 2.2)


def fluted(s, z0, z1, r, n=20, dp=0.2):
    """Fluted column shaft with entasis: land / groove star section lofted through four stations."""
    star = []
    for i in range(n):
        for j, d in enumerate((0.0, 0.55, 1.0, 0.55)):
            ang = 2 * math.pi * (i + j / 4) / n
            star.append(((r - dp * d) * math.cos(ang), (r - dp * d) * math.sin(ang)))
    secs = []
    for f, k in ((0.0, 1.0), (0.3, 1.03), (0.7, 0.98), (1.0, 0.88)):
        secs.append(([(x * k, y * k) for x, y in star], z0 + (z1 - z0) * f))
    s.loft(secs, pal.Limestone_Tex, cap_bottom=False, cap_top=False)


def orrery(s):
    """Chrome armillary (three meridians, equator), gold ecliptic and polar axis, gilded ringed planet, moons on arms."""
    with s.at((0, 0, ORB_Z)):
        for az in (0, 60, 120):
            with s.at((0, 0, 0), (90, 0, az)):
                s.lathe(ring(ORB_R - 0.13, ORB_R, -0.13, 0.13), pal.Chrome, seg=64, cap_bottom=False, cap_top=False)
        s.lathe(ring(ORB_R - 0.26, ORB_R - 0.14, -0.15, 0.15), pal.Chrome, seg=64, cap_bottom=False, cap_top=False)
        with s.at((0, 0, 0), (0, 23.5, 0)):
            s.lathe(ring(ORB_R - 0.4, ORB_R - 0.28, -0.1, 0.1), pal.Gold, seg=64, cap_bottom=False, cap_top=False)
            s.lathe([(0.0, -ORB_R - 0.35), (0.07, -ORB_R - 0.35), (0.07, ORB_R + 0.35), (0.0, ORB_R + 0.35)], pal.Gold,
                    seg=8)
            ball(s, (0, 0, ORB_R + 0.5), 0.2, pal.Gold, seg=12)
            s.lathe([(0.0, ORB_R + 0.65), (0.06, ORB_R + 0.65), (0.0, ORB_R + 1.1)], pal.Chrome, seg=6)
            n = 16
            s.lathe([(1.25 * math.sin(math.pi * k / n), -1.25 * math.cos(math.pi * k / n)) for k in range(n + 1)],
                    pal.Gold, seg=32)  # planet
            with s.at((0, 0, 0), (18, 0, 0)):
                s.lathe(ring(1.6, 2.25, -0.03, 0.03), pal.Chrome, seg=48, cap_bottom=False, cap_top=False)
                s.lathe(ring(1.75, 1.95, -0.035, 0.035), pal.Gold, seg=48, cap_bottom=False, cap_top=False)
            for z, az, L, rm, mat in ((1.4, 20, 4.4, 0.52, pal.Enamel_Ivory), (-0.6, 140, 3.9, 0.4, pal.Enamel_Coral),
                                      (0.5, 250, 4.8, 0.36, pal.Enamel_Teal), (-1.5, 320, 2.5, 0.3, pal.Chrome)):
                p0, p1 = (0.0, 0.0, z), pol(L, az, z + 0.4)
                strut(s, p0, p1, 0.09, pal.Chrome)
                ball(s, p1, rm, mat, seg=16, n=8)
                ball(s, (0.0, 0.0, z), 0.16, pal.Gold, seg=10, n=5)
    for sd in (-1, 1):  # gold cradle from the capital to the meridian ring
        strut(s, (sd * 1.1, 0.0, 21.4), (sd * ORB_R * math.sin(math.radians(28)), 0.0,
                                          ORB_Z - ORB_R * math.cos(math.radians(28)) + 0.05), 0.16, pal.Gold)


@asset("CircusMonument", "Skyport", bevel=0.0)
def circus_monument(s):
    basin(s)
    plinth(s)
    s.lathe([(2.2, 7.0), (2.2, 7.25), (1.95, 7.45), (1.75, 7.5)], pal.Chrome, seg=40, cap_bottom=False, cap_top=False)
    fluted(s, 7.45, 19.8, 1.75)
    for z in (11.0, 16.0):
        s.lathe(ring(1.6, 1.86, z - 0.12, z + 0.12), pal.Gold, seg=40, cap_bottom=False, cap_top=False)
    s.lathe([(1.45, 19.7), (1.7, 19.8), (1.7, 20.05), (2.1, 20.4), (2.5, 20.85), (2.5, 21.1), (1.3, 21.35),
             (0.0, 21.4)], pal.Gold, seg=40, cap_bottom=False)
    band(s, 2.52, 20.88, 21.05, pal.Chrome, 40)
    orrery(s)


# ----------------------------------------------------------------- SkyRailStation

RT, PT = 22.0, 22.5            # rail top (beam top), platform top
PX0, PX1, PL = 1.8, 5.8, 20.0  # platform inner / outer edge, half length
EAVE, CROWN, CW = 26.1, 28.1, 6.8  # canopy eave z, crown z, half span
OB, TOWER = 26.4, (9.6, 17.6)  # overbridge floor, lift tower axis
LINK = (16.0, 19.2)            # east platform -> tower link, y range
NAME = "HARBOUR"


def canopy_z(x):
    return EAVE + (CROWN - EAVE) * (1 - (x / CW) ** 2)


def platform(s, sx):
    """One side platform (sx = +1 east, -1 west): terrazzo deck, chrome nosing, mustard edge line, ivory fascia with a
    teal stripe, box girder, three streamline piers with limestone feet and teal fins, windscreen, benches, lamps."""
    x0, x1 = sorted((sx * PX0, sx * PX1))
    s.box((x0, -PL, PT - 0.35), (x1, PL, PT), pal.Terrazzo_Ivory)
    xe = sx * PX0
    s.box((min(xe, xe + sx * 0.5), -PL, PT), (max(xe, xe + sx * 0.5), PL, PT + 0.012), pal.Enamel_Mustard)
    s.box((min(xe, xe - sx * 0.06), -PL, PT - 0.4), (max(xe, xe - sx * 0.06), PL, PT + 0.02), pal.Chrome)
    xo = sx * (PX1 + 0.08)
    s.box((min(sx * 2.2, xo), -PL, PT - 1.35), (max(sx * 2.2, xo), PL, PT - 0.35), pal.Enamel_Ivory)
    s.box((min(xo, xo + sx * 0.05), -PL, PT - 1.0), (max(xo, xo + sx * 0.05), PL, PT - 0.7), pal.Enamel_Teal)
    s.box((min(sx * 3.2, sx * 4.4), -PL + 1.0, PT - 2.5), (max(sx * 3.2, sx * 4.4), PL - 1.0, PT - 1.35),
          pal.Steel_Ivory)
    for y in (-14.0, 0.0, 14.0):
        x = sx * 3.8
        zs = [1.2, 3.5, 8.0, 13.0, 16.5, 18.6, 19.6, PT - 2.45]
        sc = [(1.3, 1.25), (1.0, 1.0), (0.88, 0.86), (0.82, 0.8), (0.82, 0.8), (0.95, 0.95), (1.3, 1.25), (1.7, 1.55)]
        tube(s, [(x, y, z) for z in zs], superell(0.8, 1.1, p=3, n=16), pal.Enamel_Ivory, up=(1, 0, 0), scale=sc)
        s.box((x - 1.5, y - 1.8, 0.0), (x + 1.5, y + 1.8, 1.3), pal.Limestone_Tex)
        s.box((x - 1.53, y - 1.83, 0.85), (x + 1.53, y + 1.83, 1.05), pal.Enamel_Teal)
        xf = sorted((x + sx * 0.7, x + sx * 1.25))
        s.box((xf[0], y - 0.12, 1.3), (xf[1], y + 0.12, 16.0), pal.Enamel_Teal)  # outboard fin
    xw, y0, y1 = sx * (PX1 - 0.25), -PL + 0.15, (LINK[0] if sx > 0 else PL - 0.15)
    n = round((y1 - y0) / 3.0)  # glazed windscreen along the outer edge (open onto the tower link on the east)
    for k in range(n):
        ya, yb = y0 + (y1 - y0) * k / n, y0 + (y1 - y0) * (k + 1) / n
        s.box((xw - 0.02, ya + 0.08, PT + 0.15), (xw + 0.02, yb - 0.08, PT + 2.3), pal.Glass)
        s.box((xw - 0.05, ya - 0.05, PT), (xw + 0.05, ya + 0.05, PT + 2.4), pal.Chrome)
    s.box((xw - 0.05, y1 - 0.05, PT), (xw + 0.05, y1 + 0.05, PT + 2.4), pal.Chrome)
    s.box((xw - 0.05, y0, PT + 2.35), (xw + 0.05, y1, PT + 2.45), pal.Chrome)
    for ye in (-PL + 0.15, PL - 0.15):  # end guard rails (a gap for the west stair)
        spans = ([(PX0 + 0.6, 3.25), (4.95, PX1 - 0.25)] if sx < 0 and ye > 0 else [(PX0 + 0.6, PX1 - 0.25)])
        for a, b in spans:
            xa, xb = sorted((sx * a, sx * b))
            s.box((xa, ye - 0.04, PT + 1.02), (xb, ye + 0.04, PT + 1.1), pal.Chrome)
            for x in (xa, xb, (xa + xb) / 2):
                s.box((x - 0.04, ye - 0.04, PT), (x + 0.04, ye + 0.04, PT + 1.02), pal.Chrome)
    for y in (-9.0, 3.0):  # benches
        xb = sx * (PX1 - 1.0)
        s.box((xb - 0.3, y - 1.1, PT + 0.42), (xb + 0.3, y + 1.1, PT + 0.5), pal.Enamel_Coral)
        s.box((xb + sx * 0.25 - 0.04, y - 1.1, PT + 0.5), (xb + sx * 0.25 + 0.04, y + 1.1, PT + 0.95), pal.Enamel_Coral)
        for dy in (-0.9, 0.9):
            s.box((xb - 0.2, y + dy - 0.05, PT), (xb + 0.2, y + dy + 0.05, PT + 0.42), pal.Chrome)
    for y in (-15.0, -3.0, 9.0):
        lamp_post(s, sx * (PX1 - 0.7), y, PT, 3.0)


def canopy(s, name=NAME):
    """Streamline canopy over both platforms and the track: arched ivory shell with tapered ends, chrome eave beads,
    speed ribs, lamp strips, columns on the platforms, the dorsal fin with the lit station name."""
    n = 12
    top = [(CW * (1 - 2 * k / n), canopy_z(CW * (1 - 2 * k / n)) - EAVE) for k in range(n + 1)]
    bot = [(x * 0.97, (canopy_z(x) - EAVE) * 0.9 - 0.35) for x, _ in top[::-1]]
    sec_ = top + bot  # CCW in the tube's (a, b) frame (a = -X on a +Y path; the arch is symmetric)
    ys = [-19.0, -18.0, -16.5, -14.0, 14.0, 16.5, 18.0, 19.0]
    sc = [(0.55, 0.5), (0.8, 0.75), (0.95, 0.93), (1.0, 1.0), (1.0, 1.0), (0.95, 0.93), (0.8, 0.75), (0.55, 0.5)]
    tube(s, [(0.0, y, EAVE) for y in ys], sec_, pal.Enamel_Ivory, scale=sc)
    for sd in (-1, 1):
        s.sweep([(sd * CW * k0, y, EAVE + 0.02) for y, (k0, _) in zip(ys, sc)], 0.1, pal.Chrome, seg=6)
        s.box((sd * 4.0 - 0.18, -16.0, EAVE + (canopy_z(4.0) - EAVE) * 0.9 - 0.4), (sd * 4.0 + 0.18, 16.0,
              EAVE + (canopy_z(4.0) - EAVE) * 0.9 - 0.34), pal.Lamp)
        for y in (-15.0, -5.0, 5.0, 15.0):  # columns
            x, zt = sd * 5.3, EAVE + (canopy_z(5.3) - EAVE) * 0.9 - 0.3
            tube(s, [(x, y, PT), (x, y, PT + 0.4), (x, y, zt - 0.6), (x, y, zt)], superell(0.16, 0.22, p=3, n=10),
                 pal.Enamel_Ivory, up=(1, 0, 0), scale=[(1.4, 1.4), (1.0, 1.0), (1.0, 1.0), (1.8, 1.6)])
            with s.at((x, y, 0.0)):
                s.lathe([(0.3, PT), (0.3, PT + 0.12), (0.0, PT + 0.14)], pal.Chrome, seg=10, cap_bottom=False)
                s.lathe([(0.26, zt - 0.7), (0.26, zt - 0.55)], pal.Gold, seg=10, cap_bottom=False, cap_top=False)
    for y in (-15.5, -10.5, -5.5, 5.5, 10.5, 15.5):  # speed ribs over the shell
        k0 = 1.0 if abs(y) < 16.0 else 0.9
        rib = [(x * k0, 0.0, canopy_z(x * k0) + 0.02) for x in (CW * (1 - 2 * k / n) for k in range(n + 1))]
        tube(s, [(x, y, z) for x, _, z in rib], [(0.0, -0.14), (0.55, -0.14), (0.55, 0.14), (0.0, 0.14)],
             pal.Enamel_Teal, up=(0, 1, 0))
    fin = [(-13.0, 0.0), (13.0, 0.0), (11.5, 3.4), (-9.5, 3.4)]
    with s.at((0.0, 0.0, CROWN - 0.1), (90, 0, 90)):
        s.extrude([(y, z) for y, z in fin], -0.3, 0.3, pal.Enamel_Coral)
        s.extrude([(-11.0, 0.3), (10.6, 0.3), (9.8, 2.6), (-9.0, 2.6)], -0.33, 0.33, pal.Enamel_Teal)
    for rot, x in (((90, 0, 90), 0.33), ((90, 0, -90), -0.33)):
        with s.at((x, 0.8, CROWN + 1.35), rot):
            if name:
                word(s, name, 1.25, 0.2, 0.0, 0.05, pal.Lamp)
    for sd in (-1, 1):  # hanging name boards facing the track
        xb = sd * 4.9
        for y in (-8.0, 8.0):
            zb = 25.1
            s.box((xb - 0.06, y - 2.9, zb), (xb + 0.06, y + 2.9, zb + 0.9), pal.Enamel_Teal)
            for dy in (-2.4, 2.4):
                cable_z = EAVE + (canopy_z(4.9) - EAVE) * 0.9 - 0.35
                s.box((xb - 0.02, y + dy - 0.02, zb + 0.9), (xb + 0.02, y + dy + 0.02, cable_z), pal.Chrome)
            with s.at((xb - sd * 0.065, y, zb + 0.45), (90, 0, -90 if sd > 0 else 90)):
                if name:
                    word(s, name, 0.5, 0.08, 0.0, 0.02, pal.Lamp)


def tower(s):
    """Lift tower at the +Y end on the east: glazed lift tube with a helical stair about it, ground plinth, link to the
    east platform, overbridge over the track to a stair down onto the west platform, crown machine room."""
    tx, ty = TOWER
    with s.at((tx, ty, 0.0)):
        s.lathe([(3.8, 0.0), (3.8, 0.3), (0.0, 0.3)], pal.Limestone_Tex, seg=32, cap_bottom=False)
        helix(s, 1.75, 3.1, 0.3, PT, 6.0, 108, a0=180.0)  # tops out at 180 deg, onto the platform link
        for k in range(4):
            a = 45 + 90 * k
            strut(s, pol(3.35, a, 0.3), pol(3.35, a, OB + 3.2), 0.24, pal.Enamel_Ivory)
        for z in (6.0, 12.0, 18.0, PT - 0.1, OB):
            s.lathe(ring(3.25, 3.45, z - 0.12, z + 0.12), pal.Steel_Ivory, seg=32, cap_bottom=False, cap_top=False)
        s.lathe([(3.6, OB + 3.2), (3.6, OB + 3.7), (2.4, OB + 4.4), (0.0, OB + 4.5)], pal.Enamel_Ivory, seg=32,
                cap_bottom=False)
        band(s, 3.62, OB + 3.3, OB + 3.55, pal.Enamel_Coral, 32)
        ball(s, (0, 0, OB + 4.7), 0.3, pal.Signal_Red, seg=10, n=5)
    lift_tube(s, tx, ty, 0.3, OB + 3.2, r=1.5, car=9.0, frame=4.5, back=None)
    s.box((PX1 - 0.1, LINK[0], PT - 0.35), (tx - 2.9, LINK[1], PT), pal.Terrazzo_Ivory)  # link to the east platform
    s.box((PX1 - 0.1, LINK[0], PT - 0.8), (tx - 2.9, LINK[1], PT - 0.35), pal.Enamel_Ivory)
    s.box((tx - 1.2, ty + 1.3, OB - 0.35), (tx + 1.2, 21.3, OB), pal.Terrazzo_Ivory)  # lift landing to the bridge
    yb, xw = 22.6, -4.1
    P = [(tx + 1.4, yb, OB - 0.3), (-5.7, yb, OB - 0.3)]
    tube(s, P, [(-1.4, 0.0), (1.4, 0.0), (1.4, 2.4), (0.9, 3.1), (-0.9, 3.1), (-1.4, 2.4)], pal.Enamel_Ivory)
    tube(s, P, [(-1.43, 1.2), (1.43, 1.2), (1.43, 2.3), (-1.43, 2.3)], pal.Window_Tower, caps=False)
    tube(s, P, [(-1.45, 0.25), (1.45, 0.25), (1.45, 0.5), (-1.45, 0.5)], pal.Enamel_Teal, caps=False)
    for x in (tx + 1.4, -5.7):  # coral end frames
        tube(s, [(x - 0.25, yb, OB - 0.3), (x + 0.25, yb, OB - 0.3)],
             [(-1.55, -0.12), (1.55, -0.12), (1.55, 2.45), (1.0, 3.25), (-1.0, 3.25), (-1.55, 2.45)], pal.Enamel_Coral)
    tube(s, [(xw, yb, 1.2), (xw, yb, 3.5), (xw, yb, OB - 2.2), (xw, yb, OB - 0.3)], superell(0.75, 0.8, p=3, n=16),
         pal.Enamel_Ivory, up=(1, 0, 0), scale=[(1.35, 1.3), (1.0, 1.0), (0.9, 0.9), (1.5, 1.6)])  # bridge pier
    s.box((xw - 1.4, yb - 1.4, 0.0), (xw + 1.4, yb + 1.4, 1.3), pal.Limestone_Tex)
    s.box((xw - 1.43, yb - 1.43, 0.85), (xw + 1.43, yb + 1.43, 1.05), pal.Enamel_Teal)
    flight(s, xw - 0.8, xw + 0.8, 14.8, yb - 1.35, PT, OB, 22)  # down onto the west platform (rises toward +Y)


def _station(s, name):
    for sx in (-1, 1):
        platform(s, sx)
    canopy(s, name)
    tower(s)


@asset("SkyRailStation", "Skyport", bevel=0.0)
def sky_rail_station(s):
    _station(s, NAME)


@asset("SkyRailStation_Blank", "Skyport", bevel=0.0)
def sky_rail_station_blank(s):
    """SkyRailStation with no name on the dorsal fin and the hanging boards (brief 020 C: HARBOUR was baked into the kit piece; a station's name is the plan's, set by a sign)."""
    _station(s, "")


# ----------------------------------------------------------------- SkyRail_Pylon_Tall

TALL_H = 12.0  # trunk top: the stretched SkyRail_Pylon_Shaft starts here (DecoKitSkyRail footTop = ground + 12)


@asset("SkyRail_Pylon_Tall", "Skyport", bevel=0.0)
def pylon_tall(s):
    """Trunk replacing SkyRail_Pylon_Foot where the rail runs 22-34 m up (the stock foot + a 17-29 m shaft reads as a
    1.5 m stick): stepped limestone plinth with a teal band, flared ivory trunk tapering to exactly the Shaft's base
    section at 12 m (same superellipse and axes, open top), teal fins fore and aft, chrome collar at the joint."""
    s.box((-2.2, -2.6, 0.0), (2.2, 2.6, 1.0), pal.Limestone_Tex)
    s.box((-2.23, -2.63, 0.55), (2.23, 2.63, 0.8), pal.Enamel_Teal)
    s.box((-1.9, -2.3, 1.0), (1.9, 2.3, 1.6), pal.Limestone_Tex)
    k = ((1.6, 2.1), (3.0, 1.75), (5.5, 1.4), (8.5, 1.15), (11.0, 1.02), (TALL_H, 1.0))
    tube(s, [(0.0, 0.0, z) for z, _ in k], superell(0.75, 0.55, p=3, n=20), pal.Enamel_Ivory, up=(1, 0, 0),
         scale=[(f, f) for _, f in k], caps=False)
    tube(s, [(0.0, 0.0, TALL_H - 0.45), (0.0, 0.0, TALL_H - 0.15)], superell(0.8, 0.6, p=3, n=20), pal.Chrome,
         up=(1, 0, 0), caps=False)
    fin = [(1.0, 1.6), (2.5, 1.6), (2.5, 2.6), (0.6, 9.8), (0.45, 9.8)]
    with s.at((0, 0, 0), (0, 0, 90)), s.at((0, 0, 0), FRONT):  # local (x, y, z) -> world (z, x, y): YZ plane
        for sd in (1, -1):
            s.extrude([(sd * y, z) for y, z in (fin if sd > 0 else fin[::-1])], -0.14, 0.14, pal.Enamel_Teal)
