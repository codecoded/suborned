"""Brief 009 look options (Vela, 15 Oct 2026; Aster's open decision on the crossings' presence): candidate pieces for the options sheet, never adopted.
The crossing as built reads as two thin dotted lines at play zoom; the concepts read as a lit band. The levers tried, one piece family each:
  LightDotBand2_2/_1, LightDotBand3_2/_1   (b) the dot rows two or three dots wide: LightDotRow's dots, pitch and hairlines, more rows across
  LightBar_2, LightBar_1                   (c) the end modules LightBar_4 lacks, so a bar line is 4 + 4 + 2 + 1 m on any whole-metre road (the plan's `bars` form is [4] only)
  LightPool_2/_1, LightField_2/_1          (d) light spilt on the road: one additive quad per module under a line (pool, 1.3 m) or across the strip (field, 3.9 m), edges fading to nothing
Same conventions as crossing_flat.py: metres, Z up, long axis X, centred, faces up, 4 to 6 mm proud, no sides, nothing below z = 0, brass only as hairline and ring (SF-21).
The pool and the field are kind `glow` (DK_Cross_Pool: seafoam, translucent; Latch draws it additive and double-sided like the beam and halo). Their UVs are the box projection of the
face: u = metres along the line, v = metres across it from the centre, so the fade is 1 - |v| / half-width (half = 0.65 for the pool, 1.95 for the field)."""

from crossing_common import BRASS, LIGHT, flat, strip
from crossing_flat import DOT, DOT_RING, DP, F, PITCH
from geo import circle, rrect
from kit import asset
from mats import LIB, pal, register

register("DK_Cross_Pool", "glow", LIB["DK_Enamel_Seafoam"][1], 2.0, 0.40)   # the lights' seafoam; alpha is what the quad adds, the fade is the shader's
GLOW = 0.002                                                                # the glow quad lies under the lens (4 mm) and the brass (6 mm)
POOL_W, FIELD_W = 1.3, 3.9


def quad(s, length, width, z, mat):
    h, w = length / 2, width / 2
    flat(s, [(-h, -w), (h, -w), (h, w), (-h, w)], z, mat)


def band(s, rows, count, lod):
    """rows x count dots at the 0.5 m pitch both ways (each ring-framed at L0), between two hairlines 0.2 m outside the outer dot centres."""
    for r in range(rows):
        y = (r - (rows - 1) / 2) * PITCH
        for i in range(count):
            x = (i - (count - 1) / 2) * PITCH
            if lod == 0:
                strip(s, circle(DOT_RING, 8, x, y), circle(DOT, 8, x, y), BRASS, pal.Brass_Civic, closed=True)
                flat(s, circle(DOT, 8, x, y), LIGHT, pal.Cross_Light)
            else:
                flat(s, circle(DOT, 4, x, y), LIGHT, pal.Cross_Light)
    half, y0 = count * PITCH / 2, (rows - 1) * PITCH / 2 + 0.20
    for y in (-y0, y0):
        flat(s, [(-half, y - 0.015), (half, y - 0.015), (half, y + 0.015), (-half, y + 0.015)], BRASS, pal.Brass_Civic)


@asset("LightDotBand2_2", "Ground", bevel=0.0, near=lambda s: band(s, 2, 4, 1))
def light_dot_band2_2(s):
    """2 m module, two dots wide (8 dots, 0.22 m, brass ring each) at a 0.5 m pitch both ways, two 3 cm brass hairlines 0.9 m apart; long axis X, centred, ends butt."""
    band(s, 2, 4, 0)


@asset("LightDotBand2_1", "Ground", bevel=0.0, near=lambda s: band(s, 2, 2, 1))
def light_dot_band2_1(s):
    """1 m end module of LightDotBand2_2 (4 dots)."""
    band(s, 2, 2, 0)


@asset("LightDotBand3_2", "Ground", bevel=0.0, near=lambda s: band(s, 3, 4, 1))
def light_dot_band3_2(s):
    """2 m module, three dots wide (12 dots, 0.22 m, brass ring each) at a 0.5 m pitch both ways, two 3 cm brass hairlines 1.4 m apart; long axis X, centred, ends butt."""
    band(s, 3, 4, 0)


@asset("LightDotBand3_1", "Ground", bevel=0.0, near=lambda s: band(s, 3, 2, 1))
def light_dot_band3_1(s):
    """1 m end module of LightDotBand3_2 (6 dots)."""
    band(s, 3, 2, 0)


def bar_n(s, length, lod):
    """crossing_flat.bar() at any module length: the 5 cm brass frame, the seafoam lens inside it."""
    hi = lod == 0
    outer, inner = (rrect(length, DP, 0.14, 2), rrect(length - 2 * F, DP - 2 * F, 0.09, 2)) if hi else (rrect(length, DP, 0, 1), rrect(length - 2 * F, DP - 2 * F, 0, 1))
    strip(s, outer, inner, BRASS, pal.Brass_Civic, closed=True)
    flat(s, inner, LIGHT, pal.Cross_Light)


@asset("LightBar_2", "Ground", bevel=0.0, near=lambda s: bar_n(s, 2.0, 1))
def light_bar_2(s):
    """2 m end module of LightBar_4: seafoam lens 1.90 x 0.34 m in the same 5 cm brass frame, 2.00 x 0.44 m overall."""
    bar_n(s, 2.0, 0)


@asset("LightBar_1", "Ground", bevel=0.0, near=lambda s: bar_n(s, 1.0, 1))
def light_bar_1(s):
    """1 m end module of LightBar_4: lens 0.90 x 0.34 m in the 5 cm frame, 1.00 x 0.44 m overall."""
    bar_n(s, 1.0, 0)


@asset("LightPool_2", "Ground", bevel=0.0, near=lambda s: quad(s, 2.0, POOL_W, GLOW, pal.Cross_Pool))
def light_pool_2(s):
    """2 m of light spilt on the road under a line: one 2.0 x 1.3 m glow quad (kind glow, additive), 2 mm proud; fade = 1 - |v| / 0.65 (v = metres across from the centre). Ends butt."""
    quad(s, 2.0, POOL_W, GLOW, pal.Cross_Pool)


@asset("LightPool_1", "Ground", bevel=0.0, near=lambda s: quad(s, 1.0, POOL_W, GLOW, pal.Cross_Pool))
def light_pool_1(s):
    """1 m end module of LightPool_2 (1.0 x 1.3 m)."""
    quad(s, 1.0, POOL_W, GLOW, pal.Cross_Pool)


@asset("LightField_2", "Ground", bevel=0.0, near=lambda s: quad(s, 2.0, FIELD_W, GLOW, pal.Cross_Pool))
def light_field_2(s):
    """2 m of light spilt across the whole strip: one 2.0 x 3.9 m glow quad (kind glow, additive), 2 mm proud; fade = 1 - |v| / 1.95. Ends butt. Sits between the brass edge lines (y = +-2.0)."""
    quad(s, 2.0, FIELD_W, GLOW, pal.Cross_Pool)


@asset("LightField_1", "Ground", bevel=0.0, near=lambda s: quad(s, 1.0, FIELD_W, GLOW, pal.Cross_Pool))
def light_field_1(s):
    """1 m end module of LightField_2 (1.0 x 3.9 m)."""
    quad(s, 1.0, FIELD_W, GLOW, pal.Cross_Pool)
