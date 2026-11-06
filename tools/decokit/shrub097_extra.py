"""Brief 097 final pass (Vela): ShrubMound's testbed planting. The street kind `planter-bed:shrub` (capstone-rules.ts `shrub`) draws a 0.95 x 0.99 m cushion; this
cuts that patch from the testbed's own bed planting (CivicBedPlanting_office-west in the 077 root: the 077 leaf, petal and soil cards, vertex colour as albedo), sets it
on the ground at the old piece's height, and adds it to the extra root as a rebuild of ShrubMound (it joins the catalogue as ShrubMound_TB). Every LOD level is cut
the same way from the bed's own level. Collision: ShrubMound's volumes before brief 097 (collide097.json), so the walk and sight inputs stay as they were.

/Applications/Blender.app/Contents/MacOS/Blender -b -t 1 -P tools/decokit/shrub097_extra.py -- [--root out/decokit-097-extra]
  run after vertex_ao_extra.py (which rewrites the root's manifest from out/decokit-077). Idempotent: the row is replaced, never duplicated."""
import hashlib
import json
import sys
from pathlib import Path

import bmesh
import bpy

HERE = Path(__file__).resolve().parent
sys.path.insert(0, str(HERE))
import vertex_ao_extra as X  # noqa: E402  (its main() runs only as a script: guarded below)

REPO = HERE.parents[1]
ARGS = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
ROOT = REPO / (ARGS[ARGS.index("--root") + 1] if "--root" in ARGS else "out/decokit-097-extra")
BED, NAME = "CivicBedPlanting_office-west", "ShrubMound"
SIZE = json.loads((HERE / "collide097.json").read_text())["pieces"]["ShrubMound_TB"]   # size [x, h, z] and volumes of the old ShrubMound
HW, HD, H = SIZE["size"][0] / 2, SIZE["size"][2] / 2, SIZE["size"][1]


def cut(objs):
    """Keep the faces whose centre lies in the patch window, then set the patch on the ground and squash it to the old height."""
    zs = []
    for o in objs:
        bm = bmesh.new()
        bm.from_mesh(o.data)
        m = 0.12   # a card may lean this far past the window; a face reaching further (the bed's long soil sheet) goes
        drop = [f for f in bm.faces if not (abs(f.calc_center_median().x) <= HW and abs(f.calc_center_median().y) <= HD)
                or any(abs(v.co.x) > HW + m or abs(v.co.y) > HD + m for v in f.verts)]
        bmesh.ops.delete(bm, geom=drop, context="FACES")
        bm.to_mesh(o.data)
        bm.free()
        zs += [v.co.z for v in o.data.vertices]
    if not zs:
        return
    z0, z1 = min(zs), max(zs)
    k = H / max(z1 - z0, 1e-6)
    for o in objs:
        for v in o.data.vertices:
            v.co.z = (v.co.z - z0) * k


def main():
    man_path = ROOT / "decokit_manifest.json"
    man = json.loads(man_path.read_text())
    bed = next(r for r in man["assets"] if r["name"] == BED)
    levels = bed["lods"]   # from the bed's "close" level down: its inspection level (L0) would make a 0.95 m cushion 1.7x the old piece's triangles
    files, lods = [], []
    for k, lv in enumerate(levels):
        objs = X.load(ROOT / lv["file"])
        for o in objs:
            o.name = o.name.replace(f"DK_{BED}", f"DK_{NAME}")
        cut(objs)
        objs = [o for o in objs if len(o.data.polygons)]
        rel = f"Models/Plaza/DK_{NAME}" + (f"_L{k}" if k else "") + ".fbx"
        info = X.export(objs, ROOT / rel)
        tris = X.tris(objs)
        if k == 0:
            files.append(dict(file=rel, tris=tris, **info, objects=[dict(name=o.name, materials=[m.name for m in o.data.materials],
                                                                         shadows="off" if o.name.endswith("_soil") else "on") for o in objs]))
        else:
            lods.append(dict(file=rel, tris=tris, screen=lv["screen"], **({"level": lv["level"]} if "level" in lv else {}), **info))
        print(f"[DK-SHRUB] level {k}: {tris} tris -> {rel}")
    row = dict(name=NAME, category="Plaza", source="generated", status="candidate", fbx=files[0]["file"], tris=files[0]["tris"], size=SIZE["size"],
               bounds=[-HW, HW, -HD, HD], materials=sorted({m for f in files for o in f["objects"] for m in o["materials"]}), files=files, lods=lods,
               lod0_level=bed["lods"][0].get("level", "close"), lodSize=bed.get("lodSize", 4), lodRule=bed.get("lodRule"), collide=SIZE["volumes"],
               collide_note="declared: ShrubMound's volumes before brief 097 (tools/decokit/collide097.json), so the engine's walk and sight inputs do not move",
               replaces={"id": "ShrubMound"},
               provenance=dict(brief="097 final pass", generator="tools/decokit/shrub097_extra.py", cut_from=BED,
                               sha256=hashlib.sha256((HERE / "shrub097_extra.py").read_bytes()).hexdigest()))
    man["assets"] = [r for r in man["assets"] if r["name"] != NAME] + [row]
    man_path.write_text(json.dumps(man, indent=1))
    print(f"[DK-SHRUB] {NAME} -> {ROOT}")


main()
