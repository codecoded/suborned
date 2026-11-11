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
from mathutils import Matrix, Vector

sys.path.insert(0, str(Path(__file__).parent))
import kit  # noqa: E402
import mats  # noqa: E402
import regen  # noqa: E402
import tables  # noqa: E402
import unity_mats  # noqa: E402
import vertex_ao  # noqa: E402
import collide097  # noqa: E402

REPO = Path(__file__).resolve().parents[2]
DST = Path(os.environ.get("DECOKIT_OUT") or REPO / "out/decokit")  # generated binaries are never committed
SRC = mats.SOURCE_ART
ARGS = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
MATS_ONLY = "--mats-only" in ARGS  # brief 072-F: no piece is built; the textures, the materials table (merged, never shrunk) and the Unity .mat set are rewritten
LODS_ONLY, NO_LODS, NO_AO = "--lods-only" in ARGS, "--no-lods" in ARGS, "--no-ao" in ARGS  # lods-only: adopted pieces keep their binaries, only LOD files are written
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
    # texture paths: the writer joins the output folder onto each image name (absolute Filename/Path), so the bytes changed with the
    # folder the piece was exported into. Keep the name only, under a fixed virtual root; Unity binds textures by guid, never by this path.
    orig_vid = eb._gen_vid_path

    def vid_path(img, scene_data):
        rel = os.path.basename(orig_vid(img, scene_data)[1].replace("\\", "/"))
        return "/decokit/" + rel, rel
    eb._gen_vid_path = vid_path


def make_lods(n, cat, src, srcs, tris):
    """Decimated distance versions of an asset (and its parts), exported beside it. Plain collapse: the symmetric variant is not reproducible run to run.
    An asset with a hand-built far version (kit.asset far=) exports that as its L2 instead."""
    out = []
    for tag, ratio, screen, floor in LOD_SPEC:
        hand = kit.REGISTRY[n].get({"L1": "near", "L2": "far"}[tag])  # a hand-built level (kit.asset near= / far=) replaces the collapse
        if tris < floor and not hand:  # a hand-built level is made whatever the piece's weight (L1: brief 009; L2: brief 072, the per-storey stack modules; every other far= piece is over the floor)
            continue
        if hand:
            fobj = kit.build_lod(n, tag)
            far = baked_tree(fobj, lambda nm: nm)
            for o in [fobj, *fobj.children_recursive]:
                bpy.data.meshes.remove(o.data)
        else:
            for o in srcs:
                dec = o.modifiers.new("LOD", "DECIMATE")
                dec.ratio = ratio
            far = baked_tree(src, lambda nm: nm.replace(f"DK_{n}", f"DK_{n}_{tag}", 1))  # not _LOD1: Unity reads that suffix as its LODs
        for nm, b in far:
            b.name = nm
        lo = [b for _, b in far]
        for b in lo:
            _split_warped_ngons(b.data)
        if not NO_AO and n in vertex_ao.PIECES:  # brief 097: the LODs take the AO too, without the contact cuts
            vertex_ao.bake(lo, "SRGB", cut_contacts=False, separate=True)
        for b in lo:  # decimate leaves ~1 ulp noise in a few UVs between runs; snap to 1e-3 (a texel at 1k) so the file is reproducible
            for layer in b.data.uv_layers:
                for d in layer.data:
                    d.uv = (round(d.uv[0], 3), round(d.uv[1], 3))
        path = f"Models/{cat}/DK_{n}_{tag}.fbx"
        export_fbx(lo, DST / path)
        out.append(dict(file=path, tris=tri_count(lo), screen=screen))
        for b in lo:
            bpy.data.meshes.remove(b.data)
        if not hand:
            for o in srcs:
                o.modifiers.remove(o.modifiers["LOD"])
    return out


