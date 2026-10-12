"""Deco Kit alleys family: the service side of the frontage (6 m alleys cut between building runs; their walls are the
14 m side returns). Engineered, lived-in retro-future: panel seams, rounded ducts, vent banks, pipe and cable runs,
antennae, portholes, chrome-railed fire stairs. Spans / pipes live in alleys_span, props in alleys_props.

Wall modules (skyline.py frame turned into the alley): face -Y, pivot = front-centre at the walk surface, 6 m bays tile
along X. Render face y = -0.02 (2 cm proud of the pivot line), slab back 0.35, fixtures ~0.5 m proud (door canopy
0.87, stair 1.42). Floor lines G + k FH and parapet tops match the frontage partner:
  AlleyWall_6 / _9    StreamBay_6 / _9: seafoam parapet band, ivory coping, top 26.5 / 36.7 m
  AlleyWall_10        DecoBay_10: teal parapet band, top 39.95 m
  AlleyWall_6D        AlleyWall_6 over a loading dock: mustard roller door, roller drum, granite dock, buffers, steps
  AlleyStair_9        AlleyWall_9's frame behind a zig-zag fire stair: cantilever landings per floor, drop ladder,
                      gooseneck roof ladder
Runs that meet their neighbours across joints: conduits z 5.22 / 5.34, cable swag 14.8 (and 28.4 on 8+ floors), top
duct run centred 0.7 under the roof deck, panel seams; the riser duct (x -2.45) and drainpipe (x +2.6) repeat every
6 m. Every run is symmetric, so mirror X for variety.
"""

from alleys_parts import (HW, SKIN, ALONG_X, ac_unit, antennas, band, cable_run, canopy, caged_lamp, catwalk, conduit,
                          door, drainpipe, duct_run, grille, porthole, rails, riser, round_poly, slab, straight,
                          vent_bank, window)
from booths import sub
from geo import circle, dome, fillet
from kit import asset
from mats import DECAL, cell, pal
from plaza_parts import offset
from skyline import FH, G, top_of
from mega_parts import strut
from structures import sec, tube
import alleys_props  # noqa: F401  (registers the props)
import alleys_span  # noqa: F401  (registers the spans and pipe bundle)

XA, XB, XF, XD, XP = -1.35, 0.55, 1.85, -2.45, 2.6  # window columns A / B, fixture column, riser duct, drainpipe
# per floor, cycled: (column A, column B, fixture column). k = 2 is the bridge floor: flush across x -1.4..1.4.
# The fixture column stacks louvre banks up the wall between the AC floors: the building's breathing side.
FLOORS = (("port", ("win", "Blind_Coral", False), "ac"),
          (("win", None, True), "port", "vents"),
          (("win", "Blind_Cream", False), ("win", None, False), "grille"),
          ("port", "bank", "vents"))
CATWALK = 4  # service catwalk on floor 4's slab (18.6 m), continuous along a run


def floor_items(k, zb):
    """(holes, draw calls) for upper floor k at slab level zb."""
    holes, draws = [], []
    for x, it in ((XA, FLOORS[k % 4][0]), (XB, FLOORS[k % 4][1])):
        if it == "port":
            holes.append(("c", x, zb + 1.62, 0.42, pal.Chrome))
            draws.append((porthole, (x, zb + 1.62)))
        elif it == "bank":
            holes.append(("r", x - 0.55, zb + 1.08, x + 0.55, zb + 2.02, pal.Enamel_Charcoal))
            draws.append((vent_bank, (x, zb + 1.08, 1.1, 0.94)))
        else:
            _, blind, hood = it
            holes.append(("r", x - 0.45, zb + 1.0, x + 0.45, zb + 2.25, pal.Enamel_Ivory))
            draws.append((window, (x, zb + 1.0, 0.9, 1.25, blind, hood)))
    fix = FLOORS[k % 4][2]
    if fix == "vents":
        holes.append(("r", XF - 0.33, zb + 0.72, XF + 0.33, zb + 2.52, pal.Enamel_Charcoal))
        draws.append((vent_bank, (XF, zb + 0.72, 0.66, 1.8)))
    else:
        draws.append({"ac": (ac_unit, (XF, zb + 1.55)), "grille": (grille, (XF, zb + 1.62))}[fix])
    return holes, draws


