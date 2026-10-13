"""Deco Kit landmark megastructures (Concepts/Streets/spaceage, mega): the 1950s space-age city's colossal
machines, seen down the boulevard from 150-900 m. Pivot = footprint centre at ground (z = 0), front -Y, no bevel;
the painted hull plating (hull_sheet.py, 8 m repeat) carries the hard-surface detail at scale.
  SpaceHQ        space-fleet headquarters, the flagship's civic showpiece: limestone podium and grand stair, then
                 spacehq.tower: four stepped fluted cream tiers with gold ribs and turquoise speed bands, eight
                 streamline buttresses, two chrome-and-gold galleries, a chrome-and-gold atom over a needle mast; 252 m.
  SphereStation  (sphere_station) 120 m panelled sphere on six lattice legs and a ring girder, equatorial gantry
                 trench, service towers, crown radome; 172 m.
  LinerDock      (liner_dock) 150 m rocket liner standing on its fins inside a 176 m lattice gantry with swing arms.
  Skylon         (skylon_avenue) 90 m silver spindle floating 18 m up on cables from four lattice pylons; its
                 pylons stand +-13 m either side (place over a 26 m wide way).
  ParabolicArch  (atomic_crowns) 68 m gateway, feet +-16 m on X: spans the boulevard, frames the HQ beyond.
  DiscTower      (sky_bridges) 157 m gate tower: fluted ringed shaft, observation disc, crane jib, mast.
  SkyBridge      72 m enclosed streamline bridge along X between two DiscTowers 88 m apart.
"""

import math

from geo import circle
from kit import asset
from mats import pal, register
from mega_parts import FRONT, cable, girder, lens, ogive, ring_girder, rocket, strut, truss
from structures import sec, tube

register("DK_Hull_Ivory", "enamel", "#F4EFE3")
register("DK_Hull_Silver", "metal", "#C9CDD2")
register("DK_Steel_Ivory", "enamel", "#E4DFD2")  # painted lattice steel


# ----------------------------------------------------------------- SpaceHQ

def fluted_shaft(s, r, z0, z1, ribs=24, rib=None):
    """Cylinder with proud ribs (limestone unless `rib`) and dark slot windows between them."""
    s.lathe([(r, z0), (r, z1)], pal.Window_Tower, seg=ribs * 2, cap_bottom=False, cap_top=False)
    for k in range(ribs):
        a = 360 * k / ribs
        with s.at((0, 0, 0), (0, 0, a)):
            s.box((r - 0.2, -0.35, z0), (r + 0.55, 0.35, z1), rib or pal.Limestone_Tex)


def grand_stair(s, w=14.0, run=(48.0, 30.0), rise=5.0, n=31):
    """Monumental stair up the podium on +Y (toward the boulevard once placed): n risers from the ground at
    y = run[0] to the podium top, tread ends buried in the drum; limestone cheek walls with bronze copings.
    Tops stop 2 cm under the podium decks where they overlap them (no coplanar faces)."""
    y0, y1 = run
    for k in range(n):
        top = min(rise * (k + 1) / n, rise - 0.02)
        top = top - 0.02 if abs(top - 2.4) < 0.02 else top
        s.box((-w, y1, 0.0), (w, y0 - (y0 - 36.0) * k / n, top), pal.Limestone_Tex)
    cheek = [(y0 + 0.6, 0.0), (y0 + 0.6, 1.0), (36.0, rise + 0.9), (y1 + 3.0, rise + 0.9), (y1 + 3.0, 0.0)]
    cope = [(y0 + 0.6, 1.0), (36.0, rise + 0.9), (y1 + 3.0, rise + 0.9), (y1 + 3.0, rise + 1.1), (36.0, rise + 1.1),
            (y0 + 0.8, 1.2), (y0 + 0.8, 1.0)]
    for x in (-w - 1.4, w):
        with s.at((0, 0, 0), (0, 0, 90)), s.at((0, 0, 0), FRONT):  # local (x, y, z) -> world (z, x, y)
            s.extrude(cheek, x, x + 1.4, pal.Limestone_Tex)
            s.extrude(cope, x - 0.1, x + 1.5, pal.Bronze)


