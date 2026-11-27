"""Lantern shop construction. Local -Y faces the street. Dimensions are in metres."""

from contextlib import contextmanager
from math import atan2, cos, hypot, pi, sin, tau
from pathlib import Path
import sys
from mathutils import Matrix, Vector

sys.path.insert(0, str(Path(__file__).resolve().parents[1] / "district"))
from ferry import Model
from atlas_layout import atlas_uv


class Building(Model):
    def __init__(self, name, rank):
        super().__init__(name, rank)
        self.transform = Matrix.Identity(4)
        self.count = (8, 4, 2)[rank]

    @contextmanager
    def place(self, x, y, heading=0, z=0):
        parent = self.transform
        self.transform = parent @ Matrix.Translation((x, y, z)) @ Matrix.Rotation(heading, 4, "Z")
        try:
            yield
        finally:
            self.transform = parent

    def face(self, points, slot, uv=None, smooth=False):
        if not isinstance(slot, str):
            slot = "shadow"  # underside of a stock profile
        if uv is None:
            uv = (
                [(0, 0), (1, 0), (1, 1), (0, 1)]
                if len(points) == 4
                else [
                    (0.5 + 0.48 * cos(tau * i / len(points)), 0.5 + 0.48 * sin(tau * i / len(points)))
                    for i in range(len(points))
                ]
            )
        idx = []
        for p in points:
            key = tuple(round(x, 5) for x in self.transform @ Vector(p))
            if key not in self.indices:
                self.indices[key] = len(self.verts)
                self.verts.append(key)
            idx.append(self.indices[key])
        if len(set(idx)) < 3:
            return
        self.faces.append(idx)
        self.smooth.append(smooth)
        self.uv.extend(atlas_uv(slot, u, v) for u, v in uv)

    def box(self, x, y, z, w, d, h, finish):
        a, b = x - w / 2, x + w / 2
        c, e = y - d / 2, y + d / 2
        rim = [(a, c), (b, c), (b, e), (a, e)]
        for i, p in enumerate(rim):
            q = rim[(i + 1) % 4]
            self.face([(*p, z), (*q, z), (*q, z + h), (*p, z + h)], finish)
        self.face([(*p, z + h) for p in rim], finish)
        self.face([(*p, z) for p in reversed(rim)], finish)

    def panel(self, left, right, y, low, high, finish, uv=(0, 0, 1, 1)):
        u, v, r, t = uv
        self.face(
            [(left, y, low), (right, y, low), (right, y, high), (left, y, high)],
            finish,
            [(u, v), (r, v), (r, t), (u, t)],
        )

    def rim(self, w, d, r, h0, h1, slot):
        loop = self.contour(0, 0, w, d, r, self.count)
        for i, p in enumerate(loop):
            q = loop[(i + 1) % len(loop)]
            self.face([(*p, h0), (*q, h0), (*q, h1), (*p, h1)], slot)


GOODS = [
    ("bureau", "exchange_name", "air_poster"),
    ("flowers", "flower_name", "sea_poster"),
    ("produce", "market_name", "sea_poster"),
    ("studio", "passage_name", "air_poster"),
]


