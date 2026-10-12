"""Deco Kit facades family: corner, run-end and variety frontage that mixes into skyline.py's runs.

Frontage convention (skyline.py / plaza.py): face -Y, pivot = footprint front-centre at z = 0 (the walk surface), facade
plane y = 0, depth +Y, no bevel. 6 m bays tile along X with joints at x = +-3 and share skyline's ground floor (plinth,
shopfront, fascia, 1.6 m canopy at 3.85 m), floor lines (G 5.0 + k * 3.4), eyebrows and parapets; every bay keeps its
trims inside x = +-3 so a run can bend round an R >= 35 m arc. Corners use HallCornerWing's cell logic: the -Y facade
ends at the +X cell edge (the next bay continues half a bay beyond), the -X facade at the back edge (a bay rotated -90
about Z continues); each is symmetric about its diagonal, so mirror X for the right-hand corner.
  StreamCorner_6   StreamCorner_9's 8 x 8 m quarter round at 6 floors; pairs with StreamBay_6 (next bay at x + 7).
  DecoCorner_10    8 x 8 m, a 4.2 m 45 deg chamfer between wrapping pylons: double-height portal, bowed marquee,
                   stepped crown and beacon spire; pairs with DecoBay_10 (next bay at x + 7, -X one at y + 11).
  DecoEnd_10       a DecoBay_10 run's last bay: its 6 m front and a 14 m pier-and-spandrel return on -X with a square
                   corner pier (mirror X for +X); the return side keeps its canopy (it faces the side street).
  CornerTower_12   (facades_tower.py) 10 x 10 m junction landmark.
  PortholeBay_7    6 x 14 m: rows of chrome-rimmed round portholes in white render, a teal fin on the +X joint.
  BalconyBay_8     6 x 14 m: half-round cantilever balconies (chrome rails, planters) alternating side by floor over
                   glass doors; ribbons, eyebrows and sills run on at both joints.
  ArcadeBay_3      6 x 14 m Googie arcade: tapered coral columns under a boomerang canopy, starbursts, mustard
                   spandrels, folded-plate roof over a clerestory.
"""

import math

from geo import arc
from kit import asset
from mats import pal, register
from plaza import canopy_plan, corner_path
from plaza_parts import bead, glazing, hedge, lift, ribbon
from skyline import (BACK_Y, D, FH, G, GLASS_Y, HW, SP, floor_trims, ground_trims, mullions, ribbon_floors,
                     shopfront, top_of, wrap_block)
from structures import ccw, sec, tube
from facades_parts import (band, canopy, deco_column, fascia, lamps, mast, moff, parapet_trims, penthouse, plinth,
                           porthole, rail_ring, roof_deck, roof_plant, run, soft, sputnik, starburst, stepped_pier,
                           sunburst, trim, water_tank, zigzag_plate)

register("DK_Neon_Coral", "emit", "#FF7A62", 5.0)  # raised sign letters

ST = [(-HW, 0.0), (HW, 0.0)]
R2 = math.sqrt(2.0)


# ----------------------------------------------------------------- StreamCorner_6

def stream_corner(n, body="Limestone_Tex", roof=True):
    """StreamCorner_9's quarter round at n floors: corner fin with teal bands and a mast, roof plant, penthouse, tank
    (not when `roof` is False: a podium deck is laid over it)."""
    def build(s):
        wrap_block(s, corner_path(n=16), (4.0, 8.0), n, getattr(pal, body))
        zt = top_of(n)
        with s.at((2.0 - 6.0 * math.sqrt(0.5), 6.0 - 6.0 * math.sqrt(0.5), 0.0), (0, 0, -45)):
            s.rbox((-0.25, -1.3, G), (0.25, 0.6, zt + 6.0), 0.2, pal.Enamel_Ivory)
            for k in range(3):
                z = zt + 2.6 + 1.0 * k
                s.box((-0.27, -1.32, z), (0.27, 0.4, z + 0.32), pal.Enamel_Teal)
            mast(s, 0.0, -0.4, zt + 6.0, 3.2)
        if roof:
            roof_plant(s, 1.6, 5.4, zt + 0.95, rot=90)
            penthouse(s, -3.5, -1.5, 5.6, 7.7, zt + 0.95)
            water_tank(s, 3.0, 7.0, zt + 0.95)
    return build


asset("StreamCorner_6", "Facades", bevel=0.0)(stream_corner(6))


# ----------------------------------------------------------------- DecoCorner_10

