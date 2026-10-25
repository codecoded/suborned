"""The crossing's flush pieces (brief 009, A): a light bar across the strip, rows of light dots, the seafoam kerb inlay arc. Brass frames the light: a frame line
5 cm wide on the bar, hairlines 3 cm, never a field (SF-21). Faces point up, 3 to 6 mm proud, no sides, nothing below z = 0.
Brief 061 (Sky, 2026-10-25): LightDotRow_2 / _1 are re-cut without brass (a charcoal enamel seat under each light, UV x in metres along the row); CrossEdgeLine_1/2 and CrossInlayArc_R24_W08 are marked
retired (their builders stay until Splice has re-cut the export); LightBar_4 is untouched (the A pick has no bar).

Bar: LightBar_4 spans the strip (every crossing strip is 4 m wide); its long axis is X, centred, so yaw it to lie across the walking direction; pitch it 1.0 m.
Dots: rows run along X, centred, in 2 m (4 dots) and 1 m (2 dots) modules at a 0.5 m pitch; end to end they join (no end frame), so any whole metre is a row.
Arc: a quarter ring, pivot at the arc centre, sweeping +X to +Y as the civic InlayArc pieces do: rotate about Z in quarter turns, and stand the centre at (corner + R, R)
from the crossing mouth's corner in the pavement frame; a pair, one each side of the mouth, rounds both corners."""

from crossing_common import BRASS, INLAY, LIGHT, flat, strip  # noqa: F401
from geo import arc, circle, rrect
from kit import asset
from mats import pal

W, DP, F = 4.0, 0.44, 0.05    # bar: overall length, width, frame line
PITCH, DOT, DOT_RING = 0.5, 0.11, 0.15
R0, R1, HL = 2.0, 2.8, 0.03   # arc: inner and outer radius of the seafoam band, hairline


def bar(s, lod):
    hi = lod == 0
    outer, inner = (rrect(W, DP, 0.14, 2), rrect(W - 2 * F, DP - 2 * F, 0.09, 2)) if hi else (rrect(W, DP, 0, 1), rrect(W - 2 * F, DP - 2 * F, 0, 1))
    strip(s, outer, inner, BRASS, pal.Brass_Civic, closed=True)
    flat(s, inner, LIGHT, pal.Cross_Light)


@asset("LightBar_4", "Ground", bevel=0.0, near=lambda s: bar(s, 1))
def light_bar_4(s):
    """Seafoam light bar 3.90 x 0.34 m in a 5 cm brass frame, 4.00 x 0.44 m overall, flush in the carriageway."""
    bar(s, 0)


SEAT_Z, DOT_Z = 0.003, 0.005   # brief 061: the charcoal enamel seat and the light on it, 3 and 5 mm proud of the carriageway


def _uvface(s, pts, mat, uvs):
    """One polygon with explicit UVs (geo.Shape.decal's way, for any vertex count)."""
    bv = s.face(pts, mat)
    f = bv[0].link_faces[0]
    f[s._uvkeep] = 1
    s._pending_uv.append((f, uvs))


def dots(s, count, lod):
    """Brief 061 re-cut: no brass. Each dot is a light disc on a charcoal enamel seat (a dark dot when its light is gated off). UV u = metres along the module from its -X end, of the dot's centre,
    the same for every vertex of the dot (a dot is lit or dark, never cut); v = y in metres across the row. Latch gates a dot by _LitFrom <= u <= _LitTo."""
    half = count * PITCH / 2
    for i in range(count):
        x = (i - (count - 1) / 2) * PITCH
        for ring, z, mat in ((circle(DOT_RING, 8 if lod == 0 else 6, x, 0), SEAT_Z, pal.Enamel_Charcoal), (circle(DOT, 8 if lod == 0 else 4, x, 0), DOT_Z, pal.Cross_Light)):
            _uvface(s, [(px, py, z) for px, py in ring], mat, [(x + half, py) for _, py in ring])


@asset("LightDotRow_2", "Ground", bevel=0.0, near=lambda s: dots(s, 4, 1))
def light_dot_row_2(s):
    """2 m row of four light dots (0.22 m, 8-sided) at a 0.5 m pitch, each on a 0.30 m charcoal enamel seat (brief 061: the brass rings and hairlines are gone). UV x = metres along the row."""
    dots(s, 4, 0)


@asset("LightDotRow_1", "Ground", bevel=0.0, near=lambda s: dots(s, 2, 1))
def light_dot_row_1(s):
    """1 m end module of LightDotRow_2: two dots, the same pitch, seats and UVs (u 0 to 1)."""
    dots(s, 2, 0)


def inlay(s, lod):
    n = 10 if lod == 0 else 4
    strip(s, arc(0, 0, R1, 0, 90, n), arc(0, 0, R0, 0, 90, n), INLAY, pal.Inlay_Seafoam)
    strip(s, arc(0, 0, R1 + HL, 0, 90, n), arc(0, 0, R1, 0, 90, n), BRASS, pal.Brass_Civic)
    strip(s, arc(0, 0, R0, 0, 90, n), arc(0, 0, R0 - HL, 0, 90, n), BRASS, pal.Brass_Civic)


@asset("CrossInlayArc_R24_W08", "Ground", bevel=0.0, near=lambda s: inlay(s, 1), retired="brief 061 (Sky 2026-10-25): the seafoam inlay arcs go; the HaloRing marks where to wait")
def cross_inlay_arc(s):
    """Quarter ring of seafoam faience, centreline R 2.4 m, 0.8 m wide (R 2.0 to 2.8), a 3 cm brass hairline on each edge; pivot = arc centre, +X to +Y."""
    inlay(s, 0)


EW = 0.04   # edge line width


def edge(s):
    flat(s, [(-1.0, -EW / 2), (1.0, -EW / 2), (1.0, EW / 2), (-1.0, EW / 2)], BRASS, pal.Brass_Civic)


def edge1(s):
    flat(s, [(-0.5, -EW / 2), (0.5, -EW / 2), (0.5, EW / 2), (-0.5, EW / 2)], BRASS, pal.Brass_Civic)


@asset("CrossEdgeLine_2", "Ground", bevel=0.0, near=edge, retired="brief 061 (Sky 2026-10-25): no brass on the crossing edge; the dot rows are the edge")
def cross_edge_line_2(s):
    """Brass edge line, 2.00 x 0.04 m, 6 mm proud, one quad: the frame line on each long edge of the crossing strip (crossing-future-play-2). Long axis X, centred; ends butt, no seam."""
    edge(s)


@asset("CrossEdgeLine_1", "Ground", bevel=0.0, near=edge1, retired="brief 061 (Sky 2026-10-25): no brass on the crossing edge; the dot rows are the edge")
def cross_edge_line_1(s):
    """1 m end module of CrossEdgeLine_2 (1.00 x 0.04 m): a line of any whole metre is 2 m modules plus at most one of these."""
    edge1(s)
