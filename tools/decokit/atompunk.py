"""Atompunk pass (brief 007 C): pieces regenerated for the plan's space-age sites. Front -Y, z = 0 at ground, no bevel.
  SkyRailStation_Twin  plan standards.skyrail sizes: platform / car floor at 24.0 (levels.concourse), TWIN beams on
                       6 m centres (x = -3, +3; beam top 23.0, underside 21.5), side platforms 50 m long outboard of the
                       cars (x 4.7..8.9). The beams are NOT part of the asset: |x| < 4.5 is clear from the ground to the
                       canopy soffit (29.0). Streamline arched canopy with turquoise speed ribs, lit HARBOUR dorsal fin,
                       portholes in the platform fascia, tapered piers, a glazed lift tower with a helical stair and an
                       overbridge to the west platform. Footprint x -16.5..16.5, y -30..30.
Palette: v0.17.0 enamel accents (flamingo, turquoise, sunshine) registered here as DK_Enamel_* roles; no mustard or red
(guard / cones signal colours)."""

import math

from geo import ring, rrect
from kit import asset
from mats import GOLD_TRIM, accent, pal, register
from mega_parts import strut
from skyport_parts import ball, band, bead, flight, helix, lift_tube, pol, word
from structures import superell, tube

register("DK_Enamel_Flamingo", "enamel", accent("flamingo"))
register("DK_Enamel_Sunshine", "enamel", accent("sunshine"))
register("DK_Enamel_Cobalt", "enamel", accent("cobalt"))
register("DK_Enamel_Turquoise", "enamel", accent("turquoise"))
register("DK_Enamel_Mint17", "enamel", accent("mint"))
register("DK_Enamel_Coral17", "enamel", accent("coral"))
register("DK_Gold17", "metal", GOLD_TRIM)  # v0.17 world gold (dE2000 20.3 from guard; style contract gold_trim override)
register("DK_Water", "glass", "#6FB7C4")
register("DK_Spray", "glass", "#EEF9FA")

PT = 24.0                       # platform top = car floor = levels.concourse
BX, PX0, PX1, PL = 3.0, 4.7, 8.9, 25.0   # beam centre, platform inner / outer edge, half length
EAVE, CROWN, CW = PT + 3.7, PT + 5.7, 10.4
OB = PT + 4.4                   # overbridge floor
TX, TY = 13.0, 22.6             # lift tower axis
LINK = (TY - 1.6, TY + 1.6)
YB, XW = 27.7, -6.8             # overbridge axis y, west stair x
NAME = "HARBOUR"


def canopy_z(x):
    return EAVE + (CROWN - EAVE) * (1 - (x / CW) ** 2)


def lamp_post(s, x, y, z, h=3.0):
    with s.at((x, y, z)):
        s.lathe([(0.2, 0.0), (0.2, 0.25), (0.08, 0.4), (0.06, h - 0.3), (0.12, h - 0.2), (0.0, h - 0.15)], pal.Chrome,
                seg=8, cap_bottom=False)
    ball(s, (x, y, z + h + 0.15), 0.3, pal.Lamp, seg=10, n=5)


def porthole(s, x, y, z, sx, r=0.38):
    with s.at((x, y, z), (0, 90 * sx, 0)):
        s.lathe([(r + 0.08, 0.0), (r + 0.08, 0.06), (r, 0.06), (r, 0.0)], pal.Chrome, seg=14, cap_bottom=False)
        s.lathe([(r, 0.01), (0.0, 0.01)], pal.GlassDark, seg=14, cap_bottom=False)


