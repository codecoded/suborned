"""Brief 103 (Vela, 2026-11-09): the Records Office public front with each globe variant (globe_variants.py), rendered at the play camera.
Scratch, renders only: the entrance is built in this Blender session from the generator (kit.build, a patched globe); nothing is exported,
the kit and Unity are not touched (the tower model is read from the kit copy, never written).

/Applications/Blender.app/Contents/MacOS/Blender -b -P briefs/103/vela/globe/make/render.py -- <out_dir> [--test] [--threads N] [--samples N] [--only v1,v2] [--masks-only]

Scene, in the entrance's own frame (x along the face, -y out of it, z up, the door's landing at 0.75):
  - RecordsTower_TB (the game's Records Office) from unity/Assets/Suborned/Kit/Models/Landmarks, 10.5 m behind the entrance's pivot, as the
    world export places both (plan (-160, -294) and (-170.5, -294), the same yaw);
  - a pale paving plane; a 1.75 m stand-in for the door guard at the Office entrance point (block (64, -41.25) = the landing, 0.75 m out);
  - the play light (unity/Assets/Suborned/Editor/World/light.json): sun pitch 31, yaw 330, #ffddb5, 1.65 (x pi for Cycles' units); a sky
    whose teal and cream match what the chrome mirrors in Sky's frame; chrome and steel get no sun (light-linked out), as the game's
    _SpecularHighlights 0 on DK_Chrome and DK_Steel; enamels matte (brief 096 K2), stone with the kit's 2 m detail map, the brown glass film.
Cameras: orthographic, pitch 38, at the game's yaws. In the old town the Office front faces the camera at yaw 210 (60 deg off its normal)
and at yaw 300 (30 deg off); at 30 and 120 it faces away. Unity forward (sin yaw, cos yaw) in (x, z) is (cos yaw, -sin yaw) here.
Density: the Air's fullscreen play zoom (2880 x 1864, half-height 10 m: 93.2 px/m).
Writes <out>/<variant>-y<yaw>.png, <out>/mat-<finish>.png (A's finishes, close), <out>/mask-*.png (the door's hover-outline masks)."""
import json
import math
import os
import sys
from pathlib import Path

import bpy
import bmesh
from mathutils import Vector

HERE = Path(__file__).resolve().parent
REPO = HERE.parents[4]
os.environ.setdefault("DECOKIT_TEXDIRS_EXTRA", str(REPO / "out/decokit/Textures"))
sys.path.insert(0, str(REPO / "tools/decokit"))
sys.path.insert(0, str(HERE))
import mats  # noqa: E402
import civic_records  # noqa: E402,F401  (registers RecordsEntrance)
import globe_variants as gv  # noqa: E402

A = sys.argv[sys.argv.index("--") + 1:]
OUT = Path(A[0])
OUT.mkdir(parents=True, exist_ok=True)
TEST = "--test" in A


def opt(k, d):
    return A[A.index(k) + 1] if k in A else d


THREADS, SAMPLES = int(opt("--threads", 6)), int(opt("--samples", 16 if TEST else 64))
ONLY = opt("--only", "")
PPM = float(opt("--ppm", 93.2 / (2 if TEST else 1)))
KIT = REPO / "unity/Assets/Suborned/Kit"
LIGHT = json.loads((REPO / "unity/Assets/Suborned/Editor/World/light.json").read_text())
PITCH = 38.0
GLOSSY = float(opt("--glossy", 1.9))


def lin(c):
    return tuple(x / 12.92 if x <= 0.04045 else ((x + 0.055) / 1.055) ** 2.4 for x in c)


def hexc(h):
    h = h.lstrip("#")
    return tuple(int(h[i:i + 2], 16) / 255 for i in (0, 2, 4))


