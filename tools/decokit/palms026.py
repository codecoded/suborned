"""CoconutPalm_Street (brief 026, Vela): the old city's street palm at the plan's street-tree size, so the `tree` slot stops filling with cypress and topiary.

Sky, 2026-10-20: "I don't like our street trees being topiary - they're palm in our old world." The old world's street palm is the kit's CoconutPalm (plants.py: ringed leaning trunk, coconut
boot, a crown of feather fronds; old-world captures out/old-world/faithful/close-plant-CoconutPalm.png): the testbed's own palm (art/palm/palm-gameplay-v1.glb, a bent ringed trunk and a
three-tier crown) is the same idea in a lower-quality asset. CoconutPalm itself is a 6 m trunk and an 8 m piece with 6.3k triangles, built for a plaza; the plan's avenue trees are tall
(streetscape.trees.species.phoenix top 11 m spread 8, washingtonia top 16 m spread 4) and placed every 12 m, so this is the same builder cut for the street:
  trunk 8.6 m instead of 6 (a taller shaft, the same ring pattern and 12 sides), and CoconutPalm's whole crown unchanged: 22 fronds in the same four tiers at the same lengths, 7 coconuts. Nothing is reduced (Sky, 20 Oct: compression goes through Sky); only the trunk is longer.
The `tree` slot scales a piece to the species height (kit tag `scale`), so the one piece serves phoenix (x1.0) and washingtonia (x1.45).
Pivot = trunk foot on the ground; it leans toward +X like CoconutPalm (turn by the seed yaw the generator already gives trees).
"""

import math
import random

from mathutils import Vector

from kit import asset
from mats import pal
from plants import Cards, Z, feather_frond, rings_sweep, sphere


@asset("CoconutPalm_Street", "Plants", bevel=0.0)
def coconut_palm_street(s):
    rng = random.Random(4311)
    cd = Cards(s)
    H = 8.6
    lean = lambda z: (0.8 * (z / H) ** 1.8 + 0.06 * math.sin(z * 0.7), 0.12 * (z / H) ** 2, z)  # noqa: E731
    rad = lambda z: 0.14 + 0.09 * math.exp(-z / 0.45) - 0.015 * z / H  # noqa: E731
    rings_sweep(s, H, rad, pal.Bark_Palm, lean, seg=12, band=0.13, depth=0.07)
    top = Vector(lean(H))
    with s.at(top):
        s.lathe([(0.12, -0.05), (0.17, 0.05), (0.19, 0.18), (0.15, 0.3), (0.07, 0.38), (0, 0.4)], pal.Bark_Fibre, seg=12)
    for i in range(7):
        a = 2 * math.pi * i / 7 + rng.uniform(-0.3, 0.3)
        sphere(s, top + Vector((math.cos(a) * 0.2, math.sin(a) * 0.2, rng.uniform(-0.08, 0.06))), rng.uniform(0.085, 0.105), pal.Coconut)
    cc, cr = top + Z * 0.25, 3.2
    tiers = ((3, 62, 50, 2.4, 30), (7, 40, 95, 3.5, 34), (8, 18, 100, 3.7, 38), (4, -8, 72, 3.2, 42))
    i = 0
    for count, el0, droop, length, fold in tiers:
        for _ in range(count):
            yaw = (i * 137.5 + rng.uniform(-12, 12)) % 360
            base = top + Z * (0.3 - 0.08 * i / 6) + Vector((math.cos(math.radians(yaw)), math.sin(math.radians(yaw)), 0)) * 0.08
            feather_frond(cd, base, yaw, el0 + rng.uniform(-6, 6), droop + rng.uniform(-10, 10), length * rng.uniform(0.9, 1.08), fold, rng.uniform(-35, 35), cc, cr, pal.Leaf_PalmFeather)
            i += 1
