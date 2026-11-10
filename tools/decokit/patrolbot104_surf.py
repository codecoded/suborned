"""Brief 104 (Vela, 2026-11-10): the egg body of the patrol robot PatrolBot104 (patrolbot104.py builds the piece) and the plates laid on it.

Body frame: Blender metres, Z up, front -Y. A body point is (theta, z): theta in degrees about Z from the front (0 = -Y, +90 = +X, the robot's own left), z its
height above the ground. The cross-section at z is an ellipse, half-width A(z) across and DEPTH(z) x A(z) front to back; A comes from the turnaround sheet's front view
(briefs/104/concepts/patrol-robot-turnaround.png, 413 px/m with the ground line at y 703), the depth from its side view.

Plates are raised panels laid over a surface (the egg, or the ball's pole caps): a sheet t proud of it with a rounded lip down into it, so a colour change reads
as a panel line, as on the sheets. A grid of (p, q) surface parameters is given bottom row first, p to the right and q up as seen from outside; loops run
counter-clockwise seen from outside, region on their left."""

import math

import bmesh
from mathutils import Vector

PROFILE = [(0.0, 0.401), (0.033, 0.407), (0.068, 0.421), (0.1, 0.443), (0.14, 0.487), (0.19, 0.558), (0.228, 0.64), (0.25, 0.71), (0.27, 0.78),
           (0.279, 0.85), (0.276, 0.91), (0.262, 0.96), (0.237, 1.0), (0.2, 1.035), (0.15, 1.062), (0.09, 1.083), (0.04, 1.093), (0.0, 1.096)]
TIP, TOP = PROFILE[0][1], PROFILE[-1][1]


def _spline(pts, n=24):
    """Centripetal Catmull-Rom through 2D points (no overshoot at the rounded ends), n samples a span; z kept rising."""
    P = [Vector(p) for p in pts]
    P = [2 * P[0] - P[1]] + P + [2 * P[-1] - P[-2]]
    out = []
    for i in range(1, len(P) - 2):
        p0, p1, p2, p3 = P[i - 1], P[i], P[i + 1], P[i + 2]
        t0 = 0.0
        t1 = t0 + max((p1 - p0).length, 1e-9) ** 0.5
        t2 = t1 + max((p2 - p1).length, 1e-9) ** 0.5
        t3 = t2 + max((p3 - p2).length, 1e-9) ** 0.5
        for k in range(n):
            t = t1 + (t2 - t1) * k / n
            a1 = p0 * ((t1 - t) / (t1 - t0)) + p1 * ((t - t0) / (t1 - t0))
            a2 = p1 * ((t2 - t) / (t2 - t1)) + p2 * ((t - t1) / (t2 - t1))
            a3 = p2 * ((t3 - t) / (t3 - t2)) + p3 * ((t - t2) / (t3 - t2))
            b1 = a1 * ((t2 - t) / (t2 - t0)) + a2 * ((t - t0) / (t2 - t0))
            b2 = a2 * ((t3 - t) / (t3 - t1)) + a3 * ((t - t1) / (t3 - t1))
            out.append(b1 * ((t2 - t) / (t2 - t1)) + b2 * ((t - t1) / (t2 - t1)))
    out.append(P[-2])
    keep = [out[0]]
    for v in out[1:]:
        if v.y > keep[-1].y + 1e-7:
            keep.append(v)
    return [(max(v.x, 0.0), v.y) for v in keep]


_DENSE = _spline(PROFILE)


def A(z):
    """The body's half-width at height z (0 outside the body)."""
    if z <= TIP or z >= TOP:
        return 0.0
    lo, hi = 0, len(_DENSE) - 1
    while hi - lo > 1:
        mid = (lo + hi) // 2
        if _DENSE[mid][1] <= z:
            lo = mid
        else:
            hi = mid
    (r0, z0), (r1, z1) = _DENSE[lo], _DENSE[hi]
    return r0 + (r1 - r0) * (z - z0) / (z1 - z0)


def _smooth(x):
    x = min(1.0, max(0.0, x))
    return x * x * (3 - 2 * x)


