"""Deco Kit plaza family (ref/plaza.png): the limestone civic hall kit and its plaza furniture.

Hall modules face -Y. Pivot = footprint FRONT-centre at z = 0: the facade plane is y = 0, depth runs +Y.
  HallBay            6 x 8 m slice, 11.1 m to the parapet lip; tiles along X (joints at x = +-3).
  HallCornerWing     8 x 8 m cell (x -4..4, y 0..8), quarter-round front-left corner (R 6 m about (2, 6)).
                     Its -Y facade ends at x = +4 (a HallBay continues at x + 7), its -X facade ends at y = 8
                     (a HallBay rotated -90 deg about Z continues). Symmetric about its diagonal: for the
                     right-hand corner mirror X (scale -1), or rotate +90 deg and place at (+4, +4).
  HallEntranceTower  12 x 8 m, 21 m; its entrance floor and landing sit at z = 1.1, landing out to y = -2.
  GrandStair         10 x 3 m, pivot = bottom-nosing front-centre, rises 1.1 m toward +Y (at the tower: y = -5).
Plaza pieces pivot at their footprint centre: PlazaFountain, CafeSet, PlanterBed, PlanterHex, PlanterArc."""

import math

from geo import arc, dome, fillet, ring, rrect, stadium
from kit import asset
from mats import pal, register
from plaza_parts import (TROUGH, Z_HEAD, Z_PLINTH, Z_ROOF, Z_SILL, Z_SOIL, armillary, ball, banner, bead, courses,
                         curved_glazing, glazing, globe, hedge, joints, lift, offset, parasol)
from structures import along, arch, arch_band, box2, sec, soft_extrude, tube, xz

register("DK_Water", "glass", "#6FB7C4")
register("DK_Fabric_Banner", "fabric", "#04897C")  # banner slab edge/back field: the comet sheet's teal
register("DK_Limestone_Joint", "stone", "#C8B899")
register("DK_Spray", "glass", "#EEF9FA")
register("DK_Lobby", "enamel", "#4A3826")
register("DK_GroundCover", "stone", "#6FA866")  # PlanterArc ground cover (park / median family, a shade deeper), dE2000 36.7 from guard

FRONT = (90, 0, 0)  # local XY -> world XZ, extrusion -> world -Y
SIDE = (90, 0, 90)  # local X -> world Y, local Y -> world Z, extrusion -> world X


# ----------------------------------------------------------------- HallBay

@asset("HallBay", "Plaza", bevel=0.008)
def hall_bay(s):
    hw, wx, D = 3.0, 1.5, 8.0
    for x0, x1 in ((-hw, -wx), (wx, hw)):
        s.box((x0, 0, 0.5), (x1, D, Z_ROOF), pal.Limestone_Tex)
    s.box((-wx, 0, 0.5), (wx, D, Z_SILL), pal.Limestone_Tex)
    s.box((-wx, 0, Z_HEAD), (wx, D, Z_ROOF), pal.Limestone_Tex)
    s.box((-wx, 0.76, Z_SILL), (wx, 1.6, Z_HEAD), pal.Enamel_Charcoal)
    s.box((-wx, 1.6, Z_SILL), (wx, D, Z_HEAD), pal.Limestone_Tex)
    glazing(s, -wx, wx, 0.36, Z_SILL, Z_HEAD)
    for x0, x1 in ((-hw + 0.4, -wx), (wx, hw - 0.4)):
        joints(s, x0, x1, Z_PLINTH, 8.9, 0.0, block=1.1)
    for x0, x1 in ((-hw, -hw + 0.4), (hw - 0.4, hw)):  # half pilasters: a full one forms on every joint
        s.box((x0, -0.1, Z_PLINTH - 0.05), (x1, 0.1, 8.9), pal.Limestone_Tex)
    courses(s, [(-hw, 0), (hw, 0)])
    hedge(s, [(-2.62 + 0.75 * k, 0.46) for k in range(8)], Z_SOIL)


# ----------------------------------------------------------------- HallCornerWing

CC, CR = (2.0, 6.0), 6.0


def corner_path(n=28):
    """-X facade (from the back joint) -> quarter round -> -Y facade (to the side joint): building on the left."""
    return [(-4.0, 8.0)] + arc(CC[0], CC[1], CR, 180, 270, n) + [(4.0, 0.0)]


