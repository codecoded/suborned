"""Metre-scale Blender geometry. +Y runs into the frontage, −Y faces the street."""

import math
from contextlib import contextmanager
from mathutils import Matrix, Vector
import bpy
import bmesh

NAMES = (
    "limestone terracotta jade nickel bronze charcoal ivory teal "
    "flutes tile rattan ribglass window foliage roof lamp "
    "stamp clock post air tickets dispatch meridian closed "
    "room0 room1 room2 room3 poster faience bloom iron"
).split()


class Part:
    def __init__(self, name, level, material):
        self.name = name
        self.level = level
        self.material = material
        self.points = []
        self.faces = []
        self.coords = []
        self.service_coords = []
        self.frame = Matrix.Identity(4)

    @contextmanager
    def at(self, x=0, y=0, z=0, heading=0):
        was = self.frame.copy()
        self.frame = was @ Matrix.Translation((x, y, z)) @ Matrix.Rotation(heading, 4, "Z")
        try:
            yield
        finally:
            self.frame = was

    def face(self, points, finish, uv=None):
        if len(points) < 3:
            return
        start = len(self.points)
        pts = [Vector(p) for p in points]
        self.points.extend(tuple(self.frame @ p) for p in pts)
        self.faces.append(tuple(range(start, start + len(pts))))
        if uv is None:
            if len(pts) == 4:
                uv = [(0, 0), (1, 0), (1, 1), (0, 1)]
            elif len(pts) == 3:
                uv = [(0, 0), (1, 0), (0.5, 1)]
            else:
                span = [max(p[j] for p in pts) - min(p[j] for p in pts) for j in range(3)]
                a, b = sorted(sorted(range(3), key=lambda k: -span[k])[:2])
                uv = [
                    (
                        (p[a] - min(q[a] for q in pts)) / max(0.001, span[a]),
                        (p[b] - min(q[b] for q in pts)) / max(0.001, span[b]),
                    )
                    for p in pts
                ]
        ix = NAMES.index(finish)
        col, row = ix % 8, ix // 8
        self.coords.extend(
            [((col + 0.021 + u * 0.958) / 8, 1 - (row + 0.979 - v * 0.958) / 4) for u, v in uv]
        )
        service = [(0., 1.)] * len(pts)
        if self.name.startswith("LetterBox") and finish == "jade" and max(p.y for p in pts) < -.251:
            service = [(p.x / .74 + .5, (p.z - .15) / 1.48) for p in pts]
        if "Vault" in self.name and finish == "bronze" and all(.68 < p.z < .84 and 2.38 < p.y < 3.42 for p in pts):
            service = [(max(.001, min(.999, .5 + p.x / 6.)), min(1., max(0., p.y - 2.4))) for p in pts]
        self.service_coords.extend(service)

    def solid(self, loop, low, high, finish, top=None):
        bot = [(x, y, low) for x, y in loop]
        cap = [(x, y, high) for x, y in loop]
        self.face(bot[::-1], finish)
        self.face(cap, top or finish)
        for i, a in enumerate(bot):
            j = (i + 1) % len(bot)
            self.face([a, bot[j], cap[j], cap[i]], finish)

    def box(self, x, y, z, w, d, h, finish):
        self.solid(
            [(x - w / 2, y - d / 2), (x + w / 2, y - d / 2), (x + w / 2, y + d / 2), (x - w / 2, y + d / 2)],
            z,
            z + h,
            finish,
        )

    def rounded(self, x, y, z, w, d, h, r, finish, top=None):
        count = (6, 4, 2)[self.level]
        r = min(r, w / 2 - 0.003, d / 2 - 0.003)
        if r < 0.01:
            return self.box(x, y, z, w, d, h, finish)
        loop = []
        for i, (cx, cy) in enumerate(
            (
                (w / 2 - r, -d / 2 + r),
                (w / 2 - r, d / 2 - r),
                (-w / 2 + r, d / 2 - r),
                (-w / 2 + r, -d / 2 + r),
            )
        ):
            for j in range(count + 1):
                a = (i - 1 + j / count) * math.pi / 2
                loop.append((x + cx + r * math.cos(a), y + cy + r * math.sin(a)))
        self.solid(loop, z, z + h, finish, top)

    def panel(self, x, y, z, w, h, finish):
        self.face(
            [(x - w / 2, y, z), (x + w / 2, y, z), (x + w / 2, y, z + h), (x - w / 2, y, z + h)], finish
        )

    def beam(self, a, b, r, finish, n=None, end=None):
        a, b = Vector(a), Vector(b)
        v = (b - a).normalized()
        n = n or (10, 7, 5)[self.level]
        t = v.cross(Vector((0, 0, 1)) if abs(v.z) < 0.85 else Vector((0, 1, 0))).normalized()
        s = v.cross(t).normalized()
        end = r if end is None else end
        ring = [t * math.cos(i * math.tau / n) + s * math.sin(i * math.tau / n) for i in range(n)]
        ra = [a + r * j for j in ring]
        rb = [b + end * j for j in ring]
        self.face(ra[::-1], finish)
        self.face(rb, finish)
        for i in range(n):
            j = (i + 1) % n
            self.face([ra[i], ra[j], rb[j], rb[i]], finish)

    def disc(self, x, y, z, r, finish, n=32):
        # A front elevation disk with a planar print, facing down the local Y axis.
        a = [(math.cos(i * math.tau / n), math.sin(i * math.tau / n)) for i in range(n)]
        self.face(
            [(x + r * c, y, z + r * s) for c, s in a], finish, [(0.5 + 0.5 * c, 0.5 + 0.5 * s) for c, s in a]
        )

    def make(self):
        mesh = bpy.data.meshes.new(self.name)
        mesh.from_pydata(self.points, [], self.faces)
        mesh.materials.append(self.material)
        uv = mesh.uv_layers.new(name="Surface")
        for elem, coord in zip(uv.data, self.coords):
            elem.uv = coord
        service = mesh.uv_layers.new(name="Postal service")
        for elem, coord in zip(service.data, self.service_coords):
            elem.uv = coord
        mesh.uv_layers.active_index = 0
        uv.active_render = True
        editable = bmesh.new()
        editable.from_mesh(mesh)
        bmesh.ops.triangulate(editable, faces=list(editable.faces))
        editable.to_mesh(mesh)
        editable.free()
        mesh.update()
        mesh.calc_loop_triangles()
        ob = bpy.data.objects.new(self.name, mesh)
        bpy.context.collection.objects.link(ob)
        return ob, len(mesh.loop_triangles)
