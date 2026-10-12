"""Plants (plaza.png planting; props sheet potted palm): coconut palm, fan palm, Italian cypress, clipped
topiary cone, shrub mound, flower-bed filler, potted topiary.

Foliage is alpha-cutout cards (two-sided, alpha-test 0.5 in Unity, no translucency), so the read comes from
card density, a dark hidden core behind dense crowns and bent normals: every card vertex takes a normal blended
from its surface / lobe / crown centre (vesper/foliage.py), so a crown shades as one soft form while the cutout
keeps a leafy edge. Palm fronds are bent strips along a drooping arc, V-folded along the rachis; card UVs follow
the frond measured in the texture so the stem sits at the attachment point.
"""

import math
import random

from mathutils import Matrix, Vector

import mats
from geo import circle, dome, ring
from kit import asset
from mats import linear, pal, register, srgb

register("DK_Bark_Palm", "stone", "#8C7B62")
register("DK_Bark_Fibre", "stone", "#5F4B38")
register("DK_Petiole", "stone", "#7F7A40")
register("DK_Coconut", "stone", "#7D6E34")
register("DK_Leaf_CoreConifer", "stone", "#22382A")  # hidden core: gaps read as shaded depth, not sky
register("DK_Leaf_CoreShrub", "stone", "#222D17")
# olive variant of the (blue-green) cypress sheet: plaza.png conifers are sunlit yellow-olive
register("DK_Leaf_CypressOlive", "cutout", "#EEF0A2")
mats.TEXTURES.setdefault("DK_Leaf_CypressOlive", mats.TEXTURES["DK_Leaf_Cypress"])

Z = Vector((0, 0, 1))
FULL = (0.0, 0.0, 1.0, 1.0)
# feather frond in its texture: rachis base / tip (UV), stations (s, rachis offset, left, right half-width)
FB, FE = Vector((0.146, 0.100)), Vector((0.925, 0.80))
FEATHER = ((0, 0.0, 0.03, 0.03), (1 / 6, 0.07, 0.28, 0.29), (2 / 6, 0.105, 0.33, 0.36), (3 / 6, 0.13, 0.31, 0.375),
           (4 / 6, 0.126, 0.30, 0.35), (5 / 6, 0.08, 0.27, 0.27), (1, 0.04, 0.15, 0.13))
FAN_STEM, FAN_HUB = 0.05, 0.287  # fan texture: petiole end / blade hub (v at u = 0.5)


def tinted(name):
    """Cutout whose LIB colour multiplies its texture, as URP does (_BaseColor x _BaseMap); mats.get links the
    texture straight to Base Color, so the multiply is added here for this module's variants only."""
    m = mats.get(name)
    if not m.get("dk_tint"):
        nt = m.node_tree
        tex = next(n for n in nt.nodes if n.type == "TEX_IMAGE")
        mix = nt.nodes.new("ShaderNodeMix")
        mix.data_type, mix.blend_type = "RGBA", "MULTIPLY"
        sock = {s.identifier: s for s in mix.inputs}
        sock["Factor_Float"].default_value = 1.0
        sock["B_Color"].default_value = (*linear(srgb(mats.LIB[name][1])), 1.0)
        nt.links.new(tex.outputs["Color"], sock["A_Color"])
        out = next(s for s in mix.outputs if s.identifier == "Result_Color")
        nt.links.new(out, nt.nodes["Principled BSDF"].inputs["Base Color"])
        m["dk_tint"] = True
    return m


def rot(v, axis, deg):
    return Matrix.Rotation(math.radians(deg), 3, axis) @ v


def lerp(a, b, t):
    return a + (b - a) * t


