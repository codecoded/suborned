"""Paving v2: the Streets study default (Concepts/Streets/study/limestone_flags.png). Honed cream
limestone flags in stretcher bond, a flamed-granite kerb, a two-course sett gutter. Surfaces are sheet-mapped
(pave_sheet.py: albedo / normal / mask at 1 mm texels), so each module is a flat quad, not ~440 tris of joints.

Pave_Flags_<i>: 2 x 2 m walk surface at z = 0.15. i picks a quadrant of the 4 x 4 m sheet; a module at world grid
cell (ix, iy) takes i = (ix & 1) + 2 * (iy & 1) and the bond runs on seamlessly. Courses run along local X: lay X
parallel to the kerb.
Kerb_Granite_<i>: 2 m run of two 1 m granite stones (0.3 m deep, 0.15 m upstand, 35 mm road-side arris; lots A / B,
order flipped by i) and a 0.375 m two-course gutter; i = x parity (gutter sheet half). Pivot = kerb line at road
level, as Kerb_Straight: stone on +Y sits 4 mm proud over the paving module's edge, gutter on -Y.
Kerb_GraniteCorner_<i>: R = 2 m block corner (Tile_CornerCurb geometry), flags inside from quadrant i.
Kerb_GraniteNose: half-round end of a 2 m island / kerb extension. TreePit_Flags_<i>: flags module with a 1.2 m pit,
granite surround and bronze sunburst grille (hero study streets plant palms and planes in these).
"""

import math

from geo import arc, circle
from ground import J, SIDE, TOP, inset, plate, rect, revolve, sector
from kit import asset
from mats import pal, register

register("DK_Pave_Flags", "paving", "#FFFFFF")
register("DK_Gutter_Setts", "paving", "#FFFFFF")
register("DK_Granite_Kerb", "stone", "#FFFFFF")
register("DK_Granite_Kerb_B", "stone", "#EAE6DF")  # second quarry lot: stones alternate A / B, as real kerbs vary
register("DK_Asphalt_Fine", "stone", "#FFFFFF")  # street road deck (scene slab, metric UVs)
register("DK_Bronze", "satin", "#9A7443")  # cast tree grilles

KERB_W, ARRIS, GUTTER = 0.3, 0.035, 0.375


YF, ZT = -0.004, TOP + 0.004  # kerb road face (4 mm proud of the line) and top (4 mm over the paving)


def _lot(k):
    """Kerb stone lot A / B (looked up per build: a scene reset frees cached materials)."""
    return pal.Granite_Kerb_B if k % 2 else pal.Granite_Kerb

# (y, z) section, y from the road face toward the walk: rounded road arris, 4 mm chamfer on the back edge
STONE = ([(YF, 0.0), (KERB_W, 0.0), (KERB_W, ZT - 0.004), (KERB_W - 0.004, ZT)]
         + arc(YF + ARRIS, ZT - ARRIS, ARRIS, 90, 180, 6))


