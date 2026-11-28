"""Original modular postal concourse, with reductions designed around its silhouette."""

import math
import random


def opening(s, x, w, art, sign):
    # A shaded arcade, an actual sheltered recess and a service bench in front of each room plate.
    s.box(x, 4.1, 0, w, 1.4, 4.25, "flutes")
    s.panel(x, 3.385, 0.54, w - 0.55, 2.87, art)
    s.box(x, 2.94, 0.22, w - 0.40, 0.78, 0.49, "teal")
    s.rounded(x, 2.9, 0.69, w - 0.30, 1.0, 0.13, 0.09, "bronze")
    s.panel(x, 3.37, 3.47, w - 0.46, 0.52, sign)
    if s.level < 2:
        for xx in (-w * 0.46, w * 0.46):
            s.box(x + xx, 2.03, 0.02, 0.27, 2.75, 3.52, "limestone")
        for dx in (-w * 0.46, w * 0.46):
            s.box(x + dx, 3.19, 0.72, 0.14, 0.37, 2.7, "bronze")
        s.box(x, 3.17, 3.36, w - 0.44, 0.40, 0.13, "bronze")
        if s.level == 0:
            s.box(x + 0.21 * w, 3.06, 0.81, 0.055, 0.12, 2.54, "nickel")
            s.box(x - 0.26 * w, 2.63, 0.82, 0.55, 0.30, 0.37, "rattan")
            for dx in (-0.30, -0.12, 0.07, 0.25):
                s.box(x + dx * w, 3.04, 0.84, 0.035, 0.11, 0.92, "bronze")
            s.box(x, 3.01, 1.77, w - 0.46, 0.11, 0.045, "bronze")


def vault(s, art="room0", sign="dispatch"):
    # Six-metre shell. Each web meets the spring course at the same level as its neighbour.
    w = 6.0
    n = (22, 12, 6)[s.level]
    z0 = 3.55
    arch = lambda t: 6.25 * math.sin(math.pi * t) ** 0.79

    def curve(y, level, offset=0):
        return [
            (-w / 2 + offset + (w - 2 * offset) * i / n, y, z0 + level + arch(i / n)) for i in range(n + 1)
        ]

    def sheet(y1, y2, top, thick, outer, inner, edge):
        a, b = curve(y1, top), curve(y2, top + 0.17)
        c, d = curve(y1, top - thick), curve(y2, top + 0.17 - thick)
        for i in range(n):
            j = i + 1
            s.face([a[i], a[j], b[j], b[i]], outer)
            s.face([d[i], d[j], c[j], c[i]], inner)
            s.face([c[i], c[j], a[j], a[i]], edge)
            s.face([b[i], b[j], d[j], d[i]], edge)
        for i in (0, n):
            points = [a[i], b[i], d[i], c[i]]
            s.face(points if i == 0 else points[::-1], edge)

    sheet(-1.35, 6.5, 0, 0.24, "jade", "limestone", "ivory")
    sheet(-1.58, -1.32, 0.10, 0.42, "ivory", "bronze", "limestone")
    # Rooflights follow the actual sampled shell. Shallow raised laps catch the sun at squad height.
    first, last = max(1, round(n * 0.30)), min(n - 1, round(n * 0.70))

    def roof_point(i, y, lift):
        return (-3 + 6 * i / n, y, z0 + arch(i / n) + 0.17 * (y + 1.35) / 7.85 + lift)

    def roof_band(lo, hi, y1, y2, lift, finish):
        for i in range(lo, hi):
            s.face(
                [
                    roof_point(i, y1, lift),
                    roof_point(i + 1, y1, lift),
                    roof_point(i + 1, y2, lift),
                    roof_point(i, y2, lift),
                ],
                finish,
            )

    roof_band(first, last, 0.42, 5.67, 0.035, "ribglass")
    for y in (0.43, 1.76, 3.08, 4.40, 5.60) if s.level < 2 else (0.43, 3.08, 5.60):
        roof_band(first, last, y, y + 0.09, 0.055, "nickel")
    if s.level < 2:
        for i in (first, last):
            s.beam(roof_point(i, 0.41, 0.044), roof_point(i, 5.68, 0.044), 0.039, "bronze", 6)
    if s.level < 2:
        sheet(-1.61, -1.58, -0.012, 0.051, "bronze", "bronze", "bronze")
        sheet(1.28, 1.45, -0.20, 0.19, "bronze", "bronze", "ivory")
        for k in (-2.73, 2.73):
            s.beam((k, 0.63, 0.23), (k, -1.26, 3.58), 0.13, "bronze", end=0.19)
            s.beam((k, 0.63, 0.03), (k, 0.63, 3.75), 0.19, "flutes", end=0.125)
        if s.level == 0:
            for i in (4, 10, 16):
                a = -3 + 6 * i / n
                z = z0 + arch(i / n) - 0.28
                s.beam((a, 0.2, z), (a, 3.15, z + 0.08), 0.058, "lamp", 8)
    # Rear clerestory sits behind the shadow line. Painted reed-glass panels have dimensional mullions.
    for i in range(n):
        j = i + 1
        a = -3 + 6 * i / n
        b = -3 + 6 * j / n
        za = z0 + arch(i / n) - 0.23
        zb = z0 + arch(j / n) - 0.23
        if max(za, zb) > 4.06:
            s.face(
                [(a, 4.8, 4.03), (b, 4.8, 4.03), (b, 4.8, max(4.04, zb)), (a, 4.8, max(4.04, za))], "ribglass"
            )
    if s.level < 2:
        for a in (-2, -1, 0, 1, 2):
            h = z0 + arch((a + 3) / 6) - 4
            if h > 0.2:
                s.box(a, 4.62, 4.03, 0.075, 0.21, h, "bronze")
        s.box(0, 4.61, 5.83, 5.44, 0.19, 0.08, "bronze")
    opening(s, 0, 6, art, sign)
    s.box(0, 1.45, 3.72, 6, 0.52, 0.38, "teal")
    s.box(0, 1.15, 3.71, 6, 0.12, 0.063, "bronze")
    s.box(0, 6.1, 0, 6, 2.35, 4.23, "limestone")