class Cards:
    """Explicit-UV foliage quads on a Shape plus per-vertex bent normals. Shape.build has no normal hook, so
    this instance's build is wrapped: after it, the cards' custom normals replace the weighted-normal pass."""

    def __init__(self, s):
        self.s, self.n = s, {}
        build = s.build

        def wrapped(col=None, **kw):
            obj = build(col, **kw)
            self._apply(obj)
            return obj
        s.build = wrapped

    def quad(self, pts, uv, mat, nrm):
        P = [Vector(p) for p in pts]
        if (P[1] - P[0]).cross(P[2] - P[0]).dot(sum(nrm, Vector())) < 0:  # front face = bent-normal side
            P, uv, nrm = P[::-1], uv[::-1], nrm[::-1]
        i0 = len(self.s.bm.verts)
        self.s.decal([tuple(p) for p in P], mat, tuple(tuple(c) for c in uv), lift=0.0)
        m = self.s.m.to_3x3().inverted_safe().transposed()
        for k, n in enumerate(nrm):
            self.n[i0 + k] = (m @ Vector(n)).normalized()

    def grid(self, P, UV, N, mat):
        """Rows x cols vertex grid -> one quad per cell."""
        for i in range(len(P) - 1):
            for j in range(len(P[0]) - 1):
                ix = ((i, j), (i, j + 1), (i + 1, j + 1), (i + 1, j))
                self.quad([P[a][b] for a, b in ix], [UV[a][b] for a, b in ix], mat, [N[a][b] for a, b in ix])

    def _apply(self, obj):
        me = obj.data
        wn = obj.modifiers.get("WeightedNormal")
        if wn:
            obj.modifiers.remove(wn)
        N = [(0.0, 0.0, 0.0)] * len(me.vertices)  # zero = keep auto normal (trunks, planters)
        for i, n in self.n.items():
            N[i] = tuple(n)
        for p in me.polygons:
            if p.vertices[0] in self.n:
                p.use_smooth = True
        me.normals_split_custom_set_from_vertices(N)


def frame(n, up=Z):
    """Card frame (n, t, b): t = `up` projected onto the card plane, b = t x n (texture u axis)."""
    n = n.normalized()
    t = up - n * up.dot(n)
    if t.length < 1e-4:
        t = Vector((1, 0, 0)) - n * n.x
    t.normalize()
    return n, t, t.cross(n)


def card(cd, rng, c, n, size, mat, nf, tilt=(-25, 25), roll=(-20, 20), uv=FULL, bend=0.22, up=Z, jit=0.3):
    """2 x 2 card centred at c facing n, edges curled back (-n) so silhouettes tuck into the crown. `jit` tilts
    the card's bent normals by a random per-card offset: clumps shade apart and the sheen breaks up."""
    j = Vector((rng.gauss(0, 1), rng.gauss(0, 1), rng.gauss(0, 1))).normalized() * jit
    n, t, b = frame(n, up)
    a = rng.uniform(*tilt)
    n, t = rot(n, b, a), rot(t, b, a)
    r = rng.uniform(*roll)
    t, b = rot(t, n, r), rot(b, n, r)
    u0, v0, u1, v1 = uv
    if rng.random() < 0.5:
        u0, u1 = u1, u0
    h = size / 2
    P = [[c + b * (x * h) + t * (y * h) - n * (bend * h * (x * x + y * y)) for x in (-1, 0, 1)] for y in (-1, 0, 1)]
    UV = [[(lerp(u0, u1, (x + 1) / 2), lerp(v0, v1, (y + 1) / 2)) for x in (-1, 0, 1)] for y in (-1, 0, 1)]
    cd.grid(P, UV, [[(nf(p) + j).normalized() for p in row] for row in P], mat)


# ----------------------------------------------------------------- dense crowns

def profile_fn(prof):
    """(z, r) samples -> r(z), dr/dz (linear)."""
    def R(z):
        z = min(max(z, prof[0][0]), prof[-1][0])
        for (z0, r0), (z1, r1) in zip(prof, prof[1:]):
            if z <= z1:
                return lerp(r0, r1, (z - z0) / (z1 - z0)), (r1 - r0) / (z1 - z0)
        return prof[-1][1], 0.0
    return R


