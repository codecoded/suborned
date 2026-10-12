"""Vehicle-family helpers on top of geo.Shape: painted quad grids, rounded-plan lofts (streamlined bodies
stacked in Z), profile sweeps, wheels, lamps and boolean carving (wheel arches, window pockets).

Plan convention (vehicles): length along Y, nose at -Y. A plan is CCW seen from +Z: right side (x=+hw) from
yf to yr, superelliptic rear cap of depth dr, left side back to yf, front cap of depth df (tip at yf - df).
"""

import math

import bmesh
import bpy
from mathutils import Vector

from geo import Shape
from mats import pal, register

register("DK_Whitewall", "rubber", "#ECE7DA")

UP = Vector((0, 0, 1))


def rot_to(n):
    """Euler (deg) that turns local +Z onto direction n (for s.at)."""
    e = UP.rotation_difference(Vector(n).normalized()).to_euler("XYZ")
    return tuple(math.degrees(a) for a in e)


def _se(t, e):
    c, s = math.cos(t), math.sin(t)
    return math.copysign(abs(c) ** (2 / e), c), math.copysign(abs(s) ** (2 / e), s)


def plan(hw, yf, yr, df, dr, sides, nc=16, e=2.6):
    """CCW plan outline; sides = side stations as fractions 0..1 of yf..yr (both ends included)."""
    ys = [yf + (yr - yf) * t for t in sides]
    pts = [(hw, y) for y in ys]
    pts += [(hw * cx, yr + dr * sy) for cx, sy in (_se(math.pi * k / nc, e) for k in range(1, nc))]
    pts += [(-hw, y) for y in reversed(ys)]
    pts += [(hw * cx, yf + df * sy) for cx, sy in (_se(math.pi * (1 + k / nc), e) for k in range(1, nc))]
    return pts


def cap_pt(hw, yf, df, x, e=2.6, rear=False, yr=0.0):
    """Point + outward XY normal on a superelliptic end cap at lateral offset x."""
    u = min(abs(x) / hw, 0.999)
    dy = df * (1 - u ** e) ** (1 / e)
    g = Vector((math.copysign(u ** (e - 1) / hw, x), (dy / df) ** (e - 1) / df, 0)).normalized()
    if rear:
        return Vector((x, yr + dy, 0)), g
    g.y = -g.y
    return Vector((x, yf - dy, 0)), g


def stations(y0, y1, keys, step):
    """Side-station fractions: every key y plus fill so no gap exceeds step."""
    ys = sorted({y0, y1, *[k for k in keys if y0 < k < y1]})
    out = [ys[0]]
    for a, b in zip(ys, ys[1:]):
        n = max(1, math.ceil((b - a) / step - 1e-6))
        out += [a + (b - a) * i / n for i in range(1, n + 1)]
    return [(y - y0) / (y1 - y0) for y in out]


def mesh(s, rows, mat, closed=True, caps=((0, None), (-1, None)), paint=None, smooth=True):
    """Quad grid over rows[i][j] (local 3D points, rows advance like loft sections). closed wraps j.
    paint(i, j, centre) -> material per quad. caps: (row, material|None=mat), or (row, mat, [j...]) to
    split a cap into several ngons."""
    flip = s.m.to_3x3().determinant() < 0
    V = [[s.bm.verts.new(s.m @ Vector(p)) for p in row] for row in rows]
    n = len(rows[0])

    def face(vs, m):
        if flip:
            vs = vs[::-1]
        try:
            f = s.bm.faces.new(vs)
        except ValueError:
            return
        f.material_index = s._mi(m)
        f.smooth = smooth

    for i in range(len(rows) - 1):
        for j in range(n if closed else n - 1):
            j1 = (j + 1) % n
            q = [V[i][j], V[i][j1], V[i + 1][j1], V[i + 1][j]]
            m = mat
            if paint:
                c = sum((Vector(rows[a][b]) for a, b in ((i, j), (i, j1), (i + 1, j1), (i + 1, j))), Vector()) / 4
                m = paint(i, j, c) or mat
            face(q, m)
    if closed and caps:
        for cp in caps:
            row, cm = cp[0], cp[1] or mat
            js = cp[2] if len(cp) > 2 else range(n)
            vs = [V[row][j] for j in js]
            face(vs[::-1] if row == 0 else vs, cm)


def vloft(s, secs, mat, sides, nc=16, e=2.6, **kw):
    """Streamlined body: plan sections stacked in Z. secs: [(z, hw, yf, yr, df, dr), ...] bottom to top."""
    rows = [[(x, y, z) for x, y in plan(hw, yf, yr, df, dr, sides, nc, e)] for z, hw, yf, yr, df, dr in secs]
    mesh(s, rows, mat, **kw)
    return rows


