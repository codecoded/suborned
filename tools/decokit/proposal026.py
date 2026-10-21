#!/usr/bin/env python3
"""Brief 020 B and C delivery data (no Blender): reads out/decokit-026 (the generator's manifest and materials) and contracts/world/kit.json and writes
  out/decokit-026/catalogue.proposal.json   KitAsset rows (contracts/world/kit.schema.ts) for CafeBay_v2 and CafeInterior (B), Sedan_Official, Sedan_Getaway, the _Play slim copies and SkyRailStation_Blank (C), status candidate, every file with its sha256
and, with --write-kit, adds the two rows to contracts/world/kit.json (nothing else in it changes). Materials the kit already has keep the file rows they have there;
the new ones (DK_Cafe_*, DK_Glass_Clear, DK_Glass_Sheen), their textures and the pieces' models and LODs are `root: generated` under out/decokit-026 (adopting = the same
relative paths under out/decokit, which `bun run decokit --only CafeBay_v2,CafeInterior` writes).
Run: python3 -I tools/decokit/proposal020.py [--write-kit]"""

import hashlib
import json
import math
import subprocess
import sys
from pathlib import Path

REPO = Path(__file__).resolve().parents[2]
OUT = REPO / "out/decokit-026"
KIT = REPO / "contracts/world/kit.json"
MAN = {a["name"]: a for a in json.loads((OUT / "decokit_manifest.json").read_text())["assets"]}
MATS = {m["name"]: m for m in json.loads((OUT / "decokit_materials.json").read_text())["materials"]}
sha = lambda b: hashlib.sha256(b).hexdigest()  # noqa: E731
r3 = lambda v: round(v + 0.0, 3)  # noqa: E731

SPEC = {}
_LAMP = "Brief 026 (Glint's night plan, briefs/026/glint/cafe-lighting.md): "
for _h in ():
    SPEC[f"TableLamp_{_h}"] = dict(use=["lamp:table"], sight=None, budget=400, order=0, script="lamps026.py", note=_LAMP + f"atom-age table lamp, 0.29 m, brass foot, chrome collar, opal diffuser DK_Lamp_Table_{_h} (emissive 2.2 at night, 0 by day; the pool light of the same hue is Latch's). Pivot: foot centre on the table top.")
SPEC["AwningLamp"] = dict(use=["lamp:wall"], sight=None, budget=300, order=1, script="lamps026.py", note=_LAMP + "terrace wall / awning under-lamp: chrome plate, opal strip DK_Lamp_Wall (emissive 4), projecting chrome eyelid. Pivot: wall face centre; projects toward -Y.")
SPEC["PalmUplight"] = dict(use=["lamp:uplight"], sight=None, budget=300, order=2, script="lamps026.py", note=_LAMP + "palm uplight can on a 25 degree tilt (leans toward +Y), recessed lens DK_Lamp_Uplight (emissive 2.3). Pivot: ground, base disc centre.")
SPEC["StreetLampGlobe"] = dict(use=["lamp:globe"], sight=None, budget=500, order=3, script="lamps026.py", note=_LAMP + "0.46 m opal globe (DK_Lamp_Globe, emissive 4) in a chrome collar for the street lamps' DK_Lamp heads, which are only 0.2 m. Pivot: under the neck on the head axis.")
_R = "Brief 026 (testbed road, briefs/026/vela/testbed-vs-now.html): "
for _n, _w in (("RoadRibbon_Straight_6m", "a continuous warm lens 0.12 m wide set flush beside the gutter between brass hairlines, with a light pool on the carriageway side"), ("RoadRibbon_Arc90_R4", "the same round a 90 degree corner, 4 m to the lens centre"), ("RoadRibbon_Arc90_R8", "the same round a 90 degree corner, 8 m to the lens centre")):
    SPEC[_n] = dict(use=["ground:road-ribbon"], sight=None, budget=200, order=4, script="road_ribbon.py", note=_R + _w + ". Lies along +X, carriageway on +Y (yaw 180 for the far kerb); lens DK_Road_Lens emissive, pool DK_Road_Pool (glow quad, shader fade, UV v 0..1 across); lit by the lighting rig (off by day).")