@asset("SpaceHQ", "Landmarks", bevel=0.0)
def space_hq(s):
    # podium: stepped round limestone plinth with a gold band (the civic base stays stone; the tower is gold and chrome)
    s.lathe([(40, 0), (40, 2.4), (36, 2.4), (36, 5.0), (0, 5.0)], pal.Limestone_Tex, seg=64)
    s.lathe([(36.05, 3.2), (36.05, 4.2)], pal.Gold, seg=64, cap_bottom=False, cap_top=False)
    grand_stair(s)
    # the flagship tower (spacehq.tower): stepped fluted tiers, buttresses, galleries, atom on a needle mast; tip 252.
    # Sky 2026-10-13: an atom over a needle mast, not a rocket (the landing pad, liner and gantry are gone).
    from spacehq import tower
    tower(s, 5.0, fin_r=35.6)


# ----------------------------------------------------------------- SphereStation

SR, SZ = 60.0, 108.0  # sphere radius, centre height


@asset("SphereStation", "Landmarks", bevel=0.0)
def sphere_station(s):
    n = 24
    s.lathe([(SR * math.sin(math.pi * k / n), SZ - SR * math.cos(math.pi * k / n)) for k in range(n + 1)],
            pal.Hull_Ivory, seg=72)
    # equatorial trench: a dark recessed band, gantry walkways and chrome rails either side
    s.lathe([(SR + 0.05, SZ - 2.2), (SR + 0.05, SZ + 2.2)], pal.Window_Tower, seg=72, cap_bottom=False, cap_top=False)
    for dz in (-2.6, 2.6):
        s.lathe([(SR - 0.2, SZ + dz - 0.35), (SR + 2.6, SZ + dz - 0.1), (SR + 2.6, SZ + dz + 0.1),
                 (SR - 0.2, SZ + dz + 0.35)], pal.Steel_Ivory, seg=72, cap_bottom=False, cap_top=False)
    for lat in (-32.0, 30.0):  # secondary service rings
        a = math.radians(lat)
        r, z = SR * math.cos(a) + 0.1, SZ + SR * math.sin(a)
        s.lathe([(r, z - 0.5), (r + 0.9, z), (r, z + 0.5)], pal.Steel_Ivory, seg=72, cap_bottom=False, cap_top=False)
    # six lattice legs from the ground out at r 62 to the sphere's lower shoulder, tied by a ring girder at 34 m
    for k in range(6):
        a = 2 * math.pi * (k + 0.5) / 6
        foot = (62 * math.cos(a), 62 * math.sin(a), 0.0)
        head = (46 * math.cos(a), 46 * math.sin(a), SZ - 40.0)
        truss(s, foot, head, 2.6, 1.8, 14, pal.Steel_Ivory, chord=0.7, lace=0.35)
        s.box((foot[0] - 4, foot[1] - 4, 0.0), (foot[0] + 4, foot[1] + 4, 3.0), pal.Limestone_Tex)
    ring_girder(s, 54.0, 34.0, 12, 0.8, pal.Steel_Ivory, depth=3.0)  # legs pass r 54 at 34 m
    # central lift core and plinth
    s.lathe([(9.0, 0.0), (9.0, 6.0), (6.0, 6.0), (6.0, SZ - SR + 1.0)], pal.Limestone_Tex, seg=32, cap_bottom=False)
    s.lathe([(6.05, 12.0), (6.05, SZ - SR - 2.0)], pal.Window_Tower, seg=32, cap_bottom=False, cap_top=False)
    # two service towers against the flank, with cantilevered walkways
    for x in (-1, 1):
        c = (x * (SR + 9.0), -8.0)
        truss(s, (*c, 0.0), (*c, SZ + 30.0), 2.4, 2.0, 20, pal.Steel_Ivory, chord=0.5, lace=0.25)
        for z in (SZ - 30.0, SZ, SZ + 24.0):
            strut(s, (c[0], c[1], z), (x * (SR * math.cos(math.asin((z - SZ) / SR)) - 1.0), -8.0, z), 1.2,
                  pal.Steel_Ivory)
    # crown: radome on a collar, antenna
    s.lathe([(8.0, SZ + SR - 1.0), (8.0, SZ + SR + 2.0), (6.5, SZ + SR + 2.0)], pal.Steel_Ivory, seg=32,
            cap_bottom=False)
    s.lathe([(6.5 * math.cos(math.pi * k / 16), SZ + SR + 2.0 + 6.5 * math.sin(math.pi * k / 16)) for k in range(9)],
            pal.Hull_Ivory, seg=32, cap_bottom=False)
    s.lathe([(0.0, SZ + SR + 8.4), (0.2, SZ + SR + 8.4), (0.04, SZ + SR + 20.0), (0.0, SZ + SR + 20.0)], pal.Chrome, seg=8)