# ----------------------------------------------------------------------------- stage
def stage():
    bpy.ops.wm.read_factory_settings(use_empty=True)
    sc = bpy.context.scene
    sc.render.engine = "CYCLES"
    sc.cycles.device = "CPU"
    sc.render.threads_mode, sc.render.threads = "FIXED", THREADS
    sc.cycles.samples, sc.cycles.use_denoising = SAMPLES, True
    sc.cycles.max_bounces, sc.cycles.glossy_bounces, sc.cycles.transparent_max_bounces = 6, 3, 8
    sc.view_settings.view_transform, sc.view_settings.look = "ACES 1.3", "None"
    sc.view_settings.exposure = 0.0
    sc.render.film_transparent = False
    # sky: what the old town's zone probe gives the chrome in Sky's frame (teal-blue over a warm cream horizon); below it the ground plane answers
    w = bpy.data.worlds.new("sky")
    sc.world = w
    w.use_nodes = True
    nt = w.node_tree
    tc, sep, mr, cr = (nt.nodes.new(n) for n in ("ShaderNodeTexCoord", "ShaderNodeSeparateXYZ", "ShaderNodeMapRange", "ShaderNodeValToRGB"))
    mr.inputs["From Min"].default_value, mr.inputs["From Max"].default_value = -1.0, 1.0
    stops = ((0.0, "#c4beb2"), (0.47, "#e4ddce"), (0.5, "#f4e3c3"), (0.56, "#d8ebe6"), (0.68, "#a8d7e0"), (1.0, "#6eb0cb"))
    els = cr.color_ramp.elements
    for i, (pos, h) in enumerate(stops):
        e = els[i] if i < len(els) else els.new(pos)
        e.position, e.color = pos, (*lin(hexc(h)), 1)
    nt.links.new(tc.outputs["Generated"], sep.inputs[0])
    nt.links.new(sep.outputs["Z"], mr.inputs["Value"])
    nt.links.new(mr.outputs["Result"], cr.inputs["Fac"])
    # a soft glow toward the sun (the probe's sun-glow term) on top of the gradient
    s = LIGHT["sun"]
    p, y = math.radians(s["pitch"]), math.radians(s["yaw"])
    to_sun = Vector((-math.cos(y) * math.cos(p), math.sin(y) * math.cos(p), math.sin(p)))
    dot, glow, add = nt.nodes.new("ShaderNodeVectorMath"), nt.nodes.new("ShaderNodeMapRange"), nt.nodes.new("ShaderNodeMix")
    dot.operation = "DOT_PRODUCT"
    dot.inputs[1].default_value = to_sun
    nt.links.new(tc.outputs["Generated"], dot.inputs[0])
    glow.inputs["From Min"].default_value, glow.inputs["From Max"].default_value = 0.8, 1.0
    glow.interpolation_type = "SMOOTHSTEP"
    nt.links.new(dot.outputs["Value"], glow.inputs["Value"])
    add.data_type, add.blend_type = "RGBA", "ADD"
    nt.links.new(glow.outputs["Result"], add.inputs[0])
    nt.links.new(cr.outputs["Color"], add.inputs[6])
    add.inputs[7].default_value = (*lin(hexc("#fff1d8")), 1)
    nt.links.new(add.outputs[2], nt.nodes["Background"].inputs[0])
    # the zone probe is brighter than the ambient it lights with: glossy rays see the sky x GLOSSY (chrome and steel read as pale as in Sky's frame)
    lp, mx = nt.nodes.new("ShaderNodeLightPath"), nt.nodes.new("ShaderNodeMapRange")
    mx.inputs["To Min"].default_value, mx.inputs["To Max"].default_value = 0.85, 0.85 * GLOSSY
    nt.links.new(lp.outputs["Is Glossy Ray"], mx.inputs["Value"])
    nt.links.new(mx.outputs["Result"], nt.nodes["Background"].inputs[1])
    # sun (light.json): Unity Euler(31, 330, 0); light travels along Unity (sin330 cos31, -sin31, cos330 cos31) -> here (cos(330) .., -sin(330) ..)
    travel = Vector((math.cos(y) * math.cos(p), -math.sin(y) * math.cos(p), -math.sin(p)))
    sun = bpy.data.objects.new("sun", bpy.data.lights.new("sun", "SUN"))
    sun.data.energy = s["intensity"] * math.pi
    sun.data.color = lin(hexc(LIGHT["palette"]["light.sun"]["hex"]))
    sun.data.angle = math.radians(1.2)
    sun.rotation_euler = travel.to_track_quat("-Z", "Y").to_euler()
    sc.collection.objects.link(sun)
    # paving
    bpy.ops.mesh.primitive_plane_add(size=900)
    g = bpy.context.object
    g.name = "paving"
    g.data.materials.append(stone_mat("paving", "#E2D2B4", 0.8))
    return sc, sun


