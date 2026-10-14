"""The crossing's flush pieces (brief 009, A): a light bar across the strip, rows of light dots, the seafoam kerb inlay arc. Brass frames the light: a frame line
5 cm wide on the bar and 4 cm round each dot, hairlines 3 cm, never a field (SF-21). Faces point up, 4 to 6 mm proud, no sides, nothing below z = 0.

Bar: LightBar_4 spans the strip (every crossing strip is 4 m wide); its long axis is X, centred, so yaw it to lie across the walking direction; pitch it 1.0 m.
Dots: rows run along X, centred, in 2 m (4 dots) and 1 m (2 dots) modules at a 0.5 m pitch; end to end they join (no end frame), so any whole metre is a row.
Arc: a quarter ring, pivot at the arc centre, sweeping +X to +Y as the civic InlayArc pieces do: rotate about Z in quarter turns, and stand the centre at (corner + R, R)
from the crossing mouth's corner in the pavement frame; a pair, one each side of the mouth, rounds both corners."""

from crossing_common import BRASS, INLAY, LIGHT, flat, strip
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


def dots(s, count, lod):
    for i in range(count):
        x = (i - (count - 1) / 2) * PITCH
        if lod == 0:
            strip(s, circle(DOT_RING, 8, x, 0), circle(DOT, 8, x, 0), BRASS, pal.Brass_Civic, closed=True)
            flat(s, circle(DOT, 8, x, 0), LIGHT, pal.Cross_Light)
        else:
            flat(s, circle(DOT, 4, x, 0), LIGHT, pal.Cross_Light)
    half = count * PITCH / 2
    for y in (-0.20, 0.20):
        flat(s, [(-half, y - 0.015), (half, y - 0.015), (half, y + 0.015), (-half, y + 0.015)], BRASS, pal.Brass_Civic)


@asset("LightDotRow_2", "Ground", bevel=0.0, near=lambda s: dots(s, 4, 1))
def light_dot_row_2(s):
    """2 m row of four seafoam dots (0.22 m, 8-sided) at a 0.5 m pitch, each in a brass ring, between two 3 cm brass hairlines 0.4 m apart."""
    dots(s, 4, 0)


@asset("LightDotRow_1", "Ground", bevel=0.0, near=lambda s: dots(s, 2, 1))
def light_dot_row_1(s):
    """1 m end module of LightDotRow_2: two dots, the same pitch and hairlines."""
    dots(s, 2, 0)


def inlay(s, lod):
    n = 10 if lod == 0 else 4
    strip(s, arc(0, 0, R1, 0, 90, n), arc(0, 0, R0, 0, 90, n), INLAY, pal.Inlay_Seafoam)
    strip(s, arc(0, 0, R1 + HL, 0, 90, n), arc(0, 0, R1, 0, 90, n), BRASS, pal.Brass_Civic)
    strip(s, arc(0, 0, R0, 0, 90, n), arc(0, 0, R0 - HL, 0, 90, n), BRASS, pal.Brass_Civic)


@asset("CrossInlayArc_R24_W08", "Ground", bevel=0.0, near=lambda s: inlay(s, 1))
def cross_inlay_arc(s):
    """Quarter ring of seafoam faience, centreline R 2.4 m, 0.8 m wide (R 2.0 to 2.8), a 3 cm brass hairline on each edge; pivot = arc centre, +X to +Y."""
    inlay(s, 0)