def meter(s, x, z0, z_up=5.22):
    """Ivory meter box with a teal lid, city-utility plate, latch and its conduit up to the cross run."""
    s.rbox((x - 0.24, SKIN - 0.2, z0), (x + 0.24, SKIN + 0.01, z0 + 0.62), 0.03, pal.Enamel_Ivory, seg=1)
    s.rbox((x - 0.26, SKIN - 0.215, z0 + 0.62), (x + 0.26, SKIN + 0.01, z0 + 0.7), 0.03, pal.Enamel_Teal, seg=1)
    y = SKIN - 0.201
    s.decal([(x - 0.17, y, z0 + 0.3), (x + 0.17, y, z0 + 0.3), (x + 0.17, y, z0 + 0.49), (x - 0.17, y, z0 + 0.49)],
            pal.Decal_Transit, sub(DECAL["city_utility"], 14, 49, 242, 178))
    s.box((x + 0.16, SKIN - 0.23, z0 + 0.12), (x + 0.2, SKIN - 0.19, z0 + 0.24), pal.Chrome)
    s.sweep([(x, SKIN - 0.08, z0 + 0.7), (x, SKIN - 0.08, z_up)], 0.02, pal.Chrome, seg=6)


def plinth(s, gaps):
    """Dark granite base course (the frontage's), broken at door openings."""
    xs = [-HW] + [v for g in sorted(gaps) for v in g] + [HW]
    for x0, x1 in zip(xs[::2], xs[1::2]):
        tube(s, straight(0.0, 0.0, x0, x1), sec(SKIN - 0.06, 0.05, 0.0, 0.45, 0.02, n=1), pal.Stone_Dark)


def ground_door(s):
    """Service door under its canopy, caged lamp, barred high window over a louvre, meter box."""
    door(s, -0.9)
    canopy(s, -0.9, 2.62)
    with s.at((0.25, SKIN, 2.15)):
        caged_lamp(s, seg=10)
    window(s, 1.2, 1.9, 0.9, 0.8)
    for k in range(3):
        x = 1.2 - 0.225 + 0.225 * k
        strut(s, (x, SKIN + 0.06, 1.9), (x, SKIN + 0.06, 2.7), 0.026, pal.Chrome)
    grille(s, 1.2, 0.78)
    meter(s, 2.05, 1.05)
    plinth(s, [(-1.52, -0.28)])
    return [("r", -1.5, 0.15, -0.3, 2.5, pal.Enamel_Ivory), ("r", 0.75, 1.9, 1.65, 2.7, pal.Enamel_Ivory)], \
        [("v", -2.0, 0.5, 4.6), ("v", 0.55, 0.5, 4.6), ("h", 3.2, -HW, -2.0), ("h", 3.2, 0.55, HW)]