def _split_warped_ngons(me):
    """Collapse decimation can leave an n-gon (> 4 sides) out of its plane; Unity's FBX importer drops such polygons as
    self-intersecting (LinerDock L1, brief 007 round 3). Triangulate only those, so a clean LOD's file is unchanged."""
    import bmesh
    bm = bmesh.new()
    bm.from_mesh(me)
    warped = []
    for f in bm.faces:
        if len(f.verts) <= 4:
            continue
        n, p0 = f.normal, f.verts[0].co
        tol = 1e-3 * max(1.0, math.sqrt(max(f.calc_area(), 0.0)))
        if max(abs((v.co - p0).dot(n)) for v in f.verts) > tol:
            warped.append(f)
    if warped:
        bmesh.ops.triangulate(bm, faces=warped, quad_method="BEAUTY", ngon_method="BEAUTY")
        bm.to_mesh(me)
        print(f"[DK-EXPORT] {me.name}: triangulated {len(warped)} warped n-gons")
    bm.free()


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
    if "EdgeSoften" in obj.modifiers:
        # Determinism (brief 020): the Bevel modifier interpolates the UVs of the faces it cuts, and its float sums run in an order that depends on memory addresses, so a few
        # dozen UVs differ in the last bit between two runs (1.2e-7 on CoffeeCart: a different FBX each time). Snap them to 1e-3 (a metre-tiled texture's mm, a 1k sheet's texel),
        # as the LODs always were; pieces built with bevel 0 are byte-stable already and are not touched.
        for layer in me.uv_layers:
            for d in layer.data:
                d.uv = (round(d.uv[0], 3), round(d.uv[1], 3))
    out = bpy.data.objects.new(obj.name, me)
    bpy.context.scene.collection.objects.link(out)
    if obj.matrix_world != Matrix.Identity(4):  # brief 104: a part with its own pivot (geo.Shape.part pivot=) keeps its place; every other piece stands at the origin, unchanged
        out.matrix_world = obj.matrix_world.copy()
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
                extra = [mats.tex_path(file.replace(".png", f"_{k}.png")) or mats.TEXDIRS[0] / file.replace(".png", f"_{k}.png") for k in ("normal", "mask")]
                for f in extra:
                    if f.exists():
                        shutil.copy2(f, out / f.name)
                rows.append((name, file, *(f.name if f.exists() else "" for f in extra)))
                break
        else:  # the material then exports untextured: a piece using it gets other FBX bytes than on a machine that has the file
            print(f"[DK-EXPORT] WARNING texture {file} ({name}) not found in {', '.join(str(d) for d in mats.TEXDIRS)}")
    hdri = DST / "HDRI"
    hdri.mkdir(parents=True, exist_ok=True)
    for f in (SRC / "HDRI").glob("*"):
        shutil.copy2(f, hdri / f.name)
    return {n: (f, nm, mk) for n, f, nm, mk in rows}


def copy_ground():
    """The budgeted ground set (ground_tex.py): its PNGs and .metas into Textures/, the role table as ground.json beside the manifest. Run `python3 tools/decokit/ground_tex.py` first
    (run.ts does); a stale or missing set stops here rather than export ground materials with no textures."""
    src = mats.GROUND_DIR
    if not (src / "ground.json").exists():
        raise SystemExit(f"[DK-EXPORT] no ground set in {src}: run python3 tools/decokit/ground_tex.py")
    out = DST / "Textures"
    out.mkdir(parents=True, exist_ok=True)
    for f in sorted(p for g in ("ground_*", "robot_*", "car_*", "cabinet_*", "pavilion_*", "fountain_*", "signal_*", "horizon_*", "surface_*", "wall_*") for p in src.glob(g) if p.suffix != ".json"):
        shutil.copy2(f, out / f.name)
    shutil.copy2(src / "ground.json", DST / "ground.json")


