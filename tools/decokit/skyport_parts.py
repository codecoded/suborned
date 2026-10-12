"""Skyport helpers (skyport.py, skyport_civic.py): aircar landing discs (plated soffit, painted deck, rim lamps, guard
rail), glazed gate lounges, lift tubes, pipe bundles, louvre banks, antennas, stairs and stroke lettering.
Kit frame: metres, Z up, front -Y. Helpers draw in the caller's s.at() frame; `face(a)` turns local -Y outward at
azimuth a (deg), so parts designed facing -Y dress a round core."""

import math

from glyphs import G, layout
from mats import pal, register
from mega_parts import FRONT, strut
from structures import tube

register("DK_Window_Tower", "enamel", "#34434B")  # as skyline.py (register is first-wins)
register("DK_Enamel_Coral", "enamel", "#E07560")  # as skyline.py
register("DK_Hull_Ivory", "enamel", "#F4EFE3")    # as landmarks.py (hull_panels_v1 plating, 8 m)
register("DK_Steel_Ivory", "enamel", "#E4DFD2")   # as landmarks.py
register("DK_Concrete", "stone", "#B5AEA1")       # as ground.py

DIGIT = {"1": [[(0.08, 0.78), (0.34, 1), (0.34, 0)], [(0.06, 0), (0.62, 0)]], "2": G["2"],
         "3": [[(0, 1), (0.6, 1), (0.6, 0), (0, 0)], [(0.2, 0.5), (0.6, 0.5)]]}


def pol(r, a, z=None):
    a = math.radians(a)
    return (r * math.cos(a), r * math.sin(a)) if z is None else (r * math.cos(a), r * math.sin(a), z)


def face(a):
    """Rotation turning local -Y to azimuth a (local +X runs CCW along the tangent)."""
    return (0, 0, a + 90)


def ball(s, c, r, mat, seg=12, n=6):
    with s.at(c):
        s.lathe([(r * math.sin(math.pi * k / n), -r * math.cos(math.pi * k / n)) for k in range(n + 1)], mat, seg=seg)


def band(s, r, z0, z1, mat, seg=48):
    s.lathe([(r, z0), (r, z1)], mat, seg=seg, cap_bottom=False, cap_top=False)


def annulus(s, r0, r1, z, mat, seg=48):
    """Flat ring facing up (r1 > r0): painted deck markings."""
    s.lathe([(r1, z), (r0, z)], mat, seg=seg, cap_bottom=False, cap_top=False)


def bead(s, r, z, mat, seg=48, h=0.05, d=0.07):
    s.lathe([(r, z - h), (r + d, z), (r, z + h)], mat, seg=seg, cap_bottom=False, cap_top=False)


def bars(s, lines, w, z0, z1, mat):
    """Each segment of each 2D polyline as a w-wide bar extruded z0..z1 (glyph strokes, deck markings)."""
    for ln in lines:
        for (x0, y0), (x1, y1) in zip(ln, ln[1:]):
            L = math.hypot(x1 - x0, y1 - y0) or 1.0
            ux, uy = (x1 - x0) / L * w / 2, (y1 - y0) / L * w / 2
            s.extrude([(x0 - ux + uy, y0 - uy - ux), (x1 + ux + uy, y1 + uy - ux), (x1 + ux - uy, y1 + uy + ux),
                       (x0 - ux - uy, y0 - uy + ux)], z0, z1, mat)


def word(s, text, h, w, z0, z1, mat, gap=0.3):
    """Stroke lettering centred on the local origin (x right, y up), cap height h."""
    placed, (ew, _) = layout(text, gap)
    bars(s, [[((ox + x - ew / 2) * h, (oy + y - 0.5) * h) for x, y in st] for sts, (ox, oy) in placed for st in sts],
         w, z0, z1, mat)


def lamp(s, x, y, z, r=0.16, mat=None, seg=6):
    with s.at((x, y, z)):
        s.lathe([(r, 0.0), (r * 0.75, r * 0.7), (0.0, r)], mat or pal.Lamp, seg=seg, cap_bottom=False)


# ----------------------------------------------------------------- landing disc