def rotunda(s):
    r = 4.55
    n = (32, 20, 12)[s.level]
    loop = [(r * math.cos(a * math.tau / n), r * math.sin(a * math.tau / n)) for a in range(n)]
    s.solid(loop, 0, 0.46, "limestone")
    for i in range(n):
        a = math.tau * (i + 0.5) / n
        w = 2 * r * math.sin(math.pi / n)
        dist = r * math.cos(math.pi / n)
        with s.at(dist * math.cos(a), dist * math.sin(a), 0, a + math.pi / 2):
            room = ["room1", "room3", "room2", "room0"][(i // max(1, n // 8)) % 4]
            across = max(1, n // 8)
            u0 = (i % across) / across
            u1 = (i % across + 1) / across
            s.face(
                [(-w / 2, 0.08, 0.43), (w / 2, 0.08, 0.43), (w / 2, 0.08, 3.21), (-w / 2, 0.08, 3.21)],
                room,
                [(u0, 0), (u1, 0), (u1, 1), (u0, 1)],
            )
            if s.level == 0 and i % 2 == 0:
                s.box(-w / 2, 0.015, 0.41, 0.10, 0.17, 2.86, "bronze")
            for z in (4.1, 6.86):
                s.face(
                    [(-w / 2, 0.03, z), (w / 2, 0.03, z), (w / 2, 0.03, z + 1.71), (-w / 2, 0.03, z + 1.71)],
                    "window",
                    [(u0, 0), (u1, 0), (u1, 1), (u0, 1)],
                )
                if s.level == 0 and i % 2 == 0:
                    s.box(0, -0.02, z, 0.085, 0.15, 1.73, "ivory")
    for z, rad, h, mat in [
        (3.24, 5.53, 0.25, "bronze"),
        (3.49, 5.62, 0.32, "jade"),
        (3.81, 4.62, 0.32, "ivory"),
        (5.8, 4.67, 1.1, "limestone"),
        (8.55, 4.72, 0.42, "ivory"),
        (8.97, 4.56, 0.15, "bronze"),
    ]:
        rim = [(x * rad / r, y * rad / r) for x, y in loop]
        s.solid(rim, z, z + h, mat, "roof")
    if s.level < 2:
        for z in (3.53, 3.70, 6.0, 8.68):
            rad = 5.65 if z < 4 else 4.72
            s.solid([(x * rad / r, y * rad / r) for x, y in loop], z, z + 0.048, "nickel")
    s.rounded(0, 0.2, 9.1, 4.95, 4.8, 0.72, 1.10, "teal", "roof")
    # Raised round lantern over the public rotunda. Its glass and ribs carry through to the overhead view.
    segments = (24, 16, 8)[s.level]
    ring = [
        (2.04 * math.cos(i * math.tau / segments), 0.20 + 2.04 * math.sin(i * math.tau / segments))
        for i in range(segments)
    ]
    s.solid(ring, 9.81, 10.01, "bronze")
    for i, (x, y) in enumerate(ring):
        nx, ny = ring[(i + 1) % segments]
        s.face(
            [
                (x, y, 10.01),
                (nx, ny, 10.01),
                (nx * 0.285, 0.20 + (ny - 0.20) * 0.285, 10.86),
                (x * 0.285, 0.20 + (y - 0.20) * 0.285, 10.86),
            ],
            "ribglass",
        )
        if i % 2 == 0 and s.level < 2:
            s.beam((x, y, 10.03), (x * 0.285, 0.20 + (y - 0.20) * 0.285, 10.88), 0.037, "nickel", 5)
    s.beam((0, 0.20, 10.83), (0, 0.20, 11.03), 0.585, "bronze", segments, end=0.52)


def tower(s):
    for z, w, d, h, r, m in [
        (0, 7, 7, 0.45, 0.48, "limestone"),
        (0.45, 6.55, 6.5, 12.3, 0.55, "limestone"),
        (12.75, 5.85, 5.9, 5.45, 0.60, "ivory"),
        (18.2, 4.65, 4.9, 4.1, 0.44, "flutes"),
        (22.3, 3.55, 3.8, 2.55, 0.40, "ivory"),
        (24.85, 2.25, 2.65, 1.3, 0.32, "jade"),
    ]:
        s.rounded(0, 0, z, w, d, h, r, m)
    for side in range(4):
        with s.at(heading=side * math.pi / 2):
            s.box(0, -3.22, 0.55, 2.92, 0.20, 10.15, "teal")
            if s.level < 2:
                for x in [-0.96, -0.48, 0, 0.48, 0.96]:
                    s.box(x, -3.38, 2.0, 0.11, 0.23, 8.22, "bronze")
            s.panel(0, -3.36, 10.75, 2.75, 1.62, "meridian")
            for x in (-1.85, 1.85):
                s.rounded(x, -2.91, 1, 0.44, 1.02, 16.78, 0.14, "flutes")
            s.box(0, -2.88, 13.7, 3.04, 0.21, 3.05, "teal")
            n = (36, 24, 14)[s.level]
            s.beam((0, -3.10, 15.22), (0, -3.32, 15.22), 1.36, "bronze", n)
            s.disc(0, -3.325, 15.22, 1.25, "clock", n)
            for x in (-1.05, 0, 1.05):
                s.box(x, -2.42, 18.7, 0.2, 0.21, 3.13, "jade")
            s.box(0, -1.85, 22.65, 0.76, 0.16, 1.68, "ribglass")
    s.beam((0, 0, 26.1), (0, 0, 31.5), 0.22, "bronze", end=0.037)
    for i in range(8):
        a = i * math.pi / 4
        s.beam((0, 0, 27.2), (1.13 * math.cos(a), 1.13 * math.sin(a), 28.6), 0.066, "bronze", 6, end=0.025)
    s.beam((0, 0, 27.6), (0, 0, 28.7), 0.38, "lamp", 12, end=0.18)


def residence(s, room="room2"):
    s.box(0, 4, 0, 6, 8, 10.25, "limestone")
    opening(s, 0, 5.95, "room2", "tickets")
    s.panel(0, -0.035, 0.46, 5.39, 2.65, room)
    if s.level < 2:
        for dx in (-1.95, -0.55, 1.25):
            s.box(dx, -0.095, 0.42, 0.09, 0.23, 2.67, "bronze")
    s.rounded(0, -0.24, 3.15, 6, 1.6, 0.24, 0.36, "teal")
    for z in (4.2, 7.0):
        s.panel(0, -0.026, z, 5.31, 1.92, "window")
        s.box(0, -0.15, z - 0.21, 5.65, 0.56, 0.2, "ivory")
        for x in (-1.47, 0, 1.47) if s.level < 2 else (0,):
            s.box(x, -0.07, z, 0.105, 0.19, 1.94, "bronze")
    s.rounded(0, 4, 10.25, 6.2, 8.2, 0.24, 0.24, "ivory", "roof")
    if s.level < 2:
        s.box(-2.75, -0.18, 3.46, 0.36, 0.48, 6.77, "flutes")
        s.box(2.75, -0.18, 3.46, 0.36, 0.48, 6.77, "flutes")


def dispatch_end(s):
    # Fits the exposed right end of an 8 m deep bay. Rotate the socket for a left-hand return.
    with s.at(3.025, 4, 0, math.pi / 2):
        s.box(0, -0.05, 0.08, 7.80, 0.13, 1.12, "faience")
        s.box(0, -0.12, 1.17, 7.82, 0.15, 0.085, "bronze")
        s.box(-1.85, -0.105, 0.21, 2.94, 0.15, 4.15, "teal")
        for x in (-2.51, -1.18):
            s.box(x, -0.203, 0.27, 1.26, 0.08, 2.81, "bronze")
            s.panel(x, -0.25, 0.46, 1.06, 1.02, "teal")
            s.panel(x, -0.25, 1.55, 1.06, 1.34, "ribglass")
            if s.level < 2:
                s.box(x + 0.36, -0.32, 1.04, 0.067, 0.12, 0.57, "nickel")
        s.panel(-1.85, -0.19, 3.12, 2.61, 1.01, "dispatch")
        s.rounded(-1.83, -0.67, 4.29, 3.92, 1.66, 0.21, 0.31, "terracotta", "jade")
        s.box(-1.83, -1.48, 4.32, 3.37, 0.042, 0.051, "nickel")
        if s.level < 2:
            for x in (-3.36, -0.37):
                s.beam((x, -0.14, 5.15), (x, -1.29, 4.53), 0.034, "bronze", 6)
        s.box(1.92, -0.11, 1.34, 2.56, 0.22, 3.60, "bronze")
        s.panel(1.92, -0.228, 1.45, 2.33, 3.38, "poster")
        for z in (5.75, 8.24):
            s.box(0, -0.07, z - 0.21, 7.37, 0.20, 0.18, "ivory")
            s.panel(0.05, -0.049, z, 6.96, 1.25, "window")
            if s.level < 2:
                for x in (-2.15, 0.12, 2.38):
                    s.box(x, -0.09, z, 0.085, 0.16, 1.26, "bronze")
        # The return is a pneumatic exchange. Pair of pipes terminate in a fin visible over the awning.
        s.box(0.18, -0.19, 1.22, 0.53, 0.41, 8.68, "terracotta")
        s.box(0.18, -0.45, 1.27, 0.11, 0.10, 8.53, "bronze")
        s.disc(0.18, -0.47, 6.84, 0.60, "stamp", (24, 16, 10)[s.level])
        if s.level < 2:
            for x in (3.43, 3.69):
                s.beam((x, -0.18, 0.19), (x, -0.18, 9.97), 0.082, "bronze", 7)
                for z in (1.1, 4.2, 7.42):
                    s.box(x, -0.18, z, 0.23, 0.23, 0.081, "nickel")


def blade(s):
    s.rounded(0, 0, 0, 0.90, 0.66, 0.15, 0.13, "limestone")
    s.rounded(0, 0, 0.15, 0.74, 0.51, 1.48, 0.13, "jade")
    s.box(0, -0.273, 0.82, 0.53, 0.10, 0.27, "bronze")
    s.panel(0, -0.326, 0.835, 0.45, 0.22, "closed")
    s.disc(0, -0.269, 1.34, 0.21, "stamp", 24)
    s.rounded(0, 0, 1.63, 0.87, 0.61, 0.13, 0.17, "bronze")
    s.beam((0, 0, 1.76), (0, 0, 1.94), 0.15, "lamp", 14, end=0.10)


def skyline(s):
    for z, w, d, h in [(0, 11, 10, 13), (13, 8.6, 8.5, 6.8), (19.8, 6, 6.2, 4.6), (24.4, 3.9, 4, 4.2)]:
        s.rounded(0, 0, z, w, d, h, 0.82, "limestone", "roof")
        for i in range(4):
            with s.at(heading=i * math.pi / 2):
                wide, deep = (w, d) if i % 2 == 0 else (d, w)
                s.panel(0, -deep / 2 - 0.014, z + 0.55, wide * 0.52, h - 0.85, "window")
                for dx in (-1, 1):
                    s.box(dx * wide * 0.23, -deep / 2 - 0.04, z + 0.45, 0.22, 0.22, h - 0.57, "ivory")
    s.beam((0, 0, 28.6), (0, 0, 33.0), 0.19, "bronze", end=0.03)


def bench(s):
    for x in (-0.70, 0.70):
        s.box(x, 0, 0.015, 0.13, 0.51, 0.40, "teal")
    for y in (-0.21, -0.07, 0.07, 0.21):
        s.rounded(0, y, 0.42, 1.90, 0.117, 0.075, 0.036, "rattan")
    for x in (-0.76, 0.76):
        s.beam((x, 0.17, 0.45), (x, 0.31, 0.98), 0.048, "bronze", 8)
    for z in (0.69, 0.85):
        s.box(0, 0.25, z, 1.88, 0.087, 0.108, "rattan")


def marquee(s):
    s.rounded(0, 0, 0, 10.4, 2, 0.19, 0.6, "bronze")
    s.rounded(0, 0, 0.19, 10.48, 2.05, 0.89, 0.63, "teal")
    s.rounded(0, 0, 1.08, 10.54, 2.1, 0.085, 0.62, "bronze")
    if s.level < 2:
        for x in (-3.6, -1.8, 0, 1.8, 3.6):
            s.beam((x, -0.54, -0.06), (x, -0.54, 0), 0.11, "lamp", 12)
    for x in (-3.05, 3.05):
        s.beam((x, 0.62, 0.86), (x, 1.85, 1.83), 0.085, "bronze", 6)
    # The baked face is the far counterpart of the extruded channel letters mounted here.
    if s.level == 2:
        s.panel(0, -1.034, 0.22, 8.6, 0.80, "air")


def trolley(s):
    s.rounded(0, 0, 0.34, 1.74, 0.80, 0.12, 0.105, "teal")
    for x in (-0.56, 0.58):
        for y in (-0.315, 0.315):
            s.beam((x, y - 0.048, 0.20), (x, y + 0.048, 0.20), 0.19, "iron", 12)
            if s.level < 2:
                s.beam((x, y - 0.052, 0.20), (x, y + 0.052, 0.20), 0.09, "bronze", 10)
    for y in (-0.30, 0.30):
        s.beam((0.65, y, 0.43), (0.98, y, 1.18), 0.048, "bronze", 8)
    s.beam((0.98, -0.33, 1.18), (0.98, 0.33, 1.18), 0.057, "teal", 8)
    for x, y, z, w, d, h, skin in [
        (-0.23, 0.01, 0.46, 1.01, 0.65, 0.42, "rattan"),
        (-0.39, -0.03, 0.89, 0.65, 0.54, 0.37, "terracotta"),
        (0.36, 0.02, 0.46, 0.45, 0.56, 0.62, "jade"),
    ]:
        s.rounded(x, y, z, w, d, h, 0.047, skin)
        if s.level < 2:
            for u in (-0.26, 0.26):
                s.box(x + u * w, y, z + 0.004, 0.034, d + 0.013, h + 0.004, "bronze")
            s.panel(x, y - d / 2 - 0.007, z + h * 0.28, w * 0.30, h * 0.41, "closed")


def planter(s):
    rng = random.Random(434)
    s.rounded(0, 0, 0, 3.75, 1.23, 0.22, 0.52, "flutes")
    s.rounded(0, 0, 0.22, 3.86, 1.34, 0.15, 0.59, "ivory")
    s.rounded(0, 0, 0.37, 3.60, 1.12, 0.024, 0.49, "iron")
    for i in range((34, 21, 10)[s.level]):
        x = rng.uniform(-1.45, 1.45)
        y = rng.uniform(-0.31, 0.31)
        a = rng.uniform(0, math.tau)
        length = rng.uniform(0.27, 0.55)
        for j in range((7, 5, 3)[s.level]):
            t = a + j * 2.40
            c = math.cos(t)
            v = math.sin(t)
            w = rng.uniform(0.036, 0.091)
            h = rng.uniform(0.18, 0.48)
            l = length * rng.uniform(0.55, 1.15)
            base = (x, y, 0.39)
            left = (x + c * l * 0.53 - v * w, y + v * l * 0.53 + c * w, 0.43 + h)
            right = (x + c * l * 0.53 + v * w, y + v * l * 0.53 - c * w, 0.43 + h)
            tip = (x + c * l, y + v * l, 0.42 + h * 0.64)
            mid = (x + c * l * 0.56, y + v * l * 0.56, 0.48 + h)
            for pts, uv in [
                ([base, right, mid], [(0.5, 0), (1, 0.48), (0.5, 0.5)]),
                ([right, tip, mid], [(1, 0.48), (0.5, 1), (0.5, 0.5)]),
                ([tip, left, mid], [(0.5, 1), (0, 0.48), (0.5, 0.5)]),
                ([left, base, mid], [(0, 0.48), (0.5, 0), (0.5, 0.5)]),
            ]:
                s.face(pts, "foliage", uv)
        if i % 3:
            z = 0.66 + rng.random() * 0.22
            for k in range((5, 4, 3)[s.level]):
                t = a + math.tau * k / (5 if s.level == 0 else (4 if s.level == 1 else 3))
                c = math.cos(t)
                v = math.sin(t)
                s.face(
                    [
                        (x, y, z),
                        (x + c * 0.085 + v * 0.042, y + v * 0.085 - c * 0.042, z + 0.05),
                        (x + c * 0.124, y + v * 0.124, z + 0.041),
                        (x + c * 0.085 - v * 0.042, y + v * 0.085 + c * 0.042, z + 0.05),
                    ],
                    "bloom",
                )


PARTS = {
    "Vault": vault,
    "SortingVault": lambda s: vault(s, "room3", "post"),
    "BookingVault": lambda s: vault(s, "room1", "tickets"),
    "Rotunda": rotunda,
    "ClockTower": tower,
    "RibbonBay": residence,
    "DispatchBay": lambda s: residence(s, "room1"),
    "LetterBox": blade,
    "Spire": skyline,
    "Bench": bench,
    "Marquee": marquee,
    "ParcelTrolley": trolley,
    "FlowerTrough": planter,
    "DispatchEnd": dispatch_end,
}
