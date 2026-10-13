"""Civic paving (brief 008 B1): the slab material and the seafoam inlay pieces, as in the 26 Sep paintings (gameplay-plan,
mission-block, inez-cutaway): large pale slabs (1 x 2 m, running bond, soft joints) with seafoam bands 0.6 to 1.2 m wide and
arcs round the lamps and the fountain.

DK_Pave_Civic  stone, 4 m metric tile (pave_sheet.py sheet "pave_civic": albedo + normal + mask at 1 mm texels). Mean unlit
               #E4DACA, dE2000 25.3 from the guard colour; 0% of texels under 20. Apply to the plot surfaces of the square, the
               café terrace and the office apron (world UVs, Asphalt_Fine's way).
Inlay*         flat seafoam faience strips 4 mm proud of the slab: the band design is out/decokit-008/paving-bands.json.
               Straight bands are 2 m modules; arcs are quarter turns with the pivot at the arc centre (lamp or fountain),
               sweeping from +X to +Y; rotate about Z to set the start angle."""

import math

from geo import arc
from kit import asset
import mats
from mats import pal, register

register("DK_Pave_Civic", "stone", "#FFFFFF")
mats.TEXTURES.setdefault("DK_Pave_Civic", ("pave_civic_v1.png", 4.0))

PROUD = 0.004


@asset("InlayBand_06_2", "Ground", bevel=0.0)
def inlay_band_06(s):
    s.extrude([(-1.0, -0.3), (1.0, -0.3), (1.0, 0.3), (-1.0, 0.3)], 0.0, PROUD, pal.Inlay_Seafoam, bottom=False)


@asset("InlayBand_12_2", "Ground", bevel=0.0)
def inlay_band_12(s):
    s.extrude([(-1.0, -0.6), (1.0, -0.6), (1.0, 0.6), (-1.0, 0.6)], 0.0, PROUD, pal.Inlay_Seafoam, bottom=False)


def _arc_band(s, radius, width, n):
    r0, r1 = radius - width / 2, radius + width / 2
    outer = arc(0, 0, r1, 0, 90, n)
    inner = arc(0, 0, r0, 0, 90, n)[::-1]
    s.extrude(outer + inner, 0.0, PROUD, pal.Inlay_Seafoam, bottom=False)


@asset("InlayArc_R2_W06", "Ground", bevel=0.0)
def inlay_arc_r2(s):
    """Quarter ring, centreline R 2.0 m, 0.6 wide: four make the ring round a sphere lamp."""
    _arc_band(s, 2.0, 0.6, 10)


@asset("InlayArc_R4_W06", "Ground", bevel=0.0)
def inlay_arc_r4(s):
    """Quarter ring, centreline R 4.3 m, 0.6 wide: the inner ring of the fountain, between the pool coping (R 3.6) and the planters (R 5.36)."""
    _arc_band(s, 4.3, 0.6, 14)


@asset("InlayArc_R9_W12", "Ground", bevel=0.0)
def inlay_arc_r9(s):
    """Quarter ring, centreline R 8.6 m, 1.2 wide: the outer ring of the fountain, 1.5 m outside the planter band (R 7.06)."""
    _arc_band(s, 8.6, 1.2, 20)
