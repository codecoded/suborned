"""Export the Deco Kit to Unity: one FBX per asset + manifest + material table + textures + HDRIs.

Blender -b -P Tools/Blender/decokit/export_kit.py -- [--only A,B] [--cats Street,Signals]
  Assets/TheSuborned/Art/DecoKit/Models/<Category>/DK_<Name>.fbx   (modifiers applied, custom normals; movable
                                                                   parts as sibling objects, DK_<Name>_<Part>)
  Assets/TheSuborned/Art/DecoKit/Models/<Category>/DK_<Name>_L1/_L2.fbx   (decimated distance versions, manifest `lods`)
  Assets/TheSuborned/Art/DecoKit/decokit_manifest.json             (name, category, tris, materials)
  Assets/TheSuborned/Art/DecoKit/decokit_materials.json            (mats.LIB, sRGB, + texture/normal/mask files)
  Assets/TheSuborned/Art/DecoKit/Textures/*, HDRI/*                  (only what the materials reference)
FBX axes: forward -Z, up Y, space transform baked, so Blender front (-Y) becomes Unity +Z with identity rotation.
"""

import json
import math
import os
import shutil
import sys
from pathlib import Path

import bpy
from mathutils import Vector

sys.path.insert(0, str(Path(__file__).parent))
import kit  # noqa: E402
import mats  # noqa: E402
import regen  # noqa: E402

REPO = Path(__file__).resolve().parents[2]
DST = Path(os.environ.get("DECOKIT_OUT") or REPO / "out/decokit")  # generated binaries are never committed
SRC = mats.SOURCE_ART
ARGS = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
LODS_ONLY, NO_LODS = "--lods-only" in ARGS, "--no-lods" in ARGS  # lods-only: adopted pieces keep their binaries, only LOD files are written
# LOD1 ~25% and LOD2 ~6% of the triangles; a level is skipped when the piece is too light to be worth it. screen = Unity LOD transition height [S].
LOD_SPEC = [("L1", 0.25, 0.35, 400), ("L2", 0.06, 0.12, 1500)]


def _fixed_fbx_header():
    """Reproducible FBX: the writer stamps the header (and the footer id derived from it) with now(); fix it, and the object ids."""
    import datetime
    import hashlib
    from io_scene_fbx import export_fbx_bin as eb, fbx_utils
    orig = eb.fbx_header_elements
    # object ids come from hash(key): str hashes are randomised per process (Blender ignores PYTHONHASHSEED), so derive them from the key text
    fbx_utils._key_to_uuid = lambda uuids, key: int.from_bytes(hashlib.sha256(repr(key).encode()).digest()[:8], "big") >> 1 or 1
    eb.fbx_header_elements = lambda root, scene_data, time=None: orig(root, scene_data, datetime.datetime(2026, 1, 1))


def make_lods(n, cat, src, srcs, tris):
    """Decimated distance versions of an asset (and its parts), exported beside it. Plain collapse: the symmetric variant is not reproducible run to run."""
    out = []
    for tag, ratio, screen, floor in LOD_SPEC:
        if tris < floor:
            continue
        for o in srcs:
            dec = o.modifiers.new("LOD", "DECIMATE")
            dec.ratio = ratio
        far = baked_tree(src, lambda nm: nm.replace(f"DK_{n}", f"DK_{n}_{tag}", 1))  # not _LOD1: Unity reads that suffix as its LODs
        for nm, b in far:
            b.name = nm
        lo = [b for _, b in far]
        for b in lo:  # decimate leaves ~1 ulp noise in a few UVs between runs; snap to 1e-3 (a texel at 1k) so the file is reproducible
            for layer in b.data.uv_layers:
                for d in layer.data:
                    d.uv = (round(d.uv[0], 3), round(d.uv[1], 3))
        path = f"Models/{cat}/DK_{n}_{tag}.fbx"
        export_fbx(lo, DST / path)
        out.append(dict(file=path, tris=tri_count(lo), screen=screen))
        for b in lo:
            bpy.data.meshes.remove(b.data)
        for o in srcs:
            o.modifiers.remove(o.modifiers["LOD"])
    return out


def baked(obj):
    """Evaluated copy (modifiers + custom normals applied). The bevel can emit NaN UVs on degenerate
    corners, which the FBX writer rejects: zero them and say how many."""
    dg = bpy.context.evaluated_depsgraph_get()
    me = bpy.data.meshes.new_from_object(obj.evaluated_get(dg), preserve_all_data_layers=True, depsgraph=dg)
    bad = 0
    for layer in me.uv_layers:
        for d in layer.data:
            if not all(math.isfinite(c) for c in d.uv):
                d.uv, bad = (0.0, 0.0), bad + 1
    if bad:
        print(f"[DK-EXPORT] {obj.name}: zeroed {bad} non-finite UVs from the bevel")
    out = bpy.data.objects.new(obj.name, me)
    bpy.context.scene.collection.objects.link(out)
    return out


def baked_tree(src, rename):
    """Baked copies of an asset and its movable parts (geo.Shape.part) as sibling roots: the FBX axis bake misplaces
    child objects, and every part shares the asset origin, so the engine turns a head and its face together."""
    return [(rename(o.name), baked(o)) for o in [src, *src.children_recursive]]