CH = 3.0  # chamfer leg: the 45 deg face runs (-4, 3) -> (-1, 0), 4.24 m
DP = [(-4.0, 8.0), (-4.0, CH), (CH - 4.0, 0.0), (4.0, 0.0)]
CL = CH * R2           # chamfer length
PA, PB = 0.6, CL - 0.6  # portal span along the chamfer, between the pylons
MIRROR = dict(loc=(-4.0, 4.0, 0.0), rot=(0, 0, 90), scale=(1, -1, 1))  # reflect about the diagonal y = x + 4


def cplan(a, notch=0.0):
    """Corner plan set back a (faces and chamfer), optionally notched to `notch` deep between the pylons (the portal
    vestibule), closed at the back corner."""
    pts = moff(DP, a)
    if notch:
        ux, uy = 1 / R2, -1 / R2

        def c(x, y):
            return (-4.0 + x * ux - y * uy, CH + x * uy + y * ux)
        pts = pts[:2] + [c(PA, a), c(PA, notch), c(PB, notch), c(PB, a)] + pts[2:]
    return pts + [(4.0, 8.0)]


def tier(d, e):
    """Crown tier plan: the chamfered corner set back d, reaching e along both faces."""
    return moff([(-4.0, e), (-4.0, CH), (CH - 4.0, 0.0), (e - 4.0, 0.0)], d) + [(e - 4.0, e)]


def deco_corner_half(s, n, zt):
    """The -Y face from the chamfer edge to the joint, with its wrapping pylon (mirrored for the -X face)."""
    body = pal.Limestone_Tex
    glazing(s, -0.4, 3.6, GLASS_Y, 0.45, 4.0, nv=3, dz=2.75)
    s.box((3.6, 0.0, 0.45), (4.0, 8.0, 4.0), body)
    fascia(s, [(-0.4, 0.0), (4.0, 0.0)], pal.Enamel_Teal)
    ribbon(s, [(-0.4, 1.2), (4.0, 1.2)], G, G + FH, pal.Enamel_Charcoal)
    for x0, x1 in ((-0.4, 1.35), (1.95, 3.7)):
        deco_column(s, x0, x1, range(n))
    s.box((3.7, -0.25, G), (4.0, 1.35, zt + 2.2), body)
    s.box((1.35, -0.4, G), (1.95, 1.35, zt + 3.4), body)
    s.box((1.45, -0.3, zt + 3.4), (1.85, 1.0, zt + 4.6), body)
    q = [(CH - 4.0 - PA / R2, PA / R2), (CH - 4.0, 0.0), (-0.4, 0.0)]
    s.extrude(band(trim(q, -0.06), -0.46, 1.35), 0.0, 0.5, pal.Stone_Dark)
    stepped_pier(s, q, 0.5, [(-0.4, 1.35, 0.0, zt + 4.0), (-0.25, 0.5, 0.15, zt + 5.6), (-0.1, 0.35, 0.3, zt + 6.6)],
                 body)
    with s.at((-0.78, -0.12, zt + 6.6)):
        s.lathe([(0.0, 0.0), (0.12, 0.0), (0.12, 0.2), (0.05, 0.28), (0.0, 0.34)], pal.Signal_Red, seg=8)
    s.box((-0.88, -0.44, G + 0.4), (-0.68, -0.4, zt + 3.4), pal.Enamel_Teal)  # flute inlays, both pylon faces
    s.box((-0.92, -0.46, zt + 3.4), (-0.64, -0.4, zt + 3.6), pal.Gold)
    with run(s, (-4.0, CH), (CH - 4.0, 0.0)):
        s.box((3.926, -0.44, G + 0.4), (4.126, -0.4, zt + 3.4), pal.Enamel_Teal)
        s.box((3.886, -0.46, zt + 3.4), (4.166, -0.4, zt + 3.6), pal.Gold)
        s.box((PB, 1.2, 0.45), (PB + 0.3, 2.7, 4.0), body)  # vestibule side wall under the slab