_detail = {}


def detail(nt, b, base_rgb, tile=2.0, coords="UV"):
    """URP _DETAIL_MULX2 as the kit's stone draws it: albedo x detail x 4.59 (linear), the detail normal at 0.5."""
    if not _detail:
        for k, f in (("alb", "surface_limestone_detail_v1.png"), ("nrm", "surface_limestone_detail_v1_normal.png")):
            p = KIT / "Textures" / f
            _detail[k] = bpy.data.images.load(str(p), check_existing=True) if p.exists() else None
            if _detail[k] is not None and k == "nrm":
                _detail[k].colorspace_settings.name = "Non-Color"
    if _detail.get("alb") is None:
        b.inputs["Base Color"].default_value = (*base_rgb, 1)
        return
    tc, mp = nt.nodes.new("ShaderNodeTexCoord"), nt.nodes.new("ShaderNodeMapping")
    mp.inputs["Scale"].default_value = (1 / tile, 1 / tile, 1 / tile)
    nt.links.new(tc.outputs[coords], mp.inputs["Vector"])
    im = nt.nodes.new("ShaderNodeTexImage")
    im.image = _detail["alb"]
    if coords == "Object":
        im.projection = "BOX"
    nt.links.new(mp.outputs[0], im.inputs[0])
    k = nt.nodes.new("ShaderNodeMix")
    k.data_type, k.blend_type = "RGBA", "MULTIPLY"
    k.inputs["Factor"].default_value = 1.0
    k.inputs[6].default_value = (*base_rgb, 1)
    nt.links.new(im.outputs["Color"], k.inputs[7])
    g = nt.nodes.new("ShaderNodeVectorMath")
    g.operation, g.inputs["Scale"].default_value = "SCALE", 4.5948
    nt.links.new(k.outputs[2], g.inputs[0])
    nt.links.new(g.outputs["Vector"], b.inputs["Base Color"])
    if _detail.get("nrm") is not None:
        n = nt.nodes.new("ShaderNodeTexImage")
        n.image = _detail["nrm"]
        if coords == "Object":
            n.projection = "BOX"
        nt.links.new(mp.outputs[0], n.inputs[0])
        nm = nt.nodes.new("ShaderNodeNormalMap")
        nm.inputs["Strength"].default_value = 0.5
        nt.links.new(n.outputs["Color"], nm.inputs["Color"])
        nt.links.new(nm.outputs["Normal"], b.inputs["Normal"])


def stone_mat(name, h, rough):
    m = bpy.data.materials.new(name)
    m.use_nodes = True
    b = m.node_tree.nodes["Principled BSDF"]
    b.inputs["Roughness"].default_value = rough
    detail(m.node_tree, b, lin(hexc(h)), 2.0, "Object")
    return m


def flat(name, h, rough=0.6, metal=0.0):
    m = bpy.data.materials.new(name)
    m.use_nodes = True
    b = m.node_tree.nodes["Principled BSDF"]
    b.inputs["Base Color"].default_value = (*lin(hexc(h)), 1)
    b.inputs["Roughness"].default_value = rough
    b.inputs["Metallic"].default_value = metal
    return m


# ----------------------------------------------------------------------------- the game's material response
SUN_OFF = ("DK_Chrome", "DK_Steel")