def tri_count(objs):
    return sum(len(p.vertices) - 2 for o in objs for p in o.data.polygons)


def export_fbx(objs, path):
    bpy.ops.object.select_all(action="DESELECT")
    for o in objs:
        o.select_set(True)
    bpy.context.view_layer.objects.active = objs[0]
    path.parent.mkdir(parents=True, exist_ok=True)
    bpy.ops.export_scene.fbx(filepath=str(path), use_selection=True, object_types={"MESH"},
                             apply_unit_scale=True, apply_scale_options="FBX_SCALE_ALL",
                             axis_forward="-Z", axis_up="Y", bake_space_transform=True,
                             use_mesh_modifiers=False, mesh_smooth_type="OFF", use_tspace=False,
                             add_leaf_bones=False, path_mode="STRIP", embed_textures=False)


def copy_textures():
    out = DST / "Textures"
    out.mkdir(parents=True, exist_ok=True)
    rows = []
    for name, (file, _) in mats.TEXTURES.items():
        for d in mats.TEXDIRS:
            if (d / file).exists():
                shutil.copy2(d / file, out / file)
                extra = [mats.TEXDIRS[0] / file.replace(".png", f"_{k}.png") for k in ("normal", "mask")]
                for f in extra:
                    if f.exists():
                        shutil.copy2(f, out / f.name)
                rows.append((name, file, *(f.name if f.exists() else "" for f in extra)))
                break
    hdri = DST / "HDRI"
    hdri.mkdir(parents=True, exist_ok=True)
    for f in (SRC / "HDRI").glob("*"):
        shutil.copy2(f, hdri / f.name)
    return {n: (f, nm, mk) for n, f, nm, mk in rows}


def main():
    bpy.ops.wm.read_factory_settings(use_empty=True)
    _fixed_fbx_header()
    reg = kit.load()
    only = [n for n in (ARGS[ARGS.index("--only") + 1].split(",") if "--only" in ARGS else []) if n]
    cats = [c for c in (ARGS[ARGS.index("--cats") + 1].split(",") if "--cats" in ARGS else []) if c]
    if cats:
        only = only + [n for n, spec in reg.items() if spec["cat"] in cats]
    names = only or list(reg)
    manifest = []
    for n in names:
        src = kit.build(n)
        cat, srcs = src["dk_cat"], [src, *src.children_recursive]
        bpy.context.view_layer.update()
        box = [o.matrix_world @ Vector(c) for o in srcs for c in o.bound_box]
        d = Vector([max(v[i] for v in box) - min(v[i] for v in box) for i in range(3)])  # Blender (x, y, z) -> Unity
        full = baked_tree(src, lambda nm: nm)                                            # size (x, z, y), checks scale
        objs = [b for _, b in full]
        tris = tri_count(objs)
        row = dict(name=n, category=cat, fbx=f"Models/{cat}/DK_{n}.fbx", tris=tris,
                   size=[round(d.x, 4), round(d.z, 4), round(d.y, 4)],
                   bounds=[round(-max(v.x for v in box), 3), round(-min(v.x for v in box), 3),  # footprint about the pivot in kit axes
                           round(-max(v.y for v in box), 3), round(-min(v.y for v in box), 3)],  # [x0, x1, z0, z1]; the FBX import mirrors X (kit x = -Blender x) and kit z = -Blender y
                   materials=list(dict.fromkeys(m.name for o in objs for m in o.data.materials)))
        if n in regen.ORDERS:  # a regen order's piece: which order, and what was built (fit table)
            row.update(source="generated", order=dict(id=n, hash=regen.ORDERS[n]["hash"]), built=regen.measure(n, src))
        if len(objs) > 1:
            row["parts"] = [nm for nm, _ in full[1:]]
        if tris >= LOD_SPEC[0][3] and not NO_LODS:
            row["lods"] = make_lods(n, cat, src, srcs, tris)
        for o in srcs:
            bpy.data.meshes.remove(o.data)  # also frees the object
        for nm, b in full:
            b.name = nm
        if not LODS_ONLY:
            export_fbx(objs, DST / "Models" / cat / f"DK_{n}.fbx")
        manifest.append(row)
        print(f"[DK-EXPORT] {n:18s} {cat:10s} tris={tris}" + (" lods=" + "/".join(str(l["tris"]) for l in row["lods"]) if "lods" in row else "")
              + (f" parts={','.join(row['parts'])}" if "parts" in row else ""))
        for b in objs:
            bpy.data.meshes.remove(b.data)
    tex = copy_textures()
    mats.export_json(DST / "decokit_materials.json")
    table = json.loads((DST / "decokit_materials.json").read_text())
    for row in table["materials"]:
        _, row["normal"], row["mask"] = tex.get(row["name"], ("", "", ""))
    (DST / "decokit_materials.json").write_text(json.dumps(table, indent=1))
    old = {m["name"]: m for m in json.loads((DST / "decokit_manifest.json").read_text())["assets"]} \
        if only and (DST / "decokit_manifest.json").exists() else {}
    old.update({m["name"]: m for m in manifest})
    (DST / "decokit_manifest.json").write_text(json.dumps(dict(assets=list(old.values())), indent=1))
    print(f"[DK-EXPORT] {len(manifest)} assets -> {DST}")


main()
