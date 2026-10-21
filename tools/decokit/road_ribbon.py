"""The testbed's road, as kit pieces (brief 026, Vela): light set in the road, white pearl lozenges, chrome panel seams, over a sealed enamel carriageway.

What the 2.5D testbed (civic-road-guides.js, civic-road-finish.js, civic-roads.js) drew and the world export has no word for:
  RoadRibbon_Straight_6m   a continuous warm lens 0.12 m wide set flush in the carriageway beside the gutter, between two brass hairlines, with a soft light pool on the carriageway side
  RoadRibbon_Arc90_R4/R8   the same round a 90 degree corner (inner radius 4 or 8 m to the lens centre), for the kerb returns
  RoadMark_Lozenge         the pearl diamond that replaces the dashed lane line (1.7 x 0.48 m, every 8.5 m on the centre line): no paint, no zebra
  RoadMark_Seam            a chrome joint across the enamel (4.6 m, every 9 m): the carriageway reads as panels, not poured
Frame (all four): lying along +X, centred, z 0 = the carriageway top (flush: the lens is 4 mm up, the hairlines 7 mm), the carriageway on the +Y side of the ribbon (mirror with yaw 180 for the
far kerb). The ribbon is light the way the crossings are: emissive, drawn unlit (DK_Road_Lens); its state (off by day, warm at evening and night: testbed `setLighting`, auto / on / off) is the
lighting rig's, not the piece's. The pool is a `glow` quad (alpha is what it adds; the fade across it is the shader's, as DK_Cross_Pool).

Surface materials (ground set, textures from road_tex.py out of the testbed's art): DK_Ground_RoadEnamel (#AED0E6, the testbed's powder blue: the carriageway, Sky's pick of 20 Oct), DK_Ground_RoadPearl (#F1EEE6, kerb cap and lozenge stone). Gold of the testbed's kerb crown is DK_Brass_Civic here (brass, not gold, at street level).
This family is not in kit.FAMILIES yet: add "road_ribbon" there to build the pieces (the review script loads it itself).
"""

import math

import mats
from kit import asset
from mats import TEXTURES, pal, register

# ----------------------------------------------------------------------------- materials
register("DK_Road_Lens", "emit", "#FFE6B5", 4.0)             # testbed ribbonTint #ffe6b5 x strength 2.5: warm cream, drawn unlit
register("DK_Road_Pool", "glow", "#FFE6B5", 2.0, 0.22)       # the light pool on the carriageway: alpha added, fade in the shader
register("DK_Ground_RoadEnamel", "ground", "#FFFFFF")
register("DK_Ground_RoadPearl", "ground", "#FFFFFF")
for _role in ("RoadEnamel", "RoadPearl"):
    TEXTURES.setdefault(f"DK_Ground_{_role}", (f"ground_{_role.lower()}_v1.png", 2.0))
mats.GROUND.setdefault("DK_Ground_RoadEnamel", dict(albedo="ground_roadenamel_v1.png", normal="ground_roadenamel_v1_normal.png", mask="ground_roadenamel_v1_mask.png", tile=(2.0, 2.0), smoothness=1.0))
mats.GROUND.setdefault("DK_Ground_RoadPearl", dict(albedo="ground_roadpearl_v1.png", normal="ground_roadpearl_v1_normal.png", mask="ground_roadpearl_v1_mask.png", tile=(2.0, 2.0), smoothness=1.0))

LENS_W, HAIR_W = 0.12, 0.02
Z_LENS, Z_HAIR, Z_POOL = 0.004, 0.007, 0.010
POOL_W = 1.0


def _frame(path, d0, d1):
    """Points of the offset line d0..d1 across a polyline (left of travel = +d): [(p_d0, p_d1), ...]."""
    out = []
    for i, p in enumerate(path):
        a, b = path[max(i - 1, 0)], path[min(i + 1, len(path) - 1)]
        dx, dy = b[0] - a[0], b[1] - a[1]
        n = math.hypot(dx, dy)
        nx, ny = -dy / n, dx / n
        out.append(((p[0] + nx * d0, p[1] + ny * d0), (p[0] + nx * d1, p[1] + ny * d1)))
    return out


def _band(s, path, d0, d1, z, mat):
    """A flat strip along `path` from offset d0 to d1 at height z, facing +Z."""
    fr = _frame(path, d0, d1)
    for (a0, a1), (b0, b1) in zip(fr, fr[1:]):
        s.face([(a0[0], a0[1], z), (b0[0], b0[1], z), (b1[0], b1[1], z), (a1[0], a1[1], z)], mat)


def ribbon(s, path):
    """The ribbon section along `path` (the lens centre line): brass hairline, lens, brass hairline, and the pool on the +d side."""
    h = LENS_W / 2
    _band(s, path, -h - HAIR_W, -h, Z_HAIR, pal.Brass_Civic)
    _band(s, path, -h, h, Z_LENS, pal.Road_Lens)
    _band(s, path, h, h + HAIR_W, Z_HAIR, pal.Brass_Civic)
    _band(s, path, h + HAIR_W, h + HAIR_W + POOL_W, Z_POOL, pal.Road_Pool)


def _line(length, n=1):
    return [(-length / 2 + length * i / n, 0.0) for i in range(n + 1)]


def _arc(radius, deg=90.0, n=12):
    """Counter-clockwise arc from (0,-r) .. the carriageway on the inside (+d = toward the centre) so the piece reads as a kerb return seen from the road."""
    return [(radius * math.sin(math.radians(deg * i / n)), -radius * math.cos(math.radians(deg * i / n)) + radius) for i in range(n + 1)]


@asset("RoadRibbon_Straight_6m", "Ground", bevel=0.0)
def ribbon_straight(s):
    ribbon(s, _line(6.0, 2))


@asset("RoadRibbon_Arc90_R4", "Ground", bevel=0.0)
def ribbon_arc4(s):
    ribbon(s, _arc(4.0, n=10))


@asset("RoadRibbon_Arc90_R8", "Ground", bevel=0.0)
def ribbon_arc8(s):
    ribbon(s, _arc(8.0, n=14))


@asset("RoadMark_Lozenge", "Ground", bevel=0.0)
def road_lozenge(s):
    """The pearl diamond (testbed marking(): 1.7 x 0.48 m, a flat lozenge, no paint): a stone inlay 5 mm proud, a brass hairline round it."""
    w, h = 0.85, 0.24
    for (ww, hh), z, mat in (((w + 0.05, h + 0.03), 0.0045, pal.Brass_Civic), ((w, h), 0.005, pal.Ground_RoadPearl)):
        s.face([(-ww, 0, z), (0, -hh, z), (ww, 0, z), (0, hh, z)], mat)


@asset("RoadMark_Seam", "Ground", bevel=0.0)
def road_seam(s):
    """A chrome joint across the enamel: 4.6 m x 0.03 m, 4 mm proud (the testbed's `seamParts`, every 9 m)."""
    s.face([(-0.015, -2.3, 0.004), (0.015, -2.3, 0.004), (0.015, 2.3, 0.004), (-0.015, 2.3, 0.004)], pal.Chrome)