def lerp_rows(rows, z, i):
    """Column i of z-keyed section rows, linear in z, clamped to the end rows."""
    for a, b in zip(rows, rows[1:]):
        if a[0] <= z <= b[0] and b[0] > a[0]:
            return a[i] + (b[i] - a[i]) * (z - a[0]) / (b[0] - a[0])
    return rows[0][i] if z < rows[0][0] else rows[-1][i]


def inset(poly, d):
    """Miter offset of a CCW 2D polygon inward by d (small d; keeps the point count)."""
    n, out = len(poly), []
    for i in range(n):
        p0, p1, p2 = Vector(poly[i - 1]), Vector(poly[i]), Vector(poly[(i + 1) % n])
        e0, e1 = (p1 - p0).normalized(), (p2 - p1).normalized()
        m = Vector((-e0.y, e0.x)) + Vector((-e1.y, e1.x))
        m = m.normalized() if m.length > 1e-6 else Vector((-e1.y, e1.x))
        out.append(tuple(p1 + m * (d / max(m.dot(Vector((-e1.y, e1.x))), 0.35))))
    return out


def slab(s, prof, hw, r, mat, n=4, x0=0.0, paint=None):
    """Rounded-edge slab: CCW side profile in (Y, Z) spanning X from x0-hw to x0+hw, edges rounded to r."""
    xs = [(-(hw - r) - r * math.cos(math.pi / 2 * k / n), r * (1 - math.sin(math.pi / 2 * k / n)))
          for k in range(n + 1)]
    xs += [(-x, d) for x, d in reversed(xs)]
    rows = [[(x0 + x, y, z) for y, z in (inset(prof, d) if d > 1e-6 else prof)] for x, d in xs]
    mesh(s, rows, mat, paint=paint)
    return rows


def patch(s, rad, a0, a1, zs, t, mat, n=10, round_ends=True):
    """Thin raised panel conformal to a surface of revolution about Z: rad(z) = body radius; spans angle a0..a1
    (deg) over heights zs; thickness t proud of the body (plus 1 cm buried)."""
    rows = []
    for k, z in enumerate(zs):
        e = 0.0
        if round_ends and k in (0, len(zs) - 1):
            e = 0.35 * (a1 - a0) / n
        r = rad(z)
        arc_ = [math.radians(a0 + e + (a1 - a0 - 2 * e) * i / n) for i in range(n + 1)]
        outer = [((r + t) * math.cos(a), (r + t) * math.sin(a), z) for a in arc_]
        inner = [((r - 0.01) * math.cos(a), (r - 0.01) * math.sin(a), z) for a in reversed(arc_)]
        rows.append(outer + inner)
    mesh(s, rows, mat)


def psweep(s, path, sec, mat, side=(1, 0, 0), caps=True, paint=None, closed_path=False):
    """Sweep a closed CCW 2D section (u along `side`, v along the in-plane normal) along a 3D path.
    sec may be a list or a function i -> list (tapers)."""
    P = [Vector(p) for p in path]
    B = Vector(side).normalized()
    rows = []
    for i, p in enumerate(P):
        if closed_path:
            t = P[(i + 1) % len(P)] - P[i - 1]
        else:
            t = P[min(i + 1, len(P) - 1)] - P[max(i - 1, 0)]
        t.normalize()
        b = (B - t * B.dot(t)).normalized()
        nrm = t.cross(b)
        sc = sec(i) if callable(sec) else sec
        rows.append([tuple(p + u * b + v * nrm) for u, v in sc])
    if closed_path:
        rows.append(rows[0])
    mesh(s, rows, mat, caps=((0, None), (-1, None)) if caps and not closed_path else (), paint=paint)
    return rows


def loop(pts):
    """Closed 3D polyline for Shape.sweep (repeat the first point)."""
    return list(pts) + [pts[0]]


def plathe(s, prof, mats, seg=32):
    """Lathe (no caps) whose profile segment k (prof[k] -> prof[k+1]) takes material mats[k]."""
    bv = s.lathe(prof, mats[0], seg=seg, cap_bottom=False, cap_top=False)
    idx, i = {}, 0
    for k, (r, _) in enumerate(prof):
        n = 1 if r < 1e-6 else seg
        for v in bv[i:i + n]:
            idx[v] = k
        i += n
    for f in {f for v in bv for f in v.link_faces}:
        f.material_index = s._mi(mats[min(idx[v] for v in f.verts)])


