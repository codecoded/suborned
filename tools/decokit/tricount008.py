"""Triangles per material of registered pieces (evaluated, as exported). Blender -b -P tricount008.py -- --ids A,B
With --block: the block's new LOD0 sum against the 110k ceiling, from Vantage's placements (contracts/world/civic-block.adjust.json
`dressing`, read only), the café tables of civic-block.json and the inlay pieces of out/decokit-008/paving-bands.json."""
import json
import sys
from collections import Counter
from pathlib import Path

import bpy

sys.path.insert(0, str(Path(__file__).parent))
import kit  # noqa: E402

REPO = Path(__file__).resolve().parents[2]
CEILING = 110_000
ARGS = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
reg = kit.load()


def count(n):
    obj = kit.build(n)
    dg = bpy.context.evaluated_depsgraph_get()
    per, tot = {}, 0
    for o in [obj, *obj.children_recursive]:
        me = bpy.data.meshes.new_from_object(o.evaluated_get(dg), depsgraph=dg)
        for p in me.polygons:
            m = me.materials[p.material_index].name if me.materials else "?"
            per[m] = per.get(m, 0) + len(p.vertices) - 2
            tot += len(p.vertices) - 2
        bpy.data.meshes.remove(me)
    for o in [obj, *obj.children_recursive]:
        bpy.data.objects.remove(o, do_unlink=True)
    return tot, per


def block_counts():
    d = json.loads((REPO / "contracts/world/civic-block.adjust.json").read_text())["dressing"]
    blk = json.loads((REPO / "contracts/world/civic-block.json").read_text())
    bands = json.loads((REPO / "out/decokit-008/paving-bands.json").read_text())
    c = Counter()
    for p in d["pieces"]:
        c[p["piece"]] += 1
    c["Parasol_Stripe"] += len(d["parasols"]["on"])
    c["CafeSet_Tulip_Play"] += sum(1 for o in blk["objects"] if o.get("kind") == "table")
    c["PlazaSphereLamp_Play"] += len(d["lamps"]["at"])
    c["BollardSphere"] += len(d["bollards"]["at"])
    for r in d["planterRuns"]["runs"]:
        for m in r["modules"]:
            c[m["piece"]] += 1
    if any(s["id"] == "square-boarding-pavilion" for s in blk["structures"]):
        c["BoardingPavilion_Square_v2"] += 1
    for p in bands["pieces"]:
        c[p["piece"]] += 1
    return c


if "--block" in ARGS:
    c = block_counts()
    sums, rows = 0, []
    for n in sorted(c):
        t, _ = count(n)
        sums += t * c[n]
        rows.append((n, c[n], t, t * c[n]))
        print(f"[BLOCK] {n}: {c[n]} x {t} = {t * c[n]}")
    print(f"[BLOCK] new LOD0 placed by the dressing: {sums} of {CEILING} ({100 * sums / CEILING:.1f}%), headroom {CEILING - sums}")
    for n, k in (("Cypress_Play", 6), ("PottedTopiary_Play", 6)):
        t, _ = count(n)
        print(f"[BLOCK] not placed yet: {k} x {n} = {k * t} (sum would be {sums + k * t})")
else:
    ids = ARGS[ARGS.index("--ids") + 1].split(",") if "--ids" in ARGS else list(reg)
    for n in ids:
        tot, per = count(n)
        print(f"[TRI] {n}: {tot} | " + ", ".join(f"{k.replace('DK_', '')}={v}" for k, v in sorted(per.items(), key=lambda kv: -kv[1])))