def deco_portal(s):
    """Double-height glazed screen at the back of the vestibule: gold grid, doors, teal jamb linings, lintel with a gold
    sunburst over it."""
    zh = G + FH
    s.face([(PA, 2.58, 0.45), (PB, 2.58, 0.45), (PB, 2.58, zh), (PA, 2.58, zh)], pal.Lobby)
    y = 0.9
    s.face([(PA, y, 0.45), (PB, y, 0.45), (PB, y, zh), (PA, y, zh)], pal.Glass)
    for x in (PA + 0.05, PA + (PB - PA) / 3, PA + 2 * (PB - PA) / 3, PB - 0.05):
        s.box((x - 0.05, y - 0.07, 0.45), (x + 0.05, y + 0.02, zh), pal.Gold)
    for z in (0.5, 3.0, 5.7, zh - 0.06):
        s.box((PA, y - 0.07, z - 0.05), (PB, y + 0.02, z + 0.05), pal.Gold)
    xm = CL / 2
    s.box((xm - 0.03, y - 0.1, 0.45), (xm + 0.03, y - 0.04, 3.0), pal.Gold)
    for sx in (-1, 1):  # kick plates and push bars
        x0, x1 = sorted((xm, xm + sx * 0.95))
        s.box((x0, y - 0.05, 0.45), (x1, y - 0.01, 0.8), pal.Gold)
        s.sweep([(xm + sx * 0.2, y - 0.02, 1.4), (xm + sx * 0.2, y - 0.14, 1.4), (xm + sx * 0.2, y - 0.14, 2.3),
                 (xm + sx * 0.2, y - 0.02, 2.3)], 0.022, pal.Chrome, seg=8)
    for x0, x1 in ((PA, PA + 0.05), (PB - 0.05, PB)):
        s.box((x0, -0.4, 0.45), (x1, 0.9, zh), pal.Enamel_Teal)
    for x in (PA + 0.02, PB - 0.02):
        s.sweep([(x, -0.41, 0.5), (x, -0.41, zh)], 0.03, pal.Gold, seg=8)
    s.box((PA, -0.1, zh), (PB, 1.3, zh + SP), pal.Limestone_Tex)
    s.box((PA, -0.14, zh - 0.02), (PB, -0.1, zh + 0.1), pal.Gold)
    sunburst(s, xm, -0.1, zh + 0.1, 0.9)


def deco_marquee(s):
    """Bowed marquee off the chamfer (thicker than the run canopy, which dies into it): teal and mint fascia, gold
    beads, lamp domes."""
    mq = canopy_plan(hw=3.0, d=2.9, n=20)
    front = mq[1:-1]
    s.extrude(mq, 3.8, 4.12, pal.Enamel_Ivory)
    tube(s, lift(front), sec(-0.07, 0.3, 3.78, 4.0), pal.Enamel_Teal)
    tube(s, lift(front), sec(-0.1, 0.34, 4.0, 4.16), pal.Enamel_Mint)
    for z in (3.8, 4.0):
        bead(s, front, -0.08, z, 0.025, pal.Gold)
    lamps(s, [(x, y * 0.7 - 0.2) for x, y in front[3:-3:3]], 3.8)