# ----------------------------------------------------------------- LinerDock

@asset("LinerDock", "Landmarks", bevel=0.0)
def liner_dock(s):
    s.extrude(circle(34.0, 48), 0.0, 3.0, pal.Concrete)
    s.extrude(circle(18.0, 48), 3.0, 3.4, pal.Iron)  # blast deck
    rocket(s, 3.4, 150.0, 7.0)
    # gantry: four lattice columns on the pad's corners, open to the front (-Y), ring frames every 22 m
    cols = [(-19.0, -19.0), (19.0, -19.0), (19.0, 19.0), (-19.0, 19.0)]
    for x, y in cols:
        truss(s, (x, y, 3.0), (x, y, 176.0), 1.8, 1.8, 30, pal.Steel_Ivory, chord=0.6, lace=0.3)
    for z in range(25, 176, 22):
        for i in (1, 2, 3):  # back and side frames only: the front stays open to the boulevard
            (x0, y0), (x1, y1) = cols[i], cols[(i + 1) % 4]
            strut(s, (x0, y0, z), (x1, y1, z), 1.0, pal.Steel_Ivory)
            strut(s, (x0, y0, z - 22), (x1, y1, z), 0.5, pal.Steel_Ivory)
    for z in (60.0, 100.0, 140.0):  # swing arms from the back columns to the liner
        for x in (-19.0, 19.0):
            strut(s, (x, 19.0, z), (x * 0.3, 7.0, z), 1.4, pal.Steel_Ivory)
            strut(s, (x * 0.3, 7.0, z), (x * 0.3, 7.0, z + 3.0), 1.4, pal.Steel_Ivory)
    s.box((-21.0, -21.0, 176.0), (21.0, 21.0, 179.0), pal.Steel_Ivory)  # crown frame deck
    truss(s, (0.0, 12.0, 179.0), (0.0, 12.0, 205.0), 0.8, 0.25, 8, pal.Steel_Ivory, chord=0.3, lace=0.15)


# ----------------------------------------------------------------- Skylon

SK_LOW, SK_LEN, SK_R, SK_PY, SK_PH = 18.0, 90.0, 3.2, 13.0, 30.0


@asset("Skylon", "Landmarks", bevel=0.0)
def skylon(s):
    half = SK_LEN * 0.5
    up = [(x, SK_LOW + half + z) for x, z in ogive(SK_R, half, 14)]
    down = [(x, SK_LOW + half - z) for x, z in ogive(SK_R, half, 14)][::-1]
    s.lathe(down + up[1:], pal.Hull_Silver, seg=24)
    waist = SK_LOW + half * 0.72
    for x in (-1, 1):
        for y in (-1, 1):
            base, top = (x * 22.0, y * SK_PY, 0.0), (x * 22.0, y * SK_PY, SK_PH)
            truss(s, base, top, 1.1, 0.5, 8, pal.Steel_Ivory, chord=0.3, lace=0.15)
            cable(s, top, (x * SK_R * 0.6, y * SK_R * 0.6, waist))
            cable(s, top, (0.0, 0.0, SK_LOW + 1.5))
            cable(s, (x * 34.0, y * SK_PY, 0.0), top)  # back-stay


