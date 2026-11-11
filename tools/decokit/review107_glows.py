"""Brief 107 (Vela): review renders of the night glows (glows107.py), the café square as the world export places it, EEVEE, the kit's material rows.

/Applications/Blender.app/Contents/MacOS/Blender -b -P tools/decokit/review107_glows.py -- <out_dir> [--gen out/decokit-107] [--extra out/decokit-107-extra] [--only name,...]
  Every civic kit piece in the café square (out/world/harbour-district.world.json `civic`, plan box below) at its placement: KitPlacer's frame, the FBX origin
  at `at`, Unity euler y = -yaw, so Blender (plan x, plan y, up) takes the builder frame turned by pi + yaw. Today's pieces are the adopted copies in
  unity/Assets/Suborned/Kit; the candidates are the inlays from --gen and the planters from --extra.
  day_today.png / day_glow.png    the play camera (orthographic, 38 deg down, 20 m tall: zoom 10 at 1080p), sun; the candidates with their glows at the
                                  day value (inlay emission black, foot pool alpha 0, no shadow: shadows off, as Latch's import). The printed max / mean
                                  difference between the two proves day unchanged.
  night_today.png / night_glow.png  the same frame at night: moon, a dark teal sky, lamp pools at the sphere and globe lamps (stand-ins for Latch's
                                  TimeOfDay097.Lamps), bloom (threshold 1, as the grade's clamp); the glows at glows107's night values.
  night_close.png                 a closer frame (8 m tall) on the terrace planter's foot and the inlay spine.
  plan_glow.png                   top down, the glow strips drawn bright red over the stone: every strip outside its planter, none inside."""
import json
import math
import sys
from pathlib import Path

import bpy
from mathutils import Vector

HERE = Path(__file__).resolve().parent
REPO = HERE.parents[1]
sys.path.insert(0, str(HERE))
A = sys.argv[sys.argv.index("--") + 1:]
opt = lambda k, d: A[A.index(k) + 1] if k in A else d  # noqa: E731
OUT = Path(A[0])
OUT.mkdir(parents=True, exist_ok=True)
GEN, EXTRA = REPO / opt("--gen", "out/decokit-107"), REPO / opt("--extra", "out/decokit-107-extra")
ONLY = [s for s in opt("--only", "").split(",") if s]
KIT = REPO / "unity/Assets/Suborned/Kit"
ROWS = {r["name"]: r for r in json.loads((REPO / "out/decokit/decokit_materials.json").read_text())["materials"]}
ROWS.update({r["name"]: r for r in json.loads((GEN / "decokit_materials.json").read_text())["materials"]})
ASSETS = {a["id"]: a for a in json.loads((REPO / "contracts/world/kit.json").read_text())["assets"]}
BOX = (-190.0, -130.0, -258.0, -200.0)        # plan x0, x1, y0, y1: the café, its terrace and the fountain square
INLAYS = {"InlayBand_06_2", "InlayBand_12_2", "InlayArc_R2_W06", "InlayArc_R4_W06", "InlayArc_R9_W12"}

# glows107's values without importing it (it registers through mats, which needs the generator's modules): read them from its source
_G = {}
exec("\n".join(l for l in (HERE / "glows107.py").read_text().splitlines() if l.startswith(("FOOT_HEX", "INLAY_HEX", "EDGE_HEX", "EDGE_W"))), _G)
FOOT_HEX, FOOT_STRENGTH, FOOT_NIGHT = _G["FOOT_HEX"], _G["FOOT_STRENGTH"], _G["FOOT_ALPHA_NIGHT"]
INLAY_HEX = _G["INLAY_HEX"]
EDGE_HEX, EDGE_STRENGTH, EDGE_NIGHT, EDGE_W = _G["EDGE_HEX"], _G["EDGE_STRENGTH"], _G["EDGE_ALPHA_NIGHT"], _G["EDGE_W"]
# the band's own glow as the game draws it now (roadlight.json: 0 since 0.2.317, Sky's "that blue circle"), not glows107's first values
INLAY_NIGHT = next((g["night"] for g in json.loads((REPO / "unity/Assets/Suborned/Resources/Atmosphere/roadlight.json").read_text())["glow"] if g["material"] == "DK_Inlay_SeafoamNight"), 0.0)
# the dusk multipliers RoadLight already puts on the kit's emitters at night (roadlight.json glow, mode emission): the café interior is x0.1
NIGHT_MULT = {g["material"]: g["night"] for g in json.loads((REPO / "unity/Assets/Suborned/Resources/Atmosphere/roadlight.json").read_text())["glow"] if g["mode"] == "emission"}