def deco_crown(s, zt):
    """Stepped tiers over the chamfer with fluted faces and gold bands, a beacon spire with antenna whips."""
    body = pal.Limestone_Tex
    z0 = zt + 0.95
    for d, e, z1 in ((0.3, 5.4, zt + 3.2), (0.9, 4.9, zt + 5.6), (1.5, 4.4, zt + 7.8)):
        s.extrude(tier(d, e), z0, z1, body)
        s.extrude(tier(d - 0.03, e + 0.03), z1 - 0.35, z1 - 0.15, pal.Gold)
        a, b = moff([(-4.0, CH), (CH - 4.0, 0.0)], d)
        with run(s, a, b) as L:
            for k in range(1, 6):
                x = L * k / 6
                s.box((x - 0.07, -0.12, z0), (x + 0.07, 0.0, z1 - 0.35), body)
        z0 = z1
    c = (-1.0, 3.0)
    with s.at((*c, z0)):
        s.lathe([(0.9, 0.0), (0.9, 0.7), (0.7, 0.9), (0.45, 0.9)], body, seg=24, cap_bottom=False)
        s.lathe([(0.92, 0.4), (0.92, 0.55)], pal.Gold, seg=24, cap_bottom=False, cap_top=False)
        s.lathe([(0.45, 0.9), (0.32, 3.0), (0.16, 7.5), (0.04, 10.4), (0.0, 10.6)], pal.Chrome, seg=16)
        for z, r in ((1.6, 0.52), (3.4, 0.42), (5.2, 0.33)):
            s.lathe([(r - 0.2, z), (r, z + 0.1), (r, z + 0.3), (r - 0.2, z + 0.4)], pal.Gold, seg=16,
                    cap_bottom=False, cap_top=False)
        s.lathe([(0.0, 7.7), (0.2, 7.8), (0.2, 8.05), (0.0, 8.15)], pal.Signal_Red, seg=8)
        # cantilevered observation gallery round the spire foot: deck, gold edge, posted rail
        s.lathe([(0.45, 0.9), (1.45, 0.9), (1.45, 1.0), (0.45, 1.0)], pal.Steel_Ivory, seg=32, cap_bottom=False,
                cap_top=False)
        s.lathe([(1.46, 0.88), (1.46, 1.02)], pal.Gold, seg=32, cap_bottom=False, cap_top=False)
        s.lathe([(0.45, 0.5), (1.2, 0.88), (0.45, 0.88)], body, seg=32, cap_bottom=False, cap_top=False)
    rail_ring(s, c, 1.38, z0 + 1.95, 0.025, pal.Chrome)
    rail_ring(s, c, 1.38, z0 + 1.5, 0.015, pal.Chrome)
    for k in range(12):
        a = 2 * math.pi * k / 12
        x, y = c[0] + 1.38 * math.cos(a), c[1] + 1.38 * math.sin(a)
        s.box((x - 0.02, y - 0.02, z0 + 1.0), (x + 0.02, y + 0.02, z0 + 1.95), pal.Chrome)
    # floodlight cans on the chamfer set-back ledges, tilted back to wash the tier above
    for d, z in ((0.6, zt + 3.2), (1.2, zt + 5.6)):
        a, b = moff([(-4.0, CH), (CH - 4.0, 0.0)], d)
        with run(s, a, b) as L:
            for f in (0.2, 0.5, 0.8):
                with s.at((L * f, 0.0, z)):
                    s.box((-0.12, -0.1, 0.0), (0.12, 0.1, 0.06), pal.Steel)
                    with s.at((0.0, 0.0, 0.06), (-30, 0, 0)):
                        s.lathe([(0.0, 0.0), (0.1, 0.0), (0.15, 0.24), (0.13, 0.26)], pal.Chrome, seg=10,
                                cap_top=False)
                        s.lathe([(0.13, 0.25), (0.0, 0.25)], pal.Lamp, seg=10, cap_bottom=False, cap_top=False)
    for dx, dy, h in ((0.9, -0.6, 3.4), (-0.6, 0.9, 2.6)):
        mast(s, c[0] + dx, c[1] + dy, zt + 5.6, h, beacon=False)


@asset("DecoCorner_10", "Facades", bevel=0.0)
def deco_corner(s):
    n, body = 10, pal.Limestone_Tex
    zt = top_of(n)
    s.extrude(cplan(2.6), 0.45, G + FH, pal.Lobby)
    s.extrude(cplan(GLASS_Y), 0.3, 0.45, pal.Terrazzo_Ivory)
    s.extrude(cplan(0.0, 2.7), 4.0, G, body)
    s.extrude(cplan(1.3, 2.7), G, G + FH, body)
    s.extrude(cplan(1.3), G + FH, zt, body)
    ribbon(s, moff(DP, 1.2), G + FH, zt, pal.Enamel_Charcoal)
    s.extrude(cplan(0.0), zt, zt + 0.95, body)
    path = soft(DP)
    plinth(s, path)
    canopy(s, path)
    tube(s, lift(path), sec(-0.12, 0.0, zt + 0.3, zt + 0.6), pal.Enamel_Teal)
    deco_corner_half(s, n, zt)
    with s.at(**MIRROR):
        deco_corner_half(s, n, zt)
    with run(s, (-4.0, CH), (CH - 4.0, 0.0)):
        deco_portal(s)
        for x0, x1 in ((PA, CL / 2 - 0.15), (CL / 2 + 0.15, PB)):
            deco_column(s, x0, x1, range(1, n), bare=(1,))
        s.box((CL / 2 - 0.15, -0.25, G + FH + SP), (CL / 2 + 0.15, 1.35, zt + 0.95), body)
    with s.at((-4.0 + CH / 2, CH / 2, 0.0), (0, 0, -45)):
        deco_marquee(s)
    deco_crown(s, zt)
    roof_plant(s, 2.2, 5.6, zt + 0.95, rot=90)


# ----------------------------------------------------------------- DecoEnd_10

EP = [(-HW, D), (-HW, 0.0), (HW, 0.0)]


def end_plan(a):
    return moff(EP, a) + [(HW, D)]


