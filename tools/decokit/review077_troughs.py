"""Brief 077 step 2 (Vela): review renders of the café troughs, testbed | now | proposed, at play zoom and close, day and night.

/Applications/Blender.app/Contents/MacOS/Blender -b -P tools/decokit/review077_troughs.py -- <out_dir> <scene> <light> [px]
  scene  testbed  : the testbed's own baked café beds (plaza-garden*.bin.gz bed-west, bed-east, bed-side: its meshes, atlas v2, normal,
                    ORM, soil maps, its pearl trough shell) on a plain pale ground. Same cameras and light as the other two.
         now      : the civic block exactly as review075_civic.py draws it after the 075 restyle (every kit piece from the world export,
                    the current PlanterRun_4_A/B/C domes).
         proposed : the same scene; each PlanterRun_4_<v> keeps only its limestone trough (slot 0) and PlanterRun_4_<v>_TB
                    (out/decokit/troughs077/Models, imported back from its FBX) is placed at the trough's own transform.
  [px] [level]  render width (1920); force one planting level in every view (default: the testbed's choice per view)
  light  day | night   (night: a Blender approximation, moon + dark teal sky + a warm point light at every lamp; not Glint's grade)
Cameras: play = orthographic half-height 14 at pitch 38, yaw 30 and 210 (the game's play zoom); close = half-height 3 at pitch 38, same yaws.
The planting LOD drawn in each view is the testbed's own choice for that view at 1080 rows (garden-runtime.js: projected 0.4 m leaf span):
play zoom 38.6 px/m -> 15 px -> gameplay; close 180 px/m -> 72 px -> inspection. Both scenes use the same choice."""
import json
import math
import sys
from pathlib import Path

import bpy
from mathutils import Vector

HERE = Path(__file__).resolve().parent
sys.path.insert(0, str(HERE))
import troughs077_lib as L  # noqa: E402

ARGS = sys.argv[sys.argv.index("--") + 1:]
OUT, SCENE, LIGHT = Path(ARGS[0]), ARGS[1], ARGS[2]
PX = int(ARGS[3]) if len(ARGS) > 3 else 1920
FORCE = ARGS[4] if len(ARGS) > 4 else None  # draw this planting level in every view (e.g. inspection = LOD0 at play zoom)
OUT.mkdir(parents=True, exist_ok=True)
M = L.manifest()
log = {"scene": SCENE, "light": LIGHT, "views": {}, "placed": []}

if SCENE in ("now", "proposed"):
    # The 075 civic review scene, unchanged, up to its cameras (it renders nothing before them).
    src_path = HERE / "review075_civic.py"
    src = src_path.read_text().split("# ---------------------------------------------------------------- cameras")[0]
    sys.argv = [sys.argv[0], "--", str(OUT / "_075"), "after", "-", str(PX)]
    g = {"__file__": str(src_path), "__name__": "review075_stage"}
    exec(compile(src, str(src_path), "exec"), g)
    sc = g["sc"]
    W = g["W"]
    log["world"] = "out/world/harbour-district.world.json"
    PLAY_C, CLOSE_C = (-159.0, -224.0), (-179.5, -220.5)
else:
    bpy.ops.wm.read_factory_settings(use_empty=True)
    sc = bpy.context.scene
    sc.render.engine = "BLENDER_EEVEE_NEXT" if "BLENDER_EEVEE_NEXT" in bpy.types.RenderSettings.bl_rna.properties["engine"].enum_items.keys() else "BLENDER_EEVEE"
    sc.view_settings.view_transform = "Standard"
    sc.render.resolution_x, sc.render.resolution_y = PX, PX * 9 // 16
    if hasattr(sc.eevee, "use_shadows"):
        sc.eevee.use_shadows = True
    if hasattr(sc.eevee, "use_raytracing"):
        sc.eevee.use_raytracing = True
    sc.eevee.taa_render_samples = 32
    wd = bpy.data.worlds.new("w")
    sc.world = wd
    wd.use_nodes = True
    wd.node_tree.nodes["Background"].inputs[0].default_value = (0.42, 0.66, 0.70, 1)  # as review075_civic.py
    wd.node_tree.nodes["Background"].inputs[1].default_value = 0.85
    sun = bpy.data.lights.new("sun", "SUN")
    sun.energy, sun.color, sun.angle = 3.4, (1.0, 0.95, 0.86), math.radians(1.5)
    so = bpy.data.objects.new("sun", sun)
    sc.collection.objects.link(so)
    so.rotation_euler = (math.radians(48), 0, math.radians(-140))
    W = None
    PLAY_C, CLOSE_C = (0.0, -3.0), (5.7, -5.2)

