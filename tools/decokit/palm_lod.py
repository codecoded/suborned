"""Distance levels for the two street palms, CoconutPalm and CoconutPalm_Street (brief 028 "Palm LODs", Vela, 2026-10-22).

Sky, 2026-10-22: "why not do a LoD for the trees?" The generator's default levels (25 % and 6 % collapse decimation, 20 Oct) tore the cards apart: L2 read ragged. These are authored:
the same palm built again with less detail where the eye cannot tell, nothing removed that shows at the distance it takes over.

  L0  as built (plants.py / palms026.py), untouched                                     CoconutPalm 6,310    CoconutPalm_Street 8,230
  L1  same 22 fronds, same tilt, droop, length and twist (one random stream, in the same order) on four rows of two cards
      instead of six rows of four; the trunk's leaf-scar rings kept but one ring to two bands and eight sides instead of
      twelve; coconuts as octahedra, the boot on eight sides
  L2  the same 22 fronds again as three rows of two cards (one card each side of the rachis, the texture's leaf drawn
      whole, so the leaflets and the cutout edge come from the alpha, not the mesh), a six-sided trunk on eight stations
      along the same lean, a six-sided boot, three coconuts

Every level draws with the same four materials as L0 (Bark_Palm, Bark_Fibre, Coconut, Leaf_PalmFeather): no new texture, no new material.
The tree's silhouette, the crown's spread and the full bright feather crown are the point (Sky's flyby look, briefs/028/palm/ref-flyby-boulevard-old-city.png).

Registered here as the hand-built L1 (kit.asset near=) and L2 (far=) of both pieces. tools/kit/lod.ts FULL_DETAIL still keeps both ids at full detail in the catalogue until Sky approves,
so nothing here reaches the game: to adopt, Splice removes the two ids from FULL_DETAIL, runs `bun run decokit` and `bun run kit`.

`level(s, H, wob, seed, lv)` is the one builder, with lv = 0 reproducing the shipped pieces' geometry (checked: same triangle counts and the same vertices; palm_lod_check.py).
"""

import math
import random

from mathutils import Vector

import kit
from mats import pal
from plants import FEATHER, Cards, Z, feather_frond, rings_sweep, sphere


# --------------------------------------------------------------------------- level table
# trunk: sides, ring band (m), bump depth, stations a band; fronds: row stations along the rachis (0..1) and columns (4 = the shipped V of two bands a side, 2 = one card a side);
# width margin: the cards of a coarse level are cut a little wider than the leaf's outline so interpolating the station table never trims a leaflet tip.
LEVELS = {
    0: dict(trunk=(12, 0.13, 0.07, (0.0, 0.14, 0.38, 0.75)), rows=tuple(i / 6 for i in range(7)), cols=4, margin=1.0, boot=12, nut=(10, 7), bands=(0.45, 16.0)),
    # L1: every frond exactly as shipped (a coarser frond is the only thing that showed in the sweep); the trunk a plain eight-sided taper, the coconuts octahedra
    1: dict(trunk=(8, None, 0.0, ()), rows=tuple(i / 6 for i in range(7)), cols=4, margin=1.0, boot=8, nut=(4, 7), bands=(0.45, 16.0)),
    # L2: the same 22 fronds as cards: four rows of one card a side (the texture's leaf cut wide enough that no leaflet tip is trimmed), a six-sided taper, three coconuts
    2: dict(trunk=(6, None, 0.0, ()), rows=(0, 0.25, 0.5, 0.75, 1.0), cols=2, margin=1.10, boot=6, nut=(4, 3), bands=(0.45, 16.0)),
}
# experiments (tools/decokit/palm_lod_eval.py LEVELS=...): what each part of a level costs the look
_R6 = tuple(i / 6 for i in range(7))
OLD1 = dict(trunk=(8, 0.26, 0.07, (0.0, 0.2, 0.6)), rows=(0, 0.3, 0.6, 0.85, 1.0), cols=2, margin=1.06, boot=8, nut=(4, 7), bands=(0.45, 16.0))  # the first L1: two cards a side on four rows, rings
OLD2 = dict(trunk=(6, None, 0.0, ()), rows=(0, 0.4, 0.75, 1.0), cols=2, margin=1.10, boot=6, nut=(4, 3), bands=(0.45, 16.0))
LEVELS[11] = dict(OLD1, rows=(0, 0.2, 0.4, 0.6, 0.8, 1.0))                    # L1 with five rows
LEVELS[12] = dict(OLD1, cols=4)                                                # L1 with the V of two bands
LEVELS[13] = dict(OLD1, rows=_R6, cols=4, margin=1.0)                          # L1's trunk and coconuts only, fronds as shipped
LEVELS[14] = dict(OLD1, rows=_R6, cols=2, margin=1.0)                          # six rows, one card a side
LEVELS[16] = dict(LEVELS[1], trunk=(8, 0.39, 0.07, (0.0, 0.2, 0.6)))                # L1 fronds as shipped, rings every 0.39 m, eight sides
LEVELS[17] = dict(LEVELS[1])                              # L1 fronds as shipped, a plain eight-sided taper
LEVELS[21] = dict(OLD2)
LEVELS[22] = dict(LEVELS[2])
LEVELS[23] = dict(OLD2, rows=(0, 0.5, 1.0))
LEVELS[24] = dict(OLD2, rows=(0, 0.25, 0.5, 0.75, 1.0), cols=4)
LEVELS[15] = dict(OLD1, trunk=(12, 0.13, 0.07, (0.0, 0.14, 0.38, 0.75)), nut=(10, 7), boot=12)  # L1's fronds only, trunk as shipped