def platform(s, sx, full=True):
    x0, x1 = sorted((sx * PX0, sx * PX1))
    s.box((x0, -PL, PT - 0.35), (x1, PL, PT), pal.Terrazzo_Ivory)
    xe = sx * PX0
    s.box((min(xe, xe + sx * 0.5), -PL, PT), (max(xe, xe + sx * 0.5), PL, PT + 0.012), pal.Enamel_Sunshine)
    s.box((min(xe, xe - sx * 0.06), -PL, PT - 0.4), (max(xe, xe - sx * 0.06), PL, PT + 0.02), pal.Chrome)
    xo = sx * (PX1 + 0.08)
    s.box((min(sx * 2.6, xo), -PL, PT - 1.35), (max(sx * 2.6, xo), PL, PT - 0.35), pal.Enamel_Ivory)
    s.box((min(xo, xo + sx * 0.05), -PL, PT - 0.75), (max(xo, xo + sx * 0.05), PL, PT - 0.6), pal.Enamel_Turquoise)
    for k in range(11):  # porthole row in the outer fascia
        porthole(s, sx * (PX1 + 0.13), -PL + 3.0 + k * 4.4, PT - 0.95, sx, r=0.22)
    yp = (-18.75, -6.25, 6.25, 18.75) if full else ()   # decks-only: the building carries the platform
    xp = sx * 6.8
    if full:
        s.box((min(sx * 5.4, sx * 8.2), -PL + 1.0, PT - 2.5), (max(sx * 5.4, sx * 8.2), PL - 1.0, PT - 1.35), pal.Steel_Ivory)
    for y in yp:
        zs = [1.2, 3.5, 8.0, 13.0, 17.0, 19.8, 21.2, PT - 2.45]
        sc = [(1.3, 1.25), (1.0, 1.0), (0.88, 0.86), (0.82, 0.8), (0.82, 0.8), (0.95, 0.95), (1.3, 1.25), (1.7, 1.55)]
        tube(s, [(xp, y, z) for z in zs], superell(0.8, 1.1, p=3, n=16), pal.Enamel_Ivory, up=(1, 0, 0), scale=sc)
        s.box((xp - 1.5, y - 1.8, 0.0), (xp + 1.5, y + 1.8, 1.3), pal.Limestone_Tex)
        s.box((xp - 1.53, y - 1.83, 0.85), (xp + 1.53, y + 1.83, 1.05), pal.Enamel_Turquoise)
        xf = sorted((xp + sx * 0.7, xp + sx * 1.25))
        s.box((xf[0], y - 0.12, 1.3), (xf[1], y + 0.12, 17.0), pal.Enamel_Turquoise)
    xw, y0, y1 = sx * (PX1 - 0.25), -PL + 0.15, (LINK[0] if sx > 0 else PL - 0.15)
    if not full:
        y1 = y0  # no windscreen or end rails inside a building
    n = round((y1 - y0) / 3.0)  # glazed windscreen, open onto the tower link on the east
    for k in range(n):
        ya, yb = y0 + (y1 - y0) * k / n, y0 + (y1 - y0) * (k + 1) / n
        s.box((xw - 0.02, ya + 0.08, PT + 0.15), (xw + 0.02, yb - 0.08, PT + 2.3), pal.Glass)
        s.box((xw - 0.05, ya - 0.05, PT), (xw + 0.05, ya + 0.05, PT + 2.4), pal.Chrome)
    if full:
        s.box((xw - 0.05, y1 - 0.05, PT), (xw + 0.05, y1 + 0.05, PT + 2.4), pal.Chrome)
        s.box((xw - 0.05, y0, PT + 2.35), (xw + 0.05, y1, PT + 2.45), pal.Chrome)
    for ye in ((-PL + 0.15, PL - 0.15) if full else ()):  # end guard rails (a gap for the west stair)
        spans = ([(PX0 + 0.6, 6.25), (7.35, PX1 - 0.25)] if sx < 0 and ye > 0 else [(PX0 + 0.6, PX1 - 0.25)])
        for a, b in spans:
            xa, xb = sorted((sx * a, sx * b))
            s.box((xa, ye - 0.04, PT + 1.02), (xb, ye + 0.04, PT + 1.1), pal.Chrome)
            for x in (xa, xb, (xa + xb) / 2):
                s.box((x - 0.04, ye - 0.04, PT), (x + 0.04, ye + 0.04, PT + 1.02), pal.Chrome)
    for y in (-16.0, -4.0, 8.0, 16.0):  # benches, coral
        xb = sx * (PX1 - 1.0)
        s.box((xb - 0.3, y - 1.1, PT + 0.42), (xb + 0.3, y + 1.1, PT + 0.5), pal.Enamel_Coral17)
        s.box((xb + sx * 0.25 - 0.04, y - 1.1, PT + 0.5), (xb + sx * 0.25 + 0.04, y + 1.1, PT + 0.95), pal.Enamel_Coral17)
        for dy in (-0.9, 0.9):
            s.box((xb - 0.2, y + dy - 0.05, PT), (xb + 0.2, y + dy + 0.05, PT + 0.42), pal.Chrome)
    for y in (-20.0, -10.0, 0.0, 10.0):
        lamp_post(s, sx * (PX1 - 0.7), y, PT, 3.0)


