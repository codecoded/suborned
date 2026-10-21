"""The testbed's flooring as kit pieces (brief 026, Vela; Sky 2026-10-20: "the flooring was better I think in the testbed").

What the 2.5D testbed walked on (civic-paving-material.js, fountain-plaza.js, civic-roads.js, cafe-terrace art): honed warm-ivory limestone in 1 m slabs with fine 6 mm joints and a satin
face (2 m tile, tint #f2dfbd), set with bands and rings of glazed teal-blue ceramic tile laid as individual bevelled tiles on a 0.72 m pitch with pale grout (#669fab, bands 0.82 m, ring 0.52 m), a
pearl-marble kerb cap and tree-bed foundations, and a gold pinstripe along every edge (brass here, hairline only). Today's ground set (brief 026 rounds 1 and 2) lays the old city's flag sheet,
a random running bond in tan and grey that reads as brick at play zoom, with flat seafoam terrazzo inlays and a granite kerb with a sett gutter.

  Pave_Slab           2 x 2 m flat module, DK_Ground_PaveLimestone (the testbed's limestone sheet, 2 m tile = 2 x 2 slabs); lies at z 0.15 like Pave_Flags_i (a drop-in: same size, same pivot)
  Kerb_Pearl_0 / _1   2 m kerb run, pivot = the kerb line at road level as Kerb_Granite (stone on +Y), 0.3 m deep, 0.154 high, pearl marble (DK_Ground_RoadPearl) in two stones, a brass hairline on
                      the road-side crown edge; no gutter (the road's light ribbon takes its place, road_ribbon.py)
  TreePit_Pearl       2 x 2 m limestone module with a 1.2 m pit, a pearl surround 0.12 m wide with a brass hairline round the inside, dark soil; the pit the street palm (CoconutPalm_Street) stands in
  PaveInlay_Band_6m   6 x 0.82 m band of glazed teal tile (DK_Ground_TileTeal), a brass hairline each side: the testbed's approach bands; lies at z 0.154
  PaveInlay_Ring_Q    a quarter of the 0.52 m ring (centre-line radius 4.62 m, 90 degrees, 16 segments), pivot = the ring's centre, from +X to +Y; four make the fountain ring; scale the radius by
                      resizing for other rings is NOT possible (the tile is on a fixed pitch): ask for another radius.
Road enamel (the carriageway) and the pearl marble are in road_ribbon.py and road_tex.py. Not in kit.FAMILIES until Splice adopts: add "pave026" (kit.py has it for the generator).
"""

import math

from kit import asset
from mats import TEXTURES, pal, register
import mats

TOP = 0.15
register("DK_Ground_PaveLimestone", "ground", "#FFFFFF")
register("DK_Ground_TileTeal", "ground", "#FFFFFF")
TEXTURES.setdefault("DK_Ground_PaveLimestone", ("ground_pavelimestone_v1.png", 2.0))
TEXTURES.setdefault("DK_Ground_TileTeal", ("ground_tileteal_v1.png", 1.44))
mats.GROUND.setdefault("DK_Ground_PaveLimestone", dict(albedo="ground_pavelimestone_v1.png", normal="ground_pavelimestone_v1_normal.png", mask="ground_pavelimestone_v1_mask.png", tile=(2.0, 2.0), smoothness=1.0))
mats.GROUND.setdefault("DK_Ground_TileTeal", dict(albedo="ground_tileteal_v1.png", normal="ground_tileteal_v1_normal.png", mask="ground_tileteal_v1_mask.png", tile=(1.44, 1.44), smoothness=1.0))


@asset("Pave_Slab", "Ground", bevel=0.0)
def pave_slab(s):
    s.face([(-1, -1, TOP), (1, -1, TOP), (1, 1, TOP), (-1, 1, TOP)], pal.Ground_PaveLimestone)


def _kerb(i):
    def build(s):
        cuts = (-1.0, 0.0, 1.0) if i == 0 else (-1.0, -0.5, 0.5, 1.0)
        for a, b in zip(cuts, cuts[1:]):
            s.rbox((a + 0.003, 0.0, 0.0), (b - 0.003, 0.3, 0.154), 0.012, pal.Ground_RoadPearl, seg=2)
        s.box((-1.0, 0.0, 0.153), (1.0, 0.02, 0.157), pal.Brass_Civic)
    return build


for _i in (0, 1):
    asset(f"Kerb_Pearl_{_i}", "Ground", bevel=0.0)(_kerb(_i))


@asset("TreePit_Pearl", "Ground", bevel=0.0)
def tree_pit_pearl(s):
    o, w = 0.6, 0.12
    for a, b in (((-1, -1), (1, -o)), ((-1, o), (1, 1)), ((-1, -o), (-o, o)), ((o, -o), (1, o))):
        s.face([(a[0], a[1], TOP), (b[0], a[1], TOP), (b[0], b[1], TOP), (a[0], b[1], TOP)], pal.Ground_PaveLimestone)
    for a, b in (((-o, -o), (o, -o + w)), ((-o, o - w), (o, o)), ((-o, -o + w), (-o + w, o - w)), ((o - w, -o + w), (o, o - w))):
        s.rbox((a[0], a[1], TOP - 0.05), (b[0], b[1], TOP + 0.012), 0.008, pal.Ground_RoadPearl, seg=1)
    g = o - w
    h = 0.012
    for a, b in (((-g, -g), (g, -g + h)), ((-g, g - h), (g, g)), ((-g, -g + h), (-g + h, g - h)), ((g - h, -g + h), (g, g - h))):
        s.box((a[0], a[1], TOP + 0.002), (b[0], b[1], TOP + 0.007), pal.Brass_Civic)
    s.box((-g, -g, TOP - 0.1), (g, g, TOP - 0.06), pal.Soil)


@asset("PaveInlay_Band_6m", "Ground", bevel=0.0)
def inlay_band(s):
    z = TOP + 0.004
    s.face([(-3, -0.41, z), (3, -0.41, z), (3, 0.41, z), (-3, 0.41, z)], pal.Ground_TileTeal)
    for y in (-0.425, 0.41):
        s.box((-3, y, z - 0.001), (3, y + 0.015, z + 0.002), pal.Brass_Civic)


@asset("PaveInlay_Ring_Q", "Ground", bevel=0.0)
def inlay_ring_q(s):
    z, r0, r1, n = TOP + 0.004, 4.36, 4.88, 16
    for k in range(n):
        a, b = math.radians(90 * k / n), math.radians(90 * (k + 1) / n)
        s.face([(r0 * math.cos(a), r0 * math.sin(a), z), (r1 * math.cos(a), r1 * math.sin(a), z), (r1 * math.cos(b), r1 * math.sin(b), z), (r0 * math.cos(b), r0 * math.sin(b), z)][::-1], pal.Ground_TileTeal)
    for r in (r0 - 0.015, r1):
        for k in range(n):
            a, b = math.radians(90 * k / n), math.radians(90 * (k + 1) / n)
            s.face([(r * math.cos(a), r * math.sin(a), z + 0.002), (r * math.cos(b), r * math.sin(b), z + 0.002), ((r + 0.015) * math.cos(b), (r + 0.015) * math.sin(b), z + 0.002), ((r + 0.015) * math.cos(a), (r + 0.015) * math.sin(a), z + 0.002)][::-1], pal.Brass_Civic)
