"""Brief 077 (Sky 2026-10-30 "go", Vela): the final pieces under the original ids, testbed planting on the existing stonework, testbed LODs.

bun run guard /Applications/Blender.app/Contents/MacOS/Blender -b -P tools/decokit/domes077_final.py [-- --only RecordsTower,...]
  --only ids (brief 097): rebuild just these rows (a large parent id rebuilds the parent's own levels, not its planting companion unless that is
  listed too) and merge them into the root's manifest, stone.json and checks.json; every other file in the root is left as it is (this script's
  FBX bytes are not reproducible run to run, so a full rerun would rehash every row). DOMES077_DST=dir writes to another root.
  needs: bun tools/decokit/domes077.ts (writes out/decokit/troughs077/final_*.bin + domes077.manifest.json `finals`)
  reads: the adopted generated FBX of each parent (out/decokit/Models/<cat>/DK_<id>[_L1|_L2].fbx, sha256 checked against contracts/world/kit.json)
  -> out/decokit-077/ (a DECOKIT_OUT-style root): Models/<cat>/DK_<id>.fbx + DK_<id>_L1/_L2/_L3.fbx, decokit_manifest.json (rows in export_kit's
     format + lods with per-piece screens + declared collide + provenance), stone.json (stonework identity proof)

What each id becomes
- PlanterRun_2/4/Corner/End_<v>, PlanterArc (embed): every LOD = the piece's stonework exactly as adopted (its faces, normals, UVs and stone material
  slots, unchanged, in the same order) + one testbed planting level; planting slots DK_Soil_TB, DK_Leaf_TB, DK_Petal_TB follow the stone slots.
  The dome slots (DK_Leaf_*, DK_Bloom_*, DK_Flower_*, DK_GroundCover, DK_Leaf_CoreShrub) are gone. L0 inspection, L1 close, L2 near, L3 gameplay
  (PlanterArc: L0 close, L1 near, L2 gameplay: fountain-garden.js has three). Screens reproduce the testbed's choice at 1080 rows from the piece's size.
- CafeTerrace, CafeUpper_3, CafeUpper_4, RecordsTower (large): the parent keeps its id, its own LOD files and their switch points; each level is the
  adopted level with the dome faces (and their slots) removed, stonework unchanged. The planting is a companion piece <id>_Planting (same pivot and
  frame: placed at the parent's transform), with the four testbed LODs. Its LODGroup needs a reference size of 4.0 m (lodSize): a 23-35 m group's
  true size would put the switch heights above 1.
- CivicBedPlanting_<bed>: the testbed's bed-level planting of each of the five civic beds, pivot = the world ring's centroid at the kerb base, yaw 0.
- GlobeLamp_TB: the testbed's globe lamp, one level, no light (Glint adds the night lights).
Soil: its own object `<name>_soil` in every LOD file (DK_Soil_TB only, shadows off, today's shader; no vertex colour), apart from the leaves and petals
(DecoKit/Lit Cutaway Leaf). Footprint check: the build fails if any submesh's plan bounds leave the piece's footprint (stone box, parent's stone box, or the
bed's world ring in the piece frame) by more than 5 cm (checks.json).
Collide: every rebuilt id declares its current measured volumes (contracts/world/kit.collide.json), so colliders, cover and sight inputs stay as
they are; the planting companions and civic bed plantings declare none (render-only: the kerbs and troughs are what block). The lamp is measured."""
import hashlib
import json
import os
import re
import sys
from pathlib import Path

import bpy
import numpy as np
from mathutils import Vector

sys.path.insert(0, str(Path(__file__).resolve().parent))
import troughs077_lib as L  # noqa: E402

REPO = L.REPO
DM = json.loads((L.SRC / "domes077.manifest.json").read_text())
KIT = {a["id"]: a for a in json.loads((REPO / "contracts/world/kit.json").read_text())["assets"]}
COL = json.loads((REPO / "contracts/world/kit.collide.json").read_text())["pieces"]
GEN = REPO / "out/decokit"
DST = REPO / os.environ.get("DOMES077_DST", "out/decokit-077")  # brief 097: a scratch root to prove a rerun leaves the other rows byte-identical
PLANT = re.compile(r"Leaf|Bloom|Flower|GroundCover|CoreShrub")
ORDER = {4: ["inspection", "close", "near", "gameplay"], 3: ["close", "near", "gameplay"], 1: ["gameplay"]}
PPM = {"cafe": [180.0, 80.0, 45.0], "fountain": [80.0, 24 / 0.55]}  # px per m at the level switches (garden-runtime.js 72/32/18 px per 0.4 m; fountain 44/24 px per 0.55 m)
LOD_SIZE = 4.0
rows, stone_log, checks, PARENT_FP = [], {}, {}, {}
ARGS = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
ONLY = set(ARGS[ARGS.index("--only") + 1].split(",")) if "--only" in ARGS else None
WORLD = json.loads((REPO / 'out/world/harbour-district.world.json').read_text())