SPEC["RoadMark_Lozenge"] = dict(use=["ground:road-lozenge"], sight=None, budget=20, order=5, script="road_ribbon.py", note=_R + "the pearl diamond (1.8 x 0.54 m, brass hairline) that replaces the dashed lane line; no paint, no zebra. Along +X.")
SPEC["RoadMark_Seam"] = dict(use=["ground:road-seam"], sight=None, budget=20, order=6, script="road_ribbon.py", note=_R + "a chrome joint 4.6 m x 0.03 m across the enamel carriageway, every 9 m. Along +Y.")
SPEC["CoconutPalm_Street"] = dict(use=["tree"], sight=None, budget=5500, order=8, script="palms026.py", note="Brief 026 (Sky: street trees are palms in the old world, not topiary): the old city's street palm, CoconutPalm cut for the plan's avenue trees: 8.6 m ringed leaning trunk, 14 feather fronds, 10.3 m overall, spread 5.3 m, 4.8k tris (L1 1.2k, L2 0.3k). Slot `tree`: phoenix (top 11 m, spread 8) at x1.0, washingtonia (top 16 m) scaled x1.5; replaces Cypress and PottedTopiary_Play as the tree-pit tree. Pivot: trunk foot; leans toward +X, turn by the seed yaw.")
_F = "Brief 026 (testbed flooring, briefs/026/vela/testbed-vs-now.html): "
SPEC["Pave_Slab"] = dict(use=["ground:slab"], sight=None, budget=10, order=9, script="pave026.py", note=_F + "2 x 2 m flat module of the testbed's honed ivory limestone (DK_Ground_PaveLimestone, 1 m slabs, 6 mm joints, 2 m tile), z 0.15: drop-in for Pave_Flags_i (same size, pivot, height); one piece, the sheet is periodic. Replaces the tan-and-grey running-bond flags that read as brick.")
for _i in (0, 1):
    SPEC[f"Kerb_Pearl_{_i}"] = dict(use=["ground:kerb"], sight=None, budget=400, order=9, script="pave026.py", note=_F + "2 m kerb run in pearl marble (DK_Ground_RoadPearl), 0.3 m deep, 0.154 high, a brass hairline on the road-side crown edge; pivot and axes as Kerb_Granite_i (stone on +Y, road on -Y) but with no gutter: the road enamel meets the kerb face and the light ribbon sits 0.1 m out. The _1 split is at 0.5 m offsets, as the granite's lots alternate.")
SPEC["TreePit_Pearl"] = dict(use=["ground:tree-pit"], sight=None, budget=300, order=9, script="pave026.py", note=_F + "2 x 2 m limestone module with a 1.2 m pit, a 0.12 m pearl surround with a brass hairline round the inside, dark soil (DK_Soil); the pit CoconutPalm_Street stands in. Replaces TreePit_Flags_i (bronze grille, granite surround).")
SPEC["PaveInlay_Band_6m"] = dict(use=["inlay:tile-band"], sight=None, budget=40, order=9, script="pave026.py", note=_F + "6 x 0.82 m band of glazed teal tile (DK_Ground_TileTeal: 0.72 m pitch, 14 mm grout, bevelled, per-tile shade) with a brass hairline each side, z 0.154; the testbed's approach bands. Replaces the flat seafoam InlayBand pieces where a tile inlay is wanted.")
SPEC["PaveInlay_Ring_Q"] = dict(use=["inlay:tile-ring"], sight=None, budget=120, order=9, script="pave026.py", note=_F + "a quarter of the testbed's 0.52 m glazed tile ring (centre line radius 4.62 m, 90 degrees), pivot the ring's centre, from +X to +Y; four make the fountain ring. Other radii need another piece (the tile pitch is fixed).")
_T = "Brief 026 (testbed café, Sky: 'our testbed cafe parasols were better and the interior of the cafe'), ported from the testbed's own glb (tools/decokit/art/cafe3d/PROVENANCE.json, sha256 of sources and baked parts): "
SPEC["CafeTable_Testbed"] = dict(use=["cafe-table:testbed"], sight=None, budget=900, order=10, script="cafe_testbed.py", note=_T + "the round café table, 758 tris, brass pedestal, espresso cup and vase in the mesh; atlas DK_Cafe_TB_Atlas. Pivot: foot centre.")
SPEC["CafeChair_Testbed"] = dict(use=["cafe-chair:testbed"], sight=None, budget=700, order=10, script="cafe_testbed.py", note=_T + "the bistro chair, 520 tris, splayed legs and cushioned bow back (cloth sheen map dropped: URP Lit has none). Pivot: chair centre; turn to a - 90 degrees to face the table as the testbed did.")
SPEC["Parasol_Testbed"] = dict(use=["parasol:testbed"], sight="open", budget=3400, order=10, script="cafe_testbed.py", note=_T + "the red-and-cream scalloped eight-panel parasol, 3008 tris (Parasol_Stripe is 724), 2.56 m across, 2.8 m high, canvas weave in the atlas normal. Pivot: mast foot = table centre. Place over CafeSet_Testbed; canopy clears 2.3 m.")
for _h in ("Amber", "Teal", "Peach", "Coral", "Lilac"):
    SPEC[f"CafeLamp_Testbed_{_h}"] = dict(use=["lamp:table-testbed"], sight=None, budget=900, order=10, script="cafe_testbed.py", note=_T + f"the atom-age table lamp in its {_h} hue: the testbed's brass body (600 tris) and its own diffuser profile (cafe-table-lamps.js, 16 sides), diffuser DK_Lamp_Table_{_h} emissive 2.2 at night (day scalar 0), 760 tris. Pivot: foot centre on the tabletop; Latch's night light takes the diffuser's hue. Replaces TableLamp_{_h} (retired).")