def game_look(m):
    """A DecoKit material as the play camera draws it (mats.LIB kind and colour; unity_mats K2, SUN_OFF, GLASS)."""
    if m.get("look103"):
        return
    m["look103"] = 1
    name = m.name.split(".")[0]
    row = mats.LIB.get(name)
    if not row or not m.use_nodes:
        return
    kind, h = row[0], row[1]
    nt = m.node_tree
    b = nt.nodes.get("Principled BSDF")
    if b is None:
        return
    rgb = lin(hexc(h))
    b.inputs["Coat Weight"].default_value = 0.0
    if kind == "enamel":                                   # brief 096 K2: smoothness 0.4, no coat, no environment reflections (the sun's soft sheen stays)
        b.inputs["Base Color"].default_value = (*rgb, 1)
        b.inputs["Roughness"].default_value = 0.6
        b.inputs["Specular IOR Level"].default_value = 0.25
    elif kind == "chrome":                                 # smoothness 0.95, but its mirror is the zone's 128 px box-projected probe: soft, as Sky's frame shows it
        b.inputs["Base Color"].default_value = (*rgb, 1)
        b.inputs["Metallic"].default_value, b.inputs["Roughness"].default_value = 1.0, 0.12
    elif kind == "metal":
        b.inputs["Base Color"].default_value = (*rgb, 1)
        b.inputs["Metallic"].default_value, b.inputs["Roughness"].default_value = 1.0, 0.3
    elif kind == "brass":
        b.inputs["Base Color"].default_value = (*rgb, 1)
        b.inputs["Metallic"].default_value, b.inputs["Roughness"].default_value = 0.6, 0.4
    elif kind == "stone" and name.startswith("DK_Limestone"):
        for lk in list(b.inputs["Base Color"].links):
            nt.links.remove(lk)
        b.inputs["Roughness"].default_value = 0.75
        detail(nt, b, rgb, 2.0, "UV")
    elif kind == "stone":
        b.inputs["Roughness"].default_value = 0.75
    elif kind == "glass" and name == "DK_GlassDark":       # the game's film: warm brown (0.36, 0.29, 0.22) at alpha 0.65 over the dark backing
        b.inputs["Base Color"].default_value = (*lin((0.36, 0.29, 0.22)), 1)
        b.inputs["Roughness"].default_value = 0.08
        b.inputs["Alpha"].default_value = 0.65
    elif kind == "emit":
        b.inputs["Emission Strength"].default_value = min(b.inputs["Emission Strength"].default_value, 3.0)


def split_out(obj, pred, tag):
    """A copy of obj holding only the faces whose material passes pred (removed from obj); None when there are none."""
    idx = {i for i, sl in enumerate(obj.material_slots) if sl.material and pred(sl.material)}
    if not idx:
        return None
    o2 = obj.copy()
    o2.data = obj.data.copy()
    o2.name = f"{obj.name}-{tag}"
    for c in obj.users_collection:
        c.objects.link(o2)
    for o, keep in ((o2, True), (obj, False)):
        bm = bmesh.new()
        bm.from_mesh(o.data)
        bmesh.ops.delete(bm, geom=[f for f in bm.faces if (f.material_index in idx) != keep], context="FACES")
        bm.to_mesh(o.data)
        bm.free()
    return o2


def sun_off(sun, objs):
    """Chrome and steel take no sun (the game's _SpecularHighlights 0; a metal has no diffuse, so no sun at all is the same thing)."""
    coll = bpy.data.collections.get("sunoff") or bpy.data.collections.new("sunoff")
    sun.light_linking.receiver_collection = coll
    for o in objs:
        part = split_out(o, lambda m: m.name.split(".")[0] in SUN_OFF, "sunoff")
        if part is not None:
            coll.objects.link(part)
    for co in coll.collection_objects:
        co.light_linking.link_state = "EXCLUDE"


def meshes(root):
    return [o for o in [root, *root.children_recursive] if o.type == "MESH"]


# ----------------------------------------------------------------------------- the Office and the guard
def tower(sun):
    f = KIT / "Models/Landmarks/DK_RecordsTower_TB.fbx"
    before = set(bpy.data.objects)
    bpy.ops.import_scene.fbx(filepath=str(f))
    new = [o for o in bpy.data.objects if o not in before]
    for o in new:
        if o.parent is None:
            o.location.y += 10.5                               # the entrance's pivot is 10.5 m out in front of the tower's (same yaw)
    bpy.context.view_layer.update()
    ms = [o for o in new if o.type == "MESH"]
    for o in ms:
        for sl in o.material_slots:
            if sl.material:
                game_look(sl.material)
    pts = [o.matrix_world @ Vector(c) for o in ms for c in o.bound_box]
    print("[103] tower bounds x %.2f..%.2f y %.2f..%.2f z %.2f..%.2f" % (min(p.x for p in pts), max(p.x for p in pts), min(p.y for p in pts), max(p.y for p in pts), min(p.z for p in pts), max(p.z for p in pts)))
    sun_off(sun, ms)
    return ms