def sha(p):
    return hashlib.sha256(Path(p).read_bytes()).hexdigest()


def reset():
    bpy.ops.wm.read_factory_settings(use_empty=True)
    L.reset()


def import_fbx(path):
    before = set(bpy.data.objects)
    bpy.ops.import_scene.fbx(filepath=str(path))
    objs = [o for o in bpy.data.objects if o not in before and o.type == "MESH"]
    for o in objs:
        bpy.ops.object.select_all(action="DESELECT")
        o.select_set(True)
        bpy.context.view_layer.objects.active = o
        bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
    return objs


def face_sig(o, keep):
    """A content hash of the faces whose material passes keep(name): material name, corner positions, corner normals, UVs, in face order."""
    me = o.data
    h, n = hashlib.sha256(), 0
    uv = me.uv_layers.active
    for p in me.polygons:
        m = o.material_slots[p.material_index].material.name.split(".")[0] if o.material_slots else ""
        if not keep(m):
            continue
        n += 1
        h.update(m.encode())
        for li in p.loop_indices:
            v = me.vertices[me.loops[li].vertex_index].co
            h.update(np.round(np.array([v.x, v.y, v.z], np.float64), 5).tobytes())
            h.update(np.round(np.array(me.corner_normals[li].vector, np.float64), 4).tobytes())
            if uv:
                h.update(np.round(np.array(uv.data[li].uv, np.float64), 5).tobytes())
    return h.hexdigest(), n


def stone_only(o):
    """Delete the dome faces and their now empty slots; the stone faces and stone slots are left exactly as they were (order kept)."""
    import bmesh
    names = [s.material.name.split(".")[0] if s.material else "" for s in o.material_slots]
    bm = bmesh.new()
    bm.from_mesh(o.data)
    kill = [f for f in bm.faces if PLANT.search(names[f.material_index])]
    bmesh.ops.delete(bm, geom=kill, context="FACES")
    bm.to_mesh(o.data)
    bm.free()
    bpy.ops.object.select_all(action="DESELECT")
    o.select_set(True)
    bpy.context.view_layer.objects.active = o
    for i in reversed(range(len(o.material_slots))):
        if PLANT.search(names[i]):
            o.active_material_index = i
            bpy.ops.object.material_slot_remove()
    return len(kill)


def planting_objs(file, level, levels):
    recs = DM["files"][file]["meshes"]
    out = []
    for r in recs:
        if r["tris"] == 0:
            continue
        if r["kind"] in ("soil",) or r["level"] == level or len(levels) == 1:
            o = L.make_mesh(f"tb_{r['kind']}", L.arrays(file, r), L.tb_material(L.KIND_MAT[r["kind"]]))
            bpy.context.scene.collection.objects.link(o)
            out.append(o)
    # slot order: soil, leaf, petal (a stable order for every piece and level)
    rank = {"DK_Soil_TB": 0, "DK_Leaf_TB": 1, "DK_Petal_TB": 2, "DK_Chrome": 3, "DK_Opal_TB": 4}
    out.sort(key=lambda o: rank.get(o.data.materials[0].name.split(".")[0], 9))
    return out


def join(objs, name):
    bpy.ops.object.select_all(action="DESELECT")
    for o in objs:
        o.select_set(True)
    bpy.context.view_layer.objects.active = objs[0]
    if len(objs) > 1:
        bpy.ops.object.join()
    j = bpy.context.view_layer.objects.active
    j.name = j.data.name = name
    return j


def export(objs, rel):
    objs = objs if isinstance(objs, list) else [objs]
    path = DST / rel
    path.parent.mkdir(parents=True, exist_ok=True)
    bpy.ops.object.select_all(action="DESELECT")
    for o in objs:
        o.select_set(True)
    bpy.ops.export_scene.fbx(filepath=str(path), use_selection=True, object_types={"MESH"},
                             apply_unit_scale=True, apply_scale_options="FBX_SCALE_ALL",
                             axis_forward="-Z", axis_up="Y", bake_space_transform=True,
                             use_mesh_modifiers=False, mesh_smooth_type="OFF", use_tspace=False, colors_type="LINEAR",
                             add_leaf_bones=False, path_mode="STRIP", embed_textures=False)
    tris, h = 0, hashlib.sha256()
    for o in objs:
        o.data.calc_loop_triangles()
        tris += len(o.data.loop_triangles)
        h.update(o.name.encode())
        h.update(face_sig(o, lambda m: True)[0].encode())
    return dict(file=rel, tris=tris, sha256=sha(path), bytes=path.stat().st_size, mesh_sha256=h.hexdigest(),
                objects=[dict(name=o.name, materials=[m.name.split(".")[0] for m in o.data.materials], shadows="off" if o.name.endswith("_soil") else "on") for o in objs])