def ground_dock(s):
    """Loading dock: mustard slatted roller door under its drum, granite dock with hazard nosing and rubber buffers,
    side steps with a chrome rail, dock lamp, meter box."""
    x0, x1, zd, zh = -1.9, 1.1, 0.95, 3.85
    y = SKIN + 0.16
    s.box((x0, y + 0.02, zd), (x1, y + 0.04, zh), pal.Enamel_Charcoal)
    for k in range(12):
        z = zd + 0.02 + (zh - zd - 0.02) * k / 12
        s.box((x0 + 0.04, y - 0.03, z), (x1 - 0.04, y, z + (zh - zd) / 12 - 0.014), pal.Enamel_Mustard)
    s.box((x0 + 0.04, y - 0.05, zd), (x1 - 0.04, y, zd + 0.06), pal.Chrome)
    for xh in (x0 + 0.5, x1 - 0.5):
        s.sweep(fillet([(xh - 0.14, y - 0.03, zd + 0.3), (xh - 0.14, y - 0.1, zd + 0.3), (xh + 0.14, y - 0.1, zd + 0.3),
                        (xh + 0.14, y - 0.03, zd + 0.3)], 0.03, 3, closed=False), 0.016, pal.Chrome, seg=6)
    for xg in (x0, x1 - 0.07):
        s.box((xg, SKIN + 0.02, zd), (xg + 0.07, y + 0.02, zh), pal.Chrome)
    with s.at((x0 - 0.1, SKIN - 0.26, zh + 0.3), ALONG_X):
        s.lathe([(0.3, 0.0), (0.3, x1 - x0 + 0.2)], pal.Enamel_Ivory, seg=20)
        for zz in (0.0, x1 - x0 + 0.2):
            with s.at((0, 0, zz), (0, 180, 0) if zz == 0.0 else (0, 0, 0)):
                s.lathe([(0.31, -0.01), (0.31, 0.03), (0.12, 0.05)] + dome(0.12, 0.05, 0.05, 3)[1:], pal.Enamel_Teal,
                        seg=20)
        for zz in (0.9, 2.3):
            s.lathe([(0.305, zz - 0.04), (0.305, zz + 0.04)], pal.Chrome, seg=20, cap_bottom=False, cap_top=False)
    YD = SKIN - 1.2
    s.box((x0 - 0.2, YD, 0.0), (x1 + 0.2, SKIN + 0.02, zd - 0.05), pal.Stone_Dark)
    s.box((x0 - 0.2, YD - 0.02, zd - 0.05), (x1 + 0.2, SKIN + 0.02, zd), pal.Steel)
    for k in range(10):
        xa = x0 - 0.2 + (x1 - x0 + 0.4) * k / 10
        s.box((xa, YD - 0.03, zd - 0.16), (xa + (x1 - x0 + 0.4) / 10, YD, zd - 0.05),
              pal.Enamel_Mustard if k % 2 else pal.Enamel_Charcoal)
    for xb in (x0 + 0.45, x1 - 0.45):
        s.box((xb - 0.2, YD - 0.02, 0.3), (xb + 0.2, YD, 0.8), pal.Steel)
        s.rbox((xb - 0.15, YD - 0.16, 0.34), (xb + 0.15, YD - 0.01, 0.76), 0.04, pal.Rubber, seg=2)
    y0 = SKIN - 0.95
    s.decal([(-0.6, YD - 0.002, 0.3), (-0.2, YD - 0.002, 0.3), (-0.2, YD - 0.002, 0.7), (-0.6, YD - 0.002, 0.7)],
            pal.Decal_Transit, cell(DECAL["caution"]))
    for k in range(4):  # side steps rising toward the dock
        xs = x1 + 0.2 + 0.24 * (4 - k)
        s.box((x1 + 0.2, y0, 0.0), (xs, SKIN + 0.02, zd * (k + 1) / 4), pal.Stone_Dark)
        s.box((xs - 0.24, y0 - 0.02, zd * (k + 1) / 4 - 0.03), (xs, SKIN + 0.02, zd * (k + 1) / 4), pal.Steel)
    xr = x1 + 1.16
    s.sweep(fillet([(xr, y0 + 0.05, 0.0), (xr, y0 + 0.05, 0.95), (x1 + 0.3, y0 + 0.05, zd + 0.95),
                    (x1 + 0.3, y0 + 0.05, zd)], 0.12, 3, closed=False), 0.022, pal.Chrome, seg=6)
    with s.at((1.72, SKIN, 3.95)):
        caged_lamp(s, pal.Enamel_Mustard, seg=10)
    meter(s, 2.12, 1.45)
    plinth(s, [(x0 - 0.2, 2.2)])
    return [("r", x0, zd, x1, zh, pal.Enamel_Ivory)], [("v", 2.45, 0.5, 4.6)]