def conifer(cd, rng, prof, size, layers, gap=0.42, core=0.8, lump=0.08):
    """Rows of cards over a surface of revolution prof = [(z, r), ...] plus a dark core lathe.
    layers: (depth, card scale, materials): an inner cool layer fills depth, the outer layer carries the colour.
    `lump` modulates the radius with slow spiral harmonics: the flame tufts of plaza.png's cypresses."""
    R = profile_fn(prof)

    def nf(p):
        dr = max(-0.8, min(0.6, R(p.z)[1]))
        rad = Vector((p.x, p.y, 0))
        rad = rad.normalized() if rad.length > 1e-4 else Vector((0, 0, 0))
        return (rad - Z * dr + Z * 0.3).normalized()
    cd.s.lathe([(r * core, z) for z, r in prof], pal.Leaf_CoreConifer, seg=16)
    zs, ze = prof[0][0], prof[-1][0]
    for depth, grow, choice in layers:
        sz = size * grow
        z = zs + sz * 0.35
        while z < ze - sz * (0.3 if depth >= 1 else 0.9):  # inner layers stop short: the tip stays one colour
            k = max(4, round(2 * math.pi * R(z)[0] * depth / (sz * gap)))
            a0 = rng.uniform(0, 2 * math.pi)
            for i in range(k):
                a = a0 + 2 * math.pi * (i + rng.uniform(-0.3, 0.3)) / k
                zz = z + rng.uniform(-0.3, 0.3) * sz * gap
                rr, dr = R(zz)
                tuft = 1 + lump * (0.6 * math.sin(5 * a + 1.7 * zz) + 0.4 * math.sin(3 * a - 2.3 * zz + 1.3))
                o = Vector((math.cos(a), math.sin(a), 0))
                c = o * rr * depth * tuft * rng.uniform(0.94, 1.03) + Z * zz
                card(cd, rng, c, o - Z * max(-0.8, min(0.6, dr)), sz * rng.uniform(0.75, 1.25), rng.choice(choice),
                     nf, tilt=(-12, 30))
            z += sz * gap * 0.9
    top = layers[0][2]
    for i in range(3):  # leader: the tip is a card, not a flat top
        a = 2 * math.pi * i / 3 + rng.uniform(-0.3, 0.3)
        o = Vector((math.cos(a), math.sin(a), 0))
        card(cd, rng, o * 0.02 + Z * (ze - size * 0.42), o + Z * 0.6, size * 0.9, top[i % len(top)], nf,
             tilt=(-5, 5), roll=(-6, 6), jit=0.1)


def lobes_mound(cd, rng, lobes, mat, size, crown_c, crown_r, per=26, zmin=0.04, up_bias=0.55, tilt=(-30, 30),
                roll=(-35, 35), shell=(0.7, 0.95), w=0.6, axis=0.0, bend=0.22):
    """Vesper-style lobed mound: cards scattered over each lobe shell, biased outward from the crown. Normals
    blend lobe (w) and crown; the crown centre is the nearest point of an x segment +-axis, so a bed shades as
    one loaf rather than a row of balls."""
    c0 = Vector(crown_c)

    def centre(p):
        return c0 + Vector((max(-axis, min(axis, p.x - c0.x)), 0, 0))

    def nf_for(c, r):
        return lambda p: ((p - c) / r * w + (p - centre(p)) / crown_r * (1 - w) + Z * 0.3).normalized()
    for c, r in lobes:
        c = Vector(c)
        cc = centre(c)
        out = (c - cc).normalized() if (c - cc).length > 1e-3 else Z
        nf = nf_for(c, r)
        a0 = rng.uniform(0, 2 * math.pi)
        for i in range(per):  # Fibonacci-stratified directions: even cover, no bald patches
            z = 1 - 2 * (i + rng.uniform(0.2, 0.8)) / per
            a = a0 + i * 2.39996
            d = Vector((math.cos(a) * math.sqrt(1 - z * z), math.sin(a) * math.sqrt(1 - z * z), z))
            d = (d + out * 0.5 + Z * up_bias).normalized()
            p = c + d * r * rng.uniform(*shell)
            p.z = max(p.z, zmin + size * 0.3)
            card(cd, rng, p, d, size * rng.uniform(0.85, 1.15), mat, nf, tilt=tilt, roll=roll, bend=bend)


