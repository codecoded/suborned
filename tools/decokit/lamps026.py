"""The night lamps of the Handover (brief 026, Vela; from Glint's plan briefs/026/glint/cafe-lighting.md, which has the testbed's values): four glowing fixtures, each a mesh whose own
emissive material is the only thing that glows, so the day state is a per-material scalar of 0 (Latch/Glint) and signals stay unlit and out of the grade.

  (table lamps: retired 20 Oct; CafeLamp_Testbed_Amber|Teal|Peach|Coral|Lilac in cafe_testbed.py carry the five hues, DK_Lamp_Table_<Hue> below)
  AwningLamp                                           terrace wall / awning under-lamp: a chrome plate, an opal strip, a projecting chrome eyelid against glare from the play camera
                                                       (testbed exterior wall light, opal emissive 4). Pivot = wall face, centre; the lamp projects toward -Y; 0.27 x 0.07 x 0.08 m.
  PalmUplight                                          palm uplight can: a chrome base disc, stem and can on a 25 degree tilt with a recessed lens (testbed tree lens, emissive 2.3, #e4efc7).
                                                       Pivot = ground, centre of the base disc; the can leans toward +Y (turn the piece to aim it at the trunk).
  StreetLampGlobe                                      the globe for the street lamps' existing DK_Lamp heads (0.2 m is too small to read at play zoom): a 0.46 m opal globe in a chrome collar
                                                       on a short neck. Pivot = under the neck, on the lamp's head axis.

Emissive values (Blender strength = the testbed's night value; Unity: the same number is the night scalar, 0 by day): opal 2.2 (table), wall lamp 4, uplight lens 2.3, globe 4, sign 3.2,
sign coral 2 (cafe_fascia.py). The light objects beside each fixture are Latch's (Glint's rule: a glowing fixture and a matching light, or neither).
"""

import math

from kit import asset
from mats import pal, register

HUES = {"Amber": "#FFC066", "Teal": "#67D6CE", "Peach": "#FFD099", "Coral": "#FFA891", "Lilac": "#B79BE4"}
for _n, _h in HUES.items():
    register(f"DK_Lamp_Table_{_n}", "emit", _h, 2.2)
register("DK_Lamp_Wall", "emit", "#FFFFFF", 4.0)        # testbed "Exterior wall neutral white glow"
register("DK_Lamp_Uplight", "emit", "#E4EFC7", 2.3)     # testbed "Tree uplight recessed lens"
register("DK_Lamp_Globe", "emit", "#FFF0D0", 4.0)       # the street globe: warm opal


# The table lamps are CafeLamp_Testbed_<Hue> (cafe_testbed.py): the testbed's own geometry in its five hues. The approximated TableLamp_<Hue> pieces are retired.


@asset("AwningLamp", "Street", bevel=0.0)
def awning_lamp(s):
    """Chrome back plate (capsule), opal strip, a chrome eyelid over it (the testbed fixture). Lamp faces -Y; the wall is at y 0."""
    s.rbox((-0.135, -0.03, -0.05), (0.135, 0.0, 0.05), 0.015, pal.Chrome, seg=2)
    s.rbox((-0.1, -0.05, -0.015), (0.1, -0.03, 0.015), 0.006, pal.Lamp_Wall, seg=1)
    s.rbox((-0.136, -0.08, 0.02), (0.136, -0.025, 0.044), 0.008, pal.Chrome, seg=1)   # eyelid: projects over the strip, shielding the camera's view of the source


@asset("PalmUplight", "Street", bevel=0.0)
def palm_uplight(s):
    s.lathe([(0.0, 0.0), (0.095, 0.0), (0.095, 0.025), (0.0, 0.025)], pal.Chrome, seg=12, cap_bottom=False)
    s.sweep([(0, 0, 0.025), (0, 0, 0.09)], 0.018, pal.Chrome, seg=6, caps=False)
    with s.at((0, 0, 0.1), (-25, 0, 0)):     # tilts the can toward +Y
        s.lathe([(0.0, 0.0), (0.081, 0.0), (0.081, 0.13), (0.0, 0.13)], pal.Chrome, seg=12, cap_bottom=False, cap_top=False)
        s.lathe([(0.0, 0.13), (0.081, 0.13), (0.072, 0.139), (0.063, 0.139), (0.0, 0.136)], pal.Chrome, seg=12, cap_bottom=False, cap_top=False)
        s.lathe([(0.0, 0.1375), (0.063, 0.1375), (0.0, 0.1375)], pal.Lamp_Uplight, seg=12, cap_bottom=False, cap_top=False)


@asset("StreetLampGlobe", "Street", bevel=0.0)
def street_lamp_globe(s):
    s.lathe([(0.0, 0.0), (0.05, 0.0), (0.05, 0.06), (0.1, 0.09), (0.0, 0.09)], pal.Chrome, seg=12, cap_bottom=False)
    r = 0.23
    prof = [(r * math.sin(math.radians(a)), 0.09 + r * (1 - math.cos(math.radians(a)))) for a in range(30, 181, 15)]
    s.lathe([(0.0, 0.09)] + [(x, z) for x, z in prof], pal.Lamp_Globe, seg=14, cap_bottom=False, cap_top=False)
    s.lathe([(0.0, 0.09 + 2 * r), (0.04, 0.09 + 2 * r), (0.0, 0.09 + 2 * r + 0.03)], pal.Chrome, seg=10, cap_bottom=False, cap_top=False)