def service_wall(n, crown, ground):
    """n floors over the ground floor; crown 'stream' (StreamBay parapet) or 'deco' (DecoBay parapet)."""
    def build(s):
        zt = top_of(n)
        z1 = zt + 0.95
        holes, seams = (ground_dock if ground == "dock" else ground_door)(s)
        draws = []
        for k in range(n):
            h, d = floor_items(k, G + k * FH)
            holes += h
            draws += d
        seams += [("h", G + k * FH + 0.62, -HW, HW) for k in range(n)]
        seams += [("v", x, G + 0.62, zt - 0.2) for x in (-0.45, 1.35)] + [("v", x, 0.5, zt - 0.2) for x in (-HW, HW)]
        slab(s, z1, holes, seams, pal.Limestone_Tex)
        for fn, args in draws:
            fn(s, *args)
        engineering(s, n, zt)
        if crown == "stream":
            band(s, zt + 0.35, zt + 0.6, 0.07, pal.Enamel_Seafoam, 0.03)
            tube(s, straight(), sec(SKIN - 0.14, 0.4, z1, z1 + 0.15, 0.05), pal.Enamel_Ivory)
        else:
            band(s, zt + 0.3, zt + 0.6, 0.12, pal.Enamel_Teal, 0.0)
        head = z1 + (0.15 if crown == "stream" else 0.0)
        riser(s, XD, 0.45, head + 0.45, [G + k * FH + 0.3 for k in range(1, n, 2)] + [3.0, head + 0.3])
        cowl(s, XD, head + 0.45)
        if n >= 9:
            antennas(s, XD, SKIN - 0.18, head + 0.9)
    return build


def engineering(s, n, zt):
    """The runs: teal ground band with its gold bead, floor courses, cross conduits, cable swags, the top duct run, a
    mustard gas riser, and the drainpipe."""
    band(s, G - 0.28, G, 0.07, pal.Enamel_Teal)
    tube(s, straight(), circle(0.022, 8, SKIN - 0.07, G - 0.26), pal.Gold)
    for k in range(1, n):
        band(s, G + k * FH - 0.06, G + k * FH + 0.06, 0.07, pal.Enamel_Ivory, 0.025)
    conduit(s, 5.22, 0.028, pal.Chrome)
    conduit(s, 5.34, 0.022, pal.Enamel_Mustard)
    for x in (-1.5, 1.5):
        s.box((x - 0.03, SKIN - 0.11, 5.16), (x + 0.03, SKIN + 0.01, 5.4), pal.Steel)
    s.rbox((0.3, SKIN - 0.14, 5.08), (0.62, SKIN + 0.01, 5.46), 0.02, pal.Enamel_Ivory, seg=1)  # junction box
    s.box((0.34, SKIN - 0.15, 5.4), (0.58, SKIN - 0.12, 5.44), pal.Chrome)
    for kz in (3, 7):
        if n > kz:
            cable_run(s, G + kz * FH - 0.4)
    duct_run(s, zt - 0.7)
    if n > CATWALK:
        catwalk(s, G + CATWALK * FH)
    x = 1.3
    s.sweep([(x, SKIN - 0.05, 5.34), (x, SKIN - 0.05, zt - 0.9)], 0.03, pal.Enamel_Mustard, seg=8)
    drainpipe(s, XP, zt, [G + k * FH + 0.3 for k in range(0, n, 2)] + [2.0])


def cowl(s, x, z):
    """Mushroom exhaust cowl capping the riser duct above the parapet."""
    with s.at((x, SKIN - 0.18, z)):
        s.lathe([(0.2, -0.05), (0.2, 0.08), (0.44, 0.24), (0.46, 0.3), (0.36, 0.4), (0.0, 0.44)], pal.Enamel_Ivory,
                seg=10)
        s.lathe([(0.445, 0.22), (0.47, 0.26), (0.47, 0.3), (0.445, 0.32)], pal.Chrome, seg=10, cap_bottom=False,
                cap_top=False)


asset("AlleyWall_6", "Alleys", bevel=0.0)(service_wall(6, "stream", "door"))
asset("AlleyWall_9", "Alleys", bevel=0.0)(service_wall(9, "stream", "door"))
asset("AlleyWall_10", "Alleys", bevel=0.0)(service_wall(10, "deco", "door"))
asset("AlleyWall_6D", "Alleys", bevel=0.0)(service_wall(6, "stream", "dock"))


# ----------------------------------------------------------------- fire stair

XL, LD, LI, RUN, NR = 2.45, 1.4, 0.72, 2.1, 14  # landing half-length, depth, inner-lane depth, flight run, risers


