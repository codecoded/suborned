#!/usr/bin/env python3
"""Brief 075 civic restyle (Latch): before | after regression from Restyle075 manifests.
usage: python3 -I compare.py <manifest dir> <out json> [<swap dump dir>]
The swap dump dir (Restyle075 -restyleDump on the two swap scenes: every zone triangle of the restyle's materials with its renderer state) lets
the swap pair say what its material-blind zone hash differs by: the old builders' cutaway tag (DecoKitCutawayTags, rendering-layer bit 20)
is set per batch from its material's shader and its height, so a triangle that changes batch can gain or lose it.
Pairs: ours-before -> ours-after (our pipeline, probe scenes from the pinned export), ours-after -> harbour-after (HarbourDistrict.unity must
equal the probe it shares the export and the code with), swap-before -> swap-after (the old-built town's Handover zone).
Verdict "materials only" = outside the zone every hash equal; in the zone the batched geometry equal without its material, the kit renderers,
transforms and colliders equal; the only additions the restyle's own overlays (collider-free)."""
import json
import sys
from pathlib import Path

d, out = Path(sys.argv[1]), Path(sys.argv[2])


def load(n):
    return json.loads((d / f"{n}.json").read_text())


def strip_mat(k):
    return k.rsplit("|", 1)[0]


def pair(a_name, b_name):
    a, b = load(a_name), load(b_name)
    r = {"before": a["scene"], "after": b["scene"], "playNav": {"before": a["playNav"], "after": b["playNav"], "equal": a["playNav"] == b["playNav"]}}
    o_a, o_b = a["outside"], b["outside"]
    keys = ["batchTriangles", "batchGeometryHash", "batchGeometryWithMaterialHash", "kitRenderers", "kitHash", "kitWithMaterialHash", "transforms", "transformHash", "colliders", "colliderHash"]
    r["outsideZone"] = {k: o_a[k] == o_b[k] for k in keys}
    r["outsideZoneUnchanged"] = all(r["outsideZone"].values()) and not o_a["overlay"] and not o_b["overlay"]
    z_a, z_b = a["inZone"], b["inZone"]
    z = {}
    z["batchTriangles"] = [z_a["batchTriangles"], z_b["batchTriangles"]]
    z["batchAreaM2"] = [z_a["batchAreaM2"], z_b["batchAreaM2"]]
    z["batchGeometryEqualIgnoringMaterial"] = z_a["batchGeometryHash"] == z_b["batchGeometryHash"]
    z["batchGeometryEqualWithMaterial"] = z_a["batchGeometryWithMaterialHash"] == z_b["batchGeometryWithMaterialHash"]
    z["batchRenderers"] = [z_a["batchRenderers"], z_b["batchRenderers"]]
    mats = sorted(set(z_a["batchByMaterial"]) | set(z_b["batchByMaterial"]))
    z["batchMaterialMoves"] = {m: [z_a["batchByMaterial"].get(m, {}).get("triangles", 0), z_b["batchByMaterial"].get(m, {}).get("triangles", 0)]
                               for m in mats if z_a["batchByMaterial"].get(m, {}).get("hash") != z_b["batchByMaterial"].get(m, {}).get("hash")}
    z["kitRenderers"] = [z_a["kitRenderers"], z_b["kitRenderers"]]
    z["kitEqualIgnoringMaterial"] = z_a["kitHash"] == z_b["kitHash"]
    ka = {}
    for k in z_a["kit"]:
        ka.setdefault(strip_mat(k), []).append(k.rsplit("|", 1)[1])
    kb = {}
    for k in z_b["kit"]:
        kb.setdefault(strip_mat(k), []).append(k.rsplit("|", 1)[1])
    z["kitOnlyBefore"] = sorted(set(ka) - set(kb))[:20]
    z["kitOnlyAfter"] = sorted(set(kb) - set(ka))[:20]
    swaps = {}
    for k in set(ka) & set(kb):
        for m0, m1 in zip(sorted(ka[k]), sorted(kb[k])):
            if m0 == m1:
                continue
            s0, s1 = m0.split("+"), m1.split("+")
            diff = [f"{x}->{y}" for x, y in zip(s0, s1) if x != y] if len(s0) == len(s1) else [f"{m0}->{m1}"]
            piece = k.split("|")[1].rsplit(":", 1)[-1]
            for t in diff:
                swaps.setdefault(t, {}).setdefault(piece, 0)
                swaps[t][piece] += 1
    z["kitSlotSwaps"] = swaps
    z["kitRenderersWithASwap"] = sum(n for t in swaps.values() for n in t.values())
    z["transformsEqual"] = z_a["transformHash"] == z_b["transformHash"] and z_a["transforms"] == z_b["transforms"]
    z["transforms"] = [z_a["transforms"], z_b["transforms"]]
    z["collidersEqual"] = z_a["colliderHash"] == z_b["colliderHash"]
    z["colliders"] = [z_a["colliders"], z_b["colliders"]]
    z["overlayBefore"] = z_a["overlay"]
    z["overlayAfter"] = z_b["overlay"]
    z["overlayColliders"] = [x for x in z_b["overlay"] if x.startswith("COLLIDER")]
    r["inZone"] = z
    r["identical"] = (a["inZone"]["batchGeometryWithMaterialHash"] == b["inZone"]["batchGeometryWithMaterialHash"] and z_a["kitWithMaterialHash"] == z_b["kitWithMaterialHash"]
                      and z_a["overlay"] == z_b["overlay"] and r["outsideZoneUnchanged"])
    r["materialsOnly"] = r["identical"] or (r["outsideZoneUnchanged"] and z["batchGeometryEqualIgnoringMaterial"] and z["kitEqualIgnoringMaterial"]
                          and z["transformsEqual"] and z["collidersEqual"] and not z["overlayColliders"] and not z_a["overlay"] and r["playNav"]["equal"])
    return r