def tier_plan(ro, rf, n=16, ne=5):
    """Set-back quarter-round drum about CC: outer arc ro, ends rounded (rf) into straight back walls, CCW."""
    phi = math.degrees(math.asin(rf / (ro - rf)))

    def q(a):
        return CC[0] + (ro - rf) * math.cos(math.radians(a)), CC[1] + (ro - rf) * math.sin(math.radians(a))
    q0, q1 = q(180 + phi), q(270 - phi)
    return ([CC, (q0[0], CC[1])] + arc(*q0, rf, 90, 180 + phi, ne)[1:] + arc(*CC, ro, 180 + phi, 270 - phi, n)[1:-1]
            + arc(*q1, rf, 270 - phi, 360, ne) + [(CC[0], q1[1])])


@asset("HallCornerWing", "Plaza", bevel=0.008)
def hall_corner_wing(s):
    path = corner_path()
    s.extrude(offset(path, 0.8) + [(4.0, 8.0)], 0.5, Z_ROOF, pal.Limestone_Tex)
    tube(s, lift(path), sec(0, 0.8, 0.5, Z_SILL), pal.Limestone_Tex)
    tube(s, lift(path), sec(0, 0.8, Z_HEAD, Z_ROOF), pal.Limestone_Tex)
    for seg in (path[:2], path[-2:]):  # solid piers on the straight runs; the glazing wraps the round
        tube(s, lift(seg), sec(0, 0.8, Z_SILL, Z_HEAD), pal.Limestone_Tex)
    curved_glazing(s, path[1:-1], Z_SILL, Z_HEAD)
    s.box((3.6, -0.1, Z_PLINTH - 0.05), (4.0, 0.1, 8.9), pal.Limestone_Tex)  # half pilasters at both joints
    s.box((-4.1, 7.6, Z_PLINTH - 0.05), (-3.9, 8.0, 8.9), pal.Limestone_Tex)
    courses(s, path)
    hedge(s, [(p.x, p.y) for p, _ in along(offset(path, 0.46), 0.75, 0.3)], Z_SOIL, seed=1)
    # set-back upper tier: rounded ends, wrap-around ribbon window, teal band, planted trough
    bp = tier_plan(4.6, 1.0)
    Z1, dz = 13.9, 13.85 - 10.25
    s.extrude(offset(bp, 0.5, True), Z_ROOF, 12.62, pal.Enamel_Charcoal)
    s.extrude(offset(bp, 0.45, True), 12.5, Z1, pal.Limestone_Tex)
    tube(s, lift(bp), sec(0, 0.5, Z_ROOF - 0.05, 11.0), pal.Limestone_Tex, closed=True)
    tube(s, lift(bp), sec(0, 0.5, 12.6, Z1), pal.Limestone_Tex, closed=True)
    curved_glazing(s, bp, 11.0, 12.6, a=0.3, step=1.0, dz=9.0, closed=True, interior=False)
    tube(s, lift(bp), sec(-0.12, 0.3, 12.82, 13.28, 0.05), pal.Enamel_Teal, closed=True)
    tube(s, lift(bp), [(a, b + dz) for a, b in TROUGH], pal.Limestone, closed=True)
    tube(s, lift(bp), sec(0.06, 0.86, 10.72 + dz, Z_SOIL + dz), pal.Soil, closed=True)
    hedge(s, [(p.x, p.y) for p, _ in along(offset(bp, 0.46, True), 0.8)], Z_SOIL + dz, seed=2)


# ----------------------------------------------------------------- HallEntranceTower

TW, PX0, PX1, ZP, ZT = 6.0, 4.0, 5.6, 12.9, 15.0  # half-width, pilaster x span, pilaster shaft top, screen top
PR, PS, SU = 1.72, 10.4, 2.2  # portal half-width, arch spring, surround outer half-width
ZL, ZC0, ZC1, YN = 1.1, 6.2, 6.7, 2.2  # entrance floor, canopy soffit / top, niche depth
GC, GR = (0.0, 0.55, 9.7), 1.45  # globe centre, radius