def shop(s, w, kind, door=True):
    display, sign, poster = GOODS[kind]
    half = w / 2
    # The opaque display stands inside a real reveal. Side returns remain visible in a grazing view.
    s.box(0, 0.38, 0.08, w, 0.59, 0.29, "plinth")
    s.panel(-half + 0.12, half - 0.12, 0.59, 0.37, 3.09, display)
    for x, hand in [(-half + 0.13, 1), (half - 0.13, -1)]:
        s.face([(x, 0.59, 0.37), (x, 0, 0.37), (x, 0, 3.12), (x, 0.59, 3.12)][::hand], "bronze")
        s.box(x, 0, 0.10, 0.24, 0.32, 3.35, "stone")
    s.box(0, 0.21, 0.34, w - 0.17, 0.62, 0.11, "nickel")
    s.box(0, 0.23, 3.05, w - 0.16, 0.76, 0.13, "shadow")
    s.panel(-half + 0.16, half - 0.16, -0.073, 3.16, 3.79, sign)
    for h in (3.13, 3.80):
        s.box(0, -0.012, h, w, 0.17, 0.045, "bronze")
    doorwidth = min(1.38, w * 0.32)
    if door:
        centre = -w * 0.14
        for x in (centre - doorwidth / 2, centre + doorwidth / 2):
            s.box(x, 0.20, 0.09, 0.075, 0.29, 2.82, "bronze")
        s.panel(centre - doorwidth / 2 + 0.02, centre + doorwidth / 2 - 0.02, 0.19, 0.15, 2.78, "glass")
        s.box(centre, 0.12, 0.12, 0.046, 0.12, 2.68, "nickel")
        s.box(centre, 0.09, 0.13, doorwidth, 0.14, 0.22, "bronze")
        s.box(centre, 0.10, 2.77, doorwidth, 0.16, 0.12, "bronze")
        if s.rank == 0:
            for x in (centre - 0.115, centre + 0.115):
                s.beam((x, 0.018, 1.0), (x, 0.018, 1.38), 0.019, "bronze", 7)
    if s.rank < 2:
        for x in (-w * 0.33, w * 0.32):
            s.box(x, 0.13, 0.37, 0.052, 0.13, 2.66, "nickel")
        s.box(0, 0.12, 2.56, w - 0.24, 0.14, 0.052, "bronze")
        if w > 3.2:
            x = w * 0.34
            s.box(x, 0.02, 0.49, 0.70, 0.075, 1.14, "bronze")
            s.panel(x - 0.30, x + 0.30, -0.026, 0.55, 1.57, poster)
    if s.rank == 0:
        for x in (-half + 0.11, half - 0.11):
            s.box(x, -0.16, 0.16, 0.32, 0.29, 0.41, "plinth")
            s.box(x, -0.12, 3.44, 0.35, 0.33, 0.35, "cream")


def upper(s, w, z, h, kind):
    # Three tall lights between stone jambs. Bronze balcony rails stand in front of the window.
    l, r = -w / 2, w / 2
    s.panel(l + 0.07, r - 0.07, 0.31, z + 0.08, z + h - 0.10, "glass" if kind % 3 == 0 else "shade")
    for x in (l + 0.12, r - 0.12):
        s.box(x, 0.045, z, 0.25, 0.52, h, "stone")
    s.box(0, 0.05, z, w, 0.54, 0.18, "cream")
    s.box(0, 0.04, z + h - 0.18, w, 0.58, 0.18, "cream")
    for x in (-w / 6, w / 6):
        s.box(x, 0.075, z + 0.15, 0.075, 0.22, h - 0.3, "bronze")
    s.box(0, 0.065, z + h * 0.67, w - 0.28, 0.17, 0.056, "bronze")
    if s.rank < 2:
        s.box(0, -0.09, z + 0.08, w + 0.06, 0.70, 0.09, "cream")
        s.beam((l + 0.18, -0.31, z + 0.64), (r - 0.18, -0.31, z + 0.64), 0.025, "bronze", 5)
        for x in (l + 0.21, 0, r - 0.21):
            s.box(x, -0.30, z + 0.12, 0.035, 0.035, 0.51, "nickel")


def marquee(s, width):
    # A crescent cantilever. The fascia, rolled lip and perforated underside follow the same curve.
    near = (20, 12, 6)[s.rank]
    half = width * 0.5
    points = [(-half, 0)]
    for i in range(near + 1):
        a = pi + pi * i / near
        points.append((half * cos(a), -0.48 + 1.58 * sin(a)))
    points.append((half, 0))
    z = 3.94
    s.face([(*p, z) for p in reversed(points)], "soffit")
    for low, high, finish in [
        (z, z + 0.055, "bronze"),
        (z + 0.055, z + 0.30, "wine"),
        (z + 0.30, z + 0.345, "bronze"),
    ]:
        for i, a in enumerate(points):
            b = points[(i + 1) % len(points)]
            s.face([(*a, low), (*b, low), (*b, high), (*a, high)], finish)
    s.face([(*p, z + 0.345) for p in points], "roof")
    if s.rank < 2:
        for x in (-width * 0.28, 0, width * 0.28):
            s.beam((x, -0.95, z - 0.037), (x, -0.95, z - 0.012), 0.135, "opal", 12 if s.rank == 0 else 7)
            s.beam((x, -0.95, z - 0.013), (x, -0.95, z + 0.004), 0.17, "bronze", 12 if s.rank == 0 else 7)


