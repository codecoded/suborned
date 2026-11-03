"""Brief 077 "the domes go" (Vela): review renders for the whole dome family, testbed | now | proposed, day.

/Applications/Blender.app/Contents/MacOS/Blender -b -P tools/decokit/review077_domes.py -- <out_dir> <scene> [px]
  scene  now           the civic block as review075_civic.py draws it after the 075 restyle (today's domes, tufts, arcs, terrace)
         proposed      the same; every dome-family piece keeps only its stonework and its _TB planting is placed at its own transform:
                       PlanterRun_4_<v>_TB, PlanterArc_TB (+ GlobeLamp_TB where the testbed stands its lamp), CafeTerrace_Planting_TB,
                       BedTuft_<v>_TB at each of the 92 tuft placements (the per-unit replacement)
         proposed-bed  as proposed, but the five tufted beds take the testbed's own bed-level planting (CivicBed_<bed>_TB) instead of
                       tufts, and the flat bed-hedge fill goes (the soil sits on the kerb top, as on the troughs)
         testbed       the testbed's own meshes: its baked café beds, its baked civic beds, its fountain garden (its own code: stone,
                       soil, planting, four instances) with its four globe lamps, on a plain ground
         runs-now / runs-proposed   a studio assembly of the PlanterRun family (End_A + 2_B + Corner_C + 4_A + End_B, joined end to end)
Views: play = orthographic half-height 14 at pitch 38, yaw 30 and 210; close = half-height 3. The planting LOD is the testbed's own
choice per view at 1080 rows (café recipe: 0.4 m leaf rule; fountain: 0.55 m rule): play -> gameplay, close -> inspection / close."""
import json
import math
import re
import sys
from pathlib import Path

import bpy
from mathutils import Matrix, Vector

HERE = Path(__file__).resolve().parent
sys.path.insert(0, str(HERE))
import troughs077_lib as L  # noqa: E402

ARGS = sys.argv[sys.argv.index("--") + 1:]
OUT, SCENE = Path(ARGS[0]), ARGS[1]
PX = int(ARGS[2]) if len(ARGS) > 2 else 1920
OUT.mkdir(parents=True, exist_ok=True)
DM = json.loads((L.SRC / "domes077.manifest.json").read_text())
PATHS = json.loads((L.SRC / "paths.json").read_text())
PLANT = re.compile(r"Leaf|Bloom|Flower|GroundCover|CoreShrub")
ORDER = {4: ["inspection", "close", "near", "gameplay"], 3: ["close", "near", "gameplay"], 1: ["gameplay"]}
FOUNTAIN = {"PlanterArc_TB", "testbed_fountain"}
log = {"scene": SCENE, "views": {}, "placed": {}, "stripped": {}}
CIVIC = SCENE in ("now", "proposed", "proposed-bed")
STUDIO = SCENE.startswith("runs-")


def stage_plain():
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
    import bmesh
    bm = bmesh.new()
    for x, y in ((-120, -120), (120, -120), (120, 120), (-120, 120)):
        bm.verts.new((x, y, 0.0))
    bm.faces.new(bm.verts)
    me = bpy.data.meshes.new("ground")
    bm.to_mesh(me)
    gm = bpy.data.materials.new("ground")
    gm.use_nodes = True
    gm.node_tree.nodes["Principled BSDF"].inputs["Base Color"].default_value = (*L.hexlin("#e9dcc4"), 1)
    gm.node_tree.nodes["Principled BSDF"].inputs["Roughness"].default_value = 0.8
    me.materials.append(gm)
    sc.collection.objects.link(bpy.data.objects.new("ground", me))
    return sc


lod_objs = {}  # level -> [objects], separately for the two LOD rules


def reg(obj, level, fountain):
    lod_objs.setdefault(("f" if fountain else "c", level), []).append(obj)


if CIVIC:
    src_path = HERE / "review075_civic.py"
    src = src_path.read_text().split("# ---------------------------------------------------------------- cameras")[0]
    sys.argv = [sys.argv[0], "--", str(OUT / "_075"), "after", "-", str(PX)]
    g = {"__file__": str(src_path), "__name__": "review075_stage"}
    exec(compile(src, str(src_path), "exec"), g)
    sc, W, lib, pieces, piece = g["sc"], g["W"], g["lib"], g["_pieces"], g["piece"]