@asset("DecoEnd_10", "Facades", bevel=0.0)
def deco_end(s):
    n, body = 10, pal.Limestone_Tex
    zt = top_of(n)
    s.extrude(end_plan(2.6), 0.45, 4.0, pal.Lobby)
    s.extrude(end_plan(GLASS_Y), 0.3, 0.45, pal.Terrazzo_Ivory)
    s.extrude(end_plan(0.0), 4.0, G, body)
    s.extrude(end_plan(1.3), G, zt, body)
    ribbon(s, moff(EP, 1.2), G, zt, pal.Enamel_Charcoal)
    s.extrude(end_plan(0.0), zt, zt + 0.95, body)
    ground_trims(s, soft(EP), pal.Enamel_Teal)
    tube(s, lift(soft(EP)), sec(-0.12, 0.0, zt + 0.3, zt + 0.6), pal.Enamel_Teal)
    # front: DecoBay_10's elevation, its -X half pier replaced by the corner pier
    glazing(s, -2.7, 2.6, GLASS_Y, 0.45, 4.0, nv=3, dz=2.75)
    s.box((2.6, 0.0, 0.45), (HW, D, 4.0), body)
    for x0, x1 in ((-2.7, -0.3), (0.3, 2.7)):
        deco_column(s, x0, x1, range(n))
    s.box((2.7, -0.25, G), (HW, 1.35, zt + 2.2), body)
    s.box((-0.3, -0.4, G), (0.3, 1.35, zt + 3.4), body)
    s.box((-0.2, -0.3, zt + 3.4), (0.2, 1.0, zt + 4.6), body)
    # return: five 2.2 m columns between 0.6 m piers, shop windows under them
    with run(s, (-HW, D), (-HW, 0.0)):
        for i in range(5):
            x1 = D - 0.3 - 2.8 * i
            deco_column(s, x1 - 2.2, x1, range(n))
            glazing(s, x1 - 2.2, x1, GLASS_Y, 0.45, 4.0, nv=2, dz=2.75)
            if i < 4:
                s.box((x1 - 2.8, -0.25, G), (x1 - 2.2, 1.35, zt + 2.2), body)
                s.box((x1 - 2.8, 0.0, 0.45), (x1 - 2.2, 2.6, 4.0), body)
        s.box((0.0, -0.25, G), (0.3, 1.35, zt + 2.2), body)
        s.box((0.0, 0.0, 0.45), (0.3, 2.6, 4.0), body)
    # square corner pier: granite foot, stepped shaft, teal flutes, gold bands, beacon finial
    s.box((-3.46, -0.46, 0.0), (-2.7, 0.3, 0.5), pal.Stone_Dark)
    for (x0, y0, x1, y1), z0, z1 in (((-3.4, -0.4, -2.7, 0.3), 0.5, zt + 4.0), ((-3.3, -0.3, -2.8, 0.2), zt + 4.0,
                                      zt + 5.4), ((-3.18, -0.18, -2.92, 0.08), zt + 5.4, zt + 6.3)):
        s.box((x0, y0, z0), (x1, y1, z1), body)
        s.box((x0 - 0.02, y0 - 0.02, z1 - 0.3), (x1 + 0.02, y1 + 0.02, z1 - 0.12), pal.Gold)
    s.box((-3.15, -0.44, G + 0.4), (-2.95, -0.4, zt + 3.4), pal.Enamel_Teal)
    s.box((-3.44, -0.15, G + 0.4), (-3.4, 0.05, zt + 3.4), pal.Enamel_Teal)
    with s.at((-3.05, -0.05, zt + 6.3)):
        s.lathe([(0.0, 0.0), (0.1, 0.0), (0.1, 0.18), (0.04, 0.26), (0.0, 0.3)], pal.Signal_Red, seg=8)
    roof_plant(s, 0.6, 7.5, zt + 0.95)
    roof_deck(s, -HW, HW, 0.0, D, zt + 0.95)


# ----------------------------------------------------------------- PortholeBay_7

def skin(s, x0, x1, z0, z1, mat):
    s.face([(x0, 0.0, z0), (x1, 0.0, z0), (x1, 0.0, z1), (x0, 0.0, z1)], mat)


def fin_plan(x0, x1, y0, y1):
    r = (x1 - x0) / 2
    return ccw([(x0, y1), (x0, y0 + r)] + arc(x0 + r, y0 + r, r, 180, 360, 8)[1:-1] + [(x1, y0 + r), (x1, y1)])


def porthole_bay(n, roof=True):
    return lambda s: _porthole_bay(s, n, pal.Render_White, roof)