def canopy(s):
    n = 14
    top = [(CW * (1 - 2 * k / n), canopy_z(CW * (1 - 2 * k / n)) - EAVE) for k in range(n + 1)]
    bot = [(x * 0.97, (canopy_z(x) - EAVE) * 0.9 - 0.35) for x, _ in top[::-1]]
    sec_ = top + bot
    ys = [-24.0, -23.0, -21.0, -18.0, 18.0, 21.0, 23.0, 24.0]
    sc = [(0.55, 0.5), (0.8, 0.75), (0.95, 0.93), (1.0, 1.0), (1.0, 1.0), (0.95, 0.93), (0.8, 0.75), (0.55, 0.5)]
    tube(s, [(0.0, y, EAVE) for y in ys], sec_, pal.Enamel_Ivory, scale=sc)
    for sd in (-1, 1):
        s.sweep([(sd * CW * k0, y, EAVE + 0.02) for y, (k0, _) in zip(ys, sc)], 0.1, pal.Chrome, seg=6)
        for y in (-20.0, -10.0, 0.0, 10.0, 20.0):  # columns on the outer platform edge
            x = sd * (PX1 - 0.6)
            zt = EAVE + (canopy_z(abs(x)) - EAVE) * 0.9 - 0.3
            tube(s, [(x, y, PT), (x, y, PT + 0.4), (x, y, zt - 0.6), (x, y, zt)], superell(0.16, 0.22, p=3, n=10),
                 pal.Enamel_Ivory, up=(1, 0, 0), scale=[(1.4, 1.4), (1.0, 1.0), (1.0, 1.0), (1.8, 1.6)])
            with s.at((x, y, 0.0)):
                s.lathe([(0.3, PT), (0.3, PT + 0.12), (0.0, PT + 0.14)], pal.Chrome, seg=10, cap_bottom=False)
                s.lathe([(0.26, zt - 0.7), (0.26, zt - 0.55)], pal.Gold17, seg=10, cap_bottom=False, cap_top=False)
        s.box((sd * 6.4 - 0.18, -20.0, EAVE + (canopy_z(6.4) - EAVE) * 0.9 - 0.4), (sd * 6.4 + 0.18, 20.0,
              EAVE + (canopy_z(6.4) - EAVE) * 0.9 - 0.34), pal.Lamp)
    for y in range(-20, 21, 5):  # speed ribs over the shell
        k0 = 1.0 if abs(y) < 19 else 0.9
        rib = [(CW * (1 - 2 * k / n) * k0, canopy_z(CW * (1 - 2 * k / n) * k0) + 0.02) for k in range(n + 1)]
        tube(s, [(x, y, z) for x, z in rib], [(0.0, -0.14), (0.55, -0.14), (0.55, 0.14), (0.0, 0.14)],
             pal.Enamel_Turquoise, up=(0, 1, 0))
    fin = [(-16.0, 0.0), (16.0, 0.0), (14.0, 3.6), (-12.0, 3.6)]  # dorsal fin along the crown
    with s.at((0.0, 0.0, CROWN - 0.1), (90, 0, 90)):
        s.extrude(fin, -0.3, 0.3, pal.Enamel_Flamingo)
        s.extrude([(-13.5, 0.3), (13.0, 0.3), (12.0, 2.8), (-11.0, 2.8)], -0.33, 0.33, pal.Enamel_Sunshine)
    for rot, x in (((90, 0, 90), 0.33), ((90, 0, -90), -0.33)):
        with s.at((x, 0.8, CROWN + 1.35), rot):
            word(s, NAME, 1.4, 0.22, 0.0, 0.05, pal.Lamp)
    for sd in (-1, 1):  # hanging name boards facing the cars
        xb = sd * 5.2
        for y in (-12.0, 12.0):
            zb = PT + 3.0
            s.box((xb - 0.06, y - 3.2, zb), (xb + 0.06, y + 3.2, zb + 0.9), pal.Enamel_Turquoise)
            for dy in (-2.6, 2.6):
                s.box((xb - 0.02, y + dy - 0.02, zb + 0.9), (xb + 0.02, y + dy + 0.02,
                      EAVE + (canopy_z(5.2) - EAVE) * 0.9 - 0.35), pal.Chrome)
            with s.at((xb - sd * 0.065, y, zb + 0.45), (90, 0, -90 if sd > 0 else 90)):
                word(s, NAME, 0.5, 0.08, 0.0, 0.02, pal.Lamp)


def tower(s):
    with s.at((TX, TY, 0.0)):
        s.lathe([(3.8, 0.0), (3.8, 0.3), (0.0, 0.3)], pal.Limestone_Tex, seg=32, cap_bottom=False)
        helix(s, 1.75, 3.1, 0.3, PT, 6.0, 120, a0=180.0)
        for k in range(4):
            a = 45 + 90 * k
            strut(s, pol(3.35, a, 0.3), pol(3.35, a, OB + 3.2), 0.24, pal.Enamel_Ivory)
        for z in (6.0, 12.0, 18.0, PT - 0.1, OB):
            s.lathe(ring(3.25, 3.45, z - 0.12, z + 0.12), pal.Steel_Ivory, seg=32, cap_bottom=False, cap_top=False)
        s.lathe([(3.6, OB + 3.2), (3.6, OB + 3.7), (2.4, OB + 4.4), (0.0, OB + 4.5)], pal.Enamel_Ivory, seg=32,
                cap_bottom=False)
        band(s, 3.62, OB + 3.3, OB + 3.55, pal.Enamel_Flamingo, 32)
        ball(s, (0, 0, OB + 4.7), 0.3, pal.Lamp, seg=10, n=5)
    lift_tube(s, TX, TY, 0.3, OB + 3.2, r=1.5, car=None, frame=4.5, back=None)
    with s.at((TX, TY, 0.0)):  # flamingo capsule car parked at the platform
        c, car = 1.25, PT - 0.3
        s.lathe([(0.0, car), (c * 0.8, car), (c, car + 0.4), (c, car + 2.4), (c * 0.7, car + 2.8), (0.0, car + 2.9)],
                pal.Enamel_Flamingo, seg=12)
        band(s, c + 0.02, car + 1.2, car + 2.0, pal.Window_Tower, 12)
    s.box((PX1 - 0.1, LINK[0], PT - 0.35), (TX - 2.9, LINK[1], PT), pal.Terrazzo_Ivory)
    s.box((PX1 - 0.1, LINK[0], PT - 0.8), (TX - 2.9, LINK[1], PT - 0.35), pal.Enamel_Ivory)
    s.box((TX - 1.2, TY + 1.3, OB - 0.35), (TX + 1.2, YB - 1.3, OB), pal.Terrazzo_Ivory)
    P = [(TX + 1.4, YB, OB - 0.3), (XW - 1.6, YB, OB - 0.3)]
    tube(s, P, [(-1.4, 0.0), (1.4, 0.0), (1.4, 2.4), (0.9, 3.1), (-0.9, 3.1), (-1.4, 2.4)], pal.Enamel_Ivory)
    tube(s, P, [(-1.43, 1.2), (1.43, 1.2), (1.43, 2.3), (-1.43, 2.3)], pal.Window_Tower, caps=False)
    tube(s, P, [(-1.45, 0.25), (1.45, 0.25), (1.45, 0.5), (-1.45, 0.5)], pal.Enamel_Turquoise, caps=False)
    for x in (TX + 1.4, XW - 1.6):  # flamingo end frames
        tube(s, [(x - 0.25, YB, OB - 0.3), (x + 0.25, YB, OB - 0.3)],
             [(-1.55, -0.12), (1.55, -0.12), (1.55, 2.45), (1.0, 3.25), (-1.0, 3.25), (-1.55, 2.45)], pal.Enamel_Flamingo)
    tube(s, [(XW, YB, 1.2), (XW, YB, 3.5), (XW, YB, OB - 2.2), (XW, YB, OB - 0.3)], superell(0.75, 0.8, p=3, n=16),
         pal.Enamel_Ivory, up=(1, 0, 0), scale=[(1.35, 1.3), (1.0, 1.0), (0.9, 0.9), (1.5, 1.6)])
    s.box((XW - 1.4, YB - 1.4, 0.0), (XW + 1.4, YB + 1.4, 1.3), pal.Limestone_Tex)
    s.box((XW - 1.43, YB - 1.43, 0.85), (XW + 1.43, YB + 1.43, 1.05), pal.Enamel_Turquoise)
    flight(s, XW - 0.8, XW + 0.8, PL - 7.5, YB - 1.35, PT, OB, 22)  # down onto the west platform