def tower_mass(s):
    for sx in (-1, 1):
        box2(s, (sx * PR, 0, 0), (sx * TW, 8, ZT), pal.Limestone_Tex)
    with s.at((0, 0, 0), FRONT):  # head over the round-headed portal
        s.extrude([(PR, PS), (PR, ZT), (-PR, ZT), (-PR, PS)] + arc(0, PS, PR, 180, 0, 24)[1:-1], -8.0, 0.0,
                  pal.Limestone_Tex)
    s.box((-PR, YN, 0), (PR, 8, PS + PR), pal.Limestone_Tex)
    s.box((-PR, 0, 0), (PR, YN, ZL), pal.Limestone)
    s.box((-PR, 0, ZC0), (PR, YN, ZC1), pal.Limestone)
    tube(s, [(-5.0, 0, 0), (5.0, 0, 0)], sec(-2.0, 0.0, 0.0, ZL, 0.05), pal.Limestone_Tex)  # entrance landing
    for hw, y0, y1, z0, z1 in ((4.6, 0.6, 7.4, 14.6, 17.0), (3.2, 1.2, 6.8, 16.6, 18.9), (1.9, 1.8, 6.2, 18.5, 20.3)):
        s.rbox((-hw, y0, z0), (hw, y1, z1), 0.5, pal.Limestone_Tex)
    for sx in (-1, 1):
        x0, x1 = sorted((sx * SU, sx * PX0))
        joints(s, x0, x1, ZL, ZT - 0.05, 0.0)
        x0, x1 = sorted((sx * PX1, sx * TW))
        joints(s, x0, x1, 0.0, ZT - 0.05, 0.0)
    joints(s, -SU, SU, PS + SU + 0.1, ZT - 0.05, 0.0, block=1.7)


def tower_portal(s):
    with s.at((0, 0, 0), FRONT):
        soft_extrude(s, arch_band(PR, PR + 0.33, PS, ZL, n=24), 0.0, 0.16, pal.Enamel_Teal, 0.05)
        soft_extrude(s, arch_band(PR + 0.33, SU, PS, ZL, n=24), 0.0, 0.07, pal.Enamel_Seafoam, 0.03)
        s.extrude(arch_band(PR - 0.04, PR, PS, ZL, n=24), -YN, 0.0, pal.Enamel_Teal)  # reveal lining
    for r in (PR + 0.03, PR + 0.31):
        s.sweep(xz(arch(2 * r, PS, ZL, n=24), -0.17), 0.03, pal.Gold, seg=10)
    # niche glazing behind the globe: dark glass over a dark interior, gold grid
    head = arch(2 * PR, PS, ZC1, n=24)
    s.face([(x, YN - 0.01, z) for x, z in head], pal.Enamel_Charcoal)
    s.face([(x, YN - 0.12, z) for x, z in head], pal.GlassDark)
    s.sweep(xz(arc(0, PS, PR - 0.06, 0, 180, 24), YN - 0.14), 0.05, pal.Gold, seg=10)
    for x in (-0.55, 0.55):
        s.box((x - 0.05, YN - 0.18, ZC1), (x + 0.05, YN - 0.1, PS + math.sqrt(PR * PR - x * x) - 0.05), pal.Gold)
    for z in (ZC1 + 2.0, PS):
        s.box((-PR, YN - 0.18, z - 0.04), (PR, YN - 0.1, z + 0.04), pal.Gold)
    # fins rising from the arch head through the stepped crown
    for x, w, z0, z1, yf in ((0.0, 0.27, PS + PR + 0.25, 21.0, -0.42), (-0.62, 0.17, PS + PR + 0.1, 19.6, -0.3),
                             (0.62, 0.17, PS + PR + 0.1, 19.6, -0.3), (-1.95, 0.15, PS - 0.2, 17.6, -0.22),
                             (1.95, 0.15, PS - 0.2, 17.6, -0.22)):
        with s.at((x, 0, 0), FRONT):
            soft_extrude(s, fillet([(w, z0), (w, z1), (-w, z1), (-w, z0)], w * 0.98, n=6), -2.0, -yf,
                         pal.Enamel_Teal, 0.05)
        for ex in (-w + 0.03, w - 0.03):
            s.sweep([(x + ex, yf + 0.01, z0 + w), (x + ex, yf + 0.01, z1 - w)], 0.022, pal.Gold, seg=10)