# ---------------------------------------------------------------- planting
LOD_N = {"inspection": 0, "close": 1, "near": 2, "gameplay": 3}
lod_objs = {lv: [] for lv in L.LEVELS}  # objects drawn only at that level

if SCENE == "testbed":
    file = "testbed_cafe_beds.bin"
    for r in M["files"][file]["meshes"]:
        kind = {"foliage": "foliage", "flowers": "petals", "soil": "soil", "stone": "stone"}[r["kind"]]
        o = L.make_mesh(f"{r['name']}_{r['kind']}_{r['level']}", L.arrays(file, r), L.tb_material(kind))
        sc.collection.objects.link(o)
        if r["kind"] != "soil":
            lod_objs[r["level"]].append(o)
    # ground: pale paving under the beds (the testbed's terrace stone is not part of the bake)
    import bmesh
    bm = bmesh.new()
    for x, y in ((-40, -40), (40, -40), (40, 40), (-40, 40)):
        bm.verts.new((x, y, 0.0))
    bm.faces.new(bm.verts)
    me = bpy.data.meshes.new("ground")
    bm.to_mesh(me)
    gm = bpy.data.materials.new("ground")
    gm.use_nodes = True
    gm.node_tree.nodes["Principled BSDF"].inputs["Base Color"].default_value = (*L.hexlin("#e9dcc4"), 1)
    gm.node_tree.nodes["Principled BSDF"].inputs["Roughness"].default_value = 0.8
    me.materials.append(gm)
    go = bpy.data.objects.new("ground", me)
    sc.collection.objects.link(go)
    # the testbed beds stand on its terrace at z 0.9: drop them to the ground so the camera frames them as ours (z only)
    for lst in lod_objs.values():
        for o in lst:
            o.location.z = -0.9
    for o in sc.collection.objects:
        if o.name.startswith("testbed_") and "_soil_" in o.name:
            o.location.z = -0.9

if SCENE == "proposed":
    import bmesh
    lib = g["lib"]
    pieces = g["_pieces"]
    stripped = {}
    for v in "ABC":
        c = pieces.get(f"PlanterRun_4_{v}")
        if not c:
            continue
        for o in c.all_objects:
            if o.type != "MESH" or o.data.name in stripped:
                continue
            bm = bmesh.new()
            bm.from_mesh(o.data)
            names = [s.material.name if s.material else "" for s in o.material_slots]
            kill = [f for f in bm.faces if "Limestone" not in names[f.material_index]]
            stripped[o.data.name] = dict(removed=len(kill), kept=len(bm.faces) - len(kill))
            bmesh.ops.delete(bm, geom=kill, context="FACES")
            bm.to_mesh(o.data)
            bm.free()
    log["stripped"] = stripped
    tb = {}
    for v in "ABC":
        before = set(bpy.data.objects)
        bpy.ops.import_scene.fbx(filepath=str(L.SRC / "Models" / f"DK_PlanterRun_4_{v}_TB.fbx"), colors_type="LINEAR")
        new = [o for o in bpy.data.objects if o not in before]
        col = bpy.data.collections.new(f"k_PlanterRun_4_{v}_TB")
        lib.children.link(col)
        for o in new:
            for uc in list(o.users_collection):
                uc.objects.unlink(o)
            col.objects.link(o)
            if o.type == "MESH":
                if o.data.color_attributes:
                    o.data.color_attributes[0].name = "Col"
                for s in o.material_slots:
                    nm = s.material.name.split(".")[0]
                    s.material = L.tb_material({"DK_Leaf_TB": "foliage", "DK_Petal_TB": "petals", "DK_Soil_TB": "soil"}[nm])
                lv = next(k for k, n in LOD_N.items() if o.name.endswith(f"_LOD{n}"))
                lod_objs[lv].append(o)
        tb[v] = col
    for e in W["civic"]:
        k = e.get("kit") or {}
        if k.get("id", "").startswith("PlanterRun_4_") and not e.get("hidden"):
            v = k["id"][-1]
            x = bpy.data.objects.new(f"{e['id']}-TB", None)
            x.instance_type, x.instance_collection = "COLLECTION", tb[v]
            x.location = (k["at"][0], k["at"][1], k.get("base", 0.0))
            x.rotation_euler = (0, 0, math.pi + k.get("yaw", 0.0))
            sc.collection.objects.link(x)
            log["placed"].append(dict(id=e["id"], piece=f"PlanterRun_4_{v}_TB", at=k["at"], base=k.get("base", 0.0), yaw=k.get("yaw", 0.0)))