@asset("SkyRailStation_Twin", "Skyport", bevel=0.0)
def sky_rail_station_twin(s):
    for sx in (-1, 1):
        platform(s, sx)
    canopy(s)
    tower(s)


# ================================================================= crown catalogue (grammar section 6)
# Standalone crowns for procedural towers: pivot = base centre on the roof deck, base collar r 2.4 (fits any tier >= 6 m),
# fixed size, scale uniformly 0.8..1.4. Sizes: finial 12, atom 11.5, starburst 11, saucer 14 (D 14), antenna 16, dish 9,
# needle 20 (all m, above the pivot). Concept: AtomicAge/contact_sheet (Exposition Palace atom, Power Spire saucer,
# Monorail Terminal starburst, Grand Hotel finial), Blocks/contact_sheet (machine_crowns antenna + dish, needle_trio).

def collar(s, z1=1.0, mat=None):
    mat = mat or pal.Enamel_Ivory
    s.lathe([(2.5, 0.0), (2.5, z1 * 0.55), (2.2, z1 * 0.7), (2.2, z1)], mat, seg=24, cap_bottom=False)
    band(s, 2.52, z1 * 0.2, z1 * 0.4, pal.Enamel_Turquoise, 24)
    band(s, 2.22, z1 * 0.72, z1 * 0.9, pal.Chrome, 24)


def orbit(s, z, r, tilt, az, rr=0.08, n=36):
    with s.at((0, 0, z), (tilt, 0, az)):
        pts = [(r * math.cos(2 * math.pi * k / n), r * math.sin(2 * math.pi * k / n), 0.0) for k in range(n + 1)]
        s.sweep(pts, rr, pal.Chrome, seg=6, caps=False)
        for a in (40.0, 220.0):
            ball(s, (r * math.cos(math.radians(a)), r * math.sin(math.radians(a)), 0.0), 0.08 * r, pal.Gold17, seg=8, n=4)


@asset("Crown_Needle", "Crowns", bevel=0.0)
def crown_needle(s):
    collar(s)
    s.lathe([(2.0, 1.0), (1.4, 4.0), (0.9, 8.0), (0.45, 13.0), (0.18, 18.0), (0.04, 20.0)], pal.Enamel_Ivory, seg=16,
            cap_bottom=False)
    for z, r in ((4.0, 1.45), (8.0, 0.95), (13.0, 0.5)):
        band(s, r, z - 0.15, z + 0.15, pal.Chrome, 16)
    band(s, 1.5, 2.2, 3.4, pal.Enamel_Sunshine, 16)
    ball(s, (0, 0, 20.1), 0.18, pal.Lamp, seg=8, n=4)


@asset("Crown_Finial", "Crowns", bevel=0.0)
def crown_finial(s):
    collar(s)
    for r0, r1, z0, z1 in ((2.0, 1.7, 1.0, 3.0), (1.6, 1.35, 3.0, 4.8), (1.2, 1.0, 4.8, 6.4)):  # three fluted tiers
        s.lathe([(r0, z0), (r1, z1)], pal.Enamel_Ivory, seg=24, cap_bottom=False, cap_top=False)
        s.lathe([(r1, z1), (r1 - 0.25, z1)], pal.Chrome, seg=24, cap_bottom=False, cap_top=False)
        for k in range(8):
            a = 360 * k / 8
            with s.at(pol(r1 * 0.98, a, z0 + 0.2), (0, 0, a)):
                s.box((-0.07, -0.05, 0.0), (0.07, 0.07, z1 - z0 - 0.4), pal.Enamel_Turquoise)
    s.lathe([(1.0, 6.4), (0.7, 6.9), (0.25, 7.3), (0.0, 7.4)], pal.Enamel_Ivory, seg=20)
    ball(s, (0, 0, 8.0), 0.8, pal.Gold17, seg=16, n=8)
    s.lathe([(0.12, 8.6), (0.03, 12.0), (0.0, 12.0)], pal.Chrome, seg=8, cap_bottom=False)