def tower_pilasters(s):
    for sx in (-1, 1):
        def bx(a, b, mat, r=0.0):
            box2(s, (sx * a[0], a[1], a[2]), (sx * b[0], b[1], b[2]), mat, r)
        bx((PX0 - 0.12, -1.02, 0.0), (PX1 + 0.12, 0.2, 1.5), pal.Limestone, 0.05)
        bx((PX0, -0.9, 1.4), (PX1, 0.1, ZP), pal.Enamel_Seafoam, 0.08)
        bx((PX0 + 0.26, -0.95, 1.9), (PX1 - 0.26, -0.8, ZP - 0.4), pal.Enamel_Mint, 0.03)
        for x, y in ((PX0 + 0.025, -0.877), (PX1 - 0.025, -0.877), (PX0 + 0.27, -0.94), (PX1 - 0.27, -0.94)):
            s.sweep([(sx * x, y, 1.5), (sx * x, y, ZP - 0.05)], 0.024, pal.Gold, seg=10)
        bx((PX0 - 0.14, -1.06, ZP), (PX1 + 0.14, 0.26, ZP + 0.35), pal.Enamel_Teal, 0.06)
        bx((PX0 + 0.12, -0.84, ZP + 0.35), (PX1 - 0.12, 0.04, ZP + 0.7), pal.Enamel_Seafoam, 0.06)
        cx = sx * (PX0 + PX1) / 2
        with s.at((cx, -0.4, ZP + 0.7)):
            s.lathe([(0, 0), (0.34, 0), (0.34, 0.06), (0.2, 0.16), (0.14, 0.26), (0, 0.26)], pal.Chrome, seg=32)
        ball(s, (cx, -0.4, ZP + 1.52), 0.58, pal.Chrome, seg=40)
        x0, x1 = sorted((sx * (PX0 + 0.15), sx * (PX1 - 0.15)))
        banner(s, x0, x1, -0.98, 11.6, 7.0)


def canopy_plan(hw=3.8, d=2.4, p=3.0, n=28):
    """Marquee plan: half superellipse (bowed front, soft corners) closed by the facade line, CCW."""
    pts = []
    for k in range(1, n):
        c, sn = math.cos(math.pi * (1 + k / n)), math.sin(math.pi * (1 + k / n))
        pts.append((hw * math.copysign(abs(c) ** (2 / p), c), -d * abs(sn) ** (2 / p)))
    return [(-hw, 0.05)] + pts + [(hw, 0.05)]


def tower_canopy(s):
    path = canopy_plan()
    s.extrude(path, ZC0, ZC0 + 0.1, pal.Enamel_Ivory)
    zf = ZC1 - 0.18  # fascia top
    tube(s, lift(path), sec(-0.07, 0.3, ZC0 + 0.02, zf, 0.05), pal.Enamel_Teal)
    tube(s, lift(path), sec(-0.11, 0.36, zf, ZC1, 0.08), pal.Enamel_Mint)
    s.extrude(path, zf - 0.07, ZC1 - 0.03, pal.Enamel_Seafoam)
    for a, b in ((-0.08, zf), (-0.08, ZC0 + 0.04)):
        bead(s, path, a, b, 0.028, pal.Gold)
    for x in (-1.5, 1.5):
        with s.at((x, -1.5, ZC0)):
            bulb = [(0.18 * math.cos(t), -0.03 - 0.12 * math.sin(t)) for t in (math.pi / 2 * k / 5 for k in range(1, 6))]
            s.lathe([(0.2, 0.0), (0.2, -0.03)] + bulb, pal.Lamp, seg=24)
    for x in (-3.1, 3.1):
        with s.at((x, -0.6, ZC1 - 0.03)):
            s.lathe([(0, 0), (0.12, 0), (0.12, 0.06), (0.05, 0.12), (0.045, 0.8), (0.08, 0.86), (0, 0.9)], pal.Chrome,
                    seg=24)
        ball(s, (x, -0.6, ZC1 + 1.08), 0.24, pal.Chrome, seg=32)


def tower_doors(s):
    y = 0.5
    s.face([(-PR, YN - 0.02, ZL), (PR, YN - 0.02, ZL), (PR, YN - 0.02, ZC0), (-PR, YN - 0.02, ZC0)], pal.Lobby)
    s.face([(-PR, y, ZL), (PR, y, ZL), (PR, y, ZC0), (-PR, y, ZC0)], pal.Glass)
    for x in (-PR + 0.06, -0.85, 0.0, 0.85, PR - 0.06):
        s.box((x - 0.06, y - 0.06, ZL), (x + 0.06, y + 0.04, ZC0), pal.Gold)
    for z in (ZL + 0.05, ZC0 - 1.25, ZC0 - 0.06):
        s.box((-PR, y - 0.06, z - 0.05), (PR, y + 0.04, z + 0.05), pal.Gold)
    s.box((-0.85, y - 0.04, ZL), (0.85, y + 0.02, ZL + 0.32), pal.Gold)  # kick plates
    for x in (-0.16, 0.16):
        bar = [(x, y - 0.03, 2.2), (x, y - 0.14, 2.2), (x, y - 0.14, 3.1), (x, y - 0.03, 3.1)]
        s.sweep(bar, 0.022, pal.Gold, seg=10)