def wheel(s, c, R, w, rin=None, white=None, hub=0.6, rim=None, seg=32, dome=0.2, side=0):
    """Tyre + rim + chrome hub cap as one closed painted lathe, axle along X at c. white = (r0, r1) whitewall
    band; hub = cap radius / R; side = +1/-1 dresses only that face (the other faces the body), 0 = both."""
    rin = rin or R * 0.62
    h, rh, rc = w / 2, R * hub, w * 0.42
    CH, RUB, WW, RIM = pal.Chrome, pal.Rubber, pal.Whitewall, rim or pal.Chrome

    def face(sg):  # one face, axis -> tread: (r, z, material of the segment to the next point)
        if side and sg != side:
            return [(0, sg * h, RUB), (rin, sg * (h + 0.003), RUB)]
        top = h + 0.012 + dome * rh
        pts = [(0, top, CH), (rh * 0.45, h + 0.012 + dome * rh * 0.85, CH), (rh * 0.85, h + 0.012 + dome * rh * 0.4, CH),
               (rh, h + 0.008, CH), (rh * 1.05, h - 0.006, RIM), (rin, h - 0.014, RUB), (rin + 0.012, h + 0.002, RUB)]
        if white:
            pts += [(white[0], h + 0.004, WW), (white[1], h + 0.004, RUB)]
        return [(r, sg * z, m) for r, z, m in pts]

    sh = [(R - 0.004 - rc + rc * math.sin(a), h - rc + rc * math.cos(a)) for a in (0, math.pi / 6, math.pi / 3, math.pi / 2)]
    neg, pos = face(-1), face(1)
    prof = [(r, z) for r, z, _ in neg] + [(r, -z) for r, z in sh] + [(R, 0.0)] + sh[::-1] + \
        [(r, z) for r, z, _ in reversed(pos)]
    mats = [m for _, _, m in neg] + [RUB] * 9 + [pos[j][2] for j in range(len(pos) - 2, -1, -1)]
    with s.at(c, (0, 90, 0)):
        plathe(s, prof, mats, seg)


def lamp(s, p, n, r, lens=None, bezel=None, depth=0.04, housing=None, hr=None, hd=0.0, seg=24):
    """Round lamp at p facing n: optional body-colour housing pod, chrome bezel, domed lens."""
    lens, bezel = lens or pal.Lamp, bezel or pal.Chrome
    with s.at(tuple(p), rot_to(n)):
        if housing is not None:
            hr = hr or r * 1.45
            s.lathe([(hr * 0.9, -0.06), (hr, hd * 0.4), (hr * 0.92, hd), (r * 1.2, hd + 0.004)], housing,
                    seg=seg, cap_bottom=True, cap_top=True)
        z = hd
        s.lathe([(r * 1.22, z - 0.02), (r * 1.22, z + depth * 0.6), (r * 1.1, z + depth),
                 (r * 0.98, z + depth * 0.8), (r * 0.98, z + depth * 0.4)], bezel, seg=seg, cap_top=False)
        s.lathe([(r, z + depth * 0.4), (r * 0.8, z + depth * 0.95), (r * 0.45, z + depth * 1.3),
                 (0, z + depth * 1.4)], lens, seg=seg, cap_bottom=True)


def carve(s, body, cutters):
    """Boolean difference: body(shape) minus cutters(shape), merged into s. Cut faces take the cutter's
    material (TRANSFER), so a Rubber cutter leaves dark wheel wells / window pockets."""
    parts, objs = (Shape("_cvA"), Shape("_cvB")), []
    body(parts[0])
    cutters(parts[1])
    for sh in parts:
        me = bpy.data.meshes.new(sh.name)
        sh.bm.to_mesh(me)
        sh.bm.free()
        for m in sh.mats:
            me.materials.append(m)
        ob = bpy.data.objects.new(sh.name, me)
        bpy.context.scene.collection.objects.link(ob)
        objs.append(ob)
    oa, ob = objs
    md = oa.modifiers.new("cut", "BOOLEAN")
    md.operation, md.solver, md.object = "DIFFERENCE", "EXACT", ob
    md.material_mode, md.use_self, md.use_hole_tolerant = "TRANSFER", True, True
    ev = oa.evaluated_get(bpy.context.evaluated_depsgraph_get())
    me = ev.to_mesh()
    mats = [m.original for m in me.materials]  # evaluated (COW) materials die with the depsgraph
    bm = bmesh.new()
    bm.from_mesh(me)
    ev.to_mesh_clear()
    bmesh.ops.dissolve_degenerate(bm, dist=2e-4, edges=bm.edges[:])  # boolean slivers crash the bevel modifier
    bmesh.ops.triangulate(bm, faces=[f for f in bm.faces if len(f.verts) > 4])
    flip = s.m.to_3x3().determinant() < 0
    V = {v: s.bm.verts.new(s.m @ v.co) for v in bm.verts}
    for f in bm.faces:
        vs = [V[v] for v in f.verts]
        try:
            nf = s.bm.faces.new(vs[::-1] if flip else vs)
        except ValueError:
            continue
        nf.material_index = s._mi(mats[f.material_index] if mats else pal.Rubber)
        nf.smooth = f.smooth
    bm.free()
    for o in objs:
        m = o.data
        bpy.data.objects.remove(o)
        bpy.data.meshes.remove(m)
