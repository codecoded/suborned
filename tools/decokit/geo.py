"""Deco Kit geometry (Blender 5.2, bmesh). 1 unit = 1 m, Z up, asset pivot = base centre.

`Shape` accumulates closed solids per material through a transform stack and emits one object whose
hard edges carry a thin angle-limited bevel + weighted normals: the soft specular edge that makes
enamel and chrome read in the references.

  lathe(profile)      revolve (r, z) points about local Z     posts, bollards, bins, domes, lenses
  sweep(path, r)      round tube along a 3D polyline          lamp arms, rails, racks, handles
  extrude(poly)       2D polygon (local XY) prism along Z     slabs, panels, canopies, fins
  rbox(mn, mx, r)     box with all edges rounded (r)          cabinets, housings, seats
  box(mn, mx)         plain box
2D helpers: rrect, stadium, circle, arc, fillet. Path helper: fillet (3D) rounds polyline corners.
"""

import math
from contextlib import contextmanager

import bmesh
import bpy
from mathutils import Euler, Matrix, Vector


# ----------------------------------------------------------------- 2D / path helpers

def arc(cx, cy, r, a0, a1, n=12):
    """Points on a circular arc, angles in degrees, inclusive."""
    return [(cx + r * math.cos(math.radians(a0 + (a1 - a0) * i / n)),
             cy + r * math.sin(math.radians(a0 + (a1 - a0) * i / n))) for i in range(n + 1)]


def circle(r, n=32, cx=0.0, cy=0.0):
    return arc(cx, cy, r, 0, 360, n)[:-1]


def fillet(pts, r, n=6, closed=True):
    """Round every corner of a 2D/3D polyline with a quadratic-Bezier fillet of radius ~r."""
    P = [Vector(p) for p in pts]
    m = len(P)
    out = []
    rng = range(m) if closed else range(m)
    for i in rng:
        if not closed and i in (0, m - 1):
            out.append(P[i])
            continue
        a, p, b = P[i - 1], P[i], P[(i + 1) % m]
        d0, d1 = (p - a), (b - p)
        l0, l1 = d0.length, d1.length
        if l0 < 1e-6 or l1 < 1e-6:
            continue
        t = min(r, l0 * 0.5, l1 * 0.5)
        A, C = p - d0 / l0 * t, p + d1 / l1 * t
        for k in range(n + 1):
            s = k / n
            out.append((1 - s) ** 2 * A + 2 * (1 - s) * s * p + s * s * C)
    return [tuple(v) for v in out]


def rrect(w, d, r, n=6, cx=0.0, cy=0.0):
    """Centred rounded rectangle, CCW."""
    hw, hd = w / 2, d / 2
    r = min(r, hw, hd)
    if r <= 1e-6:
        return [(cx - hw, cy - hd), (cx + hw, cy - hd), (cx + hw, cy + hd), (cx - hw, cy + hd)]
    pts = []
    for (x, y, a0) in ((hw - r, -hd + r, 270), (hw - r, hd - r, 0), (-hw + r, hd - r, 90), (-hw + r, -hd + r, 180)):
        pts += arc(cx + x, cy + y, r, a0, a0 + 90, n)
    return pts


def stadium(w, d, n=12):
    return rrect(w, d, min(w, d) / 2, n)


def dome(r, z0, h, n=8):
    """(r, z) quarter-ellipse profile from (r, z0) up to the axis at z0 + h (for lathe)."""
    return [(r * math.cos(math.pi / 2 * k / n), z0 + h * math.sin(math.pi / 2 * k / n)) for k in range(n + 1)]


def ring(r0, r1, z0, z1):
    """Closed (r, z) loop for a lathe band/torus-like ring; lathe with caps off."""
    return [(r0, z0), (r1, z0), (r1, z1), (r0, z1), (r0, z0)]


