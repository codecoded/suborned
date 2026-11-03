"""Opt-in civic foliage candidates; never registered in the production kit.

Reuses DecoKit's old shrub cutout and bent normals, inside each original
cushion's bounds, plus rounded petals. Troughs, paths and production builders stay intact.
Call build_candidate(id); caller owns export to an isolated recovery directory.
"""
import math
import random
from contextlib import contextmanager

from mathutils import Vector

import bpy
import mats

import civic_common
import civic_street
import kit
import plants
from geo import Shape
from mats import pal


def leaf_material(base):
    """Review-only colour response; same full-resolution shrub alpha/albedo.

    Mix in linear light: 75% seeded civic green + 25% source texture. This
    suppresses photographed yellow rims/deep baked shadows without repainting
    source art. New names deliberately require explicit material integration.
    """
    name = "DK_Leaf_Refine075_" + base.name.removeprefix("DK_Leaf_")
    m = bpy.data.materials.get(name)
    if m:
        return m
    m = mats.get("DK_Leaf_Shrub").copy()
    m.name = name
    nt = m.node_tree
    tex = next(n for n in nt.nodes if n.type == "TEX_IMAGE")
    bsdf = nt.nodes["Principled BSDF"]
    mix = nt.nodes.new("ShaderNodeMixRGB")
    mix.blend_type = "MIX"
    mix.inputs[0].default_value = 0.25
    mix.inputs[1].default_value = base.diffuse_color if not base.use_nodes else base.node_tree.nodes["Principled BSDF"].inputs["Base Color"].default_value
    nt.links.new(tex.outputs["Color"], mix.inputs[2])
    nt.links.new(mix.outputs[0], bsdf.inputs["Base Color"])
    # Unity's cutout threshold, rather than transparent fractional leaf rims.
    clip = nt.nodes.new("ShaderNodeMath")
    clip.operation = "GREATER_THAN"
    clip.inputs[1].default_value = 0.5
    nt.links.new(tex.outputs["Alpha"], clip.inputs[0])
    nt.links.new(clip.outputs[0], bsdf.inputs["Alpha"])
    bsdf.inputs["Roughness"].default_value = 0.85
    bsdf.inputs["Specular IOR Level"].default_value = 0.1
    return m


def leafy_lobe(s, c, r, h, mat, seg=10, n=3):
    # Same seeded cushion, a civic-green hidden core instead of near-black.
    civic_common_original(s, c, r * 0.78, h * 0.80, mat, seg=seg, n=n)
    cd = getattr(s, "recovery_cards", None)
    if cd is None:
        cd = s.recovery_cards = plants.Cards(s)
    rng = random.Random(75029 + round(c[0] * 997) + round(c[1] * 151) + round(h * 10000))
    centre = Vector(c)
    for i in range(80):
        z = (i + 0.5) / 80
        a = i * math.pi * (3 - math.sqrt(5))
        radial = math.sqrt(1 - z * z)
        normal = Vector((math.cos(a) * radial, math.sin(a) * radial, z))
        p = centre + Vector((normal.x * r * 0.86, normal.y * r * 0.86, z * h * 0.86))
        # Build a curved leaf spray and fit its radius about its own seeded
        # attachment, preserving its shape. This is recipe-space construction;
        # no per-axis vertex clamp, global mesh fit, or import correction.
        start = len(s.bm.verts)
        plants.card(cd, rng, p, normal, min(r * 0.86, h * 0.86), leaf_material(mat),
                    lambda q: Vector(((q.x-c[0])/r, (q.y-c[1])/r, (q.z-c[2])/h + 0.35)).normalized(),
                    tilt=(-12, 12), roll=(-30, 30), jit=0.025, bend=0.12)
        s.bm.verts.ensure_lookup_table()
        verts = list(s.bm.verts)[start:]
        inv = s.m.inverted_safe()
        local = [inv @ v.co for v in verts]
        yr = r * max(abs(math.sin(2*math.pi*j/seg)) for j in range(seg))
        lo, hi = (c[0]-r, c[1]-yr, c[2]), (c[0]+r, c[1]+yr, c[2]+h)
        scale = 1.0
        for q in local:
            for axis in range(3):
                d = q[axis] - p[axis]
                if d > 0: scale = min(scale, (hi[axis]-p[axis])/d)
                elif d < 0: scale = min(scale, (lo[axis]-p[axis])/d)
        for v,q in zip(verts,local): v.co = s.m @ (p + (q-p)*scale)


civic_common_original = civic_common.dome_lobe


def petal_bloom(s, c, r, mat, seg=6):
    """One shallow five-lobed corolla, not five inflated petal domes.

    Same seed centre/colour/radius; perimeter and centre fit inside the source
    five-sided bloom's inscribed circle. A small dish catches light at play zoom.
    """
    steps = 40
    inner, outer = [], []
    for i in range(steps):
        a = 2 * math.pi * i / steps
        radius = r * (0.70 + 0.09 * math.cos(5 * a))
        outer.append((c[0] + radius * math.cos(a), c[1] + radius * math.sin(a), c[2] + r * 0.18))
        inner.append((c[0] + r * 0.16 * math.cos(a), c[1] + r * 0.16 * math.sin(a), c[2] + r * 0.10))
    for i in range(steps):
        j = (i + 1) % steps
        s.decal([inner[i], outer[i], outer[j], inner[j]], mat, lift=0)
    s.face(inner, pal.Bloom_Cream)


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
