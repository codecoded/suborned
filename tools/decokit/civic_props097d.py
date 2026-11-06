"""Brief 097 final pass (Vela, 2026-11-06): the audit's last pieces at 3 brought to the bar (briefs/097/vela/audit/audit.md). Store sizes and siting; each
declares the replaced piece's collision and catalogue size (collide097.json); palette materials; AO baked (vertex_ao.PIECES).

Finish copies (the store builder, its geometry and triangles as they are, the off-palette enamels swapped to the zone's):
  Streetlamp_Play     the old town's lamp in the zone's finish: seafoam pole, ivory dish (zone only: kitrefs.ts zone twins; the red one stays outside).
  Bollard_Play        the old bollard: seafoam bands, chrome ring (zone only).
  SandwichBoard_Play  the A-frame: teal panels, ivory kick panel; the poster faces and their UVs untouched (billboards.ts maps the sheet onto them).
  Tile_Tactile_Play   the crossing module: the blister strip in boulevard coral, the panel in the ground set's teal.
New forms on the store footprint:
  Roof_Tank_Play      the water sphere on a streamline ivory drum with chrome bands (was four lattice legs), two teal bands, a chrome cap and spire.
  Roof_Mast_Play      the beacon mast as a tapering ivory column with seafoam bands and chrome rings at the old lens heights (was a lattice), red beacon kept.
TreePit_Pearl (same id): the pit gains a chrome grille over the soil, a brass hairline frame (as before) and four opal light studs at the corners.
Pivot as the pieces they copy."""
import math

import mats
from civic_props097 import ring_prof
from geo import dome
from kit import REGISTRY, asset
from mats import pal, register
from signals import puck

register("DK_Brass_Civic", "brass", mats.BRASS)
register("DK_Enamel_Teal__boulevard", "enamel", "#C8553D")


def finish(base, swaps, extra=None):
    """A copy of `base`'s builder with materials swapped (old name -> palette attribute) and an optional addition."""
    fn = REGISTRY[base]["fn"]

    def build(s):
        fn(s)
        for a, b in swaps.items():
            m = mats.get(a)
            if m in s.mats:
                s.mats[s.mats.index(m)] = getattr(pal, b)
        if extra:
            extra(s)
    return build


def _twin(name, base, swaps, extra=None):
    spec = REGISTRY[base]
    asset(name, spec["cat"], bevel=spec["bevel"], lean=spec.get("lean", 1.0))(finish(base, swaps, extra))


_twin("Streetlamp_Play", "Streetlamp", {"DK_Enamel_Red": "Enamel_Seafoam", "DK_Enamel_Teal": "Enamel_Ivory"})
_twin("Bollard_Play", "Bollard", {"DK_Enamel_Red": "Enamel_Seafoam", "DK_Steel": "Chrome"})
_twin("SandwichBoard_Play", "SandwichBoard", {"DK_Enamel_Red": "Enamel_Teal", "DK_Enamel_Mustard": "Enamel_Ivory"})
_twin("Tile_Tactile_Play", "Tile_Tactile", {"DK_Tile_Red": "Enamel_Teal__boulevard", "DK_Tile_Blue": "Ground_TileTeal"})


# ----------------------------------------------------------------------------- roofs
def _ball(s, c, r, mat, seg=32, n=12):
    with s.at(c):
        s.lathe([(r * math.sin(math.pi * k / n), -r * math.cos(math.pi * k / n)) for k in range(n + 1)], mat, seg=seg)