# ----------------------------------------------------------------- palms

def rings_sweep(s, path_z, rad, mat, lean, seg=12, band=0.14, depth=0.05):
    """Trunk tube: path x(z), radius r(z) with sawtooth leaf-scar rings every `band` metres."""
    pts, rs, top = [], [], path_z
    nb = max(1, round(top / band))
    for i in range(nb):
        for f, k in ((0.0, 1.0), (0.14, 1.0 + depth), (0.38, 1.0 - depth * 0.2), (0.75, 1.0 - depth * 0.3)):
            zz = (i + f) * top / nb
            pts.append(lean(zz))
            rs.append(rad(zz) * k)
    pts.append(lean(top))
    rs.append(rad(top))
    s.sweep(pts, rs, mat, seg=seg, caps=True)
    return pts


def feather_frond(cd, base, yaw, el0, droop, length, fold, twist, crown_c, crown_r, mat, bands=(0.45, 16.0)):
    """Coconut frond: rachis arcs from el0 (deg) down by `droop` toward the tip; leaflets hang in a V (fold,
    +bands[1] at the outer band). Grid rows = FEATHER stations, cols = right outer .. rachis .. left outer."""
    ax = FE - FB
    nn = Vector((-ax.y, ax.x)).normalized()
    k = length / ax.length
    cc = Vector(crown_c)
    S0 = Vector((-math.sin(math.radians(yaw)), math.cos(math.radians(yaw)), 0))
    f, dfold = bands

    def tangent(s):
        el = math.radians(el0 - droop * s ** 1.4)
        return Vector((math.cos(el) * math.cos(math.radians(yaw)), math.cos(el) * math.sin(math.radians(yaw)),
                       math.sin(el)))
    P, UV, N = [], [], []
    p, s_prev = Vector(base), 0.0
    for s, c, wl, wr in FEATHER:
        p = p + tangent((s + s_prev) / 2) * (s - s_prev) * length
        s_prev = s
        T = tangent(s)
        S = rot(S0, T, twist * s * s)
        U = T.cross(S)
        cu = FB + ax * s + nn * c
        row_p, row_uv, row_n = [], [], []
        for side, w in ((-1, wr), (1, wl)):
            a1, a2 = math.radians(fold + 8 * s), math.radians(fold + 8 * s + dfold)
            q1 = p + (S * side * math.cos(a1) - U * math.sin(a1)) * w * k * f
            q2 = q1 + (S * side * math.cos(a2) - U * math.sin(a2)) * w * k * (1 - f)
            n1, n2 = (U * math.cos(a1) + S * side * math.sin(a1)), (U * math.cos(a2) + S * side * math.sin(a2))
            pts = [(q2, n2, cu + nn * side * w), (q1, n1, cu + nn * side * w * f)]
            if side > 0:
                pts = pts[::-1]
            for q, n, u in pts:
                row_p.append(q)
                row_uv.append(u)
                row_n.append(n)
            if side < 0:
                row_p.append(p)
                row_uv.append(cu)
                row_n.append(U)
        P.append(row_p)
        UV.append([tuple(u) for u in row_uv])
        N.append([(n * 0.55 + (q - cc) / crown_r * 0.45 + Z * 0.3).normalized() for q, n in zip(row_p, row_n)])
    cd.grid(P, UV, N, mat)


