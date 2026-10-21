"""CafeFascia (brief 026, Vela): the testbed café's sign, as a kit piece on CafeBay_v2's frame.

The 2.5D testbed's café carried one wide horizontal sign over the door (cafe-ambience.js `sign`; model-report.json `ambience.sign`: channel letters "CAFÉ", 4.45 m wide, letter size 0.46 m,
"warm ivory with coral Streamline wings"): a deep-teal plate in a thin pearl frame, ivory channel letters lit from inside, and three coral speed lines either side of the word. It was the first
thing the eye took at night and the clearest door mark at distance. CafeBay_v2 has only the vertical blade (BladeSign_Cafe style, Glyph's waypoint, brief 022), which at play zoom is a thin
dark fin edge-on to the camera at yaw 30.

Frame: as CafeBay_v2 (pivot = centre of the flat front, block (-10.7, -29), yaw 90; front -Y; the building on +Y; z 0 = pavement). The plate stands on the generator's upper mass just above the
seafoam belt: z 4.86 to 5.72, centred on the door (x 0), 0.09 m proud of the wall (y 0). It does not touch the belt, the awning or the blade (x 3.6). The blade stays: it reads from the side
street, the plate reads from the front.
Materials: DK_Enamel_Teal plate, chrome frame bead, brass hairline inside it (hairline only), DK_Sign_Ivory letters and DK_Sign_Coral wings (emit: lit by day at a low strength, brighter at
evening and night by the lighting rig, testbed values day 0.6 / evening 2.1 / night 3.2 for the letters and 0.35 / 1.2 / 2.0 for coral). Static: nothing animates.
This family is not in kit.FAMILIES yet: add "cafe_fascia" there to build it (the review script loads it itself).
"""

import math

import civic_cafe  # noqa: F401  (GLYPH F and E)
from furnish_parts import GLYPH, letters
from geo import fillet
from kit import asset
from mats import pal, register
from structures import ccw

register("DK_Sign_Ivory", "emit", "#FFF1D2", 3.0)   # the letters: warm ivory, drawn lit (testbed: sign illuminated ivory)
register("DK_Sign_Coral", "emit", "#FC9C84", 2.0)   # the wings: the contract's coral (testbed: sign coral accent)

# É: the E of the shared font (civic_cafe adds F and E to it) plus an acute accent over it (GLYPH units: cap height 1)
GLYPH.setdefault("\u00c9", (GLYPH["E"][0], list(GLYPH["E"][1]) + [[(0.14, 1.12), (0.36, 1.34)]]))

PLATE_W, PLATE_H = 4.45, 0.86      # testbed sign width 4.45
Z0 = 4.86                          # plate bottom: 0.16 over the generator's belt top (4.70)
DEPTH = 0.09
CAP = 0.50                         # letter cap height (testbed 0.46; a little taller so the word holds at 45 px)


def _rrect(w, h, r, n=4):
    pts = []
    for cx, cy, a0 in ((w / 2 - r, h / 2 - r, 0), (-w / 2 + r, h / 2 - r, 90), (-w / 2 + r, -h / 2 + r, 180), (w / 2 - r, -h / 2 + r, 270)):
        for i in range(n + 1):
            a = math.radians(a0 + 90 * i / n)
            pts.append((cx + r * math.cos(a), cy + r * math.sin(a)))
    return pts


@asset("CafeFascia", "Plaza", bevel=0.0)
def cafe_fascia(s):
    """The sign plate on the wall over the door: facing -Y, plate centre (0, 0, Z0 + H/2)."""
    zc = Z0 + PLATE_H / 2
    D = DEPTH
    with s.at((0, 0, zc), (90, 0, 0)):          # local +Z = world -Y (out of the wall), local +Y = world +Z
        outer = _rrect(PLATE_W, PLATE_H, 0.12)
        s.extrude(ccw(outer), 0.0, D, pal.Enamel_Teal)
        # chrome frame bead round the plate and a brass hairline inside it
        s.sweep([(x, y, D) for x, y in outer + outer[:1]], 0.022, pal.Chrome, seg=5, caps=False)
        inner = _rrect(PLATE_W - 0.2, PLATE_H - 0.2, 0.08)
        s.sweep([(x, y, D + 0.004) for x, y in inner + inner[:1]], 0.008, pal.Brass_Civic, seg=4, caps=False)
        # the word, lit
        letters(s, "CAF\u00c9", CAP, pal.Sign_Ivory, x=0.0, y=-0.02, lift=D + 0.004, wt=0.17, depth=0.05, track=0.26)
        # coral wings: three speed lines each side, longest on top (the testbed's Streamline wings)
        for sx in (-1, 1):
            for ln, dz in ((1.05, 0.15), (0.92, 0.0), (0.72, -0.15)):
                x0, x1 = sx * 0.98, sx * (0.98 + ln)
                s.box((min(x0, x1), dz - 0.014, D + 0.004), (max(x0, x1), dz + 0.014, D + 0.034), pal.Sign_Coral)