def guard(at=(-0.55, -0.72, 0.75)):
    """A 1.75 m stand-in for the door guard (ivory jacket, ink trousers and cap), on the landing at the Office entrance point."""
    x, y, z = at
    ink, ivory, skin = flat("g_ink", "#2B2A2E", 0.7), flat("g_ivory", "#D8D0C0", 0.6), flat("g_skin", "#B8896A", 0.6)
    parts = []

    def cyl(r, z0, z1, m, dx=0.0, v=16):
        bpy.ops.mesh.primitive_cylinder_add(radius=r, depth=z1 - z0, location=(x + dx, y, z + (z0 + z1) / 2), vertices=v)
        o = bpy.context.object
        o.data.materials.append(m)
        parts.append(o)
    for dx in (-0.1, 0.1):
        cyl(0.075, 0.0, 0.86, ink, dx)
    cyl(0.2, 0.84, 1.42, ivory)
    cyl(0.07, 1.42, 1.5, skin)
    bpy.ops.mesh.primitive_uv_sphere_add(radius=0.12, location=(x, y, z + 1.6), segments=16, ring_count=8)
    parts.append(bpy.context.object)
    bpy.context.object.data.materials.append(skin)
    cyl(0.13, 1.66, 1.75, ink)
    for o in parts:
        o.name = "guard"
    return parts


def door_standin():
    """Latch's door stand-in for the hover outline: the public doorway filled, 3.4 m wide over the door's brass frame, landing (0.75) to the soffit
    (4.5), 0.16 m deep in front of the door plane (the frame stands 0.18 m out; a stand-in behind it would lose its edges to the frame)."""
    bpy.ops.mesh.primitive_cube_add(size=1, location=(0.0, -0.18, (0.75 + 4.5) / 2))
    o = bpy.context.object
    o.scale = (3.4, 0.16, 4.5 - 0.75)
    o.name = "door-standin"
    o.hide_render = True
    return o


# ----------------------------------------------------------------------------- cameras
def basis(yaw):
    th, p = math.radians(yaw), math.radians(PITCH)
    fwd = Vector((math.cos(th) * math.cos(p), -math.sin(th) * math.cos(p), -math.sin(p)))
    up = Vector((math.cos(th) * math.sin(p), -math.sin(th) * math.sin(p), math.cos(p)))
    right = fwd.cross(up)
    return fwd, up, right


FRAME = [(sx * 4.4, 0.3, 0.0) for sx in (-1, 1)] + [(sx * 4.4, -3.7, 0.0) for sx in (-1, 1)] + [(sx * 2.4, 0.0, 10.4) for sx in (-1, 1)] + [(sx * 4.0, -0.4, 9.0) for sx in (-1, 1)]
CLOSE = [(sx * 2.4, -0.2, 9.6) for sx in (-1, 1)] + [(sx * 2.4, -2.0, 5.6) for sx in (-1, 1)] + [(sx * 3.2, -0.2, 4.0) for sx in (-1, 1)]


def camera(sc, yaw, pts, ppm, margin=0.4):
    fwd, up, right = basis(yaw)
    us = [Vector(p).dot(up) for p in pts]
    rs = [Vector(p).dot(right) for p in pts]
    u0, u1, r0, r1 = min(us) - margin, max(us) + margin, min(rs) - margin, max(rs) + margin
    target = right * ((r0 + r1) / 2) + up * ((u0 + u1) / 2)
    w, h = r1 - r0, u1 - u0
    cam = sc.camera or bpy.data.objects.new("cam", bpy.data.cameras.new("cam"))
    if cam.name not in sc.collection.objects:
        sc.collection.objects.link(cam)
    cam.location = target - fwd * 120
    cam.rotation_euler = fwd.to_track_quat("-Z", "Y").to_euler()
    cam.data.type = "ORTHO"
    res = (int(round(w * ppm / 2) * 2), int(round(h * ppm / 2) * 2))
    cam.data.ortho_scale = max(res) / ppm
    cam.data.clip_start, cam.data.clip_end = 1.0, 600.0
    sc.camera = cam
    sc.render.resolution_x, sc.render.resolution_y = res
    sc.render.resolution_percentage = 100
    return cam


