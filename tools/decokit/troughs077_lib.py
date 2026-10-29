"""Brief 077 (Vela): shared Blender helpers for the testbed café trough planting (troughs077.py builds the pieces, review077_troughs.py renders).

Reads what tools/decokit/troughs077.ts writes (out/decokit/troughs077: raw float32 meshes + troughs077.manifest.json): the testbed's own
composition on our PlanterRun troughs and the testbed's own baked café beds. Materials follow the testbed's three.js materials
(cafe-planting.js createFlowerBedKit) as Principled BSDF: atlas v2 x the foliage colour #afbea1 (civic-district-look.js baseline) x the
vertex colour (leaf tint and root occlusion, linear as three uses it), roughness = value x ORM green (three's roughnessMap), normal map at
0.16 (soil 0.4), double sided, opaque (no alpha: the curved meshes make the silhouettes)."""
import json
import math
from pathlib import Path

import bpy
import numpy as np

REPO = Path(__file__).resolve().parents[2]
SRC = REPO / "out/decokit/troughs077"
ART = Path("~/Projects/the-suborned-store/testbed-art/cafe-plants").expanduser()
LEVELS = ("gameplay", "near", "close", "inspection")
FOLIAGE_TINT = "#afbea1"  # civic-district-look.js baselineLook.foliage (the default: no ?look= query)


def manifest():
    return json.loads((SRC / "troughs077.manifest.json").read_text())


def lin(c):
    return c / 12.92 if c <= 0.04045 else ((c + 0.055) / 1.055) ** 2.4


def hexlin(h):
    h = h.lstrip("#")
    return tuple(lin(int(h[i:i + 2], 16) / 255) for i in (0, 2, 4))


def arrays(file, rec):
    buf = np.fromfile(SRC / file, dtype=np.uint8)
    out = {}
    for k, a in rec["attrs"].items():
        out[k] = np.frombuffer(buf, dtype="<f4", count=a["length"], offset=a["offset"]).reshape(-1, a["itemSize"])
    return out


def to_blender(p):
    """three (x, up, -plan_y) -> Blender (x, plan_y, z)."""
    return np.stack([p[:, 0], -p[:, 2], p[:, 1]], axis=1)


def make_mesh(name, a, mat):
    """A non-indexed triangle list (testbed toNonIndexed) as a Blender mesh: positions, custom normals, uv, linear vertex colour."""
    pos, n = to_blender(a["position"]), to_blender(a["normal"])
    nv = len(pos)
    me = bpy.data.meshes.new(name)
    me.vertices.add(nv)
    me.vertices.foreach_set("co", pos.astype(np.float32).ravel())
    nt = nv // 3
    me.loops.add(nv)
    me.loops.foreach_set("vertex_index", np.arange(nv, dtype=np.int32))
    me.polygons.add(nt)
    me.polygons.foreach_set("loop_start", np.arange(0, nv, 3, dtype=np.int32))
    me.update()
    if "uv" in a:
        uv = me.uv_layers.new(name="UVMap")
        uv.data.foreach_set("uv", a["uv"].astype(np.float32).ravel())
    if "color" in a:
        c = me.color_attributes.new("Col", "FLOAT_COLOR", "CORNER")
        rgba = np.concatenate([a["color"], np.ones((nv, 1), np.float32)], axis=1)
        c.data.foreach_set("color", rgba.astype(np.float32).ravel())
    me.normals_split_custom_set(n.astype(np.float32).tolist())
    me.materials.append(mat)
    o = bpy.data.objects.new(name, me)
    return o


_img = {}


def reset():
    """Forget cached Blender datablocks (call after read_factory_settings)."""
    _img.clear()


def image(name, colour):
    if name not in _img or _img[name].name not in bpy.data.images:
        im = bpy.data.images.load(str(ART / f"{name}.png"), check_existing=True)
        im.colorspace_settings.name = "sRGB" if colour else "Non-Color"
        _img[name] = im
    return _img[name]


