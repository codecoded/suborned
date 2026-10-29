"""Brief 077 step 1 (Vela): build the testbed café trough planting as candidate kit pieces PlanterRun_4_A_TB / _B_TB / _C_TB.

bun tools/decokit/troughs077.ts                     (composes, writes out/decokit/troughs077/*.bin + manifest)
/Applications/Blender.app/Contents/MacOS/Blender -b -P tools/decokit/troughs077.py
  -> out/decokit/troughs077/Models/DK_PlanterRun_4_<v>_TB.fbx (+ .blend) and pieces.json (tris, welded vertices, bounds, fit, sha256)

Each piece is the planting only (soil, foliage incl. stems, flowers) in the PlanterRun_4 local frame: x along the run, y across, z up, pivot =
footprint centre at the trough base, so it sits at the same transform as the trough it fills (KitPlacer, base 0.18). The trough itself
(DK_PlanterRun_4_<v> slot 0, DK_Limestone -> DK_Limestone_Tex restyle) stays; its planting slots 1-4 are what this replaces.
Four LODs, the testbed's own authored levels, named for Unity's importer: _LOD0 inspection, _LOD1 close, _LOD2 near, _LOD3 gameplay
(the level the testbed drew at play zoom). Nothing decimated, no cards, no texture touched. FBX export as export_kit.py (-Z forward,
Y up, so Unity = (-x, z, -y)), vertex colours written linear (they are three's linear multipliers).
Determinism: the composed .bin files are byte-stable (sha256 in troughs077.manifest.json); FBX bytes carry Blender's creation
timestamp and object UIDs (as every kit FBX), so pieces.json also records mesh_sha256 per LOD (positions, corner normals, uv,
colour, material index) which is stable from run to run."""
import hashlib
import json
import sys
from pathlib import Path

import bpy
import numpy as np

sys.path.insert(0, str(Path(__file__).resolve().parent))
import troughs077_lib as L  # noqa: E402

M = L.manifest()
OUT = L.SRC / "Models"
OUT.mkdir(parents=True, exist_ok=True)
LOD_OF = {"inspection": 0, "close": 1, "near": 2, "gameplay": 3}
COP = M["coping"]
result = {"schema": "suborned.troughs077.pieces/1", "pieces": {}}


def sha(p):
    return hashlib.sha256(Path(p).read_bytes()).hexdigest()


for v in "ABC":
    bpy.ops.wm.read_factory_settings(use_empty=True)
    L.reset()
    file = f"PlanterRun_4_{v}_TB.bin"
    recs = M["files"][file]["meshes"]
    soil = next(r for r in recs if r["kind"] == "soil")
    pid = f"PlanterRun_4_{v}_TB"
    info = {"lods": {}, "materials": ["DK_Soil_TB", "DK_Leaf_TB", "DK_Petal_TB"], "source_bin": {"file": file, "sha256": M["files"][file]["sha256"]}}
    objs = []
    fit = {"outside_xy": 0, "below_coping": 0, "zmin": 9, "zmax": -9, "xmax": 0, "ymax": 0}
    for level in L.LEVELS:
        parts, tris, welded = [], 0, 0
        for r in [soil] + [r for r in recs if r["level"] == level and r["kind"] in ("foliage", "flowers")]:
            a = L.arrays(file, r)
            kind = {"soil": "soil", "foliage": "foliage", "flowers": "petals"}[r["kind"]]
            o = L.make_mesh(f"{pid}_{kind}", a, L.tb_material(kind))
            bpy.context.scene.collection.objects.link(o)
            parts.append(o)
            tris += r["tris"]
            welded += L.weld_count(a)
            p = L.to_blender(a["position"])
            if r["kind"] != "soil":
                fit["outside_xy"] += int(np.sum((np.abs(p[:, 0]) > COP["x"][1] + 1e-4) | (np.abs(p[:, 1]) > COP["y"][1] + 1e-4)))
                fit["below_coping"] += int(np.sum(p[:, 2] < COP["top"] - 1e-4))
                fit["zmin"], fit["zmax"] = min(fit["zmin"], float(p[:, 2].min())), max(fit["zmax"], float(p[:, 2].max()))
                fit["xmax"], fit["ymax"] = max(fit["xmax"], float(np.abs(p[:, 0]).max())), max(fit["ymax"], float(np.abs(p[:, 1]).max()))
        bpy.ops.object.select_all(action="DESELECT")
        for o in parts:
            o.select_set(True)
        bpy.context.view_layer.objects.active = parts[0]
        bpy.ops.object.join()
        j = bpy.context.view_layer.objects.active
        j.name = j.data.name = f"DK_{pid}_LOD{LOD_OF[level]}"
        objs.append(j)
        zs = [c[2] for c in (j.matrix_world @ __import__("mathutils").Vector(b) for b in j.bound_box)]
        me = j.data
        h = hashlib.sha256()
        co = np.zeros(len(me.vertices) * 3, np.float32)
        me.vertices.foreach_get("co", co)
        h.update(co.tobytes())
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
        info["lods"][level] = {"lod": LOD_OF[level], "object": j.name, "tris": tris, "vertices_welded": welded, "vertices_raw": tris * 3, "mesh_sha256": h.hexdigest(),
                               "top": round(max(zs), 4), "submeshes": len(j.data.materials)}
    info["fit"] = {k: (round(x, 4) if isinstance(x, float) else x) for k, x in fit.items()}
    info["fit"]["pass"] = fit["outside_xy"] == 0 and fit["below_coping"] == 0
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
    info["fbx"] = {"path": str(path.relative_to(L.REPO)), "sha256": sha(path), "bytes": path.stat().st_size}
    info["composition"] = M["composition"][v]
    result["pieces"][pid] = info
    print(f"[077] {pid}: " + ", ".join(f"{k} {x['tris']}" for k, x in info["lods"].items()) + f" fit {info['fit']}")

(L.SRC / "pieces.json").write_text(json.dumps(result, indent=1) + "\n")
ok = all(p["fit"]["pass"] for p in result["pieces"].values())
print("TROUGHS077 " + ("PASS" if ok else "FAIL"))
