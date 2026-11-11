"""Civic paving (brief 008 B1): the slab material and the seafoam inlay pieces, as in the 26 Sep paintings (gameplay-plan,
mission-block, inez-cutaway): large pale slabs (1 x 2 m, running bond, soft joints) with seafoam bands 0.6 to 1.2 m wide and
arcs round the lamps and the fountain.

DK_Pave_Civic  stone, 4 m metric tile (pave_sheet.py sheet "pave_civic": albedo + normal + mask at 1 mm texels). Mean unlit
               #E4DACA, dE2000 25.3 from the guard colour; 0% of texels under 20. Apply to the plot surfaces of the square, the
               café terrace and the office apron (world UVs, Asphalt_Fine's way).
Inlay*         flat seafoam faience strips 4 mm proud of the slab: the band design is out/decokit-008/paving-bands.json.
               Straight bands are 2 m modules; arcs are quarter turns with the pivot at the arc centre (lamp or fountain),
               sweeping from +X to +Y; rotate about Z to set the start angle.
               Brief 107 (Vela): drawn in DK_Inlay_SeafoamNight, DK_Inlay_Seafoam's twin (glows107.py): the same look by day, emission black.
               Round 2 (Sky: the flat lit band was "that blue circle"): the band stays dark at night and a thin line of light runs along
               each LONG edge of the RINGS (InlayArc_*; glows107.EDGE, DK_Glow_InlayEdge: Cross Glow mode 2, alpha 0 by day). The quarter
               ends get none: the quarters lie end to end, so an end line would dash the ring. The straight bands (the spokes) carry none
               (Sky: "remove that thin inner line from edge to center")."""

import math

from geo import arc
from kit import asset
import glows107
import mats
from mats import pal, register

# Plaza paving (brief 048, Vela 2026-10-24; Glint: the plaza blows out white in the long view and exposure cannot fix it, the albedo is the cause): the sheet's mean was #E4DACA; the tint
# multiplies it by (.888, .807, .644) in linear light, 19 percent darker in luminance and warm (toward the testbed's buff-gold slab), the joints and slab-to-slab drift untouched.
register("DK_Pave_Civic", "stone", "#F2E8D2")
# Unity: the generator writes the .mat (it was the legacy one, never rewritten), same guid, the legacy layout (UV = world metres / 4 over a 4 m sheet: scale 1), maps by the same names, tint as above
mats.GROUND.setdefault("DK_Pave_Civic", dict(albedo="pave_civic_v1.png", normal="pave_civic_v1_normal.png", mask="pave_civic_v1_mask.png", tile=(1.0, 1.0), smoothness=1.0, tint="#F2E8D2"))
mats.TEXTURES.setdefault("DK_Pave_Civic", ("pave_civic_v1.png", 4.0))

PROUD = 0.004


def edge_line(s, path, into, left=None):
    """The edge light along one band edge (`path`, local XY polyline on the band's border): a flat strip from the border glows107.EDGE_W INTO the band
    (`into` +1: the left of travel, -1: the right), UV v = the distance from the border in metres (u the length along it), so Cross Glow mode 2 is
    brightest on the border and fades to nothing EDGE_W in. Inside the band's own outline, so the piece's footprint, size and pivot stay as they were.
    Explicit UVs (the decal path), never the box projection."""
    w, z = glows107.EDGE_W, PROUD + glows107.EDGE_LIFT
    n = len(path)
    side = []
    for i in range(n):   # the left normal at each point: given (an arc's exact radial, so its ends stay on the quarter's axes), or the averaged chord normal
        if left is not None:
            lx, ly = left[i]
        else:
            a, b = path[max(i - 1, 0)], path[min(i + 1, n - 1)]
            dx, dy = b[0] - a[0], b[1] - a[1]
            d = math.hypot(dx, dy)
            lx, ly = -dy / d, dx / d
        side.append((lx * into, ly * into))
    u = 0.0
    for i in range(n - 1):
        (x0, y0), (x1, y1) = path[i], path[i + 1]
        (nx0, ny0), (nx1, ny1) = side[i], side[i + 1]
        u1 = u + math.hypot(x1 - x0, y1 - y0)
        quad = [(x0, y0, z), (x1, y1, z), (x1 + nx1 * w, y1 + ny1 * w, z), (x0 + nx0 * w, y0 + ny0 * w, z)]
        if into < 0:   # keep it counter-clockwise seen from above (the shader draws both sides; this keeps the normal up)
            quad = quad[::-1]
            uv = ((u, w), (u1, w), (u1, 0.0), (u, 0.0))
        else:
            uv = ((u, 0.0), (u1, 0.0), (u1, w), (u, w))
        s.decal(quad, pal.Glow_InlayEdge, uv=uv, lift=0.0)
        u = u1


def _straight(s, hw):
    """A straight band: the enamel only. No edge light (Sky 2026-11-11, on the spoke from the outer ring to the fountain: "remove that thin inner line
    from edge to center"): only the rings carry it."""
    s.extrude([(-1.0, -hw), (1.0, -hw), (1.0, hw), (-1.0, hw)], 0.0, PROUD, pal.Inlay_SeafoamNight, bottom=False)


@asset("InlayBand_06_2", "Ground", bevel=0.0, height=PROUD)
def inlay_band_06(s):
    _straight(s, 0.3)


@asset("InlayBand_12_2", "Ground", bevel=0.0, height=PROUD)
def inlay_band_12(s):
    _straight(s, 0.6)


def _arc_band(s, radius, width, n):
    r0, r1 = radius - width / 2, radius + width / 2
    outer = arc(0, 0, r1, 0, 90, n)
    inner = arc(0, 0, r0, 0, 90, n)[::-1]
    s.extrude(outer + inner, 0.0, PROUD, pal.Inlay_SeafoamNight, bottom=False)
    centre = [(-math.cos(math.radians(90 * i / n)), -math.sin(math.radians(90 * i / n))) for i in range(n + 1)]   # left of CCW travel: toward the centre
    edge_line(s, arc(0, 0, r1, 0, 90, n), +1, centre)   # into the band from the outer edge
    edge_line(s, arc(0, 0, r0, 0, 90, n), -1, centre)   # and from the inner edge


@asset("InlayArc_R2_W06", "Ground", bevel=0.0, height=PROUD)
def inlay_arc_r2(s):
    """Quarter ring, centreline R 2.0 m, 0.6 wide: four make the ring round a sphere lamp."""
    _arc_band(s, 2.0, 0.6, 10)


@asset("InlayArc_R4_W06", "Ground", bevel=0.0, height=PROUD)
def inlay_arc_r4(s):
    """Quarter ring, centreline R 4.3 m, 0.6 wide: the inner ring of the fountain, between the pool coping (R 3.6) and the planters (R 5.36)."""
    _arc_band(s, 4.3, 0.6, 14)


@asset("InlayArc_R9_W12", "Ground", bevel=0.0, height=PROUD)
def inlay_arc_r9(s):
    """Quarter ring, centreline R 8.6 m, 1.2 wide: the outer ring of the fountain, 1.5 m outside the planter band (R 7.06)."""
    _arc_band(s, 8.6, 1.2, 20)