CUT = 1 << 20  # DecoKitCutawayTags.Layer


def cutaway(dump_dir):
    """Zone triangles of the restyle materials, before and after, with the cutaway bit taken off the state; and the tagged counts."""
    rows, tags = {}, {}
    for s in ("before", "after"):
        lines = [l for l in (dump_dir / f"swap-{s}.dump.txt").read_text().splitlines() if not l.startswith(("KIT ", "TF "))]
        keys = []
        tags[s] = {}
        for l in lines:
            f = l.split("|"); st = f[2].split("/"); mask = int(st[3])
            keys.append("|".join([f[0], f[1], "/".join(st[:3] + [str(mask & ~CUT)])]))
            k = ("cutaway-tagged " if mask & CUT else "untagged ") + f[3]
            tags[s][k] = tags[s].get(k, 0) + 1
        rows[s] = sorted(keys)
    from collections import Counter
    ca, cb = Counter(rows["before"]), Counter(rows["after"])
    return {"zoneTrianglesOfRestyleMaterials": [len(rows["before"]), len(rows["after"])],
            "differingWithTheCutawayTagIgnored": sum(((ca - cb) + (cb - ca)).values()), "byTag": tags}


res = {"schema": "latch.restyle075-regression/1", "pairs": {}}
for a_n, b_n in (("ours-before", "ours-after"), ("swap-before", "swap-after"), ("ours-after", "harbour-after")):
    if (d / f"{a_n}.json").exists() and (d / f"{b_n}.json").exists():
        res["pairs"][f"{a_n} -> {b_n}"] = pair(a_n, b_n)
if len(sys.argv) > 3 and "swap-before -> swap-after" in res["pairs"]:
    c = cutaway(Path(sys.argv[3]))
    p = res["pairs"]["swap-before -> swap-after"]
    p["inZone"]["oldCutawayTag"] = c
    p["materialsOnlyBesidesTheOldCutawayTag"] = (c["differingWithTheCutawayTagIgnored"] == 0 and p["outsideZoneUnchanged"] and p["inZone"]["kitEqualIgnoringMaterial"]
                                                and p["inZone"]["transformsEqual"] and p["inZone"]["collidersEqual"] and not p["inZone"]["overlayColliders"] and p["playNav"]["equal"])
out.write_text(json.dumps(res, indent=1))
for k, v in res["pairs"].items():
    z = v["inZone"]
    print(f"{k}: identical {v['identical']}; materials only {v['materialsOnly']}{'; besides the old cutaway tag ' + str(v['materialsOnlyBesidesTheOldCutawayTag']) + ' ' + json.dumps(z['oldCutawayTag']) if 'oldCutawayTag' in z else ''}; outside unchanged {v['outsideZoneUnchanged']}; zone geometry (no material) equal {z['batchGeometryEqualIgnoringMaterial']}, "
          f"kit (no material) equal {z['kitEqualIgnoringMaterial']} ({z['kitRenderers']}), transforms {z['transformsEqual']}, colliders {z['collidersEqual']} {z['colliders']}, "
          f"kit slot swaps {z['kitRenderersWithASwap']}, batch material moves {len(z['batchMaterialMoves'])}, overlay entries {len(z['overlayAfter'])}, nav equal {v['playNav']['equal']}")