def DEPTH(z):
    """Depth over width: 0.86 through the middle (side view 0.47 m deep against 0.56 m wide), rounder toward the tip and the crown (the top view's near-circle)."""
    return 0.86 + 0.12 * _smooth((0.52 - z) / 0.11) + 0.08 * _smooth((z - 0.98) / 0.11)


class Egg:
    """The body surface: P(theta, z) and its outward normal."""

    @staticmethod
    def P(th, z):
        a = A(z)
        t = math.radians(th)
        return Vector((a * math.sin(t), -a * DEPTH(z) * math.cos(t), z))

    @staticmethod
    def N(th, z):
        h, dz = 0.25, 0.0015
        n = (Egg.P(th + h, z) - Egg.P(th - h, z)).cross(Egg.P(th, z + dz) - Egg.P(th, z - dz))
        return n.normalized()


class Cap:
    """A pole cap of a sphere of radius r about local +X (the ball's hubs): p metres along the sphere toward +Y, q toward +Z."""

    def __init__(self, r):
        self.r = r

    def N(self, p, q):
        return Vector((1.0, math.tan(p / self.r), math.tan(q / self.r))).normalized()

    def P(self, p, q):
        return self.N(p, q) * self.r


ROUND = ((0.0, 1.0), (0.7071, 0.7071), (1.0, 0.0))  # the lip, in units of t (out, up) from the border: a quarter round


def lip_rows(t, sink=0.003, back=None):
    """Rows (out, up) in metres for a skirt: the quarter round from the sheet's border at height t, then down under the surface (or to a back sheet)."""
    rows = [(t * o, t * u) for o, u in ROUND]
    return rows + ([(t, back), (0.0, back)] if back is not None else [(t, -sink)])


def add_faces(s, bv, faces, mat):
    """Faces over verts already added (one shell, several materials)."""
    mi = s._mi(mat)
    for f in faces:
        try:
            bf = s.bm.faces.new([bv[i] for i in f])
        except ValueError:
            continue
        bf.material_index, bf.smooth = mi, True


def weld(s, verts):
    bmesh.ops.remove_doubles(s.bm, verts=[v for v in verts if v.is_valid], dist=1e-6)


def skirt(s, surf, loop, rows, mat, closed=True):
    """A lip along a loop of (p, q) params (counter-clockwise seen from outside, region on the left): rows of (out, up) in metres, the first the border itself."""
    n = len(loop)
    P = [surf.P(*q) for q in loop]
    N = [surf.N(*q) for q in loop]
    O = []
    for i in range(n):
        a, b = (P[i - 1], P[(i + 1) % n]) if closed else (P[max(i - 1, 0)], P[min(i + 1, n - 1)])
        O.append((b - a).cross(N[i]).normalized())
    verts = [tuple(P[i] + N[i] * up + O[i] * out) for out, up in rows for i in range(n)]
    m = n if closed else n - 1
    faces = [[(r + 1) * n + i, (r + 1) * n + (i + 1) % n, r * n + (i + 1) % n, r * n + i] for r in range(len(rows) - 1) for i in range(m)]  # outward, away from the region
    return s._add(verts, faces, mat, True)


def plate(s, surf, grid, mat, t=0.005, sink=0.003, back=None):
    """A raised plate over a grid of (p, q) params (rows bottom to top, p rightward): a sheet t proud, a rounded lip down under the surface (sink), or, with back,
    a slab whose back sheet lies `back` from the surface (a panel that opens). Returns the welded verts."""
    nv, nu = len(grid) - 1, len(grid[0]) - 1
    k = lambda i, j: j * (nu + 1) + i  # noqa: E731
    quads = [[k(i, j), k(i + 1, j), k(i + 1, j + 1), k(i, j + 1)] for j in range(nv) for i in range(nu)]
    bv = s._add([tuple(surf.P(*g) + surf.N(*g) * t) for row in grid for g in row], quads, mat, True)
    loop = grid[0] + [grid[j][nu] for j in range(1, nv + 1)] + grid[nv][::-1][1:] + [grid[j][0] for j in range(nv - 1, 0, -1)]
    bv += skirt(s, surf, loop, lip_rows(t, sink, back), mat)
    if back is not None:
        bv += s._add([tuple(surf.P(*g) + surf.N(*g) * back) for row in grid for g in row], [q[::-1] for q in quads], mat, True)
    weld(s, bv)
    return bv