# ---------------------------------------------------------------- night
if LIGHT == "night":
    wd = sc.world
    wd.node_tree.nodes["Background"].inputs[0].default_value = (0.020, 0.055, 0.075, 1)
    wd.node_tree.nodes["Background"].inputs[1].default_value = 0.6
    for o in sc.objects:
        if o.type == "LIGHT" and o.data.type == "SUN":
            o.data.energy, o.data.color = 0.12, (0.62, 0.75, 1.0)
    lamps = []
    if W:
        for e in list(W["civic"]) + list(W["street"]):
            k = e.get("kit") or {}
            at = k.get("at") or e.get("at")
            if at and "Lamp" in k.get("id", "") and abs(at[0] - PLAY_C[0]) < 45 and abs(at[1] - PLAY_C[1]) < 45:
                lamps.append((at[0], at[1], k.get("base", e.get("base", 0.0)) + 4.2))
    else:  # the testbed scene has no lamps of ours: two at the same spacing as the café kerb lamps, labelled in the review
        lamps = [(-6.0, -8.0, 4.2), (7.0, -8.0, 4.2)]
    for i, p in enumerate(lamps):
        ld = bpy.data.lights.new(f"lamp{i}", "POINT")
        ld.energy, ld.color, ld.shadow_soft_size = 450.0, (1.0, 0.78, 0.52), 0.25
        lo = bpy.data.objects.new(f"lamp{i}", ld)
        lo.location = p
        sc.collection.objects.link(lo)
    log["night_lamps"] = len(lamps)

# ---------------------------------------------------------------- cameras
cam = bpy.data.cameras.new("cam77")
co = bpy.data.objects.new("cam77", cam)
sc.collection.objects.link(co)
sc.camera = co


def show_level(level):
    for lv, objs in lod_objs.items():
        for o in objs:
            o.hide_render = lv != level


def view(name, c, yaw, half):
    yw, pc = math.radians(yaw), math.radians(38)
    fwd = Vector((math.sin(yw) * math.cos(pc), math.cos(yw) * math.cos(pc), -math.sin(pc)))
    tgt = Vector((c[0], c[1], 0.18 if W else 0.0))
    co.location = tgt - fwd * 300
    co.rotation_euler = fwd.to_track_quat("-Z", "Y").to_euler()
    cam.type, cam.sensor_fit, cam.ortho_scale, cam.clip_start, cam.clip_end = "ORTHO", "VERTICAL", 2 * half, 1.0, 900.0
    level = FORCE or L.level_for(sc.render.resolution_y / (2 * half))
    show_level(level)
    f = OUT / f"{SCENE}-{LIGHT}-{name}{'-' + FORCE if FORCE else ''}.png"
    sc.render.filepath = str(f)
    bpy.ops.render.render(write_still=True)
    log["views"][name] = dict(file=f.name, centre=c, yaw=yaw, ortho_half=half, level=level, px_per_m=round(sc.render.resolution_y / (2 * half), 2))
    print(f"[077 review] {SCENE} {LIGHT} {name} level {level} -> {f}")


view("play-y030", PLAY_C, 30, 14)
view("play-y210", PLAY_C, 210, 14)
view("close-y030", CLOSE_C, 30, 3)
view("close-y210", CLOSE_C, 210, 3)
(OUT / f"{SCENE}-{LIGHT}{'-' + FORCE if FORCE else ''}.json").write_text(json.dumps(log, indent=1) + "\n")
print("REVIEW077 DONE")