def rail(s, r, z, a0=0.0, a1=360.0, h=1.1, step=2.5, mat=None):
    """Chrome guard rail on the arc a0..a1 (deg) at radius r: top and knee rails (one chord per `step` m) on posts."""
    mat, full = mat or pal.Chrome, a1 - a0 >= 360.0
    n = max(2, round(2 * math.pi * r * (a1 - a0) / 360 / step))
    for zz, t in ((z + h - 0.04, 0.05), (z + h * 0.5, 0.03)):
        tube(s, [pol(r, a0 + (a1 - a0) * k / n, zz) for k in range(n if full else n + 1)],
             [(t, 0.0), (0.0, t), (-t, 0.0), (0.0, -t)], mat, closed=full)
    for k in range(n if full else n + 1):
        with s.at(pol(r, a0 + (a1 - a0) * k / n, z), face(a0 + (a1 - a0) * k / n)):
            s.box((-0.035, -0.035, 0.0), (0.035, 0.035, h - 0.04), mat)


def pad(s, R, zt, num=None, face_to=90.0, gap=None, seg=64, hub=3.0, depth=4.0, lights=24, ribs=12, approach=0):
    """Aircar landing disc about the local origin, deck top at zt. Plated ivory hull with a tapered soffit, radial
    ribs and a downlight ring; coral rim band between chrome beads; teal deck with a mustard edge band, an ivory
    touchdown ring, dashes and a numeral (top toward azimuth face_to; None = cross); rim lamps and a chrome rail,
    open over gap = (a0, a1) deg (the gate side); `approach` lead-in lamps run in from the rim opposite face_to."""
    rim = min(1.4, 0.35 * depth)
    soffit = [(hub, zt - depth), (hub + 0.5, zt - depth + 0.35), (R * 0.72, zt - 0.6 - rim), (R - 0.6, zt - rim - 0.05)]
    s.lathe([(0.0, zt - depth)] + soffit + [(R, zt - rim + 0.15), (R, zt - 0.1), (R - 0.08, zt)], pal.Hull_Ivory,
            seg=seg, cap_top=False)
    s.lathe([(R - 0.08, zt), (0.0, zt)], pal.Tile_Teal, seg=seg, cap_bottom=False, cap_top=False)
    band(s, R + 0.012, zt - 0.5, zt - 0.28, pal.Enamel_Coral, seg)
    if rim > 0.9:  # observation glazing round the rim of the big discs
        band(s, R + 0.012, zt - rim + 0.3, zt - 0.62, pal.Window_Tower, seg)
    bead(s, R, zt - 0.17, pal.Chrome, seg)
    (r0, z0), (r1, z1) = soffit[2], soffit[3]
    s.lathe([(r0 + (r1 - r0) * 0.25, z0 + (z1 - z0) * 0.25 - 0.03), (r0 + (r1 - r0) * 0.45, z0 + (z1 - z0) * 0.45 - 0.03)],
            pal.Lamp, seg=seg, cap_bottom=False, cap_top=False)  # downlight ring under the rim
    for k in range(ribs):
        with s.at((0, 0, 0), (0, 0, 360 * (k + 0.5) / ribs)), s.at((0, 0, 0), FRONT):
            lo = [(r, z - (0.25 + 0.35 * (1 - (r - hub) / (R - hub)))) for r, z in soffit]
            s.extrude(lo + [(r, z + 0.1) for r, z in soffit[::-1]], -0.18, 0.18, pal.Steel_Ivory)
    zm = zt + 0.015
    annulus(s, R - 2.0, R - 1.4, zm, pal.Enamel_Mustard, seg)
    rt = 0.42 * R
    annulus(s, rt, rt + 0.55, zm, pal.Enamel_Ivory, seg)
    for k in range(8):
        a = 45 * k + 22.5
        bars(s, [[pol(rt + 1.1, a), pol(R - 2.6, a)]], 0.38, zt, zm + 0.005, pal.Enamel_Ivory)
    with s.at((0, 0, 0), (0, 0, face_to - 90)):
        h = 1.3 * rt
        if num:
            bars(s, [[((x - 0.34) * h, (y - 0.5) * h) for x, y in st] for st in DIGIT[num]], 0.13 * h, zt, zm + 0.005,
                 pal.Enamel_Ivory)
        else:
            bars(s, [[(-0.5 * h, 0), (0.5 * h, 0)], [(0, -0.5 * h), (0, 0.5 * h)]], 0.1 * h, zt, zm + 0.005,
                 pal.Enamel_Ivory)
    for k in range(approach):
        lamp(s, *pol(R - 1.7 - (R - 2.9 - rt) * k / max(1, approach - 1), face_to + 180), zt, 0.2, pal.Signal_Amber)
    g0, g1 = gap or (0.0, 0.0)
    g0, g1 = (g0 + 180) % 360 - 180, (g0 + 180) % 360 - 180 + (g1 - g0)
    for k in range(lights):
        a = 360 * (k + 0.5) / lights
        if not gap or not (g0 <= a <= g1 or g0 <= a - 360 <= g1):
            lamp(s, *pol(R - 0.6, a), zt)
    if gap:
        rail(s, R - 0.25, zt, g1, g0 + 360)
    else:
        rail(s, R - 0.25, zt)