# ----------------------------------------------------------------- ParabolicArch

ARCH_A, ARCH_H = 16.0, 68.0  # half span (feet on the promenades at +-16), crest height


@asset("ParabolicArch", "Landmarks", bevel=0.0)
def parabolic_arch(s):
    """(atomic_crowns) Gateway spanning the boulevard in the XZ plane: a tapered, rounded limestone rib with teal face
    inlays and gold edge beads, bronze-panelled plinths, a chrome atom at the crest."""
    n = 40
    P = [(ARCH_A * (2 * k / n - 1), 0.0, ARCH_H * (1 - (2 * k / n - 1) ** 2)) for k in range(n + 1)]
    taper = [(1.0 - 0.45 * (1 - abs(2 * k / n - 1)), 1.0 - 0.45 * (1 - abs(2 * k / n - 1))) for k in range(n + 1)]
    tube(s, P, sec(-1.6, 1.6, -2.2, 2.2, 0.6), pal.Limestone_Tex, up=(0, 1, 0), scale=taper)
    for side in (-1, 1):
        tube(s, P, sec(-0.55, 0.55, side * 2.2 - 0.03, side * 2.2 + 0.03)[:: side], pal.Enamel_Teal, up=(0, 1, 0),
             scale=taper)
        for a in (-1.62, 1.62):
            tube(s, P, [(a + 0.07 * math.cos(t), side * 2.0 + 0.07 * math.sin(t))
                        for t in (2 * math.pi * j / 6 for j in range(6))], pal.Gold, up=(0, 1, 0), scale=taper)
    for x in (-ARCH_A, ARCH_A):
        s.box((x - 3.2, -3.4, 0.0), (x + 3.2, 3.4, 3.2), pal.Limestone_Tex)
        s.box((x - 3.25, -3.45, 0.8), (x + 3.25, 3.45, 2.4), pal.Bronze)
    c = (0.0, 0.0, ARCH_H + 4.6)
    with s.at(c):
        s.lathe([(2.6 * math.sin(math.pi * k / 12), -2.6 * math.cos(math.pi * k / 12)) for k in range(13)],
                pal.Chrome, seg=24)
        for tilt in (0, 60, 120):
            with s.at((0, 0, 0), (70, 0, tilt)):
                ring = [(6.5 * math.cos(2 * math.pi * j / 48), 3.0 * math.sin(2 * math.pi * j / 48), 0.0)
                        for j in range(48)]
                tube(s, ring, [(0.18 * math.cos(t), 0.18 * math.sin(t)) for t in
                               (2 * math.pi * j / 8 for j in range(8))], pal.Gold, closed=True)
    strut(s, (0.0, 0.0, ARCH_H - 1.0), (0.0, 0.0, ARCH_H + 2.2), 0.6, pal.Chrome)


# ----------------------------------------------------------------- DiscTower, SkyBridge

DT_R, DT_TOP = 9.0, 112.0


@asset("DiscTower", "Landmarks", bevel=0.0)
def disc_tower(s):
    """(sky_bridges) Gate tower: flared limestone foot, fluted shaft ringed every 12 m, a panelled observation disc,
    glazed drum, a lattice crane jib and a ringed mast; 157 m."""
    disc_tower_at(s, DT_TOP)