@asset("Crown_Atom", "Crowns", bevel=0.0)
def crown_atom(s):
    collar(s)
    s.lathe([(1.0, 1.0), (0.6, 2.5), (0.5, 5.0), (0.8, 6.0)], pal.Enamel_Ivory, seg=16, cap_bottom=False)
    zc = 7.6
    ball(s, (0, 0, zc), 1.15, pal.Chrome, seg=20, n=10)
    for tilt, az in ((0.0, 0.0), (62.0, 0.0), (62.0, 120.0), (62.0, 240.0)):
        orbit(s, zc, 3.4 if tilt else 2.9, tilt, az)
    s.lathe([(0.1, zc + 3.9), (0.02, zc + 4.0)], pal.Chrome, seg=6, cap_bottom=False)


@asset("Crown_Starburst", "Crowns", bevel=0.0)
def crown_starburst(s):
    collar(s)
    s.lathe([(0.9, 1.0), (0.45, 3.0), (0.35, 6.0), (0.7, 6.8)], pal.Enamel_Ivory, seg=14, cap_bottom=False)
    zc = 7.6
    ball(s, (0, 0, zc), 0.6, pal.Gold17, seg=14, n=7)
    k = 0
    for tilt in (0.0, 38.0, 76.0, 114.0, 152.0):
        n = 1 if tilt in (0.0, 152.0) else 8
        for j in range(n):
            L = 3.8 if (j + k) % 2 == 0 else 2.5
            with s.at((0, 0, zc), (tilt, 0, 360.0 * j / max(n, 1) + (22.5 if k % 2 else 0.0))):
                s.lathe([(0.16, 0.3), (0.0, L)], pal.Chrome if k % 2 == 0 else pal.Gold17, seg=5, cap_bottom=False)
        k += 1


@asset("Crown_Saucer", "Crowns", bevel=0.0)
def crown_saucer(s):
    collar(s)
    s.lathe([(1.3, 1.0), (1.0, 3.0), (1.0, 5.2)], pal.Enamel_Ivory, seg=20, cap_bottom=False, cap_top=False)
    s.lathe([(0.0, 5.0), (3.0, 5.0), (6.2, 5.5), (7.0, 6.2), (6.4, 6.9), (3.0, 7.3), (0.0, 7.5)], pal.Enamel_Ivory, seg=40)
    band(s, 7.02, 6.0, 6.35, pal.Chrome, 40)
    s.lathe([(6.98, 6.35), (6.7, 6.8)], pal.Window_Tower, seg=40, cap_bottom=False, cap_top=False)
    band(s, 4.2, 5.2, 5.45, pal.Enamel_Turquoise, 40)
    s.lathe([(2.4, 7.3), (1.8, 8.2), (0.5, 8.6), (0.0, 8.6)], pal.Glass, seg=24)
    s.lathe([(0.1, 8.6), (0.02, 14.0)], pal.Chrome, seg=6, cap_bottom=False)
    ball(s, (0, 0, 14.0), 0.2, pal.Lamp, seg=8, n=4)


@asset("Crown_Antenna", "Crowns", bevel=0.0)
def crown_antenna(s):
    collar(s)
    s.lathe([(1.2, 1.0), (0.7, 6.0), (0.5, 9.0)], pal.Enamel_Ivory, seg=12, cap_bottom=False, cap_top=False)
    for z, w in ((5.0, 3.0), (8.5, 2.2), (11.5, 1.5)):  # cross-arms with chrome loops
        s.box((-w, -0.08, z - 0.05), (w, 0.08, z + 0.05), pal.Chrome)
        for sd in (-1, 1):
            with s.at((sd * w, 0, z + 0.55), (90, 0, 0)):
                s.sweep([(0.5 * math.cos(2 * math.pi * k / 14), 0.5 * math.sin(2 * math.pi * k / 14), 0.0)
                         for k in range(15)], 0.045, pal.Enamel_Flamingo, seg=5, caps=False)
    s.sweep([(0, 0, 0.5), (0, 0, 9.0), (0, 0, 16.0)], [0.35, 0.22, 0.04], pal.Steel_Ivory, seg=8)
    band(s, 0.3, 9.0, 9.6, pal.Enamel_Sunshine, 8)
    ball(s, (0, 0, 16.1), 0.2, pal.Lamp, seg=8, n=4)