def _porthole_bay(s, n, body, roof):
    zt = top_of(n)
    shopfront(s, body)
    ground_trims(s, ST, pal.Enamel_Teal)
    floor_trims(s, ST, n, pal.Enamel_Seafoam)
    s.box((-HW, 0.42, G), (HW, D, zt), body)
    for x0, x1 in ((-HW, -HW + 0.06), (HW - 0.06, HW)):  # skin end caps: a run end never shows the cavity
        s.box((x0, 0.0, G), (x1, 0.42, zt), body)
    s.box((-HW, 0.0, zt), (HW, D, zt + 0.95), body)
    xs, r, h = (-1.9, -0.1, 1.7), 0.62, 0.9
    for k in range(n):
        zb = G + k * FH
        zc = zb + SP + h
        for x in xs:
            porthole(s, x, zc, r, h, body)
        skin(s, -HW + 0.06, HW - 0.06, zb, zc - h, body)
        skin(s, -HW + 0.06, HW - 0.06, zc + h, zb + FH, body)
        skin(s, -HW + 0.06, xs[0] - h, zc - h, zc + h, body)
        skin(s, xs[-1] + h, HW - 0.06, zc - h, zc + h, body)
    # teal fin on the +X joint: rounded nose, chrome rings over the parapet, beacon
    s.extrude(fin_plan(2.62, 2.98, -0.95, 0.3), 4.0, zt + 3.2, pal.Enamel_Teal)
    for z in (zt + 1.5, zt + 2.0, zt + 2.5):
        s.extrude(fin_plan(2.6, 2.995, -0.97, 0.3), z, z + 0.18, pal.Chrome)
    with s.at((2.8, -0.5, zt + 3.2)):
        s.lathe([(0.0, 0.0), (0.12, 0.0), (0.12, 0.16), (0.0, 0.26)], pal.Signal_Red, seg=8)
    if roof:
        roof_plant(s, -0.4, 7.6, zt + 0.95)
        roof_deck(s, -HW, HW, 0.0, D, zt + 0.95)


asset("PortholeBay_7", "Facades", bevel=0.0)(porthole_bay(7, roof=False))  # the roof: DecoKitRoofline


# ----------------------------------------------------------------- BalconyBay_8

def doors(s, x0, x1, z0, z1):
    """Four glazed door leaves: gold frames, transom, threshold."""
    for i in range(5):
        x = x0 + (x1 - x0) * i / 4
        s.box((x - 0.035, GLASS_Y - 0.06, z0), (x + 0.035, GLASS_Y + 0.02, z1), pal.Gold)
    for z in (z0, z0 + 2.3):
        s.box((x0, GLASS_Y - 0.06, z), (x1, GLASS_Y + 0.02, z + 0.07), pal.Gold)


def balcony(s, cx, zb, R=1.3):
    """Half-round cantilever slab with a chrome nose band, triple chrome rails on posts, two small planters."""
    s.extrude(ccw(arc(cx, 0.0, R, 180, 360, 12) + [(cx + R, 0.3), (cx - R, 0.3)]), zb - 0.12, zb + 0.18,
              pal.Enamel_Ivory)
    s.extrude(ccw(arc(cx, 0.0, R + 0.02, 180, 360, 12) + [(cx + R + 0.02, 0.3), (cx - R - 0.02, 0.3)]), zb - 0.02,
              zb + 0.06, pal.Chrome)
    zr = zb + 0.18
    rail = arc(cx, 0.0, R - 0.07, 180, 360, 12)
    s.sweep([(x, y, zr + 1.0) for x, y in rail], 0.03, pal.Chrome, seg=8)
    for hh in (0.38, 0.7):
        s.sweep([(x, y, zr + hh) for x, y in rail], 0.014, pal.Chrome, seg=6)
    for x, y in rail[2:-1:2]:
        s.sweep([(x, y, zr), (x, y, zr + 1.0)], 0.016, pal.Chrome, seg=6, caps=False)
    for sx in (-1, 1):
        px, py = cx + sx * 0.8, -0.32
        s.box((px - 0.22, py - 0.14, zr), (px + 0.22, py + 0.14, zr + 0.36), pal.Enamel_Teal)
        s.box((px - 0.19, py - 0.11, zr + 0.36), (px + 0.19, py + 0.11, zr + 0.37), pal.Soil)
        hedge(s, [(px, py)], zr + 0.42, size=0.5, seed=int(zb) + sx)


def balcony_bay(n, roof=True):
    return lambda s: _balcony_bay(s, n, pal.Limestone_Tex, pal.Enamel_Coral, roof)