def probe_only(sc):
    """The game's chrome mirrors one box-projected probe 8 m over the zone's centre (OldTownSwap: 'Handover zone probe'), so it never shows
    the shaded marquee or the stone beside it: here no mesh is seen by glossy rays, and the sky's lower half stands for the probe's sunlit paving."""
    for o in sc.objects:
        if o.type == "MESH":
            o.visible_glossy = False


def shoot(sc, name):
    probe_only(sc)
    sc.render.filepath = str(OUT / name)
    bpy.ops.render.render(write_still=True)
    print(f"[103] wrote {OUT / name}")


def masks(sc, yaw, tag, door, scene_objs):
    """The door's hover-outline masks (Workbench, flat object colour, no AA): full = the stand-in alone, seen = the stand-in behind everything else."""
    keep = (sc.render.engine, sc.view_settings.view_transform, sc.render.film_transparent)
    sc.render.engine = "BLENDER_WORKBENCH"
    sc.display.shading.light, sc.display.shading.color_type = "FLAT", "OBJECT"
    sc.display.render_aa = "OFF"
    sh = sc.display.shading
    sh.show_object_outline = sh.show_cavity = sh.show_shadows = sh.show_specular_highlight = sh.show_xray = False
    sc.view_settings.view_transform = "Standard"
    sc.render.film_transparent = True
    door.hide_render = False
    door.color = (1, 1, 1, 1)
    others = [o for o in scene_objs if o.type == "MESH" and o is not door and not o.hide_render]
    for o in others:
        o.color = (0, 0, 0, 1)
    for o in others:
        o.hide_render = True
    shoot(sc, f"mask-full-{tag}-y{yaw:03d}.png")
    for o in others:
        o.hide_render = False
    shoot(sc, f"mask-seen-{tag}-y{yaw:03d}.png")
    door.hide_render = True
    sc.render.engine, sc.view_settings.view_transform, sc.render.film_transparent = keep


# ----------------------------------------------------------------------------- run
def main():
    sc, sun = stage()
    tower(sun)
    guard()
    door = door_standin()
    jobs = [("today", None), ("a-nickel", None), ("b", None), ("c", None)]
    finishes = ["chrome", "nickel", "gold", "enamel"]
    if TEST:
        jobs, finishes = [("today", None)], []
    if ONLY:
        jobs = [(j, None) for j in ONLY.split(",") if not j.startswith("mat-")]
        finishes = [j[4:] for j in ONLY.split(",") if j.startswith("mat-")]
    if "--masks-only" in A:                                # the door's masks again, the beauty renders left as they are
        for variant, _ in jobs:
            ent = gv.build(variant)
            for yaw in (210, 300):
                camera(sc, yaw, FRAME, PPM)
                masks(sc, yaw, variant, door, list(sc.objects))
            for o in [o for o in bpy.data.objects if o.name.startswith(f"entrance-{variant}")]:
                bpy.data.objects.remove(o, do_unlink=True)
        return
    for variant, _ in jobs:
        ent = gv.build(variant)
        for o in meshes(ent):
            for sl in o.material_slots:
                if sl.material:
                    game_look(sl.material)
        sun_off(sun, meshes(ent))
        for yaw in ((210,) if TEST else (210, 300, 30) if variant == "today" else (210, 300)):
            camera(sc, yaw, FRAME, PPM)
            shoot(sc, f"{variant}-y{yaw:03d}.png")
            if not TEST and yaw in (210, 300):
                masks(sc, yaw, variant, door, list(sc.objects))
        for o in [o for o in bpy.data.objects if o.name.startswith(f"entrance-{variant}")]:
            bpy.data.objects.remove(o, do_unlink=True)
    for fin in finishes:
        ent = gv.build(f"a-{fin}")
        for o in meshes(ent):
            for sl in o.material_slots:
                if sl.material:
                    game_look(sl.material)
        sun_off(sun, meshes(ent))
        camera(sc, 210, CLOSE, PPM)
        shoot(sc, f"mat-{fin}-y210.png")
        for o in [o for o in bpy.data.objects if o.name.startswith(f"entrance-a-{fin}")]:
            bpy.data.objects.remove(o, do_unlink=True)


main()
