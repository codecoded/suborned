"""The testbed's flooring as kit pieces (brief 026, Vela; Sky 2026-10-20: "the flooring was better I think in the testbed").

What the 2.5D testbed walked on (civic-paving-material.js, fountain-plaza.js, civic-roads.js, cafe-terrace art): honed warm-ivory limestone in 1 m slabs with fine 6 mm joints and a satin
face (2 m tile, tint #f2dfbd), set with bands and rings of glazed teal-blue ceramic tile laid as individual bevelled tiles on a 0.72 m pitch with pale grout (#669fab, bands 0.82 m, ring 0.52 m), a
pearl-marble kerb cap and tree-bed foundations, and a gold pinstripe along every edge (brass here, hairline only). Today's ground set (brief 026 rounds 1 and 2) lays the old city's flag sheet,
a random running bond in tan and grey that reads as brick at play zoom, with flat seafoam terrazzo inlays and a granite kerb with a sett gutter.

  Pave_Slab           2 x 2 m flat module, DK_Ground_PaveLimestone (the testbed's limestone sheet, 2 m tile = 2 x 2 slabs); lies at z 0 (brief 055 K-1: a flat piece is on its pivot and the world sets its height; it was modelled at 0.15 and the host laid it back), a drop-in for Pave_Flags_i (same size, same pivot)
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

FLAT = 0.0          # a flat piece lies on its pivot: the world places it (z 0.181 for the slab), the host adds nothing (brief 055, K-1)
INLAY = 0.004       # an inlay lies 4 mm proud of the piece it is laid on
PIT = 0.110         # tree pit: the limestone face, the soil at z 0 under it; the module is 0.112 high and the world places it so that top is 3 mm over the slab, which puts this face flush
register("DK_Ground_PaveLimestone", "ground", "#FFFFFF")
register("DK_Ground_TileTeal", "ground", "#FFFFFF")
TEXTURES.setdefault("DK_Ground_PaveLimestone", ("ground_pavelimestone_v1.png", 2.0))
TEXTURES.setdefault("DK_Ground_TileTeal", ("ground_tileteal_v1.png", 1.44))
mats.GROUND.setdefault("DK_Ground_PaveLimestone", dict(albedo="ground_pavelimestone_v1.png", normal="ground_pavelimestone_v1_normal.png", mask="ground_pavelimestone_v1_mask.png", tile=(2.0, 2.0), smoothness=1.0))
mats.GROUND.setdefault("DK_Ground_TileTeal", dict(albedo="ground_tileteal_v1.png", normal="ground_tileteal_v1_normal.png", mask="ground_tileteal_v1_mask.png", tile=(1.44, 1.44), smoothness=1.0))


@asset("Pave_Slab", "Ground", bevel=0.0, floor="seat")
def pave_slab(s):
    s.face([(-1, -1, FLAT), (1, -1, FLAT), (1, 1, FLAT), (-1, 1, FLAT)], pal.Ground_PaveLimestone)


def _kerb(i):
    def build(s):
        cuts = (-1.0, 0.0, 1.0) if i == 0 else (-1.0, -0.5, 0.5, 1.0)
        for a, b in zip(cuts, cuts[1:]):
            s.rbox((a + 0.003, 0.0, 0.0), (b - 0.003, 0.3, 0.154), 0.012, pal.Ground_RoadPearl, seg=2)
        s.box((-1.0, 0.0, 0.153), (1.0, 0.02, 0.157), pal.Brass_Civic)
    return build


for _i in (0, 1):
    asset(f"Kerb_Pearl_{_i}", "Ground", bevel=0.0)(_kerb(_i))


@asset("TreePit_Pearl", "Ground", bevel=0.0, floor="seat")
def tree_pit_pearl(s):
    """Brief 055 K-1: the module stands on its pivot (soil at z 0, 0.112 high as catalogued). Limestone face flush with the slab round it, the pearl surround 2 mm proud with its brass hairline."""
    o, w = 0.6, 0.12
    for a, b in (((-1, -1), (1, -o)), ((-1, o), (1, 1)), ((-1, -o), (-o, o)), ((o, -o), (1, o))):
        s.face([(a[0], a[1], PIT), (b[0], a[1], PIT), (b[0], b[1], PIT), (a[0], b[1], PIT)], pal.Ground_PaveLimestone)
    for a, b in (((-o, -o), (o, -o + w)), ((-o, o - w), (o, o)), ((-o, -o + w), (-o + w, o - w)), ((o - w, -o + w), (o, o - w))):
        s.rbox((a[0], a[1], PIT - 0.05), (b[0], b[1], PIT + 0.002), 0.0015, pal.Ground_RoadPearl, seg=1)
    g = o - w
    h = 0.012
    for a, b in (((-g, -g), (g, -g + h)), ((-g, g - h), (g, g)), ((-g, -g + h), (-g + h, g - h)), ((g - h, -g + h), (g, g - h))):
        s.box((a[0], a[1], PIT + 0.0005), (b[0], b[1], PIT + 0.002), pal.Brass_Civic)
    s.box((-g, -g, 0.0), (g, g, 0.04), pal.Soil)


@asset("PaveInlay_Band_6m", "Ground", bevel=0.0, floor="seat")
def inlay_band(s):
    z = INLAY
    s.face([(-3, -0.41, z), (3, -0.41, z), (3, 0.41, z), (-3, 0.41, z)], pal.Ground_TileTeal)
    for y in (-0.425, 0.41):
        s.box((-3, y, z - 0.001), (3, y + 0.015, z + 0.002), pal.Brass_Civic)


@asset("PaveInlay_Ring_Q", "Ground", bevel=0.0, floor="seat")
def inlay_ring_q(s):
    z, r0, r1, n = INLAY, 4.36, 4.88, 16
    for k in range(n):
        a, b = math.radians(90 * k / n), math.radians(90 * (k + 1) / n)
        s.face([(r0 * math.cos(a), r0 * math.sin(a), z), (r1 * math.cos(a), r1 * math.sin(a), z), (r1 * math.cos(b), r1 * math.sin(b), z), (r0 * math.cos(b), r0 * math.sin(b), z)][::-1], pal.Ground_TileTeal)
    for r in (r0 - 0.015, r1):
        for k in range(n):
            a, b = math.radians(90 * k / n), math.radians(90 * (k + 1) / n)
            s.face([(r * math.cos(a), r * math.sin(a), z + 0.002), (r * math.cos(b), r * math.sin(b), z + 0.002), ((r + 0.015) * math.cos(b), (r + 0.015) * math.sin(b), z + 0.002), ((r + 0.015) * math.cos(a), (r + 0.015) * math.sin(a), z + 0.002)][::-1], pal.Brass_Civic)