def thicken(pts, t):
    """Closed CCW polygon of thickness t centred on an open 2D polyline."""
    P = [Vector((x, y)) for x, y in pts]
    L, R = [], []
    for i, p in enumerate(P):
        d = (P[min(i + 1, len(P) - 1)] - P[max(i - 1, 0)]).normalized()
        n = Vector((-d.y, d.x)) * (t / 2)
        L.append(p + n)
        R.append(p - n)
    return [tuple(v) for v in R + L[::-1]]


# ----------------------------------------------------------------- shape builder

class Shape:
    def __init__(self, name):
        self.name = name
        self.bm = bmesh.new()
        self._uvkeep = self.bm.faces.layers.int.new("uvkeep")
        self._pending_uv = []
        self.up_uv = False
        self.mats = []
        self._stack = [Matrix.Identity(4)]
        self.parts = {}
        self.lean = 1.0  # segment scale for small street props (kit.asset lean=; brief 007 round 3, Redline's per-pixel caps)

    def _seg(self, seg, lo):
        return seg if self.lean == 1.0 else max(lo, int(round(seg * self.lean)))

    @property
    def m(self):
        return self._stack[-1]

    @contextmanager
    def at(self, loc=(0, 0, 0), rot=(0, 0, 0), scale=None):
        """Nested local frame; rot = XYZ Euler in degrees; scale = float or (x, y, z)."""
        mat = Matrix.Translation(Vector(loc)) @ Euler([math.radians(a) for a in rot]).to_matrix().to_4x4()
        if scale is not None:
            s = scale if hasattr(scale, "__len__") else (scale,) * 3
            mat = mat @ Matrix.Diagonal((*s, 1.0))
        self._stack.append(self.m @ mat)
        try:
            yield self
        finally:
            self._stack.pop()

    @contextmanager
    def part(self, name):
        """A movable child mesh (a head that turns, a face the engine draws): geometry built on the yielded shape
        goes to object `<name>_<part>`, parented under this one (exported as a sibling: the FBX axis bake misplaces
        children). Parts nest, and all share the asset's origin, so a part turns about the asset's vertical axis."""
        p = self.parts.get(name)
        if p is None:
            p = self.parts[name] = Shape(f"{self.name}_{name}")
        p.up_uv = self.up_uv  # set by kit.build before the builder runs
        p._stack = self._stack  # shares the frame, so nested s.at() places part geometry like any other
        yield p

    def _mi(self, mat):
        if mat not in self.mats:
            self.mats.append(mat)
        return self.mats.index(mat)

    def _add(self, verts, faces, mat, smooth):
        flip = self.m.to_3x3().determinant() < 0
        bv = [self.bm.verts.new(self.m @ Vector(v)) for v in verts]
        mi = self._mi(mat)
        for f in faces:
            idx = list(reversed(f)) if flip else f
            try:
                bf = self.bm.faces.new([bv[i] for i in idx])
            except ValueError:
                continue
            bf.material_index = mi
            bf.smooth = smooth
        return bv

    # primitives -----------------------------------------------------------
    def lathe(self, profile, mat, seg=32, smooth=True, cap_bottom=True, cap_top=True):
        """Revolve (r, z) points (bottom to top) about local Z. r == 0 makes a pole.
        Faces point right of the travel: up a wall faces out, a flat step inward faces up, outward faces down."""
        seg = self._seg(seg, 8)
        verts, rings = [], []
        for r, z in profile:
            if r < 1e-6:
                rings.append([len(verts)])
                verts.append((0, 0, z))
            else:
                rings.append(list(range(len(verts), len(verts) + seg)))
                verts += [(r * math.cos(2 * math.pi * k / seg), r * math.sin(2 * math.pi * k / seg), z)
                          for k in range(seg)]
        faces = []
        for a, b in zip(rings, rings[1:]):
            for k in range(seg):
                k1 = (k + 1) % seg
                if len(a) == 1 and len(b) == 1:
                    continue
                if len(a) == 1:
                    faces.append([a[0], b[k1], b[k]])
                elif len(b) == 1:
                    faces.append([a[k], a[k1], b[0]])
                else:
                    faces.append([a[k], a[k1], b[k1], b[k]])
        if cap_bottom and len(rings[0]) > 1:
            faces.append(rings[0][::-1])
        if cap_top and len(rings[-1]) > 1:
            faces.append(rings[-1])
        return self._add(verts, faces, mat, smooth)

    def sweep(self, path, radius, mat, seg=12, caps=True, smooth=True):
        """Round tube along a 3D polyline; radius may be a float or a per-point list."""
        seg = self._seg(seg, 5)
        P = [Vector(p) for p in path]
        R = radius if hasattr(radius, "__len__") else [radius] * len(P)
        T = []
        for i in range(len(P)):
            a, b = P[max(i - 1, 0)], P[min(i + 1, len(P) - 1)]
            T.append((b - a).normalized())
        up = Vector((0, 0, 1)) if abs(T[0].z) < 0.9 else Vector((1, 0, 0))
        n = T[0].cross(up).cross(T[0]).normalized()
        verts = []
        for i, (p, t) in enumerate(zip(P, T)):
            if i:
                q = T[i - 1].rotation_difference(t)
                n = (q @ n).normalized()
            b = t.cross(n)
            verts += [p + R[i] * (math.cos(2 * math.pi * k / seg) * n + math.sin(2 * math.pi * k / seg) * b)
                      for k in range(seg)]
        faces = []
        for i in range(len(P) - 1):
            for k in range(seg):
                k1 = (k + 1) % seg
                faces.append([i * seg + k, i * seg + k1, (i + 1) * seg + k1, (i + 1) * seg + k])
        if caps:
            faces.append(list(range(seg))[::-1])
            last = (len(P) - 1) * seg
            faces.append(list(range(last, last + seg)))
        return self._add([tuple(v) for v in verts], faces, mat, smooth)

    def extrude(self, poly, z0, z1, mat, smooth_sides=False, top=True, bottom=True):
        """Prism of a CCW 2D polygon (local XY) from z0 to z1."""
        n = len(poly)
        verts = [(x, y, z0) for x, y in poly] + [(x, y, z1) for x, y in poly]
        faces = [[i, (i + 1) % n, n + (i + 1) % n, n + i] for i in range(n)]
        if bottom:
            faces.append(list(range(n))[::-1])
        if top:
            faces.append(list(range(n, 2 * n)))
        return self._add(verts, faces, mat, smooth_sides)

    def loft(self, sections, mat, smooth=True, cap_bottom=True, cap_top=True):
        """Skin stacked 2D sections [(poly, z), ...] (equal point counts, CCW) bottom to top.
        A section list may run up the outside and back down the inside (tubs, planters)."""
        n = len(sections[0][0])
        verts = [(x, y, z) for poly, z in sections for x, y in poly]
        faces = [[i * n + k, i * n + (k + 1) % n, (i + 1) * n + (k + 1) % n, (i + 1) * n + k]
                 for i in range(len(sections) - 1) for k in range(n)]
        if cap_bottom:
            faces.append(list(range(n))[::-1])
        if cap_top:
            last = (len(sections) - 1) * n
            faces.append(list(range(last, last + n)))
        return self._add(verts, faces, mat, smooth)

    def face(self, pts, mat, smooth=False):
        """One polygon from 3D points (CCW seen from its front)."""
        return self._add(pts, [list(range(len(pts)))], mat, smooth)

    def decal(self, pts, mat, uv=((0, 0), (1, 0), (1, 1), (0, 1)), lift=0.002):
        """Quad with explicit UVs (a cell of a decal sheet), lifted along its normal to avoid z-fighting."""
        P = [Vector(p) for p in pts]
        n = (P[1] - P[0]).cross(P[2] - P[0]).normalized() * lift
        bv = self.face([tuple(p + n) for p in P], mat)
        f = bv[0].link_faces[0]  # its verts are fresh, so this is the new face (faces[-1] lies after a bevel)
        f[self._uvkeep] = 1
        self._pending_uv.append((f, uv))

    def sheet(self, fn, nu, nv, mat, smooth=True):
        """(nu x nv) quads through fn(u, v) -> point, u, v in 0..1 (front = u right, v up), keeping (u, v) as UVs."""
        grid = [(i / nu, j / nv) for j in range(nv + 1) for i in range(nu + 1)]
        k = lambda i, j: j * (nu + 1) + i  # noqa: E731
        faces = [[k(i, j), k(i + 1, j), k(i + 1, j + 1), k(i, j + 1)] for j in range(nv) for i in range(nu)]
        bv = self._add([tuple(fn(u, v)) for u, v in grid], faces, mat, smooth)
        uv = dict(zip(bv, grid))
        for f in {f for v in bv for f in v.link_faces}:
            f[self._uvkeep] = 1
            self._pending_uv.append((f, [uv[lp.vert] for lp in f.loops]))
        return bv

    def box(self, mn, mx, mat):
        (x0, y0, z0), (x1, y1, z1) = mn, mx
        return self.extrude([(x0, y0), (x1, y0), (x1, y1), (x0, y1)], z0, z1, mat)

    def rbox(self, mn, mx, r, mat, seg=3):
        """Box with every edge rounded to radius r (bmesh bevel on this box only)."""
        seg = self._seg(seg, 1)
        (x0, y0, z0), (x1, y1, z1) = mn, mx
        r = min(r, (x1 - x0) / 2.01, (y1 - y0) / 2.01, (z1 - z0) / 2.01)
        before = set(self.bm.faces)
        bv = self.box(mn, mx, mat)
        edges = list({e for v in bv for e in v.link_edges})
        bmesh.ops.bevel(self.bm, geom=bv + edges, offset=r, segments=seg, profile=0.5,
                        affect="EDGES", clamp_overlap=True)
        mi = self._mi(mat)
        for f in set(self.bm.faces) - before:
            f.material_index = mi
            f.smooth = True
        return bv

    # output -----------------------------------------------------------------
    def build(self, col=None, bevel=0.005, sharp=38.0, seg=2, parent=None):
        """The root object; parts become children (identity transforms) in the same collection."""
        bm = self.bm
        bm.normal_update()
        uv = bm.loops.layers.uv.new("UVMap")
        for f, cell in self._pending_uv:  # decals keep their sheet UVs
            for lp, c in zip(f.loops, cell):
                lp[uv].uv = c
        for f in bm.faces:  # 1 m world box projection: tiling textures stay metric
            if f[self._uvkeep]:
                continue
            n = f.normal
            ax = max(range(3), key=lambda i: abs(n[i]))
            u, v = [(1, 2), (0, 2), (0, 1)][ax]
            for lp in f.loops:
                if self.up_uv and ax == 2:  # Robots (brief 026 round 2): v is height everywhere so a vertical gradient in a detail map holds on tops too (half of y keeps the grain 2D; 15 cm of ring top moves the gradient by 7 cm)
                    lp[uv].uv = (lp.vert.co[0], lp.vert.co[2] + 0.5 * lp.vert.co[1])
                else:
                    lp[uv].uv = (lp.vert.co[u], lp.vert.co[v])
        me = bpy.data.meshes.new(self.name)
        bm.to_mesh(me)
        bm.free()
        for m in self.mats:
            me.materials.append(m)
        me.set_sharp_from_angle(angle=math.radians(sharp))
        obj = bpy.data.objects.new(self.name, me)
        (col or bpy.context.scene.collection).objects.link(obj)
        obj.parent = parent
        if bevel > 0:
            b = obj.modifiers.new("EdgeSoften", "BEVEL")
            b.width, b.segments = bevel, seg
            b.limit_method, b.angle_limit = "ANGLE", math.radians(sharp)
            b.harden_normals = True
            b.miter_outer = "MITER_ARC"
        wn = obj.modifiers.new("WeightedNormal", "WEIGHTED_NORMAL")
        wn.keep_sharp = True
        wn.weight = 50
        for p in self.parts.values():
            p.build(col, bevel, sharp, seg, parent=obj)
        return obj