def lin(c):
    return tuple(x / 12.92 if x <= 0.04045 else ((x + 0.055) / 1.055) ** 2.4 for x in c)


def hexlin(h):
    return lin(tuple(int(h[i:i + 2], 16) / 255 for i in (1, 3, 5)))


def placements():
    w = json.loads((REPO / "out/world/harbour-district.world.json").read_text())
    out = []
    for e in w["civic"]:
        k = e.get("kit")
        if k and BOX[0] < k["at"][0] < BOX[1] and BOX[2] < k["at"][1] < BOX[3]:
            out.append(dict(id=k["id"], at=k["at"], yaw=k["yaw"], base=k.get("base", e.get("base", 0.0)), scale=k.get("scale", [1, 1, 1])))
    return out


def fbx_of(pid, new):
    a = ASSETS[pid]
    f = next(x for x in a["files"] if x["role"] == "fbx")
    if new and pid in INLAYS:
        return GEN / f["path"]
    if new and a.get("extra") and (EXTRA / f.get("src", f["path"])).exists() and pid.replace("_TB", "") in PLANTER_IDS:
        return EXTRA / f["src"]
    return KIT / f["path"]


PLANTER_IDS = {r["name"] for r in json.loads((EXTRA / "glows107.json").read_text())["pieces"]}   # the rows glows107_extra.py rebuilt