elif STUDIO:
    sc = stage_plain()
    g = None
    lib = bpy.data.collections.new("lib")
    sc.collection.children.link(lib)
    bpy.context.view_layer.layer_collection.children["lib"].exclude = True
    pieces = {}
    KITJ = {a["id"]: a for a in json.loads((L.REPO / "contracts/world/kit.json").read_text())["assets"]}

    def piece(pid):
        if pid in pieces:
            return pieces[pid]
        f = next(x for x in KITJ[pid]["files"] if x["role"] == "fbx")
        before = set(bpy.data.objects)
        bpy.ops.import_scene.fbx(filepath=str(L.REPO / "out/decokit" / f["path"]))
        c = bpy.data.collections.new(f"k_{pid}")
        lib.children.link(c)
        for o in [o for o in bpy.data.objects if o not in before]:
            for uc in list(o.users_collection):
                uc.objects.unlink(o)
            c.objects.link(o)
            if re.search(r"_LOD[1-9]$|_L[1-9]$", o.name):
                o.hide_render = True
            for s in o.material_slots:  # the kit's flat colours, from the generator's own material table
                if s.material:
                    s.material = flat(s.material.name.split(".")[0])
        pieces[pid] = c
        return c
    W = None
else:
    sc = stage_plain()
    W, g, lib, pieces = None, None, None, {}


_flat = {}


def flat(name):
    """Studio stand-in for a kit .mat: its _BaseColor from the Unity material when present (Kit/Materials), else grey."""
    if name in _flat:
        return _flat[name]
    m = bpy.data.materials.new(f"flat_{name}")
    m.use_nodes = True
    b = m.node_tree.nodes["Principled BSDF"]
    p = L.REPO / "unity/Assets/Suborned/Kit/Materials" / f"{name}.mat"
    rgb = (0.7, 0.7, 0.7)
    if p.exists():
        mm = re.search(r"_BaseColor: \{r: ([\d.e-]+), g: ([\d.e-]+), b: ([\d.e-]+)", p.read_text())
        if mm:
            rgb = tuple(L.lin(float(x)) for x in mm.groups())
    b.inputs["Base Color"].default_value = (*rgb, 1)
    b.inputs["Roughness"].default_value = 0.6
    _flat[name] = m
    return m


# ---------------------------------------------------------------- helpers: our _TB pieces, imported back from their FBX
_tb = {}


def tb_piece(pid):
    if pid in _tb:
        return _tb[pid]
    before = set(bpy.data.objects)
    bpy.ops.import_scene.fbx(filepath=str(L.SRC / "Models" / f"DK_{pid}.fbx"), colors_type="LINEAR")
    new = [o for o in bpy.data.objects if o not in before]
    col = bpy.data.collections.new(f"k_{pid}")
    lib.children.link(col)
    n = len([o for o in new if o.type == "MESH"])
    order = ORDER[n] if n in ORDER else ["gameplay"]
    for o in new:
        for uc in list(o.users_collection):
            uc.objects.unlink(o)
        col.objects.link(o)
        if o.type != "MESH":
            continue
        if o.data.color_attributes:
            o.data.color_attributes[0].name = "Col"
        for s in o.material_slots:
            s.material = L.tb_material(L.MAT_KIND[s.material.name.split(".")[0]])
        m = re.search(r"_LOD(\d)$", o.name)
        if m and n > 1:
            reg(o, order[int(m.group(1))], pid in FOUNTAIN)
    _tb[pid] = col
    log["placed"].setdefault(pid, 0)
    return col


def place(pid, loc, rot_z, name):
    x = bpy.data.objects.new(name, None)
    x.instance_type, x.instance_collection = "COLLECTION", tb_piece(pid)
    x.location, x.rotation_euler = loc, (0, 0, rot_z)
    sc.collection.objects.link(x)
    log["placed"][pid] += 1
    return x


def strip(pid):
    """Keep a kit piece's stonework only: delete every face whose material is planting (leaf, bloom, flower, ground cover, core shrub)."""
    import bmesh
    c = pieces.get(pid) if not STUDIO else piece(pid)
    if not c:
        return
    for o in c.all_objects:
        if o.type != "MESH" or o.data.name in log["stripped"]:
            continue
        bm = bmesh.new()
        bm.from_mesh(o.data)
        names = [s.material.name if s.material else "" for s in o.material_slots]
        kill = [f for f in bm.faces if PLANT.search(names[f.material_index].replace("u_", "").replace("flat_", ""))]
        log["stripped"][o.data.name] = dict(removed=len(kill), kept=len(bm.faces) - len(kill))
        bmesh.ops.delete(bm, geom=kill, context="FACES")
        bm.to_mesh(o.data)
        bm.free()