@asset("Crown_Dish", "Crowns", bevel=0.0)
def crown_dish(s):
    collar(s)
    s.lathe([(1.7, 1.0), (1.3, 3.2)], pal.Enamel_Ivory, seg=20, cap_bottom=False, cap_top=False)
    s.lathe([(1.4, 3.1), (1.6, 3.5), (0.6, 4.0), (0.0, 4.0)], pal.Enamel_Ivory, seg=20)
    with s.at((0, 0, 4.2), (0, 0, 0)):
        with s.at((0, 0, 0), (-38, 0, 0)):
            s.lathe([(0.0, 0.0), (1.8, 0.35), (3.5, 1.3), (3.6, 1.38), (3.3, 1.35), (1.7, 0.55), (0.0, 0.2)],
                    pal.Enamel_Ivory, seg=28, cap_bottom=False, cap_top=False)
            s.lathe([(0.0, 0.2), (0.05, 0.2), (0.04, 3.0), (0.0, 3.0)], pal.Chrome, seg=6, cap_bottom=False)
            ball(s, (0, 0, 3.0), 0.2, pal.Enamel_Flamingo, seg=8, n=4)
    for sd in (-1, 1):
        s.box((sd * 1.1 - 0.06, -0.06, 3.9), (sd * 1.1 + 0.06, 0.06, 5.0), pal.Chrome)


# ================================================================= atom fountain + boarding pavilions (civic block)

def _pol(r, a, z=0.0):
    return (r * math.cos(math.radians(a)), r * math.sin(math.radians(a)), z)


@asset("AtomFountain", "Plaza", bevel=0.0)
def atom_fountain(s):
    """civic-block object atom-fountain: R 3.3 round basin, height 4.2 (kind fountain, civic-block.json). Limestone
    basin with turquoise lip and DK_Water, stepped plinth, chrome atom (nucleus + three orbits) on a fluted stem,
    arcing jets. AtomicAge/exposition_palace (atom over forecourt pools)."""
    register("DK_Water", "glass", "#6FB7C4")
    register("DK_Spray", "glass", "#EEF9FA")
    s.lathe([(3.3, 0.0), (3.3, 0.46), (3.15, 0.52), (3.0, 0.46)], pal.Limestone_Tex, seg=48, cap_bottom=False, cap_top=False)
    s.lathe([(3.31, 0.3), (3.31, 0.5), (3.16, 0.55)], pal.Enamel_Turquoise, seg=48, cap_bottom=False, cap_top=False)
    s.lathe([(3.0, 0.0), (3.0, 0.34), (0.0, 0.34)], pal.Water, seg=48, cap_bottom=False)
    s.lathe([(1.0, 0.3), (1.0, 0.6), (0.75, 0.7), (0.75, 1.0), (0.5, 1.1), (0.4, 1.8)], pal.Limestone_Tex, seg=24,
            cap_bottom=False)
    s.lathe([(0.45, 1.8), (0.3, 2.4)], pal.Enamel_Ivory, seg=16, cap_bottom=False, cap_top=False)
    zc = 3.0
    ball(s, (0, 0, zc), 0.38, pal.Chrome, seg=16, n=8)
    for tilt, az in ((0.0, 0.0), (62.0, 0.0), (62.0, 120.0), (62.0, 240.0)):
        orbit(s, zc, 1.2, tilt, az, rr=0.04, n=28)
    ball(s, (0, 0, zc + 1.28), 0.07, pal.Lamp, seg=6, n=3)
    for k in range(8):  # basin jets
        a = 360 * k / 8
        pts = [_pol(0.5 + 2.4 * t, a, 0.5 + 1.6 * math.sin(math.pi * t) + 0.2 * t) for t in [i / 8 for i in range(9)]]
        s.sweep(pts, [0.035 + 0.01 * math.sin(math.pi * i / 8) for i in range(9)], pal.Spray, seg=5, caps=False)