def material(m, has_col, night, debug):
    nm = m.name.split(".")[0]
    m.use_nodes = True
    nt = m.node_tree
    for n in list(nt.nodes):
        nt.nodes.remove(n)
    o = nt.nodes.new("ShaderNodeOutputMaterial")
    if nm in ("DK_Glow_PlanterFoot", "DK_Glow_InlayEdge"):   # Cross Glow: c = colour x strength x alpha x k, added over what is beneath
        edge = nm == "DK_Glow_InlayEdge"   # mode 2 (field): k = (1 - |v| / _Radius)^1.5, _Radius = EDGE_W; mode 3 (pool): k = (1 - v)^1.5 (floor 0)
        m.surface_render_method = "BLENDED"
        uv, sep, inv, pw, mul = (nt.nodes.new(t) for t in ("ShaderNodeUVMap", "ShaderNodeSeparateXYZ", "ShaderNodeMath", "ShaderNodeMath", "ShaderNodeMath"))
        inv.operation, inv.inputs[0].default_value = "SUBTRACT", 1.0
        pw.operation, pw.inputs[1].default_value = "POWER", 1.5
        mul.operation = "MULTIPLY"
        alpha = 1.0 if debug else ((EDGE_NIGHT if edge else FOOT_NIGHT) if night else 0.0)
        mul.inputs[1].default_value = (EDGE_STRENGTH if edge else FOOT_STRENGTH) * alpha * (4.0 if debug else 1.0)
        nt.links.new(uv.outputs["UV"], sep.inputs[0])
        if edge:
            ab, dv, cl = (nt.nodes.new("ShaderNodeMath") for _ in range(3))
            ab.operation, dv.operation, cl.operation = "ABSOLUTE", "DIVIDE", "MINIMUM"
            dv.inputs[1].default_value, cl.inputs[1].default_value = EDGE_W, 1.0
            nt.links.new(sep.outputs["Y"], ab.inputs[0])
            nt.links.new(ab.outputs[0], dv.inputs[0])
            nt.links.new(dv.outputs[0], cl.inputs[0])
            nt.links.new(cl.outputs[0], inv.inputs[1])
        else:
            nt.links.new(sep.outputs["Y"], inv.inputs[1])
        nt.links.new(inv.outputs[0], pw.inputs[0])
        nt.links.new(pw.outputs[0], mul.inputs[0])
        em, tr, add = nt.nodes.new("ShaderNodeEmission"), nt.nodes.new("ShaderNodeBsdfTransparent"), nt.nodes.new("ShaderNodeAddShader")
        em.inputs["Color"].default_value = (1, 0, 0, 1) if debug else (*hexlin(EDGE_HEX if edge else FOOT_HEX), 1)
        nt.links.new(mul.outputs[0], em.inputs["Strength"])
        nt.links.new(tr.outputs[0], add.inputs[0])
        nt.links.new(em.outputs[0], add.inputs[1])
        nt.links.new(add.outputs[0], o.inputs["Surface"])
        return
    b = nt.nodes.new("ShaderNodeBsdfPrincipled")
    nt.links.new(b.outputs[0], o.inputs["Surface"])
    r = ROWS.get(nm)
    if nm.endswith("_TB") and ("Leaf" in nm or "Petal" in nm):   # the testbed planting: vertex colour is the albedo
        r = dict(kind="leaf", color=(0.4, 0.6, 0.35), emission=0.0, texture="")
    elif nm == "DK_Opal_TB":   # the testbed globe lamp's opal (no kit row): a warm emitter
        r = dict(kind="emit", color=(1.0, 0.93, 0.8), emission=1.0, texture="")
    elif nm.startswith("DK_Soil"):
        r = dict(kind="stone", color=(0.24, 0.17, 0.12), emission=0.0, texture="")
    r = r or {"kind": "enamel", "color": list(m.diffuse_color)[:3], "emission": 0.0, "texture": ""}
    kind, base = r["kind"], (*lin(r["color"]), 1.0)
    b.inputs["Base Color"].default_value = base
    b.inputs["Metallic"].default_value = 1.0 if kind in ("chrome", "metal") else 0.6 if kind == "brass" else 0.0
    b.inputs["Roughness"].default_value = {"chrome": 0.12, "metal": 0.3, "brass": 0.35, "stone": 0.75, "fabric": 0.85, "glass": 0.05, "leaf": 0.6}.get(kind, 0.45)
    if kind in ("emit", "glow"):
        b.inputs["Emission Color"].default_value = base
        b.inputs["Emission Strength"].default_value = max(1.0, r.get("emission", 1.0)) * (NIGHT_MULT.get(nm, 1.0) if night else 1.0)
    if nm == "DK_Inlay_SeafoamNight" and night:   # glows107: linear(INLAY_HEX) x level, added to the lit surface (URP Lit _EmissionColor)
        b.inputs["Emission Color"].default_value = (*hexlin(INLAY_HEX), 1)
        b.inputs["Emission Strength"].default_value = INLAY_NIGHT
    col = None
    if r.get("texture") and (KIT / "Textures" / r["texture"]).exists():
        tex = nt.nodes.new("ShaderNodeTexImage")
        tex.image = bpy.data.images.load(str(KIT / "Textures" / r["texture"]), check_existing=True)   # never the FBX's own image of that name (a /decokit/ path)
        col = tex.outputs["Color"]
    if has_col and kind not in ("emit", "glass", "glow"):
        at = nt.nodes.new("ShaderNodeVertexColor")
        at.layer_name = "Col"
        if kind == "leaf":
            col = at.outputs["Color"]
        else:   # occlusion = vertex colour R into the base colour
            mul = nt.nodes.new("ShaderNodeMix")
            mul.data_type, mul.blend_type = "RGBA", "MULTIPLY"
            mul.inputs[0].default_value = 1.0
            if col is not None:
                nt.links.new(col, mul.inputs[6])
            else:
                mul.inputs[6].default_value = base
            nt.links.new(at.outputs["Color"], mul.inputs[7])
            col = mul.outputs[2]
    if col is not None:
        nt.links.new(col, b.inputs["Base Color"])