def hatch(j):
    """Landing j's open half (-1 left / +1 right) where the flight from below comes up; landing 0 has none."""
    return 0 if j == 0 else (-1 if j % 2 else 1)


def landing(s, j, z):
    h = hatch(j)
    if h == 0:
        pts, rad = [(-XL, SKIN), (-XL, SKIN - LD), (XL, SKIN - LD), (XL, SKIN)], [0, 0.45, 0.45, 0]
    else:
        pts = [(-XL, SKIN), (-XL, SKIN - LI), (-0.05, SKIN - LI), (-0.05, SKIN - LD), (XL, SKIN - LD), (XL, SKIN)]
        rad = [0, 0.25, 0.06, 0.12, 0.45, 0]
        if h > 0:
            pts, rad = [(-x, y) for x, y in pts[::-1]], rad[::-1]
    plan = round_poly(pts, rad, 3)
    s.extrude(plan, z - 0.16, z, pal.Enamel_Ivory)
    s.extrude(offset(plan, -0.014, closed=True), z - 0.12, z - 0.05, pal.Enamel_Teal)
    for xb in (-XL + 0.45, XL - 0.45):
        with s.at((xb, 0, 0), (90, 0, 90)):
            cove = [(SKIN - LD + 0.3 + (LD - 0.3) * (1 - (1 - t) ** 1.6), z - 0.16 - 0.84 * t ** 1.4)
                    for t in (i / 5 for i in range(6))]
            s.extrude([(SKIN, z - 0.16), (SKIN - LD + 0.3, z - 0.16)] + cove[1:], -0.05, 0.05,
                      pal.Enamel_Ivory)
    yo, yi = SKIN - LD + 0.05, SKIN - LI
    for side in (-1, 1):
        xe = side * (XL - 0.05)
        if side == h:
            rails(s, [(xe, SKIN), (xe, yi), (side * 0.1, yi)], z)
        elif j < 8 and side == (-1 if j % 2 == 0 else 1):  # the flight's own rails guard the outer lane
            rails(s, [(xe, SKIN), (xe, yo)], z)
        elif j == 0:
            rails(s, [(xe, SKIN), (xe, yo), (2.14, yo)], z)
            rails(s, [(1.46, yo), (0.1, yo)], z)
        else:
            rails(s, [(xe, SKIN), (xe, yo), (side * 0.1, yo)], z)


def flight(s, j):
    """Flight j from landing j to j + 1 in the outer lane: open steel treads between teal stringers, chrome rails.
    Even flights climb the left half toward +x, odd ones the right half toward -x: the zig-zag."""
    h = -1 if j % 2 == 0 else 1
    d, z0 = -h, G + j * FH
    xt = h * 0.1
    g, r = RUN / (NR - 1), FH / NR

    def X(u):
        return xt - d * (RUN - u)
    yo, yi = SKIN - LD + 0.06, SKIN - LI - 0.04
    for k in range(1, NR):
        xa, xb = sorted((X((k - 1) * g - 0.025), X(k * g)))
        s.box((xa, yo + 0.02, z0 + k * r - 0.035), (xb, yi - 0.02, z0 + k * r), pal.Steel)
    a, b = (X(-g), 0.0, z0), (X(RUN), 0.0, z0 + FH)
    for y in (yo, yi):
        tube(s, [(a[0], y, a[2] + 0.04), (b[0], y, b[2] + 0.04)], sec(-0.018, 0.018, -0.24, 0.0), pal.Enamel_Teal)
        rail = [(a[0] + d * 0.1, y, a[2] + 1.0), (b[0] - d * 0.08, y, b[2] + 1.0)]
        s.sweep(rail, 0.024, pal.Chrome, seg=5)
        if y == yo:
            s.sweep([(p[0], y, p[2] - 0.5) for p in rail], 0.017, pal.Chrome, seg=5)
        for px, _, pz in rail:
            strut(s, (px, y, pz - 0.95), (px, y, pz), 0.035, pal.Chrome)