def _balcony_bay(s, n, body, band_mat, roof):
    zt = top_of(n)
    shopfront(s, body)
    ground_trims(s, ST, pal.Enamel_Teal)
    for k in range(n):
        zb = G + k * FH
        zs, zf = zb + SP, zb + FH
        cx = 0.0 if k == 0 else (-1.4 if k % 2 else 1.4)
        zones = [(-HW, HW, False)] if k == 0 else [(-HW, cx - 1.3, False), (cx - 1.3, cx + 1.3, True),
                                                   (cx + 1.3, HW, False)]
        for x0, x1, door in zones:
            zg = zb + 0.2 if door else zs
            s.box((x0, 0.0, zb), (x1, D, zg), body)
            s.face([(x0, GLASS_Y, zg), (x1, GLASS_Y, zg), (x1, GLASS_Y, zf), (x0, GLASS_Y, zf)], pal.GlassDark)
            s.box((x0, BACK_Y, zg), (x1, BACK_Y + 0.1, zf), pal.Enamel_Charcoal)
            s.box((x0, BACK_Y + 0.1, zg), (x1, D, zf), body)
            if door:
                doors(s, x0, x1, zg, zf)
                continue
            tube(s, lift([(x0, 0.0), (x1, 0.0)]), sec(-0.05, 0.02, zs - 0.22, zs - 0.06, 0.02), band_mat)
            if x1 - x0 > 1.4:
                mullions(s, [(x0, 0.0), (x1, 0.0)], zs, zf, GLASS_Y)
        for x0, x1 in ((-HW, -HW + 0.06), (HW - 0.06, HW)):
            s.box((x0, 0.0, zs), (x1, BACK_Y, zf), body)
        tube(s, lift(ST), sec(-0.45, 0.05, zf - 0.02, zf + 0.1, 0.05), pal.Enamel_Ivory)
        if k:
            balcony(s, cx, zb)
    s.box((-HW, 0.0, zt), (HW, D, zt + 0.95), body)
    parapet_trims(s, ST, n, band_mat)
    if roof:
        roof_plant(s, 0.9, 8.0, zt + 0.95, rot=180)
        roof_deck(s, -HW, HW, 0.0, D, zt + 0.95, flip=True)


asset("BalconyBay_8", "Facades", bevel=0.0)(balcony_bay(8, roof=False))  # the roof: DecoKitRoofline


# ----------------------------------------------------------------- ArcadeBay_3

def boomerang(x):
    return -1.6 - 1.5 * (1 - abs(x) / HW) ** 1.6


def lift_at(x, y, rise=0.45):
    """Upsweep of the boomerang canopy at plan (x, y): none at the facade and at the joints, `rise` at the apex tip."""
    ye = boomerang(x)
    return rise * (-ye - 1.6) / 1.5 * (y / ye)


def clip(pts, lim):
    """3D polyline trimmed to |x| <= lim (end points slid along their segments)."""
    def cut(a, b):
        f = (math.copysign(lim, a[0]) - a[0]) / (b[0] - a[0])
        return tuple(p + (q - p) * f for p, q in zip(a, b))
    pts = list(pts)
    if abs(pts[0][0]) > lim:
        pts[0] = cut(pts[0], pts[1])
    if abs(pts[-1][0]) > lim:
        pts[-1] = cut(pts[-1], pts[-2])
    return pts


def arcade_canopy(s, n=24):
    """Boomerang canopy: 1.6 m out at the joints (meets the run's canopy, same 3.85 - 4.0 slab), sweeping out to a
    3.1 m point and up 0.45 m at the centre; chrome nose, coral top strip, lamp domes."""
    xs = [HW * (2 * i / n - 1) for i in range(n + 1)]
    ye = [boomerang(x) for x in xs]
    lz = [lift_at(x, y) for x, y in zip(xs, ye)]
    v = []
    for x, y, l in zip(xs, ye, lz):
        v += [(x, 0.0, 3.85), (x, y, 3.85 + l), (x, y, 4.0 + l), (x, 0.0, 4.0)]
    f = []
    for i in range(n):
        a, b = 4 * i, 4 * i + 4
        f += [[a + 3, a + 2, b + 2, b + 3], [b, b + 1, a + 1, a], [a + 1, b + 1, b + 2, a + 2], [a, a + 3, b + 3, b]]
    f += [[0, 1, 2, 3], [4 * n + 3, 4 * n + 2, 4 * n + 1, 4 * n]]
    s._add(v, f, pal.Enamel_Ivory, False)
    s.sweep(clip([(x, y, 3.925 + l) for x, y, l in zip(xs, ye, lz)], HW - 0.08), 0.075, pal.Chrome, seg=8)
    tube(s, [(x, y, 4.0 + l) for x, y, l in zip(xs, ye, lz)], sec(0.05, 0.3, 0.0, 0.04), pal.Enamel_Coral)
    pts = [(-2.2, -1.0), (-0.75, -1.6), (0.0, -2.3), (0.75, -1.6), (2.2, -1.0)]
    for x, y in pts:
        lamps(s, [(x, y)], 3.85 + lift_at(x, y))