def build(new, night, debug=False):
    bpy.ops.wm.read_factory_settings(use_empty=True)
    sc = bpy.context.scene
    cache, lamps = {}, []
    for p in placements():
        if ONLY and p["id"] not in ONLY:
            continue
        path = fbx_of(p["id"], new)
        if path not in cache:
            before = set(bpy.data.objects)
            bpy.ops.import_scene.fbx(filepath=str(path), colors_type="LINEAR")
            objs = [o for o in bpy.data.objects if o not in before and o.type == "MESH"]
            for o in [o for o in bpy.data.objects if o not in before and o.type != "MESH"]:
                bpy.data.objects.remove(o)
            for o in bpy.data.objects:
                o.select_set(o in objs)
            bpy.context.view_layer.objects.active = objs[0]
            bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
            for o in objs:
                o.parent = None
                sc.collection.objects.unlink(o) if o.name in sc.collection.objects else None
                for c in o.users_collection:
                    c.objects.unlink(o)
            cache[path] = objs
        for src in cache[path]:
            o = src.copy()
            sc.collection.objects.link(o)
            o.location = (p["at"][0], p["at"][1], p["base"])
            o.rotation_euler = (0, 0, math.pi + p["yaw"])
            o.scale = (p["scale"][0], p["scale"][2], p["scale"][1])
            if o.name.split(".")[0].endswith("_glow"):   # the row's objects: shadows off
                o.visible_shadow = False
        if p["id"] in ("PlazaSphereLamp_Play", "GlobeLamp_TB"):
            lamps.append((p["at"], p["base"] + (4.4 if p["id"] == "PlazaSphereLamp_Play" else 1.3), p["id"]))
    done = set()
    for o in sc.objects:
        for s in o.material_slots:
            if s.material and s.material.name not in done:
                done.add(s.material.name)
                material(s.material, bool(o.data.color_attributes), night, debug)
    ground(sc)
    light(sc, night, lamps)
    return sc


def ground(sc):
    bpy.ops.mesh.primitive_plane_add(size=160, location=(-160, -230, 0.178))
    g = bpy.context.object
    m = bpy.data.materials.new("pave")
    m.use_nodes = True
    nt = m.node_tree
    b = nt.nodes["Principled BSDF"]
    b.inputs["Roughness"].default_value = 0.8
    tc, mp, tex = nt.nodes.new("ShaderNodeTexCoord"), nt.nodes.new("ShaderNodeMapping"), nt.nodes.new("ShaderNodeTexImage")
    mp.inputs["Scale"].default_value = (0.25, 0.25, 0.25)    # a 4 m sheet (the plots' DK_Pave_Flags at tile 4)
    tex.image = bpy.data.images.load(str(KIT / "Textures/ground_pavelimestone_v1.png"))
    nt.links.new(tc.outputs["Object"], mp.inputs["Vector"])
    nt.links.new(mp.outputs["Vector"], tex.inputs["Vector"])
    mul = nt.nodes.new("ShaderNodeMix")
    mul.data_type, mul.blend_type = "RGBA", "MULTIPLY"
    mul.inputs[0].default_value = 1.0
    mul.inputs[7].default_value = (*lin((0.95, 0.91, 0.84)), 1)
    nt.links.new(tex.outputs["Color"], mul.inputs[6])
    nt.links.new(mul.outputs[2], b.inputs["Base Color"])
    g.data.materials.append(m)


def light(sc, night, lamps):
    sc.render.engine = "BLENDER_EEVEE"
    sc.view_settings.view_transform = "AgX"
    sc.view_settings.look = "AgX - Medium High Contrast"
    sc.world = bpy.data.worlds.new("w")
    sc.world.use_nodes = True
    bg = sc.world.node_tree.nodes["Background"]
    bg.inputs[0].default_value = (*lin((0.16, 0.22, 0.27)), 1) if night else (*lin((0.78, 0.84, 0.92)), 1)
    bg.inputs[1].default_value = 0.6 if night else 1.0
    sun = bpy.data.objects.new("sun", bpy.data.lights.new("sun", "SUN"))
    sun.data.energy = 0.25 if night else 3.4
    sun.data.color = (0.62, 0.74, 1.0) if night else (1.0, 0.93, 0.82)
    sun.data.angle = math.radians(2.5)
    to_sun = Vector((-0.45, 0.55, 0.7)).normalized()
    sun.rotation_euler = (-to_sun).to_track_quat("-Z", "Y").to_euler()
    sc.collection.objects.link(sun)
    if night:
        for at, z, pid in lamps:   # stand-ins for the night lamp pools (TimeOfDay097.Lamps: shadowless points at the lamp heads)
            L = bpy.data.objects.new("lamp", bpy.data.lights.new("lamp", "POINT"))
            L.data.energy, L.data.color, L.data.shadow_soft_size = (900 if pid == "PlazaSphereLamp_Play" else 150), (1.0, 0.8, 0.56), 0.3
            L.data.use_shadow = False
            L.location = (at[0], at[1], z)
            sc.collection.objects.link(L)
        bloom(sc)