def clock_fin(s, top):
    # The rim and six stepped flutes break the sky line even edge-on; the clock is read from both streets.
    base = 4.12
    for x, h, w in [
        (-1.10, top - 1.38, 0.44),
        (-0.80, top - 0.68, 0.40),
        (-0.45, top - 0.24, 0.42),
        (0, top, 0.57),
        (0.45, top - 0.24, 0.42),
        (0.80, top - 0.68, 0.40),
        (1.10, top - 1.38, 0.44),
    ]:
        s.box(x, 0.17, base, w, 0.61, h - base, "jade" if abs(x) < 1 else "cream")
        if s.rank < 2:
            s.box(x - w * 0.40, -0.155, base + 0.10, 0.035, 0.045, h - base - 0.12, "bronze")
    z = top - 2.04
    n = (36, 22, 12)[s.rank]
    s.beam((0, -0.26, z), (0, -0.14, z), 0.98, "bronze", n)
    s.plate(0, -0.271, z, 1.91, 1.91, -pi / 2, "clock", True)
    if s.rank == 0:
        for a in (-0.41, 0, 0.41):
            s.beam((sin(a) * 1.02, -0.19, z - 0.88), (sin(a) * 1.65, -0.19, z - 2.0), 0.016, "bronze", 5)


def terrace_box(s, x, y, z, length):
    s.box(x, y, z, length, 0.42, 0.24, "terracotta")
    count = (12, 6, 2)[s.rank]
    for j in range(count):
        t = (j + 0.5) / count
        u = x + (t - 0.5) * length * 0.95
        v = y + sin(j * 1.84) * 0.14
        a = j * 2.4
        h = 0.36 + 0.18 * (0.5 + 0.5 * cos(j * 2.6))
        spread = 0.19
        for k in range(3 if s.rank == 0 else 2):
            b = a + k * tau / 3
            dx, dy = cos(b) * spread, sin(b) * spread
            points = [
                (u, v, z + 0.17),
                (u + dx - dy * 0.53, v + dy + dx * 0.53, z + 0.38),
                (u + dx * 2, v + dy * 2, z + 0.21 + h),
                (u + dx + dy * 0.53, v + dy - dx * 0.53, z + 0.37),
            ]
            s.face(points, "petals" if j % 4 == 2 and k == 0 else "leaf")
            s.face(points[::-1], "petals" if j % 4 == 2 and k == 0 else "leaf")


def horizon(s, w, d, rad, floors, hero):
    # At the end of the street only the window rhythm, light shelves and clock profile resolve.
    outline = s.contour(0, 0, w, d, rad, 3, 5.9)
    s.rim(w, d, rad, 0, 0.42, "plinth")
    for floor in range(floors):
        z = 0.42 if floor == 0 else 4.47 + (floor - 1) * 3.32
        top = 4.0 + floor * 3.32
        for i, (a, b) in enumerate(zip(outline, outline[1:] + outline[:1])):
            front = [(*a, z), (*b, z), (*b, top - 0.13), (*a, top - 0.13)]
            s.face(
                front,
                ("bureau", "flowers", "produce", "studio")[i % 4]
                if floor == 0
                else ("shade" if i % 3 else "glass"),
            )
        s.tiers(
            0,
            0,
            [
                (top - 0.14, w + 0.24, d + 0.24, rad + 0.12, "shadow"),
                (top + 0.06, w + 0.36, d + 0.36, rad + 0.18, "cream"),
                (top + 0.50, w, d, rad, "stone"),
            ],
        )
    top = 4 + (floors - 1) * 3.32
    s.shape(0, 0.15, w - 3, d - 3, max(0.75, rad - 1.5), top + 0.49, top + 1.10, "roof")
    with s.place(0, -d / 2):
        marquee(s, 7.5)
        if hero:
            clock_fin(s, top + 2.8)