# ----------------------------------------------------------------- stem dressing

def lounge(s, rs, az, span, depth, z0, h=3.4, seg=10):
    """Glazed gate lounge hugging a round core (axis at the local origin, radius rs) on a deck at z0: an annular
    sector az +- span, plated sill, dark glazing with chrome mullions, ivory roof, teal fascia, amber beacon."""
    def sector(ro, ri=rs - 0.6):
        return ([pol(ro, az - span + 2 * span * k / seg) for k in range(seg + 1)]
                + [pol(ri, az + span - 2 * span * k / seg) for k in range(seg + 1)])
    ro = rs + depth
    s.extrude(sector(ro), z0, z0 + 0.4, pal.Hull_Ivory, bottom=False)
    s.extrude(sector(ro - 0.2), z0 + 0.4, z0 + h - 0.5, pal.Window_Tower, top=False, bottom=False)
    s.extrude(sector(ro + 0.45), z0 + h - 0.5, z0 + h, pal.Enamel_Ivory)
    s.extrude(sector(ro + 0.48), z0 + h - 0.42, z0 + h - 0.18, pal.Enamel_Teal, top=False, bottom=False)
    n = max(2, round(2 * math.pi * ro * 2 * span / 360 / 1.4))
    for k in range(n + 1):
        a = az - span + 2 * span * k / n
        with s.at(pol(ro - 0.2, a, 0.0), face(a)):
            s.box((-0.05, -0.06, z0 + 0.4), (0.05, 0.02, z0 + h - 0.5), pal.Chrome)
    lamp(s, *pol(rs + depth * 0.5, az), z0 + h, 0.3, pal.Signal_Amber, seg=8)


def lift_tube(s, x, y, z0, z1, r=1.5, car=None, frame=5.0, seg=12, back=None):
    """Glazed lift tube: glass cylinder over a dark shaft half toward azimuth `back` (the core), chrome hoops every
    `frame` m, four chrome mullions, a plated cap and a mustard capsule car parked at height `car`."""
    with s.at((x, y, 0.0)):
        band(s, r, z0, z1, pal.Glass, seg)
        if back is not None:
            s.extrude([pol(r - 0.1, back - 90 + 180 * k / 6) for k in range(7)], z0, z1, pal.Window_Tower, top=False,
                      bottom=False)
        n = max(1, round((z1 - z0) / frame))
        for k in range(n + 1):
            z = z0 + (z1 - z0) * k / n
            band(s, r + 0.1, z - 0.2, z + 0.2, pal.Chrome, seg)
        for k in range(4):
            px, py = pol(r + 0.04, 45 + 90 * k)
            strut(s, (px, py, z0), (px, py, z1), 0.1, pal.Chrome)
        s.lathe([(r + 0.25, z1), (r + 0.25, z1 + 0.6), (0.0, z1 + 0.9)], pal.Hull_Ivory, seg=seg, cap_bottom=False)
        if car is not None:
            c = r - 0.25
            s.lathe([(0.0, car), (c * 0.8, car), (c, car + 0.4), (c, car + 2.4), (c * 0.7, car + 2.8), (0.0, car + 2.9)],
                    pal.Enamel_Mustard, seg=seg)
            band(s, c + 0.02, car + 1.2, car + 2.0, pal.Window_Tower, seg)


def pipes(s, r, a, z0, z1, n=3, pr=0.28, pitch=0.8, clamp=6.0):
    """Vertical service pipe bundle standing off a core at radius r, azimuth a, with clamp bands every `clamp` m."""
    with s.at(pol(r, a, 0.0), face(a)):
        for i in range(n):
            x = (i - (n - 1) / 2) * pitch
            s.sweep([(x, 0.0, z0), (x, 0.0, z1)], pr, pal.Steel_Ivory if i % 2 else pal.Chrome, seg=8)
        w = (n - 1) / 2 * pitch + pr + 0.08
        for k in range(1, int((z1 - z0) / clamp) + 1):
            z = z0 + clamp * k
            s.box((-w, -pr - 0.08, z - 0.12), (w, 0.6, z + 0.12), pal.Steel_Ivory)