def fan_leaf(cd, rng, base, d, D, crown_c, crown_r, mat, droop=0.22, cup=0.12):
    """Fan blade card: petiole end of the texture at `base`, blade along d; segment tips droop from the hub."""
    t = d.normalized()
    b = t.cross(Z) if abs(t.z) < 0.98 else Vector((0, -1, 0))
    b.normalize()
    n0 = b.cross(t)  # blade face: up / out
    r = rng.uniform(-25, 25)
    n0, b = rot(n0, t, r), rot(b, t, r)
    cc = Vector(crown_c)
    us, vs = (0.0, 0.25, 0.5, 0.75, 1.0), (0.0, FAN_HUB, 0.52, 0.76, 1.0)
    P, UV, N = [], [], []
    for v in vs:
        rp, rn = [], []
        for u in us:
            x, y = (u - 0.5) * D, (v - FAN_STEM) * D
            rho = math.hypot(x, y - (FAN_HUB - FAN_STEM) * D) / D
            q = base + b * x + t * y + n0 * (cup * abs(x) - droop * rho * rho * D)
            rp.append(q)
            rn.append((n0 * 0.6 + (q - cc) / crown_r * 0.4 + Z * 0.25).normalized())
        P.append(rp)
        N.append(rn)
        UV.append([(u, v) for u in us])
    cd.grid(P, UV, N, mat)


def sphere(s, c, r, mat, seg=10):
    prof = [(r * math.sin(math.pi * i / 6), -r * math.cos(math.pi * i / 6)) for i in range(7)]
    with s.at(c):
        s.lathe(prof, mat, seg=seg)


@asset("CoconutPalm", "Plants", bevel=0.0)
def coconut_palm(s):
    rng = random.Random(4101)
    cd = Cards(s)
    H = 6.0
    lean = lambda z: (0.8 * (z / H) ** 1.8 + 0.06 * math.sin(z * 0.8), 0.12 * (z / H) ** 2, z)  # noqa: E731
    rad = lambda z: 0.14 + 0.09 * math.exp(-z / 0.45) - 0.015 * z / H  # noqa: E731
    rings_sweep(s, H, rad, pal.Bark_Palm, lean, seg=12, band=0.13, depth=0.07)
    top = Vector(lean(H))
    with s.at(top):  # frond-base boot: the crown shaft the fronds spring from
        s.lathe([(0.12, -0.05), (0.17, 0.05), (0.19, 0.18), (0.15, 0.3), (0.07, 0.38), (0, 0.4)], pal.Bark_Fibre,
                seg=12)
    for i in range(7):  # coconuts under the frond bases
        a = 2 * math.pi * i / 7 + rng.uniform(-0.3, 0.3)
        sphere(s, top + Vector((math.cos(a) * 0.2, math.sin(a) * 0.2, rng.uniform(-0.08, 0.06))),
               rng.uniform(0.085, 0.105), pal.Coconut)
    cc, cr = top + Z * 0.25, 3.2
    # tiers: (count, el0, droop, length, fold)
    tiers = ((3, 62, 50, 2.4, 30), (7, 40, 95, 3.5, 34), (8, 18, 100, 3.7, 38), (4, -8, 72, 3.2, 42))
    i = 0
    for count, el0, droop, length, fold in tiers:
        for _ in range(count):
            yaw = (i * 137.5 + rng.uniform(-12, 12)) % 360
            base = top + Z * (0.3 - 0.08 * i / 6) + Vector((math.cos(math.radians(yaw)), math.sin(math.radians(yaw)),
                                                            0)) * 0.08
            feather_frond(cd, base, yaw, el0 + rng.uniform(-6, 6), droop + rng.uniform(-10, 10),
                          length * rng.uniform(0.9, 1.08), fold, rng.uniform(-35, 35), cc, cr, pal.Leaf_PalmFeather)
            i += 1


