"""Deco Kit facades family, the junction landmark (imported by facades.py).

  CornerTower_12  10 x 10 m cell (x -5..5, y 0..10): the -Y facade ends at x = +5 (a bay continues at x + 8), the -X
                  facade at y = 10; symmetric about its diagonal (mirror X for the right-hand corner). A 6-floor
                  streamline base (quarter round R 5 about (0, 5), shops wrapping it, wing parapets on the two 5 m
                  straights, roof plant on the wing decks) from which the R 5 drum rises free to 12 floors with ribbons
                  all round; observation disc with landing lights and a deck rail, glowing lantern, beacon mast with
                  antenna whips. Over the corner door a gold disc canopy and a revolving door; on the diagonal a blade
                  sign with raised neon HOTEL on both faces, chaser lights and a star-and-orbit. Pairs with StreamBay_6.
"""

import math

from geo import arc, circle
from kit import asset
from mats import pal
from mega_parts import lens
from plaza_parts import curved_glazing, lift, offset, ribbon
from skyline import BACK_Y, FH, G, GLASS_Y, SP, flank_walls, ground_trims, mullions, spire, top_of, wrap_cap
from structures import along, ccw, sec, tube
from facades_parts import FRONT, lamps, letters, mast, parapet_trims, rail_ring, vent_bank

TC, TR = (0.0, 5.0), 5.0
NB, NT = 6, 12  # base floors, drum floors
BASE = [(-5.0, 10.0), (-5.0, 9.6)] + arc(0.0, 5.0, 5.0, 180, 270, 10) + [(4.6, 0.0), (5.0, 0.0)]
BK = (5.0, 10.0)
WINGS = ([(-5.0, 10.0), (-5.0, 5.0)], [(0.0, 0.0), (5.0, 0.0)])
DG = (-math.sqrt(0.5), -math.sqrt(0.5))  # the corner diagonal, outward
TG = (math.sqrt(0.5), -math.sqrt(0.5))   # tangent across it


def on_diag(r, t=0.0):
    return TC[0] + r * DG[0] + t * TG[0], TC[1] + r * DG[1] + t * TG[1]


def course_trims(s, path, k0, k1, band, closed=False, r=0.05, n=2):
    """skyline.floor_trims' per-floor sill band and eyebrow for floors k0..k1-1, with a lighter fillet."""
    for k in range(k0, k1):
        zs, zt = G + k * FH + SP, G + (k + 1) * FH
        tube(s, lift(path), sec(-0.05, 0.02, zs - 0.22, zs - 0.06, r * 0.4, n), band, closed=closed)
        tube(s, lift(path), sec(-0.45, 0.05, zt - 0.02, zt + 0.1, r, n), pal.Enamel_Ivory, closed=closed)


def tower_base(s, body):
    """skyline.wrap_block's ground floor and ribbons on the quarter-round plan, with 0.4 m joint piers (the shop
    glazing runs the 5 m wings) and the parapet only on the wings: the drum carries on up from the round."""
    plan = BASE + [BK]
    ends = along(BASE, 1e3)
    for seg in (BASE[:2], BASE[-2:]):
        tube(s, lift(seg), sec(0, 0.8, 0.45, 4.0), body)
    curved_glazing(s, BASE[1:-1], 0.45, 4.0, a=GLASS_Y, step=1.5, dz=2.75, interior=False)
    s.extrude(offset(BASE, 2.6) + [BK], 0.45, 4.0, pal.Lobby)
    flank_walls(s, BASE, BK, 0.45, 4.0, body)
    s.extrude(offset(BASE, GLASS_Y) + [BK], 0.3, 0.45, pal.Terrazzo_Ivory)
    s.extrude(plan, 4.0, G, body)
    for k in range(NB):
        zb = G + k * FH
        zs, zf = zb + SP, zb + FH
        s.extrude(plan, zb, zs, body)
        ribbon(s, offset(BASE, GLASS_Y), zs, zf, pal.GlassDark)
        s.extrude(offset(BASE, BACK_Y + 0.02) + [BK], zs, zf, body)  # brief 048: solid core, charcoal skin in front (as skyline.wrap_block), no black flank
        ribbon(s, offset(BASE, BACK_Y), zs, zf, pal.Enamel_Charcoal)
        wrap_cap(s, *ends[0], zs, zf, body, True)
        wrap_cap(s, *ends[-1], zs, zf, body, False)
        mullions(s, BASE, zs, zf, GLASS_Y)
    ground_trims(s, BASE, pal.Enamel_Teal)
    course_trims(s, BASE, 0, NB, pal.Enamel_Seafoam)
    zt = top_of(NB)
    for w in WINGS:
        tube(s, lift(w), sec(0.0, 0.35, zt, zt + 0.95), body)
        parapet_trims(s, w, NB, pal.Enamel_Seafoam)
    for poly in (([(0.0, 0.0), (5.0, 0.0), (5.0, 5.0)] + arc(*TC, TR, 0, -90, 6)[1:-1]),
                 ([(5.0, 5.0), (5.0, 10.0), (0.0, 10.0)] + arc(*TC, TR, 90, 0, 6)[1:-1]),
                 ([(-5.0, 10.0), (-5.0, 5.0)] + arc(*TC, TR, 180, 90, 6)[1:-1] + [(0.0, 10.0)])):
        s.extrude(ccw(poly), zt, zt + 0.25, body)
    vent_bank(s, 3.9, 8.9, zt + 0.25, w=1.6, d=0.9, h=0.9, rot=-45)
    mast(s, 4.3, 0.9, zt + 0.25, 4.0)
    mast(s, -4.3, 9.3, zt + 0.25, 2.6, beacon=False)