SPEC["CafeSet_Testbed"] = dict(use=["cafe-set:testbed"], sight=None, budget=3600, order=10, script="cafe_testbed.py", note=_T + "one table set as the testbed laid its second one (layouts[1]): table turned -0.48 rad, four chairs 0.9 to 1.0 m out at 0.40, 1.82, 3.54, 4.95 rad, the lamp on the clear corner. 3.6k tris. Without the parasol (Parasol_Testbed is placed apart) and without the lamp (CafeLamp_Testbed_<hue> at (0.116, -0.320, 0.797) from the pivot, hue per table). 3.1k tris. Replaces CafeSet_Tulip_Play on the terrace.")
SPEC["CafeInterior_Testbed"] = dict(use=["frontage:cafe-interior:testbed"], sight="glazed", budget=19500, order=10, script="cafe_testbed.py", note=_T + "CafeInterior's own shell (C-0 unchanged: same floor, ceiling, cove, side walls with windows, back wall) with the testbed's room contents instead of the generated ones: painted counter front with walnut cabinet, register and espresso gear, back shelves, two facing petrol-velvet booths, a three-table lounge with potted plants, eight opal pendants (rods carried to our 4.6 m ceiling), amber cove and teal mood emitters, two table sets by the glass. Rigid offset dy +5.29, dz -0.95. 18.7k tris (L1 4.6k, L2 1.1k): heavier than CafeInterior (6.5k); placed once. Companion of CafeBay_v2, same pivot.")
SPEC["CafeFascia"] = dict(use=["sign:cafe-fascia"], sight=None, budget=900, order=7, script="cafe_fascia.py", note=_R + "the testbed café's sign: teal plate 4.45 x 0.86 m in a chrome bead, ivory channel letters CAFE with an acute, three coral wings a side (DK_Sign_Ivory 3.2, DK_Sign_Coral 2 at night). CafeBay_v2's frame (pivot (-10.7, -29), yaw 90); z 4.86 to 5.72, centred on the door, 0.14 m proud of the wall. The blade sign stays.")


def files_of(a, name, existing):
    """The row's files: models and LODs, the new materials and their textures from out/decokit-026; shared materials as the kit has them."""
    files = []

    def gen(role, rel):
        p = OUT / rel
        files.append({"root": "generated", "role": role, "path": rel, "sha256": sha(p.read_bytes()), "bytes": p.stat().st_size})
    gen("fbx", a["fbx"])
    for m in a["materials"]:
        if m == "DK_Road_Pool":  # a `glow` volume: no .mat is written, Latch draws it additive (mats.KIND)
            continue
        for sfx, role in ((".mat", "material"), (".mat.meta", "material-meta")):
            rel = f"Materials/{m}{sfx}"
            if m.startswith(("DK_Cafe_TB_", "DK_Cafe_Mood", "DK_Ground_PaveLimestone", "DK_Ground_TileTeal", "DK_Lamp_Table_", "DK_Lamp_Wall", "DK_Lamp_Uplight", "DK_Lamp_Globe", "DK_Road_", "DK_Ground_Road", "DK_Sign_")):
                gen(role, rel)
                if sfx == ".mat" and MATS[m]["texture"]:
                    gen("texture", f"Textures/{MATS[m]['texture']}")
                    gen("texture-meta", f"Textures/{MATS[m]['texture']}.meta")
            else:
                hit = next((f for x in existing for f in x["files"] if f["path"] == rel), None)
                if hit is None:
                    raise SystemExit(f"{rel}: no file row for it in kit.json")
                files.append(hit)
    for l in a.get("lods", []):
        gen("lod", l["file"])
    return files