def rows_grid(zs, lo, hi, nu):
    """A grid over theta in [lo(z), hi(z)] at each height in zs."""
    return [[(lo(z) + (hi(z) - lo(z)) * i / nu, z) for i in range(nu + 1)] for z in zs]


def superellipse(a, n):
    """Unit superellipse point at polar angle a (radians)."""
    c, s = math.cos(a), math.sin(a)
    k = (abs(c) ** n + abs(s) ** n) ** (-1.0 / n)
    return c * k, s * k


def squircle(u, v, n):
    """The unit square (u, v in 0..1) onto the superellipse of exponent n, edge to edge (a screen keeps its UVs 0..1 across the visible glass)."""
    x, y = 2 * u - 1, 2 * v - 1
    m = max(abs(x), abs(y))
    if m < 1e-9:
        return 0.0, 0.0
    k = m / (abs(x) ** n + abs(y) ** n) ** (1.0 / n)
    return x * k, y * k


def annulus(s, surf, inner, outer, m, rows, mat, t=0.005, sink=0.003):
    """A raised ring between two closed outlines inner(a) and outer(a) -> (p, q), a in radians counter-clockwise: rounded lips on both edges, the inner one
    falling into the hole it frames. Returns the welded verts."""
    al = [2 * math.pi * k / m for k in range(m)]
    G = [[tuple(x0 + (x1 - x0) * j / rows for x0, x1 in zip(inner(a), outer(a))) for a in al] for j in range(rows + 1)]
    idx = lambda j, k: j * m + k % m  # noqa: E731
    quads = [[idx(j, k), idx(j + 1, k), idx(j + 1, k + 1), idx(j, k + 1)] for j in range(rows) for k in range(m)]
    bv = s._add([tuple(surf.P(*g) + surf.N(*g) * t) for row in G for g in row], quads, mat, True)
    bv += skirt(s, surf, G[-1], lip_rows(t, sink), mat)
    bv += skirt(s, surf, G[0][::-1], lip_rows(t, sink), mat)
    weld(s, bv)
    return bv


def shell(s, cols, zs, mat_of, skip=lambda th0, th1, z0, z1: False):
    """The egg as one smooth skin: cols columns from theta -180, rows at zs (ends on the tip and the crown), mat_of(theta, z) per quad at its centre;
    skip(...) leaves a quad out (a hatch, or skin hidden under a plate). Returns the verts."""
    ths = [-180.0 + 360.0 * k / cols for k in range(cols)]
    verts, rings = [], []
    for z in zs:
        if A(z) < 1e-6:
            rings.append([len(verts)])
            verts.append((0.0, 0.0, z))
        else:
            rings.append(list(range(len(verts), len(verts) + cols)))
            verts += [tuple(Egg.P(th, z)) for th in ths]
    groups = {}
    for j, (a, b) in enumerate(zip(rings, rings[1:])):
        z0, z1 = zs[j], zs[j + 1]
        for k in range(cols):
            k1 = (k + 1) % cols
            th0, th1 = ths[k], ths[k] + 360.0 / cols
            if skip(th0, th1, z0, z1):
                continue
            if len(a) == 1:
                f = [a[0], b[k1], b[k]]
            elif len(b) == 1:
                f = [a[k], a[k1], b[0]]
            else:
                f = [a[k], a[k1], b[k1], b[k]]
            groups.setdefault(mat_of(0.5 * (th0 + th1), 0.5 * (z0 + z1)), []).append(f)
    items = list(groups.items())
    bv = s._add(verts, items[0][1], items[0][0], True)
    for mat, faces in items[1:]:
        add_faces(s, bv, faces, mat)
    return bv