def tower_globe(s):
    cx, cy, cz = GC
    s.rbox((-0.95, -0.35, ZC1 - 0.05), (0.95, 1.45, ZC1 + 0.3), 0.08, pal.Limestone)
    zb = ZC1 + 0.3
    with s.at((cx, cy, zb)):
        s.lathe([(0, 0), (0.75, 0), (0.75, 0.06), (0.62, 0.14), (0.3, 0.2), (0.16, 0.28), (0, 0.3)], pal.Chrome, seg=40)
    s.sweep([(cx, cy, zb + 0.2), (cx, cy, cz - GR - 0.2)], 0.1, pal.Chrome, seg=16)
    for ang, sx in ((236, -1), (304, 1)):
        end = (cx + (GR + 0.2) * math.cos(math.radians(ang)), cy, cz + (GR + 0.2) * math.sin(math.radians(ang)))
        s.sweep([(cx + 0.5 * sx, cy, zb + 0.08), end], 0.06, pal.Chrome, seg=12)
    with s.at(GC, (90, 0, 0)):
        s.lathe(ring(GR + 0.14, GR + 0.24, -0.075, 0.075), pal.Chrome, seg=72, cap_bottom=False, cap_top=False)
    with s.at(GC, (0, 23.5, 0)):
        globe(s, GR)
        for z0, z1 in ((GR - 0.02, GR + 0.16), (-GR - 0.16, -GR + 0.02)):
            s.lathe([(0, z0), (0.05, z0), (0.05, z1), (0, z1)], pal.Chrome, seg=12)


@asset("HallEntranceTower", "Plaza", bevel=0.01)
def hall_entrance_tower(s):
    tower_mass(s)
    tower_portal(s)
    tower_pilasters(s)
    tower_canopy(s)
    tower_doors(s)
    tower_globe(s)


# ----------------------------------------------------------------- GrandStair

NS, SH, SG, SW = 7, 1.1 / 7, 3.0 / 7, 4.5  # risers, rise, going, flight half-width


def pitch(y):
    return SH + y * SH / SG


@asset("GrandStair", "Plaza", bevel=0.01)
def grand_stair(s):
    pts = [(0.0, 0.0)]
    for k in range(NS):
        pts += [(k * SG, (k + 1) * SH), ((k + 1) * SG, (k + 1) * SH)]
    pts.append((NS * SG, 0.0))
    with s.at((0, 0, 0), SIDE):
        s.extrude(fillet(pts[::-1], 0.05, n=3), -SW, SW, pal.Limestone_Tex, smooth_sides=True)
    for sx in (-1, 1):  # cheek walls with a bullnosed coping, newel block at the foot
        x = sx * (SW + 0.25)
        wall = [(-0.35, 0.0), (3.0, 0.0), (3.0, pitch(3.0) + 0.5), (0.2, pitch(0.2) + 0.5), (-0.35, pitch(0.2) + 0.5)]
        with s.at((x, 0, 0), SIDE):
            s.extrude(fillet(wall, 0.05, n=2), -0.25, 0.25, pal.Limestone_Tex)
        top = fillet([(x, -0.35, pitch(0.2) + 0.5), (x, 0.2, pitch(0.2) + 0.5), (x, 3.0, pitch(3.0) + 0.5)], 0.3, n=5,
                     closed=False)
        tube(s, top, sec(-0.31, 0.31, -0.03, 0.1, 0.06), pal.Limestone)
        box2(s, (x - 0.36, -0.55, 0.0), (x + 0.36, 0.25, 0.98), pal.Limestone, 0.08)
    for x in (-2.25, 2.25):  # chrome handrails on posts, goose-necked onto the plaza and the landing
        rail = fillet([(x, -0.3, 0.0), (x, -0.3, pitch(-0.3) + 0.9), (x, 2.8, pitch(2.8) + 0.9),
                       (x, 3.3, pitch(2.8) + 0.9), (x, 3.3, NS * SH)], 0.2, n=6, closed=False)
        s.sweep(rail, 0.032, pal.Chrome, seg=12)
        for y, zb in ((-0.3, 0.0), (SG * 2.5, 3 * SH), (SG * 4.5, 5 * SH), (3.3, NS * SH)):
            with s.at((x, y, zb)):
                s.lathe([(0, 0), (0.08, 0), (0.08, 0.02), (0.045, 0.05), (0, 0.06)], pal.Chrome, seg=16)
            if 0 < y < 3:
                s.sweep([(x, y, zb), (x, y, pitch(y) + 0.9)], 0.022, pal.Chrome, seg=10)


# ----------------------------------------------------------------- PlazaFountain