def kit_of(e):
    return (e.get("kit") or {}).get("id", "")


# ---------------------------------------------------------------- proposed: every dome-family piece
if SCENE in ("proposed", "proposed-bed"):
    for v in "ABC":
        strip(f"PlanterRun_4_{v}")
    strip("PlanterArc")
    strip("CafeTerrace")
    objs = {o.name: o for o in sc.collection.objects}
    lamp = DM["composition"]["GlobeLamp_TB"]["inPlanterArcFrame"]
    for e in W["civic"]:
        k, kid = e.get("kit") or {}, kit_of(e)
        if e.get("hidden") or not kid:
            continue
        loc, rz = (k["at"][0], k["at"][1], k.get("base", 0.0)), math.pi + k.get("yaw", 0.0)
        if kid.startswith("PlanterRun_4_"):
            place(f"{kid}_TB", loc, rz, f"{e['id']}-TB")
        elif kid == "PlanterArc":
            place("PlanterArc_TB", loc, rz, f"{e['id']}-TB")
            mw = Matrix.Translation(Vector(loc)) @ Matrix.Rotation(rz, 4, "Z")
            p = mw @ Vector((lamp["at"][0], lamp["at"][1], lamp["z"]))
            place("GlobeLamp_TB", tuple(p), rz, f"{e['id']}-lamp-TB")
        elif kid == "CafeTerrace":
            place("CafeTerrace_Planting_TB", loc, rz, f"{e['id']}-TB")
        elif kid.startswith("BedTuft_"):
            o = objs.get(e["id"])
            if SCENE == "proposed":
                o.instance_collection = tb_piece(f"{kid}_TB")
                log["placed"][f"{kid}_TB"] += 1
            elif o:
                o.hide_render = True
        elif e.get("surface") == "bed-hedge" and SCENE == "proposed-bed" and e["id"] in objs:
            objs[e["id"]].hide_render = True
    if SCENE == "proposed-bed":
        for bid, t in DM["civicBeds"].items():
            place(f"CivicBed_{bid}_TB", (t["t"][0], t["t"][1], t["base"]), t["angle"], f"civic/{bid}-TB")

# ---------------------------------------------------------------- testbed: its own meshes
TB_CENTRES = {"cafe": (0.0, -3.0), "fountain": (0.0, 40.0), "beds": None}
if SCENE == "testbed":
    def add(file, fountain=False, dz=0.0, dx=(0.0, 0.0)):
        for r in DM["files"][file]["meshes"] if file in DM["files"] else L.manifest()["files"][file]["meshes"]:
            if r["tris"] == 0:
                continue
            o = L.make_mesh(f"{r['name']}_{r['kind']}_{r['level']}", L.arrays(file, r), L.tb_material(L.KIND_MAT[r["kind"]]))
            o.location = (dx[0], dx[1], dz)
            sc.collection.objects.link(o)
            if r["kind"] != "soil":
                reg(o, r["level"], fountain)
    add("testbed_cafe_beds.bin", dz=-0.9)
    add("testbed_civic_beds.bin")
    add("testbed_fountain.bin", fountain=True, dx=TB_CENTRES["fountain"])
    for t in DM["composition"]["GlobeLamp_TB"]["testbed"]:
        for r in DM["files"]["GlobeLamp_TB.bin"]["meshes"]:
            o = L.make_mesh(f"lamp{t['i']}_{r['kind']}", L.arrays("GlobeLamp_TB.bin", r), L.tb_material(r["kind"]))
            o.location = (t["plan"][0] + TB_CENTRES["fountain"][0], t["plan"][1] + TB_CENTRES["fountain"][1], t["base"])
            o.rotation_euler = (0, 0, t["i"] * math.pi / 2)
            sc.collection.objects.link(o)

# ---------------------------------------------------------------- studio: the PlanterRun family joined end to end
ASSEMBLY = [("PlanterRun_End_A", (-3.65, -1.0), math.pi), ("PlanterRun_2_B", (-2.0, -1.0), 0.0), ("PlanterRun_Corner_C", (0.0, 0.0), 0.0),
            ("PlanterRun_4_A", (1.0, 3.0), math.pi / 2), ("PlanterRun_End_B", (1.0, 5.65), math.pi / 2)]