def louvres(s, w, h, n=6, d=0.25):
    """Vent bank facing -Y (front plane y = 0, base z = 0): dark recess, chrome frame, n raked ivory slats."""
    s.box((-w / 2, 0.02, 0.0), (w / 2, d, h), pal.Iron)
    for x0, x1 in ((-w / 2 - 0.1, -w / 2), (w / 2, w / 2 + 0.1)):
        s.box((x0, -0.04, -0.1), (x1, d, h + 0.1), pal.Chrome)
    s.box((-w / 2, -0.04, h), (w / 2, d, h + 0.1), pal.Chrome)
    for k in range(n):
        z = h * (k + 0.5) / n
        with s.at((0, 0.1, z), (-35, 0, 0)):
            s.box((-w / 2, -0.12, -0.02), (w / 2, 0.12, 0.02), pal.Steel_Ivory)


def dish(s, c, R, tilt=40.0, az=0.0, mat=None):
    """Parabolic antenna on a yoke, bowl facing up-and-out toward azimuth az."""
    with s.at(c, face(az)), s.at((0, 0, 0), (tilt, 0, 0)):
        s.lathe([(0.0, 0.0), (R * 0.5, 0.06 * R), (R, 0.25 * R), (R * 1.03, 0.27 * R)], mat or pal.Steel_Ivory, seg=16)
        s.lathe([(0.0, 0.26 * R), (0.03 * R, 0.26 * R), (0.02 * R, 0.62 * R), (0.0, 0.62 * R)], pal.Chrome, seg=6)
    s.sweep([(c[0], c[1], c[2] - R * 0.7), c], 0.06 * R, pal.Steel_Ivory, seg=8)


def needle(s, c, h, r=0.12, mat=None):
    with s.at(c):
        s.lathe([(0.0, 0.0), (r, 0.0), (r * 0.25, h), (0.0, h)], mat or pal.Chrome, seg=6)


# ----------------------------------------------------------------- stairs

def flight(s, x0, x1, y0, y1, z0, z1, n, mat=None, rails=True):
    """Straight stair rising from (y0, z0) to (y1, z1) between x0 and x1: open treads on ivory stringers, chrome
    handrails 0.95 m over the nosings."""
    mat, sy = mat or pal.Steel_Ivory, 1 if y1 > y0 else -1
    run = (y1 - y0) / n
    for k in range(n):
        y, z = y0 + run * k, z0 + (z1 - z0) * (k + 1) / n
        s.box((x0 + 0.1, min(y, y + run), z - 0.06), (x1 - 0.1, max(y, y + run), z), mat)
    for x in (x0, x1):
        strut(s, (x, y0 - 0.1 * sy, z0), (x, y1, z1), 0.14, pal.Enamel_Ivory, caps=True)
        if rails:
            s.sweep([(x, y0, z0 + 0.95), (x, y1, z1 + 0.95)], 0.035, pal.Chrome, seg=6)
            for f in (0.0, 0.5, 1.0):
                s.box((x - 0.03, y0 + (y1 - y0) * f - 0.03, z0 + (z1 - z0) * f),
                      (x + 0.03, y0 + (y1 - y0) * f + 0.03, z0 + (z1 - z0) * f + 0.95), pal.Chrome)


def helix(s, r0, r1, z0, z1, turns, steps, a0=0.0, mat=None):
    """Helical stair about the local axis between radii r0..r1: wedge treads, a chrome outer handrail."""
    mat = mat or pal.Steel_Ivory
    da = 360.0 * turns / steps
    for k in range(steps):
        a, z = a0 + da * k, z0 + (z1 - z0) * (k + 1) / steps
        s.extrude([pol(r0, a), pol(r1, a), pol(r1, a + da * 1.05), pol(r0, a + da * 1.05)], z - 0.08, z, mat)
    n = steps * 2
    for r, rr, dz in ((r1 - 0.06, 0.04, 0.95), (r1 - 0.02, 0.09, -0.25)):
        s.sweep([pol(r, a0 + 360.0 * turns * k / n, z0 + (z1 - z0) * k / n + dz) for k in range(n + 1)], rr,
                pal.Chrome if rr < 0.05 else pal.Enamel_Ivory, seg=6)