def drum(s, body):
    """Floors NB..NT-1 as a free R 5 drum: spandrel rings, ribbon glass, dark core, mullions, plain-section courses."""
    circ = circle(TR, 40, *TC)
    glass, core = circle(TR - GLASS_Y, 40, *TC), circle(TR - BACK_Y, 40, *TC)
    for k in range(NB, NT):
        zb = G + k * FH
        zs, zf = zb + SP, zb + FH
        s.extrude(circ, zb, zs, body)
        ribbon(s, glass, zs, zf, pal.GlassDark, closed=True)
        s.extrude(core, zs, zf, pal.Enamel_Charcoal, top=False, bottom=False)
        mullions(s, circ + circ[:1], zs, zf, GLASS_Y, step=2.0)
    course_trims(s, circ, NB, NT, pal.Enamel_Seafoam, closed=True, r=0.0)
    zt = top_of(NT)
    s.extrude(circ, zt, zt + 0.95, body)
    tube(s, lift(circ), sec(-0.07, 0.02, zt + 0.35, zt + 0.6), pal.Enamel_Seafoam, closed=True)
    tube(s, lift(circ), sec(-0.14, 0.4, zt + 0.95, zt + 1.1), pal.Enamel_Ivory, closed=True)


def crown(s):
    """Glazed stem, observation disc (landing lights, deck rail), ribbed glowing lantern, cap, beacon mast."""
    zc = top_of(NT) + 1.1
    with s.at((*TC, 0.0)):
        s.lathe([(3.2, zc - 0.2), (3.2, zc + 1.2)], pal.Window_Tower, seg=32, cap_bottom=False, cap_top=False)
        zl = zc + 1.2
        lens(s, zl, 3.2, 7.2, 1.8, pal.Hull_Ivory, seg=48)
        zd = zl + 1.8
        s.lathe([(3.21, zd), (0.0, zd)], pal.Hull_Ivory, seg=32, cap_bottom=False, cap_top=False)
        for k in range(16):
            a = 2 * math.pi * (k + 0.5) / 16
            with s.at((6.55 * math.cos(a), 6.55 * math.sin(a), zl + 1.56)):
                s.lathe([(0.0, 0.0), (0.1, 0.0), (0.1, 0.06), (0.0, 0.15)], pal.Signal_Red if k % 4 == 0 else
                        pal.Signal_Amber, seg=6)
            with s.at((5.9 * math.cos(a), 5.9 * math.sin(a), zl + 1.6)):
                s.lathe([(0.02, 0.0), (0.02, 1.0)], pal.Chrome, seg=6, cap_bottom=False, cap_top=False)
        for z, rr in ((zl + 2.6, 0.035), (zl + 2.1, 0.015)):
            rail_ring(s, (0.0, 0.0), 5.9, z, rr, pal.Chrome, n=40)
        s.lathe([(2.1, zd), (2.1, zd + 3.4)], pal.Lamp, seg=24, cap_bottom=False, cap_top=False)
        for k in range(16):
            with s.at((0, 0, 0), (0, 0, 22.5 * k)):
                s.box((2.05, -0.05, zd), (2.3, 0.05, zd + 3.4), pal.Chrome)
        for z in (zd, zd + 3.1):
            s.lathe([(2.1, z), (2.35, z + 0.08), (2.35, z + 0.22), (2.1, z + 0.3)], pal.Chrome, seg=24,
                    cap_bottom=False, cap_top=False)
        s.lathe([(2.1, zd + 3.4), (2.6, zd + 3.5), (2.55, zd + 3.72), (1.4, zd + 4.25), (0.6, zd + 4.45),
                 (0.0, zd + 4.5)],
                pal.Enamel_Ivory, seg=32, cap_bottom=False)
        spire(s, zd + 4.4, 13.0, 0.5)
        s.lathe([(0.0, zd + 15.8), (0.14, zd + 15.9), (0.14, zd + 16.15), (0.0, zd + 16.3)], pal.Signal_Red, seg=8)
        for a, h in ((30, 3.2), (150, 2.4), (270, 2.8)):
            mast(s, 1.3 * math.cos(math.radians(a)), 1.3 * math.sin(math.radians(a)), zd + 4.2, h, beacon=a == 30)


