"""Brief 077 "the domes go" (Vela): build the dome-family replacements as candidate pieces and count today's planting.

bun tools/decokit/domes077.ts            (composes; needs domes077_paths.py's paths.json)
/Applications/Blender.app/Contents/MacOS/Blender -b -P tools/decokit/domes077.py
  -> out/decokit/troughs077/Models/DK_<piece>.fbx (+ .blend), domes_pieces.json (tris and welded vertices per LOD, mesh_sha256, bounds,
     top, materials), census.json (today's pieces: triangles by material, stonework vs planting, from the kit FBX the review places)

LODs are the testbed's own levels, named for Unity's importer: café-recipe pieces _LOD0 inspection, _LOD1 close, _LOD2 near, _LOD3 gameplay;
PlanterArc_TB _LOD0 close, _LOD1 near, _LOD2 gameplay (fountain-garden.js has three); GlobeLamp_TB one level. Export as export_kit.py."""
import hashlib
import json
import re
import sys
from pathlib import Path

import bpy
import numpy as np

sys.path.insert(0, str(Path(__file__).resolve().parent))
import troughs077_lib as L  # noqa: E402

M = json.loads((L.SRC / "domes077.manifest.json").read_text())
OUT = L.SRC / "Models"
OUT.mkdir(parents=True, exist_ok=True)
ORDER = {4: ["inspection", "close", "near", "gameplay"], 3: ["close", "near", "gameplay"], 1: ["gameplay"]}
KIT = {a["id"]: a for a in json.loads((L.REPO / "contracts/world/kit.json").read_text())["assets"]}
GEN = L.REPO / "out/decokit"
LEGACY = Path("~/Projects/the-suborned/game/Assets/TheSuborned/Art/DecoKit").expanduser()
PLANT = re.compile(r"Leaf|Bloom|Flower|GroundCover|CoreShrub")


def mesh_hash(me):
    h = hashlib.sha256()
    for attr, n, src in (("co", 3, me.vertices),):
        a = np.zeros(len(src) * n, np.float32)
        src.foreach_get(attr, a)
        h.update(a.tobytes())
    cn = np.zeros(len(me.loops) * 3, np.float32)
    me.corner_normals.foreach_get("vector", cn)
    h.update(cn.round(5).tobytes())
    for lay in me.uv_layers:
        uv = np.zeros(len(me.loops) * 2, np.float32)
        lay.data.foreach_get("uv", uv)
        h.update(uv.tobytes())
    for ca in me.color_attributes:
        cc = np.zeros(len(ca.data) * 4, np.float32)
        ca.data.foreach_get("color", cc)
        h.update(cc.tobytes())
    mi = np.zeros(len(me.polygons), np.int32)
    me.polygons.foreach_get("material_index", mi)
    h.update(mi.tobytes())
    return h.hexdigest()