@asset("PlazaFountain", "Plaza", bevel=0.008)
def plaza_fountain(s):
    L, W, RR = 7.0, 4.0, 1.1

    def rr(i):
        return rrect(L - 2 * i, W - 2 * i, RR - i, n=8)
    prof = [(0.02, 0.0), (0.0, 0.03), (0.0, 0.34), (0.02, 0.41), (0.07, 0.455), (0.15, 0.47), (0.23, 0.455),
            (0.28, 0.41), (0.3, 0.36)]
    s.loft([(rr(i), z) for i, z in prof], pal.Limestone, cap_bottom=False, cap_top=False)
    s.loft([(rr(0.3), 0.36), (rr(0.3), 0.08)], pal.Tile_Teal, cap_bottom=False, cap_top=False)
    s.extrude(rr(0.3), 0.05, 0.08, pal.Tile_Blue, bottom=False)
    s.face([(x, y, 0.33) for x, y in rr(0.29)], pal.Water)
    px = 1.7  # stepped plinth carrying the armillary sphere
    with s.at((px, 0, 0)):
        soft_extrude(s, rrect(1.9, 1.9, 0.25, n=5), 0.06, 0.6, pal.Limestone, 0.05)
        soft_extrude(s, rrect(1.4, 1.4, 0.2, n=5), 0.6, 0.85, pal.Limestone, 0.05)
        s.lathe([(0, 0.85), (0.52, 0.85), (0.52, 0.9), (0.44, 0.96), (0.18, 1.0), (0.09, 1.06), (0.07, 1.16),
                 (0.14, 1.22), (0.2, 1.26), (0, 1.27)], pal.Chrome, seg=40)
    armillary(s, (px, 0, 2.3), 1.05)
    with s.at((-1.9, 0, 0)):  # single jet
        s.lathe([(0, 0.08), (0.12, 0.08), (0.12, 0.3), (0.08, 0.36), (0.04, 0.38), (0, 0.38)], pal.Chrome, seg=20)
        s.lathe([(0.1, 0.36), (0.065, 0.6), (0.045, 1.0), (0.035, 1.3), (0.07, 1.4), (0.05, 1.5), (0, 1.53)], pal.Spray,
                seg=16)
        s.lathe([(0.1, 0.33), (0.4, 0.335), (0.26, 0.43), (0.1, 0.38)], pal.Spray, seg=24, cap_bottom=False,
                cap_top=False)


# ----------------------------------------------------------------- CafeSet

def chair(s):
    """Bistro chair: seat centre over the origin, facing -Y (back toward +Y)."""
    s.lathe([(0, 0.44), (0.2, 0.44), (0.212, 0.448), (0.218, 0.462), (0.212, 0.476), (0.2, 0.484), (0, 0.49)],
            pal.Enamel_Teal, seg=20)
    s.lathe(ring(0.19, 0.214, 0.425, 0.446), pal.Chrome, seg=20, cap_bottom=False, cap_top=False)
    for ax, ay in ((1, 1), (1, -1), (-1, 1), (-1, -1)):
        s.sweep([(ax * 0.14, ay * 0.14, 0.43), (ax * 0.2, ay * 0.2, 0.0)], 0.011, pal.Chrome, seg=10, caps=False)
    s.sweep([(0.178 * math.cos(t), 0.178 * math.sin(t), 0.18) for t in (2 * math.pi * k / 16 for k in range(17))],
            0.008, pal.Chrome, seg=10, caps=False)
    back = fillet([(-0.15, 0.13, 0.44), (-0.17, 0.2, 0.88), (0.17, 0.2, 0.88), (0.15, 0.13, 0.44)], 0.08, n=4,
                  closed=False)
    s.sweep(back, 0.012, pal.Chrome, seg=10, caps=False)
    with s.at((0, 0.2, 0.75), (-10, 0, 0)):
        s.rbox((-0.16, -0.016, -0.075), (0.16, 0.016, 0.075), 0.012, pal.Enamel_Teal)