def station(s, margin=1.0):
    """(c, wl, wr) of the feather texture at fraction s along the rachis: the shipped table, interpolated."""
    for (s0, c0, l0, r0), (s1, c1, l1, r1) in zip(FEATHER, FEATHER[1:]):
        if s <= s1 + 1e-9:
            t = 0.0 if s1 == s0 else (s - s0) / (s1 - s0)
            return c0 + (c1 - c0) * t, (l0 + (l1 - l0) * t) * margin, (r0 + (r1 - r0) * t) * margin
    return FEATHER[-1][1], FEATHER[-1][2] * margin, FEATHER[-1][3] * margin


def frond(cd, base, yaw, el0, droop, length, fold, twist, crown_c, crown_r, mat, lv):
    """feather_frond with the level's rows and columns. Level 0 calls the shipped function."""
    L = LEVELS[lv]
    if lv == 0:
        return feather_frond(cd, base, yaw, el0, droop, length, fold, twist, crown_c, crown_r, mat)
    from plants import FB, FE, rot
    ax = FE - FB
    nn = Vector((-ax.y, ax.x)).normalized()
    k = length / ax.length
    cc = Vector(crown_c)
    S0 = Vector((-math.sin(math.radians(yaw)), math.cos(math.radians(yaw)), 0))
    f, dfold = L["bands"]
    two = L["cols"] == 2

    def tangent(s):
        el = math.radians(el0 - droop * s ** 1.4)
        return Vector((math.cos(el) * math.cos(math.radians(yaw)), math.cos(el) * math.sin(math.radians(yaw)), math.sin(el)))
    P, UV, N = [], [], []
    p, s_prev = Vector(base), 0.0
    for s in L["rows"]:
        c, wl, wr = station(s, L["margin"])
        p = p + tangent((s + s_prev) / 2) * (s - s_prev) * length
        s_prev = s
        T = tangent(s)
        S = rot(S0, T, twist * s * s)
        U = T.cross(S)
        cu = FB + ax * s + nn * c
        row_p, row_uv, row_n = [], [], []
        # one card a side: the V's two bands (fold, fold + dfold) are folded into one at their mean angle, reaching as far out as the outer band does
        a1 = math.radians(fold + 8 * s)
        a2 = math.radians(fold + 8 * s + dfold)
        for side, w in ((-1, wr), (1, wl)):
            q1 = p + (S * side * math.cos(a1) - U * math.sin(a1)) * w * k * f
            q2 = q1 + (S * side * math.cos(a2) - U * math.sin(a2)) * w * k * (1 - f)
            n1, n2 = (U * math.cos(a1) + S * side * math.sin(a1)), (U * math.cos(a2) + S * side * math.sin(a2))
            if two:  # one card a side: from the rachis straight out to where the V's outer band ends
                pts = [(q2, (n1 * f + n2 * (1 - f)).normalized(), cu + nn * side * w)]
            else:
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