def door_canopy(s):
    """Gold disc canopy over the corner door on two gold columns, lamp domes, chrome rim; revolving door under it."""
    c = on_diag(6.3)
    with s.at((*c, 0.0)):
        s.lathe([(0.0, 3.72), (2.75, 3.74), (2.95, 3.84), (2.95, 3.98), (2.8, 4.1), (0.0, 4.14)], pal.Gold, seg=40)
        s.lathe([(2.97, 3.86), (2.97, 3.96)], pal.Chrome, seg=40, cap_bottom=False, cap_top=False)
    lamps(s, [on_diag(6.3 + 1.9 * math.cos(math.radians(a)), 1.9 * math.sin(math.radians(a)))
              for a in range(0, 360, 45)]
          + [on_diag(6.3)], 3.72)
    for t in (-1.9, 1.9):
        with s.at((*on_diag(7.9, t), 0.0)):
            s.lathe([(0.0, 0.0), (0.28, 0.0), (0.28, 0.12), (0.18, 0.2), (0.15, 3.5), (0.26, 3.62), (0.26, 3.72),
                     (0.0, 3.72)], pal.Gold, seg=16)
    with s.at((*on_diag(TR - 0.3), 0.0)):
        s.lathe([(1.1, 0.45), (1.1, 2.9)], pal.Glass, seg=24, cap_bottom=False, cap_top=False)
        s.lathe([(0.0, 2.9), (1.18, 2.9), (1.18, 3.3), (0.0, 3.3)], pal.Chrome, seg=24)
        s.lathe([(0.0, 0.3), (1.18, 0.3), (1.18, 0.45), (0.0, 0.45)], pal.Chrome, seg=24)
        for a in (0, 90):
            with s.at((0, 0, 0), (0, 0, a + 45)):
                s.box((-1.08, -0.03, 0.45), (1.08, 0.03, 2.9), pal.Gold)


def blade(s):
    """Vertical blade sign on the diagonal, topping out under the observation disc: ivory slab with a round nose, amber
    chaser lights, teal panels in chrome frames with raised neon letters on both faces (each reading left to right),
    a star-and-orbit on a chrome stem over its top."""
    z0, z1 = 4.1, top_of(NT) - 1.8
    pz1 = 38.4
    pz0 = pz1 - 14.1
    with s.at((*on_diag(TR), 0.0), (0, 0, 225)):  # local x out along the diagonal, y across it
        s.extrude(ccw([(-1.5, -0.24), (1.95, -0.24)] + arc(1.95, 0.0, 0.24, 270, 450, 8)[1:-1]
                      + [(1.95, 0.24), (-1.5, 0.24)]), z0, z1, pal.Enamel_Ivory)
        z = z0 + 0.8
        while z < z1 - 0.5:
            s.box((2.16, -0.05, z), (2.24, 0.05, z + 0.12), pal.Lamp)
            z += 1.1
        for side in (-1, 1):
            with s.at((0.0, 0.24 * side, 0.0), (0, 0, 0 if side < 0 else 180)):
                cx = -side * 1.0
                s.box((cx - 0.85, -0.03, pz0), (cx + 0.85, 0.0, pz1), pal.Enamel_Teal)
                for a, b in (((cx - 0.93, pz0 - 0.08), (cx + 0.93, pz0)), ((cx - 0.93, pz1), (cx + 0.93, pz1 + 0.08)),
                             ((cx - 0.93, pz0), (cx - 0.85, pz1)), ((cx + 0.85, pz0), (cx + 0.93, pz1))):
                    s.box((a[0], -0.07, a[1]), (b[0], 0.0, b[1]), pal.Chrome)
                letters(s, "HOTEL", cx, pz1 - 0.35, 2.4, pal.Neon_Coral, gap=0.35, depth=0.1)
        sx, zs = 1.8, z1 + 1.35  # emblem clear of the top eyebrow (r 5.45) and under the disc soffit
        s.box((sx - 0.08, -0.08, z1), (sx + 0.08, 0.08, zs - 0.3), pal.Chrome)
        with s.at((sx, 0.0, zs), FRONT):
            star = [(0.9 if k % 2 == 0 else 0.32, math.pi * k / 8 + math.pi / 2) for k in range(16)]
            s.extrude(ccw([(r * math.cos(a), r * math.sin(a)) for r, a in star]), -0.09, 0.09, pal.Gold)
        orbit = [(sx + 1.25 * math.cos(t), 0.5 * math.sin(t), zs + 0.35 * math.sin(t))
                 for t in (2 * math.pi * k / 32 for k in range(33))]
        s.sweep(orbit, 0.045, pal.Chrome, seg=6, caps=False)
        with s.at(orbit[5]):
            s.lathe([(0.14 * math.sin(math.pi * k / 6), -0.14 * math.cos(math.pi * k / 6)) for k in range(7)],
                    pal.Chrome, seg=10)


@asset("CornerTower_12", "Facades", bevel=0.0)
def corner_tower(s):
    body = pal.Limestone_Tex
    tower_base(s, body)
    drum(s, body)
    crown(s)
    door_canopy(s)
    blade(s)