def building(s, w, d, rad, floors, hero):
    if s.rank == 2:
        horizon(s, w, d, rad, floors, hero)
        return
    h0 = 4.0
    floor_h = 3.32
    top = h0 + (floors - 1) * floor_h
    s.tiers(
        0,
        0,
        [
            (0.0, w, d, rad, "plinth"),
            (0.16, w + 0.09, d + 0.09, rad + 0.045, "plinth"),
            (0.42, w + 0.12, d + 0.12, rad + 0.06, "stone"),
        ],
    )
    s.rim(w - 0.95, d - 0.95, rad - 0.475, 0.34, top, "shadow")
    # Straight working bays snap in whole spans. Rounded returns have separate continuous, narrow lights.
    sides = [
        (0, -d / 2, 0, w - 2 * rad),
        (w / 2, 0, pi / 2, d - 2 * rad),
        (0, d / 2, pi, w - 2 * rad),
        (-w / 2, 0, -pi / 2, d - 2 * rad),
    ]
    for side, (x, y, a, length) in enumerate(sides):
        n = max(1, round(length / 6))
        width = length / n
        for i in range(n):
            offset = (i + 0.5) * width - length / 2
            kind = (i + side) % 4
            if side == 0 and hero:
                kind = (1, 0, 2)[min(i, 2)] if n >= 3 else (0 if i == 0 else 1)
            with s.place(x + cos(a) * offset, y + sin(a) * offset, a):
                shop(s, width, kind, door=i % 2 == 0 or side == 0)
                for level in range(floors - 1):
                    z = h0 + level * floor_h + 0.57
                    upper(s, width, z, floor_h - 0.83, (kind + level) % 4)
                if side == 0 and i == n // 2:
                    marquee(s, width + 1.5)
                    if hero:
                        clock_fin(s, top + 2.8)
                if side in (0, 3) and s.rank < 2 and (i + side) % 2 == 0:
                    terrace_box(s, 0, -0.12, top + 0.51, min(width * 0.66, 3.45))
    for x, y, start in [
        (w / 2 - rad, -d / 2 + rad, -pi / 2),
        (w / 2 - rad, d / 2 - rad, 0),
        (-w / 2 + rad, d / 2 - rad, pi / 2),
        (-w / 2 + rad, -d / 2 + rad, pi),
    ]:
        sections = (6, 4, 3)[s.rank]
        for i in range(sections):
            a = start + pi * i / (2 * sections)
            b = start + pi * (i + 1) / (2 * sections)
            p = (x + rad * cos(a), y + rad * sin(a))
            q = (x + rad * cos(b), y + rad * sin(b))
            span = hypot(q[0] - p[0], q[1] - p[1])
            with s.place((p[0] + q[0]) / 2, (p[1] + q[1]) / 2, atan2(q[1] - p[1], q[0] - p[0])):
                u0 = i / sections
                u1 = (i + 1) / sections
                s.panel(-span / 2, span / 2, 0.06, 0.42, 3.65, "bureau" if hero else "produce", (u0, 0, u1, 1))
                for level in range(floors - 1):
                    z = h0 + level * floor_h + 0.48
                    s.panel(-span / 2, span / 2, 0.06, z, z + floor_h - 0.65, "glass", (u0, 0, u1, 1))
                if i % 2 == 0 or s.rank == 0:
                    s.box(-span / 2, 0.03, 0.4, 0.095, 0.2, top - 0.40, "bronze")
    for level in range(floors):
        z = h0 + level * floor_h
        s.tiers(
            0,
            0,
            [
                (z - 0.22, w + 0.24, d + 0.24, rad + 0.12, "shadow"),
                (z - 0.12, w + 0.43, d + 0.43, rad + 0.215, "cream"),
                (z + 0.03, w + 0.43, d + 0.43, rad + 0.215, "stone"),
                (z + 0.07, w + 0.31, d + 0.31, rad + 0.155, "bronze"),
                (z + 0.52, w + 0.13, d + 0.13, rad + 0.065, "stone"),
            ],
        )
    # A true setback and a finished fifth elevation matter in the command camera.
    s.shape(0, 0.15, w - 3.1, d - 3.1, max(0.8, rad - 1.55), top + 0.43, top + 1.07, "cream")
    s.shape(0, 0.15, w - 3.25, d - 3.25, max(0.75, rad - 1.6), top + 1.07, top + 1.11, "roof")