def trunk(s, H, rad, lean, lv):
    sides, band, depth, stops = LEVELS[lv]["trunk"]
    if lv == 0 or lv == 15:
        return rings_sweep(s, H, rad, pal.Bark_Palm, lean, seg=sides, band=band, depth=depth)
    pts, rs = [], []
    if band:  # leaf-scar rings, coarser: the same sawtooth, a band longer
        nb = max(1, round(H / band))
        for i in range(nb):
            for fz in stops:
                zz = (i + fz) * H / nb
                pts.append(lean(zz))
                rs.append(rad(zz) * (1.0 + depth * (1.0 if fz == stops[1] else (0.0 if fz == 0.0 else -0.2))))
    else:  # far: a plain taper, eight stations, the flare at the foot kept
        zs = [0.0]
        for zz in sorted([0.3, 0.8, 1.6] + [H * f for f in (0.35, 0.5, 0.65, 0.8, 0.9)]):
            if zz - zs[-1] >= 0.4 and H - zz >= 0.4:
                zs.append(zz)
        for zz in zs:
            pts.append(lean(zz))
            rs.append(rad(zz))
    pts.append(lean(H))
    rs.append(rad(H))
    s.sweep(pts, rs, pal.Bark_Palm, seg=sides, caps=True)
    return pts


def level(s, H, wob, seed, lv):
    """The coconut palm at trunk height H (6.0 CoconutPalm, 8.6 CoconutPalm_Street), the same random stream as the shipped pieces at every level."""
    L = LEVELS[lv]
    rng = random.Random(seed)
    cd = Cards(s)
    lean = lambda z: (0.8 * (z / H) ** 1.8 + 0.06 * math.sin(z * wob), 0.12 * (z / H) ** 2, z)  # noqa: E731
    rad = lambda z: 0.14 + 0.09 * math.exp(-z / 0.45) - 0.015 * z / H  # noqa: E731
    trunk(s, H, rad, lean, lv)
    top = Vector(lean(H))
    with s.at(top):
        boot = [(0.12, -0.05), (0.17, 0.05), (0.19, 0.18), (0.15, 0.3), (0.07, 0.38), (0, 0.4)]
        if lv == 2:
            boot = [(0.12, -0.05), (0.19, 0.18), (0.07, 0.38), (0, 0.4)]
        s.lathe(boot, pal.Bark_Fibre, seg=L["boot"])
    nut_seg, nut_n = L["nut"]
    keep = set(range(7)) if nut_n == 7 else {0, 2, 4}
    for i in range(7):  # the random stream runs through all seven, kept or not
        a = 2 * math.pi * i / 7 + rng.uniform(-0.3, 0.3)
        c = top + Vector((math.cos(a) * 0.2, math.sin(a) * 0.2, rng.uniform(-0.08, 0.06)))
        r = rng.uniform(0.085, 0.105)
        if i not in keep:
            continue
        if lv in (0, 15):
            sphere(s, c, r, pal.Coconut, seg=nut_seg)
        else:  # an octahedron a size up reads as the same dark nut at a few pixels
            with s.at(c):
                s.lathe([(0, -r * 1.25), (r * 1.1, 0), (0, r * 1.25)], pal.Coconut, seg=nut_seg)
    cc, cr = top + Z * 0.25, 3.2
    tiers = ((3, 62, 50, 2.4, 30), (7, 40, 95, 3.5, 34), (8, 18, 100, 3.7, 38), (4, -8, 72, 3.2, 42))
    i = 0
    for count, el0, droop, length, fold in tiers:
        for _ in range(count):
            yaw = (i * 137.5 + rng.uniform(-12, 12)) % 360
            base = top + Z * (0.3 - 0.08 * i / 6) + Vector((math.cos(math.radians(yaw)), math.sin(math.radians(yaw)), 0)) * 0.08
            frond(cd, base, yaw, el0 + rng.uniform(-6, 6), droop + rng.uniform(-10, 10), length * rng.uniform(0.9, 1.08), fold, rng.uniform(-35, 35), cc, cr,
                  pal.Leaf_PalmFeather, lv)
            i += 1


PIECES = {"CoconutPalm": (6.0, 0.8, 4101), "CoconutPalm_Street": (8.6, 0.7, 4311)}


def register():
    """Hand-built L1 and L2 for both pieces (kit.asset near= / far=). FULL_DETAIL (tools/kit/lod.ts) keeps them out of the catalogue until Sky says yes."""
    for name, (H, wob, seed) in PIECES.items():
        spec = kit.REGISTRY.get(name)
        if spec is None:
            continue
        spec["near"] = lambda s, H=H, wob=wob, seed=seed: level(s, H, wob, seed, 1)
        spec["far"] = lambda s, H=H, wob=wob, seed=seed: level(s, H, wob, seed, 2)


register()