@asset("FanPalm", "Plants", bevel=0.0)
def fan_palm(s):
    rng = random.Random(4202)
    cd = Cards(s)
    H = 1.0
    lean = lambda z: (0.06 * (z / H) ** 2, 0.0, z)  # noqa: E731
    rad = lambda z: 0.13 + 0.05 * math.exp(-z / 0.2) - 0.02 * z / H  # noqa: E731
    rings_sweep(s, H, rad, pal.Bark_Fibre, lean, seg=12, band=0.085, depth=0.09)
    for i in range(30):  # old leaf-base stubs: the shaggy fibrous trunk
        z = 0.12 + (H - 0.15) * i / 29
        a = math.radians(i * 137.5)
        o = Vector((math.cos(a), math.sin(a), 0))
        c = Vector(lean(z)) + o * rad(z) * 0.85
        s.sweep([c, c + (o * 0.9 + Z * 1.2).normalized() * 0.13], [0.03, 0.004], pal.Bark_Fibre, seg=5)
    top = Vector(lean(H))
    with s.at(top):
        s.lathe(dome(0.16, -0.05, 0.2, n=4), pal.Bark_Fibre, seg=12)
    cc, cr = top + Z * 0.35, 1.2
    n = 36
    for i in range(n):
        yaw = math.radians(i * 137.5 + rng.uniform(-10, 10))
        el = 88 - 76 * (i / (n - 1)) ** 1.1 + rng.uniform(-6, 6)  # vase-shaped spray (props sheet pot palm)
        L = rng.uniform(0.5, 0.7) * (0.8 if el < 30 else 1.0)
        o = Vector((math.cos(yaw), math.sin(yaw), 0))
        p0 = top + Z * 0.08 + o * 0.05
        pts, p = [p0], p0
        for k in range(1, 4):  # petiole sags under the blade
            e = math.radians(el + 8 - 22 * k / 3)
            p = p + (o * math.cos(e) + Z * math.sin(e)) * L / 3
            pts.append(p)
        s.sweep(pts, [0.014, 0.012, 0.01, 0.008], pal.Petiole, seg=5)
        d = (pts[-1] - pts[-2]).normalized()
        d = (d - Z * 0.15).normalized()
        fan_leaf(cd, rng, pts[-1] - d * 0.03, d, rng.uniform(0.74, 0.9), cc, cr, pal.Leaf_PalmFan,
                 droop=rng.uniform(0.18, 0.3))


@asset("Cypress", "Plants", bevel=0.0)
def cypress(s):
    rng = random.Random(4303)
    cd = Cards(s)
    s.lathe([(0.07, 0), (0.06, 0.4), (0, 0.45)], pal.Bark_Fibre, seg=8)
    prof = [(0.12, 0.22), (0.5, 0.45), (1.2, 0.56), (2.0, 0.58), (2.9, 0.52), (3.8, 0.41), (4.7, 0.28),
            (5.4, 0.16), (5.9, 0.07), (6.25, 0.0)]
    olive, cool = tinted("DK_Leaf_CypressOlive"), pal.Leaf_Cypress
    conifer(cd, rng, prof, 0.72, ((1.0, 1.0, (olive,)), (0.93, 0.75, (olive,)), (0.8, 1.2, (cool, olive))),
            lump=0.09)


def topiary(cd, rng, z0=0.0, h=1.66, r=0.39, size=0.33):
    cd.s.lathe([(0.045, z0), (0.035, z0 + 0.22), (0, z0 + 0.25)], pal.Bark_Fibre, seg=8)
    prof = [(0.14, 0.14), (0.24, 0.35), (0.4, 0.42), (0.62, 0.4), (0.92, 0.315), (1.22, 0.215), (1.48, 0.11),
            (1.72, 0.0)]
    k, kr = h / 1.72, r / 0.42  # prof is authored at 1.72 x 0.42
    olive, cool = tinted("DK_Leaf_CypressOlive"), pal.Leaf_Cypress
    conifer(cd, rng, [(z0 + z * k, q * kr) for z, q in prof], size * (0.5 + 0.5 * k),
            ((1.0, 1.0, (olive,)), (0.76, 1.2, (cool, olive))), gap=0.36, core=0.86, lump=0.06)


@asset("TopiaryCone", "Plants", bevel=0.0)
def topiary_cone(s):
    topiary(Cards(s), random.Random(4404))