def _sheet_uv(i):
    """Module-local (x, y) -> paving sheet UV for quadrant i."""
    u0, v0 = (i % 2) * 0.5, (i // 2) * 0.5
    return lambda p: (u0 + (p[0] + 1) / 4, v0 + (p[1] + 1) / 4)


def _uvpoly(s, poly, z, mat, uvf):
    """Flat CCW polygon at z with per-vertex UVs from uvf(p)."""
    s.decal([(x, y, z) for x, y in poly], mat, [uvf(p) for p in poly], lift=0.0)


def _pave_poly(s, poly, i):
    _uvpoly(s, poly, TOP, pal.Pave_Flags, _sheet_uv(i))


def _ring(s, c, radius, a0, a1, stones, lot0=0, u0=0.0):
    """Kerb bent round c: kerb line at radius, walk side inside. Stones split the sweep evenly (alternating lots);
    the gutter follows the arc with sheet u = arc length along the kerb line."""
    prof = [(radius - y, z) for y, z in STONE][::-1]  # mirrored section, re-wound CCW
    for k in range(stones):
        b0, b1 = a0 + (a1 - a0) * k / stones, a0 + (a1 - a0) * (k + 1) / stones
        revolve(s, prof, c, b0, b1, _lot(k + lot0), step=5.0)
    n = max(2, math.ceil((a1 - a0) / 3.0))
    r_in, r_out, v_in = radius - YF, radius + GUTTER, (GUTTER + YF) / GUTTER
    for k in range(n):
        t0, t1 = (math.radians(a0 + (a1 - a0) * j / n) for j in (k, k + 1))
        u = [u0 + radius * (t - math.radians(a0)) / 4 for t in (t0, t1)]
        pt = lambda r, t: (c[0] + r * math.cos(t), c[1] + r * math.sin(t), 0.002)
        s.decal([pt(r_in, t0), pt(r_out, t0), pt(r_out, t1), pt(r_in, t1)], pal.Gutter_Setts,
                ((u[0], v_in), (u[0], 0.0), (u[1], 0.0), (u[1], v_in)), lift=0.0)


def _quad(s, x0, y0, x1, y1, z, mat, uv0, uv1):
    (u0, v0), (u1, v1) = uv0, uv1
    s.decal([(x0, y0, z), (x1, y0, z), (x1, y1, z), (x0, y1, z)], mat, ((u0, v0), (u1, v0), (u1, v1), (u0, v1)),
            lift=0.0)


def _pave(i):
    def build(s):
        _pave_poly(s, rect(-1, -1, 1, 1), i)
    return build


def _kerb(i):
    u0 = i * 0.5

    def build(s):
        with s.at((0, 0, 0), SIDE):
            for k, x0 in enumerate((-1, 0)):
                s.extrude(STONE, x0 + J, x0 + 1 - J, _lot(k + i))
        s.box((-J, 0.02, 0.0), (J, KERB_W - 0.004, ZT - 0.006), pal.Stone_Dark)  # pointing between the stones
        _quad(s, -1, -GUTTER, 1, YF, 0.002, pal.Gutter_Setts, (u0, 0.0), (u0 + 0.5, (GUTTER + YF) / GUTTER))
    return build


def _corner(i):
    """Block corner, as Tile_CornerCurb: kerb line R = 2 m about the inner corner (-1, +1), tangent to the grid lines
    y = -1 and x = +1 where Kerb_Granite runs continue; three 1.05 m stones; flags inside the arc (quadrant i)."""
    c = (-1.0, 1.0)

    def build(s):
        _pave_poly(s, sector(c, 0, 2.0 - KERB_W + 0.02, 270, 360), i)
        _ring(s, c, 2.0, 270, 360, 3)
    return build


def _nose(s):
    """Rounded end of a 2 m island or kerb extension: kerb lines y = +-1 turn round a half circle about the pivot,
    pointing +X; straight island modules continue on -X."""
    _pave_poly(s, sector((0, 0), 0, 1.0 - KERB_W + 0.02, -90, 90), 0)
    _ring(s, (0.0, 0.0), 1.0, -90, 90, 3)


PIT, PIT_RIM = 0.6, 0.12  # half size of the pit opening in the paving, width of the granite surround


def _grille(s, h):
    """Cast bronze grille flush with the walk: sunburst of 24 radial slots round a 0.36 m trunk ring."""
    slots = []
    for k in range(24):
        a = math.radians(k * 15)
        d, n = (math.cos(a), math.sin(a)), (-math.sin(a), math.cos(a))
        r0, r1, w = 0.24, 0.43 if k % 2 == 0 else 0.36, 0.011
        slots.append([(d[0] * r + n[0] * o, d[1] * r + n[1] * o) for r, o in ((r0, -w), (r1, -w), (r1, w), (r0, w))])
    plate(s, rect(-h, -h, h, h), [circle(0.18, 24)] + slots, TOP - 0.012, TOP - 0.002, pal.Bronze)
    s.extrude(rect(-h, -h, h, h), TOP - 0.09, TOP - 0.08, pal.Soil, bottom=False)  # mulch seen through the slots


def _tree_pit(i):
    """Paving module with a 1.2 m tree pit: flags frame (quadrant i), granite surround, bronze sunburst grille."""
    def build(s):
        o, g = PIT, PIT - PIT_RIM
        for r in (rect(-1, -1, 1, -o), rect(-1, o, 1, 1), rect(-1, -o, -o, o), rect(o, -o, 1, o)):
            _pave_poly(s, r, i)
        for k, r in enumerate((rect(-o, -o, o, -g), rect(-o, g, o, o), rect(-o, -g, -g, g), rect(g, -g, o, g))):
            s.extrude(inset(r, J), TOP - 0.1, TOP + 0.002, _lot(k), bottom=False)
        _grille(s, g - J)
    return build


for _i in range(4):
    asset(f"Pave_Flags_{_i}", "Ground", bevel=0.0)(_pave(_i))
for _i in range(2):
    asset(f"Kerb_Granite_{_i}", "Ground", bevel=0.003)(_kerb(_i))
for _i in range(4):
    asset(f"Kerb_GraniteCorner_{_i}", "Ground", bevel=0.003)(_corner(_i))
    asset(f"TreePit_Flags_{_i}", "Ground", bevel=0.0)(_tree_pit(_i))
asset("Kerb_GraniteNose", "Ground", bevel=0.003)(_nose)
