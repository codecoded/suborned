"""WashingtoniaPalm_Street (brief 028, Vela): the avenue palm, slender, for the plan's 16 m avenue species (streetscape.trees.species.washingtonia: top 16 m, spread 4 m, 803 trees).

CoconutPalm_Street's crown is 5.25 m across and does not fit a 4 m room at uniform scale, so the avenue fell back to procedural (Splice, 20 Oct). Sky: palms in the tree pits, as in the old world.
This is the same builder as the kit's palms (plants.py: ring-scarred sweep trunk, fan_leaf cards on the district's fan texture, petiole sweeps), cut as a Washingtonia: a tall straight slim shaft
(13.9 m, 0.25 m at the foot to 0.16 m under the crown, 0.12 m rings), a shaggy skirt of dead leaf-bases hanging from the top 2.4 m, and a dense round head of 82 fan fronds in five tiers
(erect, rising, spreading, level, hanging) on 0.35 to 0.62 m petioles. Crown 3.6 m across (built bounds 3.57 x 3.48), whole piece 15.65 m: it fits the slot at scale 1.0 to 1.02 (height tolerance 0.35)
the 4 m room down to 0.97. Nothing reduced: no cap, no lean cut, no LOD decimation (nolod). Pivot = trunk foot on the ground; leans +0.12 m toward +X at the head (turn by the seed yaw)."""
import math
import random

from mathutils import Vector

from kit import REGISTRY, asset
from mats import pal
from plants import Cards, Z, fan_leaf, rings_sweep, sphere  # noqa: F401

H = 13.9
CROWN_R = 1.82


@asset("WashingtoniaPalm_Street", "Plants", bevel=0.0)
def washingtonia_street(s):
    rng = random.Random(4411)
    cd = Cards(s)
    lean = lambda z: (0.12 * (z / H) ** 2.0, 0.03 * (z / H) ** 2, z)  # noqa: E731
    rad = lambda z: 0.16 + 0.10 * math.exp(-z / 0.9) - 0.0 * z  # noqa: E731
    rings_sweep(s, H, rad, pal.Bark_Palm, lean, seg=12, band=0.12, depth=0.06)
    top = Vector(lean(H))
    # the skirt: dead leaf-bases hanging from the head down the top 2.4 m
    for i in range(90):
        z = H - 0.1 - 2.4 * (i / 89) ** 0.9
        a = math.radians(i * 137.5)
        o = Vector((math.cos(a), math.sin(a), 0))
        c = Vector(lean(z)) + o * rad(z) * 0.9
        ln = rng.uniform(0.42, 0.78) * (0.7 + 0.3 * (1 - (H - z) / 2.4))
        tip = c + (o * 0.55 - Z * 1.0).normalized() * ln
        mid = c + (o * 0.5 + Z * 0.05) * ln * 0.3
        s.sweep([c, mid, tip], [0.045, 0.035, 0.012], pal.Bark_Fibre, seg=5)
    with s.at(top):
        s.lathe([(0.15, -0.1), (0.2, 0.0), (0.22, 0.14), (0.17, 0.3), (0.08, 0.4), (0, 0.42)], pal.Bark_Fibre, seg=12)
    cc = top + Z * 0.55
    tiers = ((10, 80, 0.35, 1.2, 0.12), (16, 58, 0.5, 1.2, 0.18), (18, 34, 0.62, 1.1, 0.24), (22, -10, 0.62, 1.0, 0.30), (16, -45, 0.55, 0.85, 0.34))
    i = 0
    for count, el0, plen, blade, droop in tiers:
        for k in range(count):
            yaw = math.radians((i * 137.5 + rng.uniform(-8, 8)) % 360)
            el = el0 + rng.uniform(-6, 6)
            L = plen * rng.uniform(0.92, 1.08)
            o = Vector((math.cos(yaw), math.sin(yaw), 0))
            p0 = top + Z * (0.28 - 0.0006 * i) + o * 0.06
            pts, p = [p0], p0
            for j in range(1, 4):  # the petiole sags under the blade
                e = math.radians(el + 6 - 20 * j / 3)
                p = p + (o * math.cos(e) + Z * math.sin(e)) * L / 3
                pts.append(p)
            s.sweep(pts, [0.026, 0.022, 0.018, 0.014], pal.Petiole, seg=5)
            d = (pts[-1] - pts[-2]).normalized()
            d = (d - Z * 0.2).normalized()
            fan_leaf(cd, rng, pts[-1] - d * 0.03, d, blade * rng.uniform(0.9, 1.1), cc, CROWN_R, pal.Leaf_PalmFan, droop=droop * rng.uniform(0.85, 1.15))
            i += 1


REGISTRY["WashingtoniaPalm_Street"]["nolod"] = True