def main():
    bpy.ops.wm.read_factory_settings(use_empty=True)
    _fixed_fbx_header()
    unity_mats.legacy_mats()  # needed for the .mat set: fail before building, not after
    reg = kit.load()
    mats.register_robot_enamels()
    only = [n for n in (ARGS[ARGS.index("--only") + 1].split(",") if "--only" in ARGS else []) if n]
    cats = [c for c in (ARGS[ARGS.index("--cats") + 1].split(",") if "--cats" in ARGS else []) if c]
    if cats:
        only = only + [n for n, spec in reg.items() if spec["cat"] in cats]
    names = [] if MATS_ONLY else (only or list(reg))
    manifest, ao_rows = [], []
    for n in names:
        src = kit.build(n)
        cat, srcs = src["dk_cat"], [src, *src.children_recursive]
        bpy.context.view_layer.update()
        box = [o.matrix_world @ Vector(c) for o in srcs for c in o.bound_box]
        d = Vector([max(v[i] for v in box) - min(v[i] for v in box) for i in range(3)])  # Blender (x, y, z) -> Unity
        full = baked_tree(src, lambda nm: nm)                                            # size (x, z, y), checks scale
        objs = [b for _, b in full]
        if not NO_AO and n in vertex_ao.PIECES:  # brief 097: contact cuts + baked vertex-colour AO (vertex_ao.py), L0 within +15 % triangles
            ao_rows.append(dict(name=n, tris_before=tri_count(objs), tris_after=vertex_ao.bake(objs, "SRGB", separate=True)[1]))
        tris = tri_count(objs)
        row = dict(name=n, category=cat, fbx=f"Models/{cat}/DK_{n}.fbx", tris=tris,
                   size=[round(d.x, 4), round(d.z, 4), round(d.y, 4)],
                   bounds=[round(-max(v.x for v in box), 3), round(-min(v.x for v in box), 3),  # footprint about the pivot in kit axes
                           round(-max(v.y for v in box), 3), round(-min(v.y for v in box), 3)],  # [x0, x1, z0, z1]; the FBX import mirrors X (kit x = -Blender x) and kit z = -Blender y
                   materials=list(dict.fromkeys(m.name for o in objs for m in o.data.materials)))
        if kit.REGISTRY[n].get("footprint"):  # brief 079: light spilling past the thing is no part of its size (kit.asset footprint=)
            fx, fz = kit.REGISTRY[n]["footprint"]
            row["size"][0], row["size"][2] = round(fx, 4), round(fz, 4)
            row["bounds"] = [round(-fx / 2, 3), round(fx / 2, 3), round(-fz / 2, 3), round(fz / 2, 3)]
        if kit.REGISTRY[n].get("height") is not None:  # brief 107: a glow lying over the thing is no part of its height (kit.asset height=)
            row["size"][1] = round(kit.REGISTRY[n]["height"], 4)
        if src.get("dk_volumes"):  # volumes the builder declared (geo.Shape.solid), builder frame -> kit frame
            row["collide"] = [dict(shape="box", min=[round(-v["max"][0], 4), round(v["min"][2], 4), round(-v["max"][1], 4)], max=[round(-v["min"][0], 4), round(v["max"][2], 4), round(-v["min"][1], 4)],
                                   **({"stand": True} if v["stand"] else {})) for v in json.loads(src["dk_volumes"])]
            row["collide_by"] = "builder"  # who declared it (brief 103, Splice): tools/decokit/collide_law.ts never takes an older export's 097 pin, kept by a partial run's merge, for a builder's
        if n in collide097.AS:  # brief 097: the catalogue size of what the piece replaced, so the fit scale is the same (engine 0.49.0 keeps it: the piece stays where it is drawn)
            row["size"] = collide097.AS[n].get("size", row["size"])
        cv = collide097.volumes(n, d.z)  # brief 097's collision of what the piece replaced; engine 0.49.0: None, measured from the mesh as built (collide097.py PINNED)
        if cv is not None:
            row["collide"] = cv
            row["collide_by"] = "097"
        if n in regen.ORDERS:  # a regen order's piece: which order, and what was built (fit table)
            row.update(source="generated", order=dict(id=n, hash=regen.ORDERS[n]["hash"]), built=regen.measure(n, src))
        if kit.REGISTRY[n].get("retired"):  # brief 061: marked for retirement (kit.asset retired=); the builder stays until Splice has re-cut the export
            row["retired"] = kit.REGISTRY[n]["retired"]
        if len(objs) > 1:
            row["parts"] = [nm for nm, _ in full[1:]]
        if (tris >= LOD_SPEC[0][3] or kit.REGISTRY[n].get("near")) and not NO_LODS and not kit.REGISTRY[n].get("nolod"):
            row["lods"] = make_lods(n, cat, src, srcs, tris)
        if not NO_LODS:  # a level this piece no longer takes (slimmed under the floor) leaves no file from an earlier build
            for tag, *_ in LOD_SPEC:
                if f"Models/{cat}/DK_{n}_{tag}.fbx" not in [l["file"] for l in row.get("lods", [])]:
                    (DST / f"Models/{cat}/DK_{n}_{tag}.fbx").unlink(missing_ok=True)
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
    if ao_rows:
        tables.write_merged(DST / "vertex_ao.json", "pieces", ao_rows, True)
    tex = copy_textures()
    copy_ground()
    if "SignalLens" in names:  # brief 061: the head's look data (state colours, cues, blink, the cycle, the atlas and ring tables) rides beside the manifest for Latch's SignalHead and Glint
        import crossing_lens
        crossing_lens.write_data(DST / "Data" / "signal-lens.json")
    rows = mats.material_rows()
    for row in rows:
        _, row["normal"], row["mask"] = tex.get(row["name"], ("", "", ""))
    partial = bool(only) or MATS_ONLY  # brief 072-C: a partial run merges into the shared tables on disk, never shrinks them (tables.py)
    rows, kept = tables.write_merged(DST / "decokit_materials.json", "materials", rows, partial)
    if kept:
        print(f"[DK-EXPORT] materials table: {kept} rows kept from the table on disk (not registered by this partial run)")
    written = unity_mats.write(DST / "decokit_materials.json", DST, mats.WORLD_RETUNE)  # Unity .mat for the new and retuned colours
    print(f"[DK-EXPORT] materials written ({len(written)}): {', '.join(written)}")
    tables.write_merged(DST / "decokit_manifest.json", "assets", manifest, partial)
    print(f"[DK-EXPORT] {len(manifest)} assets -> {DST}")


main()