result = {"schema": "suborned.domes077.pieces/1", "pieces": {}}
for file, rec in M["files"].items():
    if file.startswith("testbed_"):
        continue
    pid = file[:-4]
    bpy.ops.wm.read_factory_settings(use_empty=True)
    L.reset()
    meshes = rec["meshes"]
    levels = sorted({r["level"] for r in meshes}, key=lambda l: ["inspection", "close", "near", "gameplay"].index(l))
    order = ORDER[len(levels)]
    soil = [r for r in meshes if r["kind"] == "soil"]
    info = {"lods": {}, "materials": [], "source_bin": {"file": file, "sha256": rec["sha256"]}, "composition": M["composition"].get(pid)}
    objs = []
    for n, level in enumerate(order):
        parts, tris, welded, zmax, zmin = [], 0, 0, -9.0, 9.0
        mins, maxs = np.full(3, 9e9), np.full(3, -9e9)
        for r in [r for r in soil + [r for r in meshes if r["level"] == level and r["kind"] != "soil"] if r["tris"] > 0]:
            a = L.arrays(file, r)
            o = L.make_mesh(f"{pid}_{r['kind']}", a, L.tb_material(L.KIND_MAT[r["kind"]]))
            bpy.context.scene.collection.objects.link(o)
            parts.append(o)
            tris += r["tris"]
            welded += L.weld_count(a)
            p = L.to_blender(a["position"])
            mins, maxs = np.minimum(mins, p.min(0)), np.maximum(maxs, p.max(0))
        bpy.ops.object.select_all(action="DESELECT")
        for o in parts:
            o.select_set(True)
        bpy.context.view_layer.objects.active = parts[0]
        if len(parts) > 1:
            bpy.ops.object.join()
        j = bpy.context.view_layer.objects.active
        j.name = j.data.name = f"DK_{pid}_LOD{n}" if len(order) > 1 else f"DK_{pid}"
        objs.append(j)
        info["materials"] = [m.name for m in j.data.materials]
        info["lods"][level] = {"lod": n, "object": j.name, "tris": tris, "vertices_welded": welded, "mesh_sha256": mesh_hash(j.data),
                               "bounds": [[round(float(x), 4) for x in mins], [round(float(x), 4) for x in maxs]], "submeshes": len(j.data.materials)}
    bpy.ops.object.select_all(action="DESELECT")
    for o in objs:
        o.select_set(True)
    path = OUT / f"DK_{pid}.fbx"
    bpy.ops.export_scene.fbx(filepath=str(path), use_selection=True, object_types={"MESH"},
                             apply_unit_scale=True, apply_scale_options="FBX_SCALE_ALL",
                             axis_forward="-Z", axis_up="Y", bake_space_transform=True,
                             use_mesh_modifiers=False, mesh_smooth_type="OFF", use_tspace=False, colors_type="LINEAR",
                             add_leaf_bones=False, path_mode="STRIP", embed_textures=False)
    bpy.ops.wm.save_as_mainfile(filepath=str(OUT / f"DK_{pid}.blend"), compress=True)
    info["fbx"] = {"path": str(path.relative_to(L.REPO)), "sha256": hashlib.sha256(path.read_bytes()).hexdigest(), "bytes": path.stat().st_size}
    result["pieces"][pid] = info
    print(f"[077 domes] {pid}: " + ", ".join(f"{k} {x['tris']}" for k, x in info["lods"].items()))
(L.SRC / "domes_pieces.json").write_text(json.dumps(result, indent=1) + "\n")

# ------------------------------------------------------------------ census of today's pieces (planting vs stonework, by material)
census = {}
for pid in [f"PlanterRun_{k}_{v}" for k in ("2", "4", "Corner", "End") for v in "ABC"] + [f"BedTuft_{v}" for v in "ABC"] + ["PlanterArc", "CafeTerrace", "OvalPlanter"]:
    a = KIT.get(pid)
    f = next((x for x in a["files"] if x["role"] == "fbx"), None) if a else None
    if not f:
        continue
    path = (GEN if f.get("root") == "generated" else LEGACY) / f["path"]
    if not path.exists():
        census[pid] = {"missing": str(path)}
        continue
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.fbx(filepath=str(path))
    by = {}
    for o in bpy.data.objects:
        if o.type != "MESH" or re.search(r"_LOD[1-9]$|_L[1-9]$", o.name):
            continue
        me = o.data
        me.calc_loop_triangles()
        for t in me.loop_triangles:
            mn = o.material_slots[t.material_index].material.name.split(".")[0] if o.material_slots else "?"
            by[mn] = by.get(mn, 0) + 1
    plant = sum(n for m, n in by.items() if PLANT.search(m))
    census[pid] = {"fbx": str(path.relative_to(L.REPO)) if L.REPO in path.parents else str(path), "kit_tris": a["tris"], "by_material": by,
                   "planting_tris": plant, "stone_tris": sum(by.values()) - plant, "l1": [l["tris"] for l in a.get("lods", [])]}
    print(f"[077 census] {pid}: planting {plant}, stone {sum(by.values()) - plant}")
(L.SRC / "census.json").write_text(json.dumps(census, indent=1) + "\n")
print("DOMES077 BUILD DONE", len(result["pieces"]))