def bloom(sc):
    try:   # Blender 5: the compositor is a node group
        ng = bpy.data.node_groups.new("comp", "CompositorNodeTree")
        sc.compositing_node_group = ng
        ng.interface.new_socket("Image", in_out="OUTPUT", socket_type="NodeSocketColor")
        rl, gl, go = ng.nodes.new("CompositorNodeRLayers"), ng.nodes.new("CompositorNodeGlare"), ng.nodes.new("NodeGroupOutput")
        gl.inputs["Type"].default_value = "Bloom"
        gl.inputs["Threshold"].default_value = 1.0
        gl.inputs["Strength"].default_value = 0.5
        gl.inputs["Size"].default_value = 0.6
        ng.links.new(rl.outputs["Image"], gl.inputs["Image"])
        ng.links.new(gl.outputs["Image"], go.inputs[0])
    except Exception as e:   # noqa: BLE001 - a review render without bloom says so
        print(f"[107] no bloom: {e!r}")


def camera(sc, target, height, yaw_deg, res=(1920, 1080)):
    cam = bpy.data.objects.new("cam", bpy.data.cameras.new("cam"))
    cam.data.type = "ORTHO"
    cam.data.ortho_scale = height * res[0] / res[1]
    cam.data.clip_end = 400
    f = Vector((math.sin(math.radians(yaw_deg)) * math.cos(math.radians(38)), math.cos(math.radians(yaw_deg)) * math.cos(math.radians(38)), -math.sin(math.radians(38))))
    cam.location = Vector(target) - f * 120
    cam.rotation_euler = f.to_track_quat("-Z", "Y").to_euler()
    sc.collection.objects.link(cam)
    sc.camera = cam
    sc.render.resolution_x, sc.render.resolution_y = res
    sc.render.film_transparent = False


def top(sc, target, size):
    cam = bpy.data.objects.new("top", bpy.data.cameras.new("top"))
    cam.data.type, cam.data.ortho_scale, cam.data.clip_end = "ORTHO", size, 400
    cam.location = (target[0], target[1], 100)
    sc.collection.objects.link(cam)
    sc.camera = cam
    sc.render.resolution_x = sc.render.resolution_y = 1600


def shot(sc, name):
    sc.render.filepath = str(OUT / name)
    bpy.ops.render.render(write_still=True)
    print(f"[107] {OUT / name}")


VIEW = dict(target=(-158.0, -229.0, 0.18), height=20.0, yaw=45.0)
CLOSE = dict(target=(-160.5, -226.6, 0.18), height=8.0, yaw=45.0)
RING = dict(target=(-153.5, -237.5, 0.18), height=10.0, yaw=45.0)   # round 2: the fountain's rings and a sphere lamp's ring, for the inlay edge light


def main():
    for new, night, name in ((False, False, "day_today.png"), (True, False, "day_glow.png"), (False, True, "night_today.png"), (True, True, "night_glow.png")):
        sc = build(new, night)
        camera(sc, VIEW["target"], VIEW["height"], VIEW["yaw"])
        shot(sc, name)
        if new and night:
            camera(sc, CLOSE["target"], CLOSE["height"], CLOSE["yaw"])
            shot(sc, "night_close.png")
        if night:
            camera(sc, RING["target"], RING["height"], RING["yaw"])
            shot(sc, "night_ring_glow.png" if new else "night_ring_today.png")
    sc = build(True, False, debug=True)
    top(sc, (-160.0, -232.0), 44.0)
    shot(sc, "plan_glow.png")
    a = bpy.data.images.load(str(OUT / "day_today.png"))
    b = bpy.data.images.load(str(OUT / "day_glow.png"))
    pa, pb = list(a.pixels), list(b.pixels)
    d = [abs(x - y) for x, y in zip(pa, pb)]
    print(f"[107] day_today vs day_glow: max |diff| {max(d) * 255:.2f} / 255, mean {sum(d) / len(d) * 255:.4f} / 255, pixels differing {sum(1 for x in d[0::4] if x > 0)}")


main()
