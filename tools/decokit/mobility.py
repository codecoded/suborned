"""Personal mobility at the kerb (props sheet: personal_mobility #1): the hover scooter. Original design in the kit
livery: cream enamel hull, seafoam nose flash, black skirt glowing teal beneath, chrome trim, coral tail fins.
Length along Y, front at -Y, resting on z = 0. The service robots are robots.py."""

from geo import fillet
from kit import asset
from mats import pal, register
from signals import puck
from vehicles_parts import lamp, plan, slab, vloft

register("DK_Enamel_Coral", "enamel", "#E07560")  # as skyline.py (register is first-wins)
register("DK_Glow_Seafoam", "emit", "#3FC9A0", 0.7)  # as booths.py
register("DK_Leather_Black", "ceramic", "#1E1E20")  # satin seat padding
SIDE = (90, 0, 90)  # local (x, y) -> world (y, z), extrusion -> world X


# ----------------------------------------------------------------- hover scooter

HS_SIDES = [k / 8 for k in range(9)]


@asset("HoverScooter", "Vehicles", bevel=0.003)
def hover_scooter(s):
    """A kerbside hover scooter: bullet-nosed cream hull with a seafoam nose flash, black skirt glowing teal
    beneath, chrome rub strip, T-bar on a chrome stem, black seat, twin coral tail fins."""
    skirt = [(0.03, 0.29, -0.48, 0.56, 0.3, 0.2), (0.07, 0.345, -0.5, 0.58, 0.33, 0.22),
             (0.19, 0.355, -0.5, 0.58, 0.34, 0.22), (0.27, 0.33, -0.48, 0.57, 0.33, 0.21)]
    vloft(s, skirt, pal.Rubber, HS_SIDES, nc=12, e=2.4, caps=((0, pal.Glow_Seafoam), (-1, None)))
    hull = [(0.26, 0.33, -0.48, 0.57, 0.33, 0.21), (0.44, 0.345, -0.48, 0.6, 0.33, 0.2),
            (0.6, 0.315, -0.4, 0.6, 0.28, 0.17), (0.68, 0.25, -0.3, 0.58, 0.21, 0.13),
            (0.72, 0.17, -0.2, 0.54, 0.14, 0.09)]
    flash = lambda i, j, c: pal.Enamel_Seafoam if c.z > 0.3 and c.y < -0.6 + 0.95 * (c.z - 0.3) else None  # noqa: E731
    vloft(s, hull, pal.Enamel_Cream, HS_SIDES, nc=12, e=2.4, paint=flash)
    rub = [(x, y, 0.275) for x, y in plan(0.337, -0.48, 0.57, 0.335, 0.215, HS_SIDES, 12, 2.4)]
    s.sweep(rub + rub[:1], 0.014, pal.Chrome, seg=8)
    lamp(s, (0, -0.765, 0.5), (0, -1, 0.15), 0.066, housing=pal.Chrome, hr=0.09, hd=0.03, depth=0.03, seg=20)
    for sg in (-1, 1):
        with s.at((sg * 0.335, -0.3, 0.4), (0, 0, 0)):  # amber marker strip on the nose flank
            for k in range(3):
                lamp(s, (sg * 0.004, -0.04 * k, 0), (sg, 0, 0), 0.008, lens=pal.Signal_Amber, depth=0.006, seg=8)
        s.rbox((sg * 0.325 - 0.012, 0.02, 0.38), (sg * 0.325 + 0.012, 0.2, 0.46), 0.008, pal.Gold)  # badge
        for k in range(3):  # rear vents
            s.box((sg * 0.325 - 0.01, 0.42, 0.42 + 0.03 * k), (sg * 0.325 + 0.01, 0.54, 0.432 + 0.03 * k), pal.Chrome)
    s.sweep(fillet([(0, -0.34, 0.64), (0, -0.33, 0.82), (0, -0.28, 1.05)], 0.08, n=4, closed=False), 0.026,
            pal.Chrome, seg=12)  # stem
    with s.at((0, -0.28, 1.06)):
        s.lathe(puck(0.042, -0.03, 0.03, 0.012), pal.Chrome, seg=16)
    s.sweep([(-0.3, -0.27, 1.07), (0.3, -0.27, 1.07)], 0.014, pal.Chrome, seg=10)
    for sg in (-1, 1):
        s.sweep([(sg * 0.19, -0.27, 1.07), (sg * 0.31, -0.27, 1.07)], 0.02, pal.Rubber, seg=10)
    seat = fillet([(-0.05, 0.7), (0.42, 0.7), (0.44, 0.76), (0.36, 0.815), (0.0, 0.805), (-0.07, 0.75)], 0.04, n=3)
    slab(s, seat, 0.16, 0.05, pal.Leather_Black)
    for sg in (-1, 1):  # twin tail fins, swept back
        with s.at((sg * 0.19, 0, 0), SIDE):
            fin = fillet([(0.36, 0.64), (0.7, 0.62), (0.78, 0.7), (0.74, 1.18), (0.66, 1.2), (0.46, 0.76)], 0.025, n=3)
            s.extrude(fin, -0.018, 0.018, pal.Enamel_Coral, smooth_sides=True)
    lamp(s, (0, 0.78, 0.5), (0, 1, 0.1), 0.03, lens=pal.Signal_Red, depth=0.02, seg=14)