def geom(objs):
    objs = objs if isinstance(objs, list) else [objs]
    box = [o.matrix_world @ Vector(c) for o in objs for c in o.bound_box]
    d = [max(v[i] for v in box) - min(v[i] for v in box) for i in range(3)]
    return dict(size=[round(d[0], 4), round(d[2], 4), round(d[1], 4)],
                bounds=[round(-max(v.x for v in box), 3), round(-min(v.x for v in box), 3), round(-max(v.y for v in box), 3), round(-min(v.y for v in box), 3)],
                largest=max(d))


def xy_box(objs):
    vs = [o.matrix_world @ v.co for o in objs for v in o.data.vertices]
    return [min(v.x for v in vs), max(v.x for v in vs), min(v.y for v in vs), max(v.y for v in vs)]


def check_footprint(pid, objs, fp, tol=0.05):
    """Fail the build if any submesh (an object's faces of one material) has plan bounds outside the piece's footprint box (+ tol)."""
    bad = []
    for o in objs:
        me, mw = o.data, o.matrix_world
        per = {}
        for p in me.polygons:
            b = per.setdefault(p.material_index, [9e9, -9e9, 9e9, -9e9])
            for vi in p.vertices:
                c = mw @ me.vertices[vi].co
                b[0], b[1], b[2], b[3] = min(b[0], c.x), max(b[1], c.x), min(b[2], c.y), max(b[3], c.y)
        for mi, b in per.items():
            if b[0] < fp[0] - tol or b[1] > fp[1] + tol or b[2] < fp[2] - tol or b[3] > fp[3] + tol:
                bad.append(f"{o.name}/{o.material_slots[mi].material.name}: {[round(x, 3) for x in b]} outside {[round(x, 3) for x in fp]}")
    assert not bad, f"{pid}: submesh outside the footprint: " + "; ".join(bad)
    checks[pid] = dict(footprint=[round(x, 3) for x in fp], tol=tol, submeshes_checked=sum(len(o.data.materials) for o in objs), passed=True)


def assemble(base, plant, name):
    """[main object (stone + leaves + petals)] + [the soil as its own object `<name>_soil`, shadows off, on the plain soil material]."""
    soil = [o for o in plant if o.data.materials[0].name.split(".")[0] == "DK_Soil_TB"]
    rest = [o for o in plant if o not in soil]
    objs = []
    if base or rest:
        objs.append(join(base + rest, name))
    if soil:
        so = join(soil, f"{name}_soil")
        objs.append(so)
    return objs


def screens(rule, size):
    return [round(min(0.999, size * p / 1080), 4) for p in PPM[rule]]


def prov(parent=None, file=None):
    p = dict(generator={f"tools/decokit/{f}": sha(REPO / "tools/decokit" / f) for f in ("domes077.ts", "domes077_final.py", "domes077_paths.py", "troughs077_lib.py")},
             testbed_sources=DM["sources"], brief="077 (Sky 2026-10-30: go)")
    if file:
        p["planting_bin"] = {"file": f"out/decokit/troughs077/{file}", "sha256": DM["files"][file]["sha256"]}
    if parent:
        p["parent_fbx"] = parent
    return p