@asset("CafeSet", "Plaza", bevel=0.004)
def cafe_set(s):
    parasol(s)
    s.lathe([(0, 2.34), (0.05, 2.34), (0.05, 2.47), (0.03, 2.49)] + dome(0.03, 2.49, 0.06, n=4)[1:], pal.Chrome, seg=16)
    s.lathe([(0.02, 0.1), (0.02, 2.36)], pal.Chrome, seg=12, cap_bottom=False, cap_top=False)
    s.lathe(ring(0.02, 0.045, 1.72, 1.8), pal.Chrome, seg=16, cap_bottom=False, cap_top=False)
    for i in range(8):  # stretchers under the canopy
        a = 2 * math.pi * i / 8
        s.sweep([(0.04 * math.cos(a), 0.04 * math.sin(a), 1.77), (0.62 * math.cos(a), 0.62 * math.sin(a), 2.18)],
                0.008, pal.Chrome, seg=10, caps=False)
    s.lathe([(0, 0.72), (0.43, 0.72), (0.45, 0.735), (0.45, 0.755), (0.43, 0.77), (0, 0.77)], pal.Enamel_Ivory, seg=40)
    s.lathe(ring(0.44, 0.458, 0.728, 0.762), pal.Chrome, seg=40, cap_bottom=False, cap_top=False)
    s.lathe([(0, 0), (0.3, 0), (0.3, 0.02), (0.24, 0.045), (0.08, 0.08), (0.045, 0.12), (0.04, 0.7), (0.12, 0.72),
             (0, 0.72)], pal.Chrome, seg=32)
    for k in range(4):
        a = 45 + 90 * k
        with s.at((0.78 * math.cos(math.radians(a)), 0.78 * math.sin(math.radians(a)), 0), (0, 0, a - 90)):
            chair(s)


# ----------------------------------------------------------------- planters

@asset("PlanterBed", "Plaza", bevel=0.008)
def planter_bed(s):
    """4 x 1 x 0.6 m, rounded ends; soil 5 cm under the rim over a 3.7 x 0.7 m opening (takes a 2 x 0.6 FlowerBed)."""
    def st(i):
        return stadium(4.0 - 2 * i, 1.0 - 2 * i, n=12)
    prof = [(0.05, 0.0), (0.03, 0.03), (0.03, 0.49), (0.0, 0.515), (0.0, 0.575), (0.02, 0.6), (0.13, 0.6),
            (0.15, 0.58), (0.15, 0.46)]
    s.loft([(st(i), z) for i, z in prof], pal.Limestone, cap_bottom=False, cap_top=False)
    s.extrude(st(0.152), 0.45, 0.55, pal.Soil, bottom=False)


def hexa(rf):
    """Hexagon, across flats 2 * rf, a flat facing -Y, CCW."""
    rc = rf / math.cos(math.pi / 6)
    return [(rc * math.cos(math.radians(60 * k)), rc * math.sin(math.radians(60 * k))) for k in range(6)]


@asset("PlanterHex", "Plaza", bevel=0.01)
def planter_hex(s):
    """Hexagonal planter 1.8 m across flats, 0.56 m; centre left free for a lamp standard."""
    prof = [(0.04, 0.0), (0.02, 0.02), (0.02, 0.09), (0.05, 0.12), (0.05, 0.4), (-0.01, 0.43), (-0.01, 0.54),
            (0.01, 0.56), (0.17, 0.56), (0.19, 0.54), (0.19, 0.44)]
    s.loft([(hexa(0.9 - i), z) for i, z in prof], pal.Limestone, cap_bottom=False, cap_top=False)
    s.extrude(hexa(0.9 - 0.192), 0.4, 0.5, pal.Soil, bottom=False)


# ----------------------------------------------------------------- the fountain garden's curved beds (brief 007 round 3)

ARC_R, ARC_HW, ARC_HALF, ARC_H = 6.21, 0.85, 13.75, 0.52   # civic-block fountain-garden-*: r 5.36..7.06 about the fountain
ARC_C = 6.12                                               # the fountain's centre on local +Y from the pivot


def _arc_pt(r, a):
    return (r * math.cos(math.radians(a)), ARC_C + r * math.sin(math.radians(a)))


def arc_outline(hw, n_arc=20, n_cap=8):
    """CCW outline of a band of half-width hw on the arc (radius ARC_R about (0, ARC_C), +-ARC_HALF deg about -Y) with
    round (stadium) ends: outer arc, right cap, inner arc, left cap. Same point count for any hw (lofts)."""
    a0, a1 = -90.0 - ARC_HALF, -90.0 + ARC_HALF
    cap = lambda ac, af: [(_arc_pt(ARC_R, ac)[0] + hw * math.cos(math.radians(af + 180.0 * k / n_cap)),   # noqa: E731
                           _arc_pt(ARC_R, ac)[1] + hw * math.sin(math.radians(af + 180.0 * k / n_cap))) for k in range(1, n_cap)]
    return ([_arc_pt(ARC_R + hw, a0 + (a1 - a0) * k / n_arc) for k in range(n_arc + 1)] + cap(a1, a1)
            + [_arc_pt(ARC_R - hw, a1 + (a0 - a1) * k / n_arc) for k in range(n_arc + 1)] + cap(a0, a0 + 180.0))


