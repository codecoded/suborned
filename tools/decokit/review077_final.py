"""Brief 077 (Vela): check renders of the FINAL pieces (out/decokit-077), placed exactly as the world export places their ids.

/Applications/Blender.app/Contents/MacOS/Blender -b -P tools/decokit/review077_final.py -- <out_dir> <scene> [level]
  scene  civic : review075_civic.py's civic block (after the 075 restyle); every id in out/decokit-077/decokit_manifest.json replaces the adopted
                 piece's contents in place (same instances, same transforms, the restyle remap re-applied to the stone slots); the companions
                 (<id>_Planting) at their parent's transforms; CivicBedPlanting_* at their rows' placements (BedTuft and bed-hedge hidden); a
                 GlobeLamp_TB in each PlanterArc.
         runs  : the PlanterRun studio assembly of review077_domes.py from the final files (the soil joint fix)
  level  auto (default: the LOD Unity picks from each row's screens at this view, orthographic, lodBias 1) or L0 / L1 / L2 / L3 everywhere."""
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
LEVEL = ARGS[2] if len(ARGS) > 2 else "auto"
OUT.mkdir(parents=True, exist_ok=True)
FD = L.REPO / "out/decokit-077"
ROWS = {r["name"]: r for r in json.loads((FD / "decokit_manifest.json").read_text())["assets"]}
DM = json.loads((L.SRC / "domes077.manifest.json").read_text())
log = {"scene": SCENE, "level": LEVEL, "views": {}, "swapped": [], "placed": []}
groups = []  # (collection objects by level index, screens, size or lodSize)

src_path = HERE / "review075_civic.py"
src = src_path.read_text().split("# ---------------------------------------------------------------- cameras")[0]
if SCENE == "runs":  # a bare stage: the civic loop still runs but the cameras look at an empty corner far away
    pass
sys.argv = [sys.argv[0], "--", str(OUT / "_075"), "after", "-", "1920"]
g = {"__file__": str(src_path), "__name__": "review075_stage"}
exec(compile(src, str(src_path), "exec"), g)
sc, W, lib, pieces, unity_mat = g["sc"], g["W"], g["lib"], g["_pieces"], g["unity_mat"]


def load_levels(row, col):
    """Import a final row's L0..Ln into `col`; return [[objects of level k]]."""
    files = [row["fbx"]] + [l["file"] for l in row.get("lods", [])]
    lv = []
    for f in files:
        before = set(bpy.data.objects)
        bpy.ops.import_scene.fbx(filepath=str(FD / f), colors_type="LINEAR")
        new = [o for o in bpy.data.objects if o not in before]
        for o in new:
            for uc in list(o.users_collection):
                uc.objects.unlink(o)
            col.objects.link(o)
            if o.type == "MESH":
                if o.data.color_attributes:
                    o.data.color_attributes[0].name = "Col"
                for s in o.material_slots:
                    nm = s.material.name.split(".")[0]
                    s.material = L.tb_material(L.MAT_KIND[nm]) if nm in L.MAT_KIND else unity_mat(nm)
        lv.append([o for o in new if o.type == "MESH"])
    size = row.get("lodSize")
    groups.append(dict(levels=lv, screens=[l["screen"] for l in row.get("lods", [])], size=size, row=row["name"]))
    return lv


def own_col(name):
    c = bpy.data.collections.new(f"f_{name}")
    lib.children.link(c)
    return c


placed_companions = {}
for name, row in ROWS.items():
    if name in pieces and pieces[name]:  # an adopted id the scene already instances: swap its contents
        col = pieces[name]
        for o in list(col.objects):
            bpy.data.objects.remove(o, do_unlink=True)
        load_levels(row, col)
        g["apply_remap"](name)
        log["swapped"].append(name)
for e in W["civic"]:
    k = e.get("kit") or {}
    kid = k.get("id", "")
    if e.get("hidden") or not kid:
        continue
    loc, rz = Vector((k["at"][0], k["at"][1], k.get("base", 0.0))), math.pi + k.get("yaw", 0.0)
    comp = ROWS.get(f"{kid}_Planting")
    if comp:
        c = placed_companions.get(kid) or own_col(f"{kid}_Planting")
        if kid not in placed_companions:
            load_levels(comp, c)
            placed_companions[kid] = c
        x = bpy.data.objects.new(f"{e['id']}-planting", None)
        x.instance_type, x.instance_collection = "COLLECTION", c
        x.location, x.rotation_euler = loc, (0, 0, rz)
        sc.collection.objects.link(x)
        log["placed"].append(f"{kid}_Planting")
    if kid == "PlanterArc":
        c = placed_companions.get("lamp") or own_col("GlobeLamp_TB")
        if "lamp" not in placed_companions:
            load_levels(ROWS["GlobeLamp_TB"], c)
            placed_companions["lamp"] = c
        a = ROWS["GlobeLamp_TB"]["placements"]["at"]
        p = Matrix.Translation(loc) @ Matrix.Rotation(rz, 4, "Z") @ Vector((a["at"][0], a["at"][1], a["z"]))
        x = bpy.data.objects.new(f"{e['id']}-lamp", None)
        x.instance_type, x.instance_collection = "COLLECTION", c
        x.location, x.rotation_euler = p, (0, 0, rz)
        sc.collection.objects.link(x)
        log["placed"].append("GlobeLamp_TB")