def _pavilion(s, L, D, H, th=0.28):
    """Streamline boarding canopy: lies along local Y (L) x X (D); underside at H - th, roof top H. Rounded swept
    roof slab (cap radius D/2.2), chrome fascia bead, flamingo speed stripe, lamp strip underneath. No supports (the
    block's pavilion-support structures stand under it)."""
    r = min(D * 0.45, 1.2)
    from structures import soft_extrude
    from geo import rrect
    poly = rrect(D, L, r, n=6)
    soft_extrude(s, poly, H - th, H, pal.Enamel_Ivory, th * 0.45)
    poly2 = rrect(D + 0.06, L + 0.06, r, n=6)
    s.extrude(poly2, H - th * 0.72, H - th * 0.52, pal.Enamel_Flamingo, smooth_sides=True, top=False, bottom=False)
    s.extrude(poly2, H - th - 0.02, H - th + 0.08, pal.Chrome, smooth_sides=True, top=False, bottom=False)
    s.box((-D * 0.12, -L * 0.42, H - th - 0.05), (D * 0.12, L * 0.42, H - th), pal.Lamp)
    n = max(3, int(L // 2.5))
    for k in range(n):  # speed ribs
        y = -L / 2 + L * (k + 0.5) / n
        s.box((-D * 0.38, y - 0.03, H), (D * 0.38, y + 0.03, H + 0.05), pal.Enamel_Turquoise)


@asset("BoardingPavilion_Office", "Plaza", bevel=0.0)
def boarding_office(s):
    """civic-block structure office-boarding-pavilion: 3.83 x 15.0, roof top 4.62 (along Y)."""
    _pavilion(s, 15.0, 3.83, 4.62)


@asset("BoardingPavilion_Square", "Plaza", bevel=0.0)
def boarding_square(s):
    """civic-block structure square-boarding-pavilion: 8.4 x 2.14, roof top 2.587; footprint is long in X, so built
    long along X here (the block polygon is 8.4 wide)."""
    with s.at((0, 0, 0), (0, 0, 90)):
        _pavilion(s, 8.4, 2.14, 2.587, th=0.2)


def _pier(s, w, h):
    s.box((-w / 2, -w / 2, 0.0), (w / 2, w / 2, 0.12), pal.Limestone_Tex)
    k = ((0.12, 1.0), (h * 0.3, 0.8), (h * 0.8, 0.72), (h - 0.1, 0.85))
    tube(s, [(0, 0, z) for z, _ in k], superell(w * 0.45, w * 0.45, p=3, n=16), pal.Enamel_Ivory, up=(1, 0, 0),
         scale=[(f, f) for _, f in k], caps=True)
    band(s, w * 0.4, h - 0.5, h - 0.3, pal.Enamel_Turquoise, 16)
    band(s, w * 0.4, 0.3, 0.45, pal.Chrome, 16)


@asset("PavilionPier_Office", "Plaza", bevel=0.0)
def pier_office(s):
    """civic-block pavilion-support 2.0 x 2.0 x 3.633."""
    _pier(s, 2.0, 3.633)


@asset("PavilionPier_Square", "Plaza", bevel=0.0)
def pier_square(s):
    """civic-block pavilion-support 1.12 x 1.12 x 1.955."""
    _pier(s, 1.12, 1.955)


@asset("SkyRailStation_Decks", "Skyport", bevel=0.0)
def sky_rail_station_decks(s):
    """The twin station's platform decks only (both side platforms, 50 m, top 24.0, x 4.7..8.9 either side of the
    twin line, along Y): terrazzo deck, sunshine edge line, ivory fascia with turquoise stripe and portholes, benches,
    lamp posts. No piers, girder, canopy, windscreen, lift tower or overbridge: the building around it (SpaceHQ drum,
    Skyport Tower frame) is the roof and the structure. Pivot = line centre at z 0; the decks hang at z 22.65..24."""
    for sx in (-1, 1):
        platform(s, sx, full=False)


# LinerDock@liner-berth moved to liner.py (brief 007 round 2: a liner terminal, not a rocket gantry).


# ================================================================= Parade gate block (plan megastructures.parade-galleries.portals[parade-gate])

def _clip_y(poly, lo=None, hi=None):
    """Clip a convex polygon to lo <= y <= hi (Sutherland-Hodgman)."""
    def clip(pts, inside, cut):
        out = []
        for a, b in zip(pts, pts[1:] + pts[:1]):
            ia, ib = inside(a), inside(b)
            if ia:
                out.append(a)
            if ia != ib:
                out.append(cut(a, b))
        return out
    pts = poly
    if lo is not None:
        pts = clip(pts, lambda p: p[1] >= lo, lambda a, b: (a[0] + (b[0] - a[0]) * (lo - a[1]) / (b[1] - a[1]), lo))
    if hi is not None:
        pts = clip(pts, lambda p: p[1] <= hi, lambda a, b: (a[0] + (b[0] - a[0]) * (hi - a[1]) / (b[1] - a[1]), hi))
    return pts


@asset("ParadeGate_Block", "Skyport", bevel=0.0)
def parade_gate_block(s):
    """The galleria's gate block (plan portal parade-gate): 24 (along the line, X) x 40 (across the Parade mall, Y),
    storeys 3-8 carried across the mall between the gate towers' podiums: belly slab 8.23..9.43 (clear 8.05 under, deck
    9.43 - 1.2 - 0.18), body to 33.7, parapet and roof programme to 34.8. Pivot = footprint centre at street z 0.
    Twin running opening 12 x 7.5 (z 21.5..29.0, arched head r 6 on the spring at 23) straight through along X at
    y = 0, lined in chrome with a coral inlay (coral transit). Floors at 9.43 + 3.6 k, the storey-7 floor at 24.0
    (storey 6 stretched). Rounded corners r 4, turquoise speed bands in pairs, ribbon windows set between 1.05 m piers
    on both faces (never across the opening), roof terrace: lap pool, cabanas, planters. BL/rail_through.png."""
    W, D, R = 24.0, 40.0, 4.0
    zb, zd, zr, zp = 8.23, 9.43, 33.7, 34.8
    body = rrect(W - 0.3, D - 0.3, R - 0.15, n=5)
    s.extrude(rrect(W, D, R, n=5), zb, zd, pal.Limestone_Tex, smooth_sides=True)                     # belly slab
    s.extrude(rrect(W + 0.0, D + 0.0, R, n=5), zd - 0.5, zd - 0.2, pal.Gold17, smooth_sides=True, top=False, bottom=False)
    s.extrude(body, zd, 21.5, pal.Limestone_Tex, smooth_sides=True)                                  # below the tunnel
    s.extrude(body, 29.0, zr, pal.Limestone_Tex, smooth_sides=True)                                  # above it
    for lo, hi in ((6.0, None), (None, -6.0)):                                                       # the tunnel zone: two side masses
        s.extrude(_clip_y(body, lo, hi), 21.5, 29.0, pal.Limestone_Tex, smooth_sides=True)
    n = 12
    arc = [(6.0 * math.cos(math.radians(90 + 90 * k / n)), 23.0 + 6.0 * math.sin(math.radians(90 + 90 * k / n))) for k in range(n + 1)]
    with s.at((0, 0, 0), (90, 0, 90)):                                                               # spandrels, extruded along X
        for sx in (1, -1):
            sp = [(0.0, 29.0)] + [(sx * x, z) for x, z in arc[1:]] + [(sx * -6.0, 29.0)]
            sp = sp if sx > 0 else sp[::-1]
            from structures import ccw
            s.extrude(ccw(sp), -W / 2 + 0.15, W / 2 - 0.15, pal.Limestone_Tex)
        for w in (-9.0, -3.0, 3.0, 9.0):
            path = [(5.96 * math.cos(math.radians(180 * k / 16)), 23.0 + 5.96 * math.sin(math.radians(180 * k / 16)), w) for k in range(17)]
            s.sweep(path, 0.07, pal.Chrome, seg=5)
    for xs in (W / 2 - 0.05, -(W / 2 - 0.05)):                                                       # chrome + coral arch on both faces
        with s.at((xs, 0, 0), (90, 0, 90)):
            for r, mat, rad in ((6.12, pal.Chrome, 0.13), (6.55, pal.Enamel_Coral17, 0.1)):
                s.sweep([(r * math.cos(math.radians(180 * k / 20)), 23.0 + r * math.sin(math.radians(180 * k / 20)), 0.0) for k in range(21)], rad, mat, seg=6)
            for x in (-6.12, 6.12):
                s.sweep([(x, 21.5, 0.0), (x, 23.0, 0.0)], 0.13, pal.Chrome, seg=6)
    # speed bands: turquoise pairs, never across the tunnel zone
    for z in (12.0, 15.6, 19.2, 30.0, 32.4):
        s.extrude(rrect(W, D, R, n=5), z, z + 0.35, pal.Enamel_Turquoise, smooth_sides=True, top=False, bottom=False)
        s.extrude(rrect(W, D, R, n=5), z + 0.6, z + 0.7, pal.Chrome, smooth_sides=True, top=False, bottom=False)
    # ribbon windows on both X faces, set between 1.05 m piers, only where the tunnel does not run
    floors = [9.43, 13.03, 16.63, 20.23, 24.0, 27.6, 31.2]
    for xs in (W / 2, -W / 2):
        sg = 1 if xs > 0 else -1
        for zf in floors:
            for z0, z1 in [(zf + 0.9, zf + 2.6)]:
                if z1 > 33.0:
                    continue
                for (ya, yb) in ((-18.4, -7.4), (7.4, 18.4)):
                    s.box((min(xs, xs - 0.02 * sg), ya, z0), (max(xs, xs - 0.02 * sg), yb, z1), pal.Glass)
                    for t in range(int((yb - ya) // 2.2) + 1):          # chrome mullions
                        y = ya + t * (yb - ya) / max(1, int((yb - ya) // 2.2))
                        s.box((min(xs, xs + 0.05 * sg) - (0 if sg > 0 else 0.0), y - 0.03, z0), (max(xs, xs + 0.05 * sg), y + 0.03, z1), pal.Chrome)
                    s.box((min(xs, xs + 0.05 * sg), ya, z1), (max(xs, xs + 0.05 * sg), yb, z1 + 0.06), pal.Chrome)
                    s.box((min(xs, xs + 0.05 * sg), ya, z0 - 0.06), (max(xs, xs + 0.05 * sg), yb, z0), pal.Chrome)
    # parapet (rounded), coping, roof programme
    ring_ = [(x, y, zr) for x, y in rrect(W - 0.3, D - 0.3, R - 0.15, n=5)]
    from structures import sec, tube as _tube
    _tube(s, ring_, sec(0.0, 0.3, 0.0, zp - zr), pal.Limestone_Tex, closed=True)
    _tube(s, [(x, y, zp) for x, y in rrect(W - 0.3, D - 0.3, R - 0.15, n=5)], sec(-0.05, 0.35, 0.0, 0.08), pal.Chrome, closed=True)
    from roofdeck import lounger, parasol, planter, pool
    s.extrude(rrect(W - 0.7, D - 0.7, R - 0.3, n=5), zr, zr + 0.03, pal.Terrazzo_Ivory)
    pool(s, 12.0, 5.0, cx=0.0, cy=0.0, z=zr + 0.03)
    for y in (-11.0, 11.0):
        planter(s, 0.0, y, zr + 0.03, l=14.0, w=1.4)
    for sd in (-1, 1):
        for k in (-1, 1):
            lounger(s, k * 4.0, sd * 4.6, zr + 0.03, yaw=90 if sd > 0 else -90)
        parasol(s, sd * 8.0, -15.0 if sd > 0 else 15.0, zr + 0.03)
    for y in (-16.5, 16.5):  # cabana: rounded roof on four posts
        with s.at((7.0, y, zr + 0.03)):
            for dx in (-1.6, 1.6):
                for dy in (-1.2, 1.2):
                    s.box((dx - 0.05, dy - 0.05, 0.0), (dx + 0.05, dy + 0.05, 2.5), pal.Chrome)
            s.extrude(rrect(4.0, 3.2, 0.8, n=4), 2.5, 2.7, pal.Enamel_Flamingo, smooth_sides=True)