def disc_tower_at(s, DT_TOP=DT_TOP):
    """DT_TOP = the shaft top / disc floor (kit 112; a regen order puts it on the storey grid)."""
    s.lathe([(11.0, 0.0), (11.0, 2.0), (10.2, 4.0), (9.5, 10.0), (DT_R, 16.0)], pal.Limestone_Tex, seg=48,
            cap_bottom=False, cap_top=False)
    s.lathe([(11.03, 2.4), (11.03, 3.2)], pal.Enamel_Teal, seg=48, cap_bottom=False, cap_top=False)
    fluted_shaft(s, DT_R, 16.0, DT_TOP, ribs=20)
    for z in range(28, int(DT_TOP) - 4, 12):
        lens(s, float(z), DT_R + 0.3, DT_R + 1.5, 0.7, pal.Enamel_Ivory, glaze=False, seg=48)
    lens(s, DT_TOP, DT_R + 0.3, 20.0, 5.0, pal.Hull_Ivory)
    s.lathe([(6.5, DT_TOP + 5.0), (6.5, DT_TOP + 11.0)], pal.Window_Tower, seg=32, cap_bottom=False, cap_top=False)
    lens(s, DT_TOP + 11.0, 0.5, 8.5, 1.6, pal.Enamel_Ivory, glaze=False, seg=48)
    s.lathe([(6.0, DT_TOP + 12.6), (0.0, DT_TOP + 12.6)], pal.Enamel_Ivory, seg=32, cap_bottom=False, cap_top=False)
    z = DT_TOP + 12.6
    truss(s, (0, 0, z), (0, 0, z + 12.0), 1.2, 1.2, 3, pal.Steel_Ivory, chord=0.4, lace=0.2)  # crane king post
    girder(s, (-8.0, 0.0, z + 12.0), (24.0, 0.0, z + 12.0), 1.8, 1.6, 12, pal.Steel_Ivory)  # jib + counter-jib
    s.box((-9.5, -1.4, z + 10.5), (-6.0, 1.4, z + 12.0), pal.Concrete)                  # counterweight
    for x in (-8.0, 24.0):
        cable(s, (0.0, 0.0, z + 18.0), (x, 0.0, z + 13.6), 0.08)
    truss(s, (0, 0, z + 12.0), (0, 0, z + 18.0), 0.5, 0.2, 2, pal.Steel_Ivory, chord=0.2, lace=0.1)
    truss(s, (4.5, 3.0, z), (4.5, 3.0, z + 28.0), 0.6, 0.18, 8, pal.Steel_Ivory, chord=0.18, lace=0.09)
    with s.at((4.5, 3.0, 0.0)):
        s.lathe([(0.0, z + 28.0), (0.1, z + 28.0), (0.02, z + 32.0), (0.0, z + 32.0)], pal.Chrome, seg=6)


SB_LEN = 72.0


@asset("SkyBridge", "Landmarks", bevel=0.0)
def sky_bridge(s):
    """Enclosed streamline bridge along X, 72 m (spans 70 m between DiscTowers 88 m apart, 1 m into each): rounded
    tube with a wrap-around window band, a teal keel stripe and ivory ribs every 4 m. Pivot = centre."""
    P = [(-SB_LEN / 2, 0.0, 0.0), (SB_LEN / 2, 0.0, 0.0)]
    tube(s, P, sec(-3.0, 3.0, -2.2, 2.2, 1.6), pal.Hull_Ivory, caps=False)
    tube(s, P, sec(-3.03, 3.03, -0.3, 1.1, 0.5), pal.Window_Tower, caps=False)
    tube(s, P, sec(-0.8, 0.8, -2.23, -2.0), pal.Enamel_Teal, caps=False)
    for k in range(1, int(SB_LEN // 4)):
        x = -SB_LEN / 2 + 4.0 * k
        tube(s, [(x - 0.25, 0.0, 0.0), (x + 0.25, 0.0, 0.0)], sec(-3.15, 3.15, -2.35, 2.35, 1.7), pal.Enamel_Ivory)