if STUDIO:
    # pieces are built with their front on -Y; the FBX round trip restores that local frame; Blender z rotation in plan
    for pid, (x, y), r in ASSEMBLY:
        if SCENE == "runs-proposed":
            strip(pid)
        e = bpy.data.objects.new(pid, None)
        e.instance_type, e.instance_collection = "COLLECTION", piece(pid)
        e.location, e.rotation_euler = (x, y, 0.0), (0, 0, r)
        sc.collection.objects.link(e)
        if SCENE == "runs-proposed":
            place(f"{pid}_TB", (x, y, 0.0), r, f"{pid}-TB")

# ---------------------------------------------------------------- cameras
cam = bpy.data.cameras.new("cam77")
co = bpy.data.objects.new("cam77", cam)
sc.collection.objects.link(co)
sc.camera = co


def show(px_per_m):
    lv_c, lv_f = L.level_for(px_per_m), L.fountain_level_for(px_per_m)
    for (rule, lv), objs in lod_objs.items():
        for o in objs:
            o.hide_render = lv != (lv_f if rule == "f" else lv_c)
    return lv_c, lv_f


def view(name, c, yaw, half, z=0.18):
    yw, pc = math.radians(yaw), math.radians(38)
    fwd = Vector((math.sin(yw) * math.cos(pc), math.cos(yw) * math.cos(pc), -math.sin(pc)))
    co.location = Vector((c[0], c[1], z)) - fwd * 300
    co.rotation_euler = fwd.to_track_quat("-Z", "Y").to_euler()
    cam.type, cam.sensor_fit, cam.ortho_scale, cam.clip_start, cam.clip_end = "ORTHO", "VERTICAL", 2 * half, 1.0, 900.0
    ppm = sc.render.resolution_y / (2 * half)
    lv = show(ppm)
    f = OUT / f"{SCENE}-{name}.png"
    sc.render.filepath = str(f)
    bpy.ops.render.render(write_still=True)
    log["views"][name] = dict(file=f.name, centre=list(c), yaw=yaw, ortho_half=half, level=lv[0], fountain_level=lv[1], px_per_m=round(ppm, 2))
    print(f"[077 domes] {SCENE} {name} {lv} -> {f.name}")


def terrace_point():
    run = PATHS["terrace"]["runs"][0]
    p = run[len(run) // 2]
    e = next(x for x in W["civic"] if kit_of(x) == "CafeTerrace")
    k = e["kit"]
    m = Matrix.Translation(Vector((k["at"][0], k["at"][1], 0))) @ Matrix.Rotation(math.pi + k["yaw"], 4, "Z")
    q = m @ Vector((p[0], p[1], 0))
    return (q.x, q.y)


if CIVIC:
    for yaw in (30, 210):
        view(f"square-y{yaw:03d}", (-159.0, -230.0), yaw, 14)
        view(f"beds-y{yaw:03d}", (-157.0, -296.0), yaw, 14)
    arc = next(x for x in W["civic"] if x["id"] == "civic/fountain-garden-north-east")["kit"]["at"]
    for yaw in (30, 210):
        view(f"close-arc-y{yaw:03d}", tuple(arc), yaw, 3)
        view(f"close-terrace-y{yaw:03d}", terrace_point(), yaw, 3)
        view(f"close-bed-y{yaw:03d}", (-171.9, -308.2), yaw, 3)
elif STUDIO:
    for yaw in (30, 210):
        view(f"runs-play-y{yaw:03d}", (-0.5, 1.5), yaw, 14, 0.0)
        view(f"runs-close-y{yaw:03d}", (-0.5, 1.5), yaw, 4, 0.0)
else:
    f0 = TB_CENTRES["fountain"]
    for yaw in (30, 210):
        view(f"square-y{yaw:03d}", (0.0, -3.0), yaw, 14, 0.0)
        view(f"fountain-y{yaw:03d}", f0, yaw, 14, 0.0)
        view(f"beds-y{yaw:03d}", (40.0, -20.0), yaw, 14, 0.0)
        view(f"close-arc-y{yaw:03d}", (f0[0] + 4.4, f0[1] + 4.4), yaw, 3, 0.0)
        view(f"close-terrace-y{yaw:03d}", (5.7, -5.2), yaw, 3, 0.0)
        view(f"close-bed-y{yaw:03d}", (26.5, 8.8), yaw, 3, 0.0)
(OUT / f"{SCENE}.json").write_text(json.dumps(log, indent=1) + "\n")
print("REVIEW077 DOMES DONE")