def tb_material(kind, name=None):
    """kind: foliage | petals | soil | stone (the testbed's pearl planter foundation: tint #fafcff on its paving stone; flat here)."""
    name = name or {"foliage": "DK_Leaf_TB", "petals": "DK_Petal_TB", "soil": "DK_Soil_TB", "stone": "TB_PearlStone", "chrome": "DK_Chrome", "globe": "DK_Opal_TB"}[kind]
    for m in bpy.data.materials:  # ours only (an FBX import brings same-named materials without the node setup)
        if m.get("tb077") == name:
            return m
    m = bpy.data.materials.new(name)
    m["tb077"] = name
    m.use_nodes = True
    nt = m.node_tree
    b = nt.nodes["Principled BSDF"]
    if kind == "stone":
        b.inputs["Base Color"].default_value = (*hexlin("#ece9e2"), 1)
        b.inputs["Roughness"].default_value = 0.5
        return m
    if kind == "chrome":  # fountain-garden-lights.js: chrome finish #d8dfe4, metalness 1, roughness .16
        b.inputs["Base Color"].default_value = (*hexlin("#d8dfe4"), 1)
        b.inputs["Metallic"].default_value, b.inputs["Roughness"].default_value = 1.0, 0.16
        return m
    if kind == "globe":  # warm opal glass #fffdf0, emissive #fff1d2 x 0.06, roughness .25
        b.inputs["Base Color"].default_value = (*hexlin("#fffdf0"), 1)
        b.inputs["Roughness"].default_value = 0.25
        b.inputs["Emission Color"].default_value = (*hexlin("#fff1d2"), 1)
        b.inputs["Emission Strength"].default_value = 0.06
        return m
    base, nrm, orm, rough, nscale = {
        "foliage": ("botanical-atlas-v2", "botanical-normal-v2", "botanical-orm-v2", 0.49, 0.16),
        "petals": ("botanical-atlas-v2", "botanical-normal-v2", "botanical-orm-v2", 0.64, 0.16),
        "soil": ("soil-base", "soil-normal", "soil-orm", 1.0, 0.4),
    }[kind]
    uv = nt.nodes.new("ShaderNodeUVMap")
    uv.uv_map = "UVMap"

    def tex(n, colour):
        t = nt.nodes.new("ShaderNodeTexImage")
        t.image = image(n, colour)
        t.interpolation = "Smart"
        if kind == "soil":
            t.extension = "REPEAT"
        else:
            t.extension = "EXTEND"
        nt.links.new(uv.outputs["UV"], t.inputs["Vector"])
        return t

    tb, tn, to = tex(base, True), tex(nrm, False), tex(orm, False)
    col = tb.outputs["Color"]
    if kind in ("foliage", "petals"):
        vc = nt.nodes.new("ShaderNodeVertexColor")
        vc.layer_name = "Col"
        mul = nt.nodes.new("ShaderNodeMix")
        mul.data_type, mul.blend_type = "RGBA", "MULTIPLY"
        mul.inputs["Factor"].default_value = 1
        nt.links.new(col, mul.inputs[6])
        nt.links.new(vc.outputs["Color"], mul.inputs[7])
        col = mul.outputs[2]
    if kind == "foliage":
        mul = nt.nodes.new("ShaderNodeMix")
        mul.data_type, mul.blend_type = "RGBA", "MULTIPLY"
        mul.inputs["Factor"].default_value = 1
        nt.links.new(col, mul.inputs[6])
        mul.inputs[7].default_value = (*hexlin(FOLIAGE_TINT), 1)
        col = mul.outputs[2]
    nt.links.new(col, b.inputs["Base Color"])
    sep = nt.nodes.new("ShaderNodeSeparateColor")
    nt.links.new(to.outputs["Color"], sep.inputs[0])
    rm = nt.nodes.new("ShaderNodeMath")
    rm.operation = "MULTIPLY"
    rm.inputs[1].default_value = rough
    nt.links.new(sep.outputs[1], rm.inputs[0])
    nt.links.new(rm.outputs[0], b.inputs["Roughness"])
    nm = nt.nodes.new("ShaderNodeNormalMap")
    nm.inputs["Strength"].default_value = nscale
    nt.links.new(tn.outputs["Color"], nm.inputs["Color"])
    nt.links.new(nm.outputs["Normal"], b.inputs["Normal"])
    return m


KIND_MAT = {"soil": "soil", "foliage": "foliage", "flowers": "petals", "stone": "stone", "chrome": "chrome", "globe": "globe"}
MAT_KIND = {"DK_Leaf_TB": "foliage", "DK_Petal_TB": "petals", "DK_Soil_TB": "soil", "DK_Chrome_TB": "chrome", "DK_Chrome": "chrome", "DK_Opal_TB": "globe"}


def fountain_level_for(px_per_m):
    """fountain-garden.js updateLOD: projected 0.55 m leaf span; close >= 44 px, near >= 24 px, else gameplay."""
    p = 0.55 * px_per_m
    return "close" if p >= 44 else "near" if p >= 24 else "gameplay"


def level_for(px_per_m):
    """The testbed's own LOD choice (garden-runtime.js updateLOD, entering from gameplay, no hysteresis): projected 0.4 m leaf span in pixels."""
    p = 0.4 * px_per_m
    return "inspection" if p >= 72 else "close" if p >= 32 else "near" if p >= 18 else "gameplay"


def weld_count(a):
    """Vertices after an importer weld (Unity's default): identical position, normal, uv and colour collapse."""
    parts = [a["position"], a["normal"]] + [a[k] for k in ("uv", "color") if k in a]
    rows = np.concatenate(parts, axis=1)
    return int(len(np.unique(np.round(rows, 6), axis=0)))