@asset("Roof_Tank_Play", "Rooftop", bevel=0.0)
def roof_tank_play(s):
    s.lathe([(0.0, 0.0), (2.0, 0.0), (2.0, 0.35), (1.85, 0.5), (0.0, 0.5)], pal.Limestone, seg=32)                       # stone base
    s.lathe([(1.7, 0.5), (1.55, 1.2), (1.45, 4.0), (1.6, 4.6)], pal.Enamel_Ivory, seg=32, cap_bottom=False, cap_top=False)  # streamline drum
    for z in (1.4, 2.6, 3.8):
        s.lathe(ring_prof(1.5, 1.56, z, z + 0.18), pal.Chrome, seg=32, cap_bottom=False, cap_top=False)                  # chrome bands
    _ball(s, (0, 0, 6.6), 2.4, pal.Hull_Ivory)
    for z in (6.4, 7.6):
        s.lathe([(2.4 - 0.25 * (z > 7), z), (2.42 - 0.25 * (z > 7), z + 0.2), (2.4 - 0.25 * (z > 7), z + 0.4)], pal.Enamel_Teal, seg=32, cap_bottom=False, cap_top=False)
    with s.at((0, 0, 8.95)):
        s.lathe([(0.6, -0.05), (0.62, 0.06)] + dome(0.62, 0.06, 0.28, n=4)[1:], pal.Chrome, seg=20, cap_bottom=False)    # chrome cap
    s.lathe([(0.0, 9.2), (0.08, 9.2), (0.02, 10.4), (0.0, 10.4)], pal.Chrome, seg=8)                                     # spire


@asset("Roof_Mast_Play", "Rooftop", bevel=0.0)
def roof_mast_play(s):
    s.lathe([(1.8, 0.0), (1.8, 1.0), (0.0, 1.0)], pal.Limestone, seg=24, cap_bottom=False)
    s.lathe([(1.83, 0.55), (1.83, 0.8)], pal.Enamel_Teal, seg=24, cap_bottom=False, cap_top=False)
    s.lathe([(0.75, 1.0), (0.55, 4.0), (0.38, 10.0), (0.22, 17.0)], pal.Enamel_Ivory, seg=20, cap_bottom=False, cap_top=False)  # tapering column
    for z, r in ((6.0, 1.7), (10.5, 1.3), (14.5, 0.9)):                                                                     # rings at the old lens heights
        col = 0.55 - (0.55 - 0.38) * (z - 4.0) / 6.0 if z < 10 else 0.38 - 0.16 * (z - 10.0) / 7.0
        s.lathe([(col, z - 0.1), (r, z - 0.04), (r, z + 0.04), (col, z + 0.1)], pal.Chrome, seg=24, cap_bottom=False, cap_top=False)
        s.lathe(ring_prof(col, col + 0.03, z - 0.6, z - 0.3), pal.Enamel_Seafoam, seg=20, cap_bottom=False, cap_top=False)
    s.lathe([(0.0, 17.0), (0.12, 17.0), (0.03, 20.6), (0.0, 20.6)], pal.Chrome, seg=8)
    _ball(s, (0, 0, 17.3), 0.28, pal.Signal_Red, seg=12, n=6)


# ----------------------------------------------------------------------------- the tree pit (same id)
_PIT = REGISTRY["TreePit_Pearl"]["fn"]


@asset("TreePit_Pearl", "Ground", bevel=0.0, floor="seat")   # loaded after pave026 (kit.FAMILIES)
def tree_pit_pearl(s):
    _PIT(s)
    import pave026
    z, g = pave026.PIT, 0.6 - 0.12
    for k in range(-4, 5):                                                                    # the chrome grille bars over the soil, round the trunk
        y = k * 0.1
        half = math.sqrt(max(0.0, g * g - 0.0)) - 0.02
        if abs(y) < 0.16:
            for x0, x1 in ((-half, -0.16), (0.16, half)):
                s.box((x0, y - 0.012, z - 0.012), (x1, y + 0.012, z - 0.002), pal.Chrome)
        else:
            s.box((-half, y - 0.012, z - 0.012), (half, y + 0.012, z - 0.002), pal.Chrome)
    for x in (-1, 1):                                                                         # the grille's chrome edge bars
        s.box((x * (g - 0.03) - 0.012, -g + 0.02, z - 0.012), (x * (g - 0.03) + 0.012, g - 0.02, z - 0.002), pal.Chrome)
    s.lathe(ring_prof(0.16, 0.18, z - 0.012, z - 0.002), pal.Chrome, seg=16, cap_bottom=False, cap_top=False)               # the trunk ring
    for x, y in ((-1, -1), (1, -1), (1, 1), (-1, 1)):                                          # opal light studs in the pearl surround's corners
        with s.at((x * 0.54, y * 0.54, z - 0.004)):
            s.lathe(puck(0.026, 0.0, 0.006, 0.002, n=1), pal.Lamp, seg=8)