@asset("ArcadeBay_3", "Facades", bevel=0.0)
def arcade_bay(s):
    n, body = 3, pal.Render_White
    zt = top_of(n)
    shopfront(s, body)
    plinth(s, ST)
    fascia(s, ST, pal.Enamel_Teal)
    arcade_canopy(s)
    for x in (-1.5, 1.5):  # tapered columns: chrome shoe, coral shaft widening upward, gold ring, chrome capital
        zc = 3.85 + lift_at(x, -1.45)
        with s.at((x, -1.45, 0.0)):
            s.lathe([(0.0, 0.0), (0.17, 0.0), (0.17, 0.1), (0.09, 0.16), (0.0, 0.16)], pal.Chrome, seg=16)
            s.lathe([(0.075, 0.14), (0.2, 3.55)], pal.Enamel_Coral, seg=16, cap_bottom=False, cap_top=False)
            s.lathe([(0.105, 1.08), (0.135, 1.11), (0.135, 1.19), (0.11, 1.22)], pal.Gold, seg=16, cap_bottom=False,
                    cap_top=False)
            s.lathe([(0.19, 3.5), (0.3, 3.66), (0.3, zc), (0.0, zc)], pal.Chrome, seg=16, cap_bottom=False)
    starburst(s, (0.0, -0.1, 4.95), 0.85)
    zr = 4.0 + lift_at(0.0, -2.7)
    s.sweep([(0.0, -2.7, zr - 0.05), (0.0, -2.7, zr + 0.9)], 0.03, pal.Chrome, seg=8)
    sputnik(s, (0.0, -2.7, zr + 1.05), 0.42)
    # upper floors: skyline ribbons and eyebrows, mustard spandrel panels with small stars, coral fins
    ribbon_floors(s, n, body)
    floor_trims(s, ST, n, pal.Enamel_Seafoam)
    for k in range(n):
        zb = G + k * FH
        s.box((-HW + 0.06, -0.03, zb + 0.15), (HW - 0.06, 0.0, zb + SP - 0.26), pal.Enamel_Mustard)
        for x in (-2.25, 0.0, 2.25):
            starburst(s, (x, -0.03, zb + 0.47), 0.26, n=8, depth=0.03)
    for x in (-1.5, 1.5):
        with s.at((x, 0.0, 0.0), (90, 0, 90)):
            s.extrude(ccw([(0.0, G), (-0.3, G), (-0.85, zt + 0.6), (0.0, zt + 0.6)]), -0.06, 0.06, pal.Enamel_Coral)
    # folded-plate roof over a clerestory: ridges on the joints and the centre, chrome edge
    zr, zv, t = zt + 2.9, zt + 1.3, 0.16
    zz = [(-HW, zr), (-1.5, zv), (0.0, zr), (1.5, zv), (HW, zr)]
    zigzag_plate(s, zz, -1.1, D - 0.4, t, pal.Enamel_Ivory)
    s.sweep(clip([(x, -1.1, z - t / 2) for x, z in zz], HW - 0.06), 0.05, pal.Chrome, seg=8)
    for (xa, za), (xb, zc) in zip(zz, zz[1:]):
        for y, mat in ((0.2, pal.GlassDark), (1.4, pal.Enamel_Charcoal)):
            s.face([(xa, y, zt + 1.1), (xb, y, zt + 1.1), (xb, y, zc - t), (xa, y, za - t)], mat)
        xm = (xa + xb) / 2
        zm = (za + zc) / 2 - t
        s.box((xm - 0.03, 0.14, zt + 1.1), (xm + 0.03, 0.22, zm), pal.Chrome)
    for x0, x1 in ((-HW, -HW + 0.05), (HW - 0.05, HW)):
        s.box((x0, 0.0, zt + 0.95), (x1, D - 0.4, zr - t - 0.06), body)
    mast(s, 0.0, 9.0, zr - 0.05, 3.0)


import facades_tower  # noqa: E402,F401  (registers CornerTower_12)
