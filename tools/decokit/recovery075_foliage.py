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
        if getattr(s, "recovery_arrange", False):
            s.recovery_sprays.append((verts, Vector(c), r, h, s.m.copy()))


civic_common_original = civic_common.dome_lobe


def petal_bloom(s, c, r, mat, seg=6):
    """One shallow five-lobed corolla, not five inflated petal domes.

    Same seed centre/colour/radius; perimeter and centre fit inside the source
    five-sided bloom's inscribed circle. A small dish catches light at play zoom.
    """
    if getattr(s, "recovery_arrange", False):
        s.recovery_blooms.append((s.m @ Vector(c), r))
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


def arrange_sprays(s):
    """Rigidly reattach EXISTING sprays; reject placements outside source bounds.

    Two opposing camera clearances are scored before accepting a pose. Neither
    sprays nor flowers are shrunk, clipped, re-tessellated or removed.
    """
    directions = [Vector((math.sin(math.radians(a))*math.cos(math.radians(38)),
                          -math.cos(math.radians(a))*math.cos(math.radians(38)),
                          math.sin(math.radians(38)))) for a in (30, 210)]
    frames = [(Vector((d.y, -d.x, 0)).normalized(), d.cross(Vector((d.y, -d.x, 0)).normalized()), d)
              for d in directions]
    def overlap(points):
        cost = 0.0
        card_centre = sum(points, Vector()) / len(points)
        for centre, radius in s.recovery_blooms:
            if (card_centre-centre).length > .65:
                continue
            # Conservative projected bloom rectangle, exact source disk radius.
            for right, up, eye in frames:
                if max((q-centre).dot(eye) for q in points) < -0.002:
                    continue
                px, py = [q.dot(right) for q in points], [q.dot(up) for q in points]
                cx, cy = centre.dot(right), centre.dot(up)
                rx = radius * .80 + .004
                ry = radius * .80 * math.sin(math.radians(38)) + .004
                dx = max(0., min(max(px), cx+rx)-max(min(px), cx-rx))
                dy = max(0., min(max(py), cy+ry)-max(min(py), cy-ry))
                cost += dx*dy
        return cost
    residual = []
    for index, (verts, c, r, h, matrix) in enumerate(s.recovery_sprays):
        inv = matrix.inverted_safe()
        old = [v.co.copy() for v in verts]
        centre = sum(old, Vector())/len(old)
        # The four original curved quads share one overall spray orientation.
        normal = (old[1]-old[0]).cross(old[2]-old[0]).normalized()
        rng = random.Random(75110 + index*109 + round(c.x*997))
        yr = r*math.sin(math.pi*2/5)
        best = None
        for trial in range(180):
            a = rng.uniform(0, 2*math.pi)
            # Upright / oblique sprays replace radial horizontal plates.
            nz = rng.uniform(-.25,.60)
            target = (matrix.to_3x3() @ Vector((math.cos(a),math.sin(a),nz))).normalized()
            rotation = normal.rotation_difference(target).to_matrix()
            z = rng.uniform(.12,.83)
            az = a + rng.uniform(-1.1,1.1)
            radial = math.sqrt(1-z*z)*rng.uniform(.64,.88)
            attachment = matrix @ (c + Vector((math.cos(az)*r*radial, math.sin(az)*yr*radial, z*h)))
            points = [attachment + rotation @ (q-centre) for q in old]
            local = [inv @ q for q in points]
            if any(not(c.x-r <= q.x <= c.x+r and c.y-yr <= q.y <= c.y+yr and c.z <= q.z <= c.z+h) for q in local):
                continue
            collision = overlap(points)
            # First priority flower clearance; then favour outer coverage.
            score = collision*10000 + (.88-radial)*.015 + abs(nz-.12)*.002
            if best is None or score < best[0]: best = (score, points, rotation, collision)
        if best is None:
            # Do not fit/scale an impossible spray; retain it and report failure.
            residual.append({"spray":index,"status":"no rigid pose inside envelope"})
            continue
        _, points, rotation, collision = best
        for v,q in zip(verts,points):
            v.co = q
            if v.index in s.recovery_cards.n:
                s.recovery_cards.n[v.index] = (rotation @ s.recovery_cards.n[v.index]).normalized()
        if collision > 1e-8: residual.append({"spray":index,"projectedBloomOverlapArea":collision})
    s.recovery_arrangement_residual = residual


def arranged_lobe(s, *args, **kw):
    if not getattr(s, "recovery_arrange", False):
        s.recovery_arrange = True
        s.recovery_sprays, s.recovery_blooms = [], []
    leafy_lobe(s, *args, **kw)
    if not getattr(s, "recovery_arrangement_hook", False):
        build = s.build
        def wrapped(*a, **k):
            s.bm.verts.index_update()
            arrange_sprays(s)
            obj = build(*a, **k)
            obj["arrangement_residual"] = str(s.recovery_arrangement_residual)
            return obj
        s.build = wrapped
        s.recovery_arrangement_hook = True


@contextmanager
def leaf_cushions(arrange=False):
    old_common, old_street = civic_common.dome_lobe, civic_street.dome_lobe
    old_bloom_common, old_bloom_street = civic_common.bloom, civic_street.bloom
    civic_common.dome_lobe = civic_street.dome_lobe = arranged_lobe if arrange else leafy_lobe
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
    with leaf_cushions(arrange=name in ("PlanterRun_4_B", "BedTuft_A")):
        return kit.build(name)