def ladder(s, x0, x1, y, z0, z1, step=0.3, path=None):
    """Chrome ladder: two stiles (straight, or along `path` offsets) and rungs every `step` from z0 to z1."""
    for x in (x0, x1):
        s.sweep(path(x) if path else [(x, y, z0), (x, y, z1)], 0.022, pal.Chrome, seg=5)
    k = int((z1 - z0 - 0.15) / step)
    for i in range(k):
        z = z0 + 0.15 + step * i
        strut(s, (x0, y, z), (x1, y, z), 0.026, pal.Chrome)


@asset("AlleyStair_9", "Alleys", bevel=0.0)
def alley_stair(s):
    n, zt = 9, top_of(9)
    z1 = zt + 0.95
    holes, seams = ground_door(s)
    for k in range(n):
        zb = G + k * FH
        xp = -1.6 if k % 2 else 1.6
        holes += [("r", -0.525, zb, 0.525, zb + 2.25, pal.Enamel_Ivory), ("c", xp, zb + 1.6, 0.36, pal.Chrome),
                  ("r", -xp - 0.3, zb + 1.2, -xp + 0.3, zb + 2.1, pal.Enamel_Charcoal)]
    seams += [("h", G + k * FH + 2.7, -HW, HW) for k in range(n)] + [("v", x, 0.5, zt - 0.2) for x in (-HW, HW)]
    slab(s, z1, holes, seams, pal.Limestone_Tex)
    for k in range(n):
        zb = G + k * FH
        xp = -1.6 if k % 2 else 1.6
        door(s, 0.0, 0.95, 2.15, zb, pal.Enamel_Coral if k % 3 == 1 else None, bar=False, light=False)
        porthole(s, xp, zb + 1.6, 0.36, seg=12)
        vent_bank(s, -xp, zb + 1.2, 0.6, 0.9)
        landing(s, k, zb)
        if k < n - 1:
            flight(s, k)
    band(s, G - 0.28, G, 0.07, pal.Enamel_Teal)
    tube(s, straight(), circle(0.022, 8, SKIN - 0.07, G - 0.26), pal.Gold)
    conduit(s, 5.22, 0.028, pal.Chrome)
    conduit(s, 5.34, 0.022, pal.Enamel_Mustard)
    for kz in (3, 7):
        cable_run(s, G + kz * FH - 0.4)
    duct_run(s, zt - 0.7)
    band(s, zt + 0.35, zt + 0.6, 0.07, pal.Enamel_Seafoam, 0.03)
    tube(s, straight(), sec(SKIN - 0.14, 0.4, z1, z1 + 0.15, 0.05), pal.Enamel_Ivory)
    drainpipe(s, 2.78, zt, [G + k * FH + 0.6 for k in range(0, n, 2)] + [2.0])
    yl = SKIN - LD - 0.07  # drop ladder hung off landing 0's right end, counterweight on a pivot arm
    ladder(s, 1.55, 2.05, yl, 2.3, G + 1.0)
    for x in (1.55, 2.05):
        s.box((x - 0.03, yl, G - 0.12), (x + 0.03, SKIN - LD + 0.02, G - 0.04), pal.Chrome)
    s.sweep([(2.2, yl, G - 0.08), (2.35, yl - 0.25, G + 0.3)], 0.02, pal.Chrome, seg=6)
    s.rbox((2.25, yl - 0.36, G + 0.22), (2.45, yl - 0.16, G + 0.5), 0.04, pal.Enamel_Mustard, seg=1)

    def goose(x):  # gooseneck over the coping onto the roof deck
        return fillet([(x, SKIN - 0.22, G + 8 * FH), (x, SKIN - 0.22, z1 + 1.2), (x, 0.55, z1 + 1.2), (x, 0.55, zt)],
                      0.45, 5, closed=False)
    ladder(s, -1.9, -1.45, SKIN - 0.22, G + 8 * FH, z1 + 0.6, path=goose)
    for z in (G + 8 * FH + 1.3, G + 8 * FH + 2.6, zt - 0.1):
        for x in (-1.9, -1.45):
            s.box((x - 0.02, SKIN - 0.22, z - 0.02), (x + 0.02, SKIN + 0.01, z + 0.02), pal.Chrome)
