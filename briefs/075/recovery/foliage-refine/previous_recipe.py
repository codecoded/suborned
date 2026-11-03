"""Opt-in civic foliage candidates; never registered in the production kit.

Reuses DecoKit's old shrub cutout and bent normals, inside each original
cushion's bounds, plus rounded petals. Troughs, paths and production builders stay intact.
Call build_candidate(id); caller owns export to an isolated recovery directory.
"""
import math
import random
from contextlib import contextmanager

from mathutils import Vector

import civic_common
import civic_street
import kit
import plants
from geo import Shape
from mats import pal


def leafy_lobe(s, c, r, h, mat, seg=10, n=3):
    # Original dome is retained as an opaque interior, slightly inset so the
    # silhouette comes from real leaf cutouts rather than a ten-sided shell.
    civic_common_original(s, c, r * 0.86, h * 0.88, pal.Leaf_CoreShrub,
                          seg=seg, n=n)
    cd = getattr(s, "recovery_cards", None)
    if cd is None:
        cd = s.recovery_cards = plants.Cards(s)
    rng = random.Random(75029 + round(c[0] * 997) + round(c[1] * 151) + round(h * 10000))
    centre = Vector(c)
    # Golden-angle distribution: no latitude bands or four-way crosses.
    for i in range(48):
        z = (i + 0.5) / 48
        a = i * math.pi * (3 - math.sqrt(5))
        radial = math.sqrt(1 - z * z)
        normal = Vector((math.cos(a) * radial, math.sin(a) * radial, z))
        p = centre + Vector((normal.x * r * 0.91, normal.y * r * 0.91, z * h * 0.88))
        start = len(s.bm.verts)
        plants.card(cd, rng, p, normal, min(r * 0.85, h * 0.8),
                    pal.Leaf_Shrub, lambda q: (q - centre).normalized(), jit=0.10)
        # A strict local envelope, including below-ground corners: detail never
        # protrudes into an adjacent route or raises the old plant's height.
        s.bm.verts.ensure_lookup_table()
        inv = s.m.inverted_safe()
        for v in list(s.bm.verts)[start:]:
            q = inv @ v.co
            q.x = max(c[0] - r, min(c[0] + r, q.x))
            q.y = max(c[1] - r, min(c[1] + r, q.y))
            q.z = max(c[2], min(c[2] + h, q.z))
            v.co = s.m @ q


civic_common_original = civic_common.dome_lobe


def petal_bloom(s, c, r, mat, seg=6):
    """Five rounded petals and a small cream centre inside the old bloom box."""
    for k in range(5):
        a = 2 * math.pi * k / 5
        civic_common_original(s, (c[0] + r * 0.48 * math.cos(a),
                                 c[1] + r * 0.48 * math.sin(a), c[2]),
                              r * 0.42, r * 0.48, mat, seg=12, n=4)
    civic_common_original(s, (c[0], c[1], c[2] + r * 0.22),
                          r * 0.25, r * 0.46, pal.Bloom_Cream, seg=12, n=4)


@contextmanager
def leaf_cushions():
    old_common, old_street = civic_common.dome_lobe, civic_street.dome_lobe
    old_bloom_common, old_bloom_street = civic_common.bloom, civic_street.bloom
    civic_common.dome_lobe = civic_street.dome_lobe = leafy_lobe
    civic_common.bloom = civic_street.bloom = petal_bloom
    try:
        yield
    finally:
        civic_common.dome_lobe, civic_street.dome_lobe = old_common, old_street
        civic_common.bloom, civic_street.bloom = old_bloom_common, old_bloom_street


def build_candidate(name):
    """LOD0 only. No cap, decimation, catalogue change or collider generation."""
    if name == "Cypress_Play":
        s = Shape("DK_Cypress_Play_Recovery075")
        plants.cypress(s)  # faithful original curved cards, same RNG/profile
        return s.build(bevel=0.0)
    if not (name.startswith("BedTuft_") or name.startswith("PlanterRun_4_")):
        raise ValueError(f"Outside recovery scope: {name}")
    with leaf_cushions():
        return kit.build(name)