objs = {o.name: o for o in sc.collection.objects}
for e in W["civic"]:
    if (kid := (e.get("kit") or {}).get("id", "")).startswith("BedTuft_") or e.get("surface") == "bed-hedge":
        if e["id"] in objs:
            objs[e["id"]].hide_render = True
for name, row in ROWS.items():
    if name.startswith("CivicBedPlanting_"):
        c = own_col(name)
        load_levels(row, c)
        pl = row["placement"]
        x = bpy.data.objects.new(f"{name}-at", None)
        x.instance_type, x.instance_collection = "COLLECTION", c
        x.location, x.rotation_euler = (pl["at"][0], pl["at"][1], pl["base"]), (0, 0, math.pi + pl["yaw"])
        sc.collection.objects.link(x)
        log["placed"].append(name)

if SCENE == "runs":
    for o in list(sc.collection.objects):
        if o.type != "LIGHT":
            o.hide_render = True
    ASSEMBLY = [("PlanterRun_End_A", (-3.65, -1.0), math.pi), ("PlanterRun_2_B", (-2.0, -1.0), 0.0), ("PlanterRun_Corner_C", (0.0, 0.0), 0.0),
                ("PlanterRun_4_A", (1.0, 3.0), math.pi / 2), ("PlanterRun_End_B", (1.0, 5.65), math.pi / 2)]
    OFF = Vector((-300.0, -500.0, 0.0))  # well away from the block
    import bmesh
    bm = bmesh.new()
    for x, y in ((-30, -30), (30, -30), (30, 30), (-30, 30)):
        bm.verts.new((x + OFF.x, y + OFF.y, 0.0))
    bm.faces.new(bm.verts)
    me = bpy.data.meshes.new("ground77")
    bm.to_mesh(me)
    me.materials.append(unity_mat("DK_Ground_Pavement"))
    sc.collection.objects.link(bpy.data.objects.new("ground77", me))
    for pid, (x, y), r in ASSEMBLY:
        c = own_col(pid + "_run")
        load_levels(ROWS[pid], c)
        g["apply_remap"](pid)
        e = bpy.data.objects.new(pid + "-run", None)
        e.instance_type, e.instance_collection = "COLLECTION", c
        e.location, e.rotation_euler = (x + OFF.x, y + OFF.y, 0.0), (0, 0, r)
        sc.collection.objects.link(e)

cam = bpy.data.cameras.new("cam77f")
co = bpy.data.objects.new("cam77f", cam)
sc.collection.objects.link(co)
sc.camera = co


def choose(half):
    """Unity's orthographic LOD choice: relative height = size / (2 x half); LOD k while above screens[k] (k = 0 first)."""
    for gr in groups:
        if any(x is None for x in gr["screens"]):  # a parent switching by distance class (RecordsTower): its L0 at every play zoom
            want = 0
        elif LEVEL != "auto":
            want = min(int(LEVEL[1]), len(gr["levels"]) - 1)
        else:
            objs = [o for o in gr["levels"][0]]
            size = gr["size"] or max(max(o.dimensions) for o in objs)
            rel = size / (2 * half)
            want = next((k for k, s in enumerate(gr["screens"]) if rel >= s), len(gr["screens"]))
        for k, lv in enumerate(gr["levels"]):
            for o in lv:
                o.hide_render = k != want


def view(name, c, yaw, half, z=0.18):
    yw, pc = math.radians(yaw), math.radians(38)
    fwd = Vector((math.sin(yw) * math.cos(pc), math.cos(yw) * math.cos(pc), -math.sin(pc)))
    co.location = Vector((c[0], c[1], z)) - fwd * 300
    co.rotation_euler = fwd.to_track_quat("-Z", "Y").to_euler()
    cam.type, cam.sensor_fit, cam.ortho_scale, cam.clip_start, cam.clip_end = "ORTHO", "VERTICAL", 2 * half, 1.0, 900.0
    choose(half)
    f = OUT / f"final-{SCENE}-{name}-{LEVEL}.png"
    sc.render.filepath = str(f)
    bpy.ops.render.render(write_still=True)
    log["views"][name] = f.name
    print(f"[077 final review] {name} -> {f.name}")


if SCENE == "runs":
    for yaw in (30, 210):
        view(f"runs-close-y{yaw:03d}", (-300.5, -498.5), yaw, 4, 0.0)
else:
    for yaw in (30, 210):
        view(f"square-y{yaw:03d}", (-159.0, -230.0), yaw, 14)
        view(f"beds-y{yaw:03d}", (-157.0, -296.0), yaw, 14)
    view("records-roof-y030", (-160.0, -294.0), 30, 6, 14.9)
    view("cafe-roof-y030", (-159.0, -214.0), 30, 6, 13.5)
(OUT / f"final-{SCENE}-{LEVEL}.json").write_text(json.dumps(log, indent=1) + "\n")
print("REVIEW077 FINAL DONE")