for pid, fin in DM["finals"].items():
    file, rule = fin["file"], fin["rule"]
    levels = sorted({r["level"] for r in DM["files"][file]["meshes"]}, key=lambda l: ["inspection", "close", "near", "gameplay"].index(l))
    order = ORDER[len(levels)]
    parent = fin.get("parent")
    a = KIT.get(parent) if parent else None
    cat = a["category"] if a else "Plaza"
    row = dict(name=pid, category=cat, source="generated", status="candidate", provenance=None)
    if ONLY is not None and pid not in ONLY and not (parent in ONLY and not fin["embed"]):
        continue
    if fin["embed"]:
        # ---------------- original id: stone (as adopted) + testbed planting, one file per LOD
        f0 = next(x for x in a["files"] if x["role"] == "fbx")
        src = GEN / f0["path"]
        assert sha(src) == f0["sha256"], f"{pid}: generated FBX differs from the catalogue's (run bun run decokit first)"
        lods = []
        for n, level in enumerate(order):
            reset()
            st = import_fbx(src)
            assert len(st) == 1, pid
            st = st[0]
            before, nf = face_sig(st, lambda m: not PLANT.search(m))
            removed = stone_only(st)
            after, nf2 = face_sig(st, lambda m: True)
            assert before == after and nf == nf2, f"{pid}: stonework changed"
            stone_slots = [s.material.name.split(".")[0] for s in st.material_slots]
            fp = xy_box([st])
            if n == 0:  # the stonework alone, for the collision measure (the troughs block; the planting doesn't): domes077_collide.ts
                row["collide_from"] = export([st], f"stone/DK_{pid}_stone.fbx")["file"]
            obj = assemble([st], planting_objs(file, level, levels), f"DK_{pid}" + (f"_L{n}" if n else ""))
            check_footprint(pid, obj, fp)
            rel = f"Models/{cat}/DK_{pid}" + (f"_L{n}" if n else "") + ".fbx"
            e = export(obj, rel)
            g = geom(obj)
            if n == 0:
                row.update(fbx=rel, tris=e["tris"], size=g["size"], bounds=g["bounds"], materials=list(dict.fromkeys(m.name.split(".")[0] for o in obj for m in o.data.materials)),
                           files=[e], stone=dict(slots=stone_slots, faces=nf, sha=before, dome_faces_removed=removed, identical=True))
                size = g["largest"]
            else:
                lods.append(dict(e, level=level))
            stone_log.setdefault(pid, []).append(dict(level=level, stone_sha=before, stone_faces=nf))
        sc = screens(rule, size)
        row["lods"] = [dict(file=l["file"], tris=l["tris"], screen=sc[i], level=l["level"], sha256=l["sha256"], mesh_sha256=l["mesh_sha256"]) for i, l in enumerate(lods)]
        row["lod0_level"] = order[0]
        row["lodRule"] = f"testbed {rule} rule at 1080 rows: switches at {PPM[rule]} px per m, screen = size {round(size, 3)} m x px/m / 1080"
        row["collide"] = COL[parent]["volumes"]
        row["collide_note"] = f"declared: kit.collide.json's measured volumes of the adopted {parent} (sha {COL[parent]['sha256'][:12]}), unchanged"
        row["provenance"] = prov({"path": f"out/decokit/{f0['path']}", "sha256": f0["sha256"]}, file)
        row["replaces"] = {"id": parent, "fbx_sha256": f0["sha256"], "tris": a["tris"], "materials": a["materials"]}
        rows.append(row)
    else:
        if parent and parent not in [r["name"] for r in rows]:
            # ---------------- large parent: its own levels, dome faces removed, stone unchanged
            prow = dict(name=parent, category=cat, source="generated", status="candidate")
            files = [x for x in a["files"] if x["role"] == "fbx"]
            plods = []
            for k, ref in enumerate([files[0]["path"]] + [l["file"] for l in a.get("lods", [])]):
                reset()
                src = GEN / ref
                if k == 0:
                    assert sha(src) == files[0]["sha256"], f"{parent}: generated FBX differs from the catalogue's"
                objs = import_fbx(src)
                sigs = []
                for o in objs:
                    before, nf = face_sig(o, lambda m: not PLANT.search(m))
                    removed = stone_only(o)
                    after, _ = face_sig(o, lambda m: True)
                    assert before == after, f"{parent} L{k}: stonework changed"
                    sigs.append(dict(object=o.name, stone_sha=before, stone_faces=nf, dome_faces_removed=removed))
                obj = join(objs, objs[0].name) if len(objs) > 1 else objs[0]
                e = export([obj], ref)
                stone_log.setdefault(parent, []).append(dict(level=f"L{k}", parts=sigs))
                if k == 0:
                    PARENT_FP[parent] = xy_box([obj])
                    prow["collide_from"] = ref  # the stripped L0 is stonework only
                    g = geom(obj)
                    prow.update(fbx=ref, tris=e["tris"], size=g["size"], bounds=g["bounds"], materials=[m.name.split(".")[0] for m in obj.data.materials], files=[e],
                                stone=dict(identical=True, parts=sigs))
                else:
                    plods.append(dict(file=ref, tris=e["tris"], screen=a["lods"][k - 1].get("screen"), sha256=e["sha256"], mesh_sha256=e["mesh_sha256"]))
            if plods:
                prow["lods"] = plods
            if a.get("lodClass"):
                prow["lodClass"] = a["lodClass"]
            prow["collide"] = COL[parent]["volumes"]
            prow["collide_note"] = f"declared: kit.collide.json's volumes of the adopted {parent} ({COL[parent]['source']}, sha {COL[parent]['sha256'][:12]}), unchanged"
            prow["companion"] = f"{parent}_Planting"
            prow["provenance"] = prov({"path": f"out/decokit/{files[0]['path']}", "sha256": files[0]["sha256"]})
            prow["replaces"] = {"id": parent, "fbx_sha256": files[0]["sha256"], "tris": a["tris"], "materials": a["materials"]}
            rows.append(prow)
            print(f"[077 final] {parent}: L0 {prow['tris']}" + "".join(f", L{k + 1} {l['tris']}" for k, l in enumerate(plods)))
        if ONLY is not None and pid not in ONLY:
            continue
        # ---------------- planting-only piece (companion, civic bed or lamp)
        lods = []
        for n, level in enumerate(order):
            reset()
            obj = assemble([], planting_objs(file, level, levels), f"DK_{pid}" + (f"_L{n}" if n else ""))
            if parent:
                check_footprint(pid, obj, PARENT_FP[parent])
            elif fin.get("place"):  # a civic bed: its world ring in the piece frame (local = R(-pi)(world - centroid))
                ring = next(e["ring"] for e in WORLD["civic"] if e["id"] == fin["place"]["world"] and e.get("surface") == "bed-kerb")
                c = fin["place"]["at"]
                loc = [(c[0] - x, c[1] - y) for x, y in ring]
                check_footprint(pid, obj, [min(p[0] for p in loc), max(p[0] for p in loc), min(p[1] for p in loc), max(p[1] for p in loc)])
            rel = f"Models/{cat}/DK_{pid}" + (f"_L{n}" if n else "") + ".fbx"
            e = export(obj, rel)
            if n == 0:
                g = geom(obj)
                row.update(fbx=rel, tris=e["tris"], size=g["size"], bounds=g["bounds"], materials=list(dict.fromkeys(m.name.split(".")[0] for o in obj for m in o.data.materials)), files=[e])
            else:
                lods.append(dict(e, level=level))
        if lods:
            sc = screens(rule, LOD_SIZE)
            row["lods"] = [dict(file=l["file"], tris=l["tris"], screen=sc[i], level=l["level"], sha256=l["sha256"], mesh_sha256=l["mesh_sha256"]) for i, l in enumerate(lods)]
            row["lod0_level"] = order[0]
            row["lodSize"] = LOD_SIZE
            row["lodRule"] = f"testbed {rule} rule at 1080 rows with LODGroup.size = {LOD_SIZE} m (the piece's own size would put the heights above 1): switches at orthographic half-height 3 / 6.75 / 12"
        if pid == "GlobeLamp_TB":
            row["note"] = "no light baked; the testbed's light is #fff1d8, range 5.8 m, decay 2, no shadows, at 2.31 m above the lamp base, off by day (Glint). Collide: measured by bun run collide (a lamp post stands)."
            row["placements"] = {"rule": "one per PlanterArc, in the arc's frame", "at": DM["composition"]["GlobeLamp_TB"]["inPlanterArcFrame"]}
        else:
            row["collide"] = []
            row["collide_note"] = "declared none: render-only planting; the trough, parent piece or bed kerb is what blocks"
        if parent:
            row["placement"] = f"at {parent}'s transform (same pivot and frame), as its companion"
        if fin.get("place"):
            row["placement"] = fin["place"]
        row["provenance"] = prov(None, file)
        rows.append(row)
    print(f"[077 final] {pid}: L0 {row['tris']}" + "".join(f", {l['level']} {l['tris']}@{l['screen']}" for l in row.get("lods", [])))

if ONLY is not None:  # merge: the rebuilt rows replace theirs in place, everything else stays as the last full run wrote it
    old = json.loads((DST / "decokit_manifest.json").read_text())["assets"]
    new = {r["name"]: r for r in rows}
    rows = [new.pop(r["name"], r) for r in old] + list(new.values())
    stone_log = {**json.loads((DST / "stone.json").read_text()), **stone_log}
    checks = {**json.loads((DST / "checks.json").read_text()), **checks}
(DST / "decokit_manifest.json").write_text(json.dumps({"assets": rows, "schema_note": "export_kit.py row format + status, lods[].level/sha256/mesh_sha256, lodSize, lodRule, stone, collide_note, replaces, provenance"}, indent=1) + "\n")
(DST / "stone.json").write_text(json.dumps(stone_log, indent=1) + "\n")
(DST / "checks.json").write_text(json.dumps(checks, indent=1) + "\n")
print("DOMES077 FINAL DONE", len(rows))