def row(name, existing):
    a, spec = MAN[name], SPEC[name]
    b = a["bounds"]
    c = [r3((b[0] + b[1]) / 2), r3((b[2] + b[3]) / 2)]
    piv = {"pivot": "origin-ground", "pivotOffset": c} if math.hypot(*c) > 0.05 else {"pivot": "footprint-centre-ground"}
    lc, scr = {"Skyport": ("signatures", (0.15, 0.05)), "Plants": ("trees", (0.08, 0.025))}.get(a["category"], ("street", (0.008, 0.003)))  # tools/kit/lod.ts BY_CATEGORY
    base = next((x for x in existing if x["id"] == spec.get("base")), None)
    tags = dict(base["tags"]) if base else {"use": spec["use"], "scale": [1, 1], "squash": 1, "wave": 2, **({"sight": spec["sight"]} if spec.get("sight") else {})}
    return {"id": name, "category": a["category"], "size": a["size"], **piv, "front": "+Z", "tris": a["tris"], "materials": a["materials"],
            "lods": [{"file": l["file"], "tris": l["tris"], "screen": s} for l, s in zip(a.get("lods", []), scr)], "lodCategory": lc,
            "sourceScript": "tools/decokit/" + spec["script"], "files": files_of(a, name, existing),  # key order as tools/kit/catalogue.ts rebuilds a generated row (kit.test.ts)
            "tags": tags, "status": "candidate", "source": "generated"}


def main():
    kit = json.loads(KIT.read_text())
    rows = [row(n, kit["assets"]) for n in sorted(SPEC, key=lambda n: SPEC[n]["order"]) if n in MAN]
    art = json.loads((REPO / "out/decokit-ground/road.json").read_text())
    (OUT / "catalogue.proposal.json").write_text(json.dumps(dict(
        schema="suborned.kit/1 (proposal: KitAsset rows, status candidate)", brief="026 Vela", generator="tools/decokit (Blender 5.2.2), --out out/decokit-026",
        roots={"generated": "out/decokit-026 (this proposal's generator output; adopting = same relative paths under out/decokit)"},
        notes={n: SPEC[n]["note"] for n in SPEC}, budgets={n: {"tris": SPEC[n]["budget"], "built": MAN[n]["tris"], "ok": MAN[n]["tris"] <= SPEC[n]["budget"]} for n in SPEC if SPEC[n]["budget"]},
        provenance=art, assets=rows), indent=1) + "\n")
    for r in rows:
        print(f"{r['id']}: {r['tris']} tris (budget {SPEC[r['id']]['budget']}), L1 {r['lods'][0]['tris'] if r['lods'] else '-'}, size {r['size']}, pivot {r['pivot']}{r.get('pivotOffset', '')}, {len(r['files'])} files")
    if "--write-kit" in sys.argv:
        merge = f"""const p='{KIT}',k=JSON.parse(await Bun.file(p).text()),rows=JSON.parse(await Bun.file('{OUT}/catalogue.proposal.json').text()).assets;
for(const r of rows){{const i=k.assets.findIndex(a=>a.id===r.id);if(i>=0){{const o=k.assets[i],d=o.files.filter(f=>f.root==='generated'&&r.files.some(g=>g.path===f.path&&g.sha256!==f.sha256));if(d.length)console.log('STALE '+r.id+' (adopted bytes differ from a fresh build; re-adopt): '+d.map(f=>f.path.split('/').pop()).join(', '));}}else{{let j=k.assets.length;while(j>0&&k.assets[j-1].source==='generated'&&k.assets[j-1].id.localeCompare(r.id)>0)j--;k.assets.splice(j,0,r);}}}}
await Bun.write(p,JSON.stringify(k,null,1)+'\\n');console.log('kit.json: '+rows.map(r=>r.id).join(', ')+' as new candidate rows (a row already in kit.json is left alone: STALE lines name the adopted pieces whose out/decokit bytes differ from a fresh build)');"""
        subprocess.run(["bun", "-e", merge], check=True)


if __name__ == "__main__":
    main()