@asset("ShrubMound", "Plants", bevel=0.0)
def shrub_mound(s):
    rng = random.Random(4505)
    cd = Cards(s)
    with s.at((0, 0, 0.22), scale=(0.25, 0.25, 0.25)):
        s.lathe([(0.8, -0.8)] + dome(1.0, 0.0, 1.0, n=6), pal.Leaf_CoreShrub, seg=16)
    lobes = [((0, 0, 0.36), 0.28)]
    for i in range(6):
        a = 2 * math.pi * i / 6 + rng.uniform(-0.3, 0.3)
        lobes.append(((math.cos(a) * 0.2, math.sin(a) * 0.2, rng.uniform(0.2, 0.27)), rng.uniform(0.2, 0.24)))
    lobes.append(((0.05, -0.03, 0.46), 0.18))
    mound = dict(crown_c=(0, 0, 0.2), crown_r=0.42, tilt=(-20, 20), bend=0.3)
    lobes_mound(cd, rng, lobes, pal.Leaf_Shrub, 0.34, per=26, shell=(0.55, 0.92), **mound)
    lobes_mound(cd, rng, lobes, pal.Leaf_Shrub, 0.4, per=10, shell=(0.2, 0.45), **mound)  # inner fill


@asset("FlowerBed", "Plants", bevel=0.0)
def flower_bed(s):
    rng = random.Random(4606)
    cd = Cards(s)
    with s.at((0, 0, 0.0), scale=(0.76, 0.16, 0.15)):
        s.lathe([(1.0, 0.0)] + dome(1.0, 0.1, 0.9, n=4), pal.Leaf_CoreShrub, seg=16)
    loaf = dict(crown_c=(0, 0, -0.05), crown_r=0.35, axis=0.7, w=0.3, bend=0.2, tilt=(-20, 20))

    def row(n, ys, z, r):  # n x len(ys) lobes along the bed: ((x, y, z), radius)
        return [((-0.8 + 1.6 * i / (n - 1) + rng.uniform(-0.04, 0.04), y + rng.uniform(-0.02, 0.02), rng.uniform(*z)),
                 rng.uniform(*r)) for i in range(n) for y in ys]
    lobes_mound(cd, rng, row(10, (-0.12, 0.0, 0.12), (0.06, 0.1), (0.12, 0.15)), pal.Leaf_Shrub, 0.2, per=6,
                zmin=0.0, up_bias=0.1, shell=(0.4, 1.0), **loaf)
    # the coral sheet is a flowering shrub seen from above: its cards crown the bed as blooming clumps
    lobes_mound(cd, rng, row(10, (-0.07, 0.07), (0.17, 0.2), (0.1, 0.12)), pal.Flower_Coral, 0.21, per=7,
                zmin=0.1, up_bias=0.7, shell=(0.5, 1.0), **loaf)


@asset("PottedTopiary", "Plants", bevel=0.0)
def potted_topiary(s):
    rng = random.Random(4707)
    cd = Cards(s)
    s.lathe([(0, 0), (0.28, 0), (0.292, 0.008), (0.294, 0.032), (0.285, 0.042), (0, 0.042)], pal.Chrome, seg=48)
    body = [(0.285, 0.04), (0.296, 0.05), (0.33, 0.57), (0.338, 0.585), (0.348, 0.6), (0.352, 0.618),
            (0.347, 0.634), (0.334, 0.643), (0.318, 0.642), (0.308, 0.632), (0.304, 0.61), (0.302, 0.56)]
    s.lathe(body, pal.Enamel_Ivory, seg=48, cap_bottom=False, cap_top=False)
    s.lathe(ring(0.296, 0.303, 0.075, 0.115), pal.Enamel_Teal, seg=48, cap_bottom=False, cap_top=False)
    s.lathe(ring(0.325, 0.3315, 0.505, 0.535), pal.Chrome, seg=48, cap_bottom=False, cap_top=False)
    s.extrude(circle(0.303, 40), 0.54, 0.575, pal.Soil)
    topiary(cd, rng, z0=0.56, h=1.38, r=0.37, size=0.33)
