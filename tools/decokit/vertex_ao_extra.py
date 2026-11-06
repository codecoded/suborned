"""Brief 097 (Vela): baked vertex AO (vertex_ao.py) for the Handover zone's brief-077 pieces, which live in the extra root out/decokit-077 (their
stonework is the adopted FBX with the domes stripped, built by domes077_final.py, which sha-checks its parents against kit.json: so AO goes on after).

/Applications/Blender.app/Contents/MacOS/Blender -b -t 1 -P tools/decokit/vertex_ao_extra.py -- [--src out/decokit-077] [--dst out/decokit-097-extra] [--only a,b] [--no-ao]
  Copies the whole src root to dst, then for each row in vertex_ao.EXTRA (or --only): imports each level (L0 + lods), applies the import transform,
  bakes AO (contact cuts on L0 only, +15 %), re-exports with domes077_final's FBX settings (colors_type LINEAR: R = occlusion as the file's value)
  and a fixed header (reproducible bytes), and rewrites the row's tris, sha256 and bytes in dst/decokit_manifest.json (row `ao`: tris before/after;
  `mesh_sha256` dropped: it proved the 077 stonework unchanged, which the cuts change on purpose). The planting objects (<id>_soil, the leaves and
  petals) keep their meshes and colours; they still occlude the stone. --no-ao: the same round trip without AO (proves the round trip itself).
  Import: bun run kit --extra out/decokit-097-extra (in place of out/decokit-077)."""
import datetime
import hashlib
import json
import os
import shutil
import sys
from pathlib import Path

import bpy

HERE = Path(__file__).resolve().parent
sys.path.insert(0, str(HERE))
import vertex_ao as V  # noqa: E402

REPO = HERE.parents[1]
ARGS = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
opt = lambda k, d: ARGS[ARGS.index(k) + 1] if k in ARGS else d  # noqa: E731
SRC, DST = REPO / opt("--src", "out/decokit-077"), REPO / opt("--dst", "out/decokit-097-extra")
ONLY = [n for n in opt("--only", ",".join(V.EXTRA)).split(",") if n]
NO_AO = "--no-ao" in ARGS


def fixed_header():
    """As export_kit._fixed_fbx_header: reproducible header time, object ids and texture paths."""
    from io_scene_fbx import export_fbx_bin as eb, fbx_utils
    orig = eb.fbx_header_elements
    fbx_utils._key_to_uuid = lambda uuids, key: int.from_bytes(hashlib.sha256(repr(key).encode()).digest()[:8], "big") >> 1 or 1
    eb.fbx_header_elements = lambda root, scene_data, time=None: orig(root, scene_data, datetime.datetime(2026, 1, 1))
    orig_vid = eb._gen_vid_path

    def vid_path(img, scene_data):
        rel = os.path.basename(orig_vid(img, scene_data)[1].replace("\\", "/"))
        return "/decokit/" + rel, rel
    eb._gen_vid_path = vid_path


def load(path):
    bpy.ops.wm.read_factory_settings(use_empty=True)
    fixed_header()
    bpy.ops.import_scene.fbx(filepath=str(path), colors_type="LINEAR")
    objs = sorted((o for o in bpy.data.objects if o.type == "MESH"), key=lambda o: o.name)
    for o in bpy.data.objects:
        o.select_set(o.type == "MESH")
    bpy.context.view_layer.objects.active = objs[0]
    bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
    for o in objs:
        for s in o.material_slots:  # an import names a duplicate material .001: the file must name the kit's material
            if s.material and "." in s.material.name:
                s.material.name = s.material.name.split(".")[0]
    return objs


def export(objs, path):
    bpy.ops.object.select_all(action="DESELECT")
    for o in objs:
        o.select_set(True)
    bpy.ops.export_scene.fbx(filepath=str(path), use_selection=True, object_types={"MESH"},
                             apply_unit_scale=True, apply_scale_options="FBX_SCALE_ALL",
                             axis_forward="-Z", axis_up="Y", bake_space_transform=True,
                             use_mesh_modifiers=False, mesh_smooth_type="OFF", use_tspace=False, colors_type="LINEAR",
                             add_leaf_bones=False, path_mode="STRIP", embed_textures=False)
    b = path.read_bytes()
    return dict(sha256=hashlib.sha256(b).hexdigest(), bytes=len(b))


def tris(objs):
    return sum(len(p.vertices) - 2 for o in objs for p in o.data.polygons)


def main():
    shutil.copytree(SRC, DST, dirs_exist_ok=True)
    man_path = DST / "decokit_manifest.json"
    man = json.loads((SRC / "decokit_manifest.json").read_text())
    rows = {r["name"]: r for r in man["assets"]}
    report = []
    for n in ONLY:
        row = rows[n]
        f0 = row["files"][0]
        levels = [f0] + row.get("lods", [])
        ao = dict(tris_before=f0["tris"])
        for k, lv in enumerate(levels):
            objs = load(SRC / lv["file"])
            t0 = tris(objs)
            if not NO_AO:
                V.bake(objs, "LINEAR", cut_contacts=(k == 0))
            t1 = tris(objs)
            lv.update(export(objs, DST / lv["file"]), tris=t1)
            lv.pop("mesh_sha256", None)
            print(f"[DK-AO] {n} level {k}: tris {t0} -> {t1} ({lv['file']})")
            if k == 0:
                row["tris"], ao["tris_after"] = t1, t1
        if not NO_AO:
            row["ao"] = dict(ao, brief="097", generator="tools/decokit/vertex_ao.py", sha256=hashlib.sha256((HERE / "vertex_ao.py").read_bytes()).hexdigest())
        report.append(dict(name=n, **ao))
    man_path.write_text(json.dumps(man, indent=1))
    (DST / "vertex_ao.json").write_text(json.dumps({"pieces": report}, indent=1))
    print(f"[DK-AO] {len(report)} rows -> {DST}")


main()