def arc_fill(s, hw, z, mat, n_arc=20, n_cap=8):
    """The band's top face as quads across it and a fan in each cap (no concave n-gon for the importer)."""
    pts = [(x, y, z) for x, y in arc_outline(hw, n_arc, n_cap)]
    m = n_arc + 1
    outer, rcap, inner, lcap = pts[:m], pts[m:m + n_cap - 1], pts[m + n_cap - 1:2 * m + n_cap - 1], pts[2 * m + n_cap - 1:]
    inner = inner[::-1]                                       # now runs a0 -> a1 like outer
    for k in range(n_arc):
        s.face([outer[k], outer[k + 1], inner[k + 1], inner[k]], mat)
    for ring_, ac, a, b in ((rcap, -90.0 + ARC_HALF, outer[-1], inner[-1]), (lcap, -90.0 - ARC_HALF, inner[0], outer[0])):
        c = (*_arc_pt(ARC_R, ac), z)
        seq = [a] + ring_ + [b]
        for k in range(len(seq) - 1):
            s.face([c, seq[k], seq[k + 1]], mat)


@asset("PlanterArc", "Plaza", bevel=0.0)
def planter_arc(s):
    """The fountain garden's curved bed (Latch's civic-square review, Splice round 3: no kit piece fitted the four
    fountain-garden-* planters; they are 1.7 m bands on r 6.21 about the AtomFountain, round-ended, 0.52 m, not discs).
    Limestone kick plinth and wall, a teal faience band, a gold bead, an overhanging limestone coping; soil, clipped boxwood
    balls and coral flower clumps along the arc. 4.65 x 1.88 x 0.52 m, the coping lip on the footprint (planting to ~1.0).
    Pivot = footprint (bbox) centre on the ground; the convex side faces -Y (front, the path) and the fountain's centre lies
    6.12 m along local +Y. The four beds are one piece turned 0 / 90 / 180 / 270 deg about the fountain."""
    import random
    from furnish_parts import mound
    from plants import Cards
    rng = random.Random(7307)
    cd = Cards(s)
    bands = (
        ((0.05, 0.0), (0.05, 0.06)), pal.Limestone,                          # kick plinth
        ((0.02, 0.06), (0.02, 0.32)), pal.Limestone_Tex,                     # wall
        ((0.035, 0.32), (0.035, 0.42)), pal.Faience_Teal,                    # faience band
        ((0.035, 0.42), (0.055, 0.435), (0.035, 0.45)), pal.Accent_Gold,     # bead
        ((0.06, 0.45), (0.07, 0.47), (0.07, 0.505), (0.05, 0.52), (-0.14, 0.52), (-0.16, 0.5), (-0.16, 0.42)), pal.Limestone,  # coping
    )
    for prof, mat in zip(bands[::2], bands[1::2]):
        s.loft([(arc_outline(ARC_HW - 0.07 + off), z) for off, z in prof], mat, smooth=True, cap_bottom=False, cap_top=False)  # coping lip = the footprint
    arc_fill(s, ARC_HW - 0.23, 0.43, pal.GroundCover)                     # low ground cover, not bare soil: the bed reads planted
    # planting: seven clipped boxwood balls on the centreline, coral flower clumps between and to the sides
    for k in range(7):
        a = math.radians(-90.0 - ARC_HALF + 2 * ARC_HALF * k / 6)
        c = (ARC_R * math.cos(a), ARC_C + ARC_R * math.sin(a), 0.72)
        with s.at(c):                                                     # solid clipped core: gaps read as shade, not soil
            s.lathe([(0.0, -0.26)] + dome(0.26, 0.0, -0.26, 2)[::-1][1:] + dome(0.26, 0.0, 0.26, 2)[1:], pal.Leaf_CoreShrub, seg=8)
        mound(cd, rng, c, 0.3, pal.Leaf_Shrub, 0.2, per=14, up=0.2)
    for k in range(6):
        a = math.radians(-90.0 - ARC_HALF + 2 * ARC_HALF * (k + 0.5) / 6)
        for dr in (-0.36, 0.36):
            r = ARC_R + dr
            mound(cd, rng, (r * math.cos(a), ARC_C + r * math.sin(a), 0.55), 0.17, pal.Flower_Coral, 0.17, per=4, fill=False, up=0.7)
