"""Hardware helpers for the landmark megastructures (landmarks.py, towers.py): struts, lattice columns, ring girders,
cables, lens discs and rocket liners. Kit frame: metres, Z up, front -Y."""

import math

from mathutils import Vector

from mats import pal
from structures import sec, tube

FRONT = (90, 0, 0)  # local XY -> world XZ, extrusion -> world -Y


def strut(s, a, b, w, mat, caps=False):
    """Square-section member from a to b (3D points), w wide. Open ends by default: they sit inside joints."""
    t = (Vector(b) - Vector(a)).normalized()
    up = (0, 0, 1) if abs(t.z) < 0.9 else (1, 0, 0)
    tube(s, [a, b], sec(-w / 2, w / 2, -w / 2, w / 2), mat, up=up, caps=caps)


def cable(s, a, b, r=0.06, mat=None):
    strut(s, a, b, 2 * r, mat or pal.Steel)


def truss(s, c0, c1, w0, w1, levels, mat, chord=0.35, lace=0.18, braced=(True, True, True, True)):
    """Square lattice column from centre c0 to c1 (vertical or leaning), half-widths w0 -> w1: 4 chords, a
    horizontal ring and one zig-zag diagonal per face per level. braced: per face (-Y, +X, +Y, -X) diagonals."""
    c0, c1 = Vector(c0), Vector(c1)
    corners = ((-1, -1), (1, -1), (1, 1), (-1, 1))

    def pt(i, f):
        c, w = c0.lerp(c1, f), w0 + (w1 - w0) * f
        return (c.x + corners[i][0] * w, c.y + corners[i][1] * w, c.z)
    for k in range(levels):
        f0, f1 = k / levels, (k + 1) / levels
        for i in range(4):
            j = (i + 1) % 4
            strut(s, pt(i, f0), pt(i, f1), chord, mat)
            strut(s, pt(i, f1), pt(j, f1), lace, mat)
            if braced[i]:
                a, b = (pt(i, f0), pt(j, f1)) if k % 2 else (pt(j, f0), pt(i, f1))
                strut(s, a, b, lace, mat)
    for i in range(4):
        strut(s, pt(i, 0), pt((i + 1) % 4, 0), lace, mat)


def ring_girder(s, r, z, n, w, mat, depth=0.0):
    """Polygonal ring of n straight members at radius r (a second, lower chord and web when depth > 0)."""
    P = [(r * math.cos(2 * math.pi * k / n), r * math.sin(2 * math.pi * k / n)) for k in range(n)]
    for k in range(n):
        (x0, y0), (x1, y1) = P[k], P[(k + 1) % n]
        strut(s, (x0, y0, z), (x1, y1, z), w, mat)
        if depth:
            strut(s, (x0, y0, z - depth), (x1, y1, z - depth), w, mat)
            strut(s, (x0, y0, z - depth), (x1, y1, z), w * 0.6, mat)


def lens(s, z, r_in, r_out, t, mat, glaze=True, seg=64, bead=None):
    """Cantilevered disc about the axis (core radius r_in): tapered soffit and deck, a rounded rim; an observation
    glazing band and beads (chrome unless `bead`) on the rim when glaze."""
    e = min(1.2, t)
    s.lathe([(r_in, z), (r_out - e, z + 0.22 * t), (r_out, z + 0.42 * t), (r_out, z + 0.62 * t),
             (r_out - e * 0.6, z + 0.86 * t), (r_in, z + t)], mat, seg=seg, cap_bottom=False, cap_top=False)
    if glaze:
        s.lathe([(r_out + 0.03, z + 0.44 * t), (r_out + 0.03, z + 0.6 * t)], pal.Window_Tower, seg=seg,
                cap_bottom=False, cap_top=False)
        for zz in (z + 0.42 * t, z + 0.62 * t):
            s.lathe([(r_out + 0.02, zz - 0.06), (r_out + 0.1, zz), (r_out + 0.02, zz + 0.06)], bead or pal.Chrome, seg=seg,
                    cap_bottom=False, cap_top=False)


def ogive(r, length, n=10):
    """Tangent-ogive nose profile (r, z) from the shoulder (r, 0) to the tip (0, length)."""
    rho = (r * r + length * length) / (2 * r)
    return [(math.sqrt(rho * rho - (length * k / n) ** 2) + r - rho, length * k / n) for k in range(n + 1)]


def rocket(s, z0, h, r, fins=4, body=None, accent=None, seg=32):
    """Upright rocket liner, base at z0, height h, body radius r: engine bells, flared skirt, panelled body with
    two accent bands, ogive nose, swept fins (fins x (radial, z) blades)."""
    body, accent = body or pal.Hull_Silver, accent or pal.Enamel_Red
    zs, zn = z0 + 0.08 * h, z0 + 0.66 * h
    s.lathe([(0.62 * r, z0 + 0.03 * h), (0.9 * r, z0 + 0.05 * h), (r, zs), (r, zn)]
            + [(x, zn + y) for x, y in ogive(r, h - (zn - z0))[1:]], body, seg=seg, cap_bottom=True)
    for f, dz in ((0.3, 0.035), (0.6, 0.02)):
        z = z0 + f * h
        s.lathe([(r + 0.02, z), (r + 0.02, z + dz * h)], accent, seg=seg, cap_bottom=False, cap_top=False)
    for k in range(4):  # engine bells
        a = 2 * math.pi * (k + 0.5) / 4
        with s.at((0.42 * r * math.cos(a), 0.42 * r * math.sin(a), z0)):
            s.lathe([(0.26 * r, 0.0), (0.16 * r, 0.035 * h), (0.12 * r, 0.05 * h)], pal.Iron, seg=12)
    blade = [(0.95 * r, zs + 0.1 * h), (0.95 * r, zs - 0.02 * h), (2.3 * r, z0 - 0.01 * h), (2.45 * r, z0 + 0.04 * h),
             (1.4 * r, zs + 0.06 * h), (0.95 * r, zs + 0.2 * h)]
    tip = [(2.03 * r, z0 + 0.004 * h), (2.3 * r, z0 - 0.01 * h), (2.45 * r, z0 + 0.04 * h), (2.19 * r, z0 + 0.065 * h)]
    for k in range(fins):
        with s.at((0, 0, 0), (0, 0, 360 * k / fins + 45)), s.at((0, 0, 0), FRONT):
            s.extrude(blade, -0.09 * r, 0.09 * r, body)
            s.extrude(tip, -0.1 * r, 0.1 * r, accent)


def girder(s, a, b, w, h, bays, mat, chord=0.4, lace=0.2):
    """Triangular lattice girder from a to b (horizontal-ish): two bottom chords w apart, one top chord h up, lacing
    zig-zagging on all three faces."""
    a, b = Vector(a), Vector(b)
    t = (b - a).normalized()
    side = Vector((0, 0, 1)).cross(t).normalized() * (w / 2)
    up = Vector((0, 0, h))
    rails = (a - side, a + side, a + up)
    for k in range(bays):
        f0, f1 = k / bays, (k + 1) / bays
        p0 = [tuple(r + (b - a) * f0) for r in rails]
        p1 = [tuple(r + (b - a) * f1) for r in rails]
        for i in range(3):
            strut(s, p0[i], p1[i], chord, mat)
            strut(s, p1[i], p1[(i + 1) % 3], lace, mat)
            strut(s, p0[i], p1[(i + 1) % 3], lace, mat)
