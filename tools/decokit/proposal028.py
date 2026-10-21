#!/usr/bin/env python3
"""Brief 028 candidates (Vela, no Blender): reads out/decokit-028 (bun tools/decokit/run.ts --only <ids> --out out/decokit-028) and contracts/world/kit.json and writes
  out/decokit-028/catalogue.proposal.json   KitAsset rows (contracts/world/kit.schema.ts), status candidate, every file with its sha256 (root generated under out/decokit-028)
and, with --write-kit, adds the rows that kit.json does not have (a row already there is left alone; STALE names changed bytes). Same shape as proposal026.py.
Run: python3 -I tools/decokit/proposal028.py [--write-kit]"""
import hashlib
import json
import math
import subprocess
import sys
from pathlib import Path

REPO = Path(__file__).resolve().parents[2]
OUT = REPO / "out/decokit-028"
KIT = REPO / "contracts/world/kit.json"
MAN = {a["name"]: a for a in json.loads((OUT / "decokit_manifest.json").read_text())["assets"]}
MATS = {m["name"]: m for m in json.loads((OUT / "decokit_materials.json").read_text())["materials"]}
sha = lambda b: hashlib.sha256(b).hexdigest()  # noqa: E731
r3 = lambda v: round(v + 0.0, 3)  # noqa: E731
GEN = ("DK_Pavilion_", "DK_Fountain_")

_P = ("Brief 028 (testbed port, Sky: \"the car park stand / bay isn't as good as testbed\"): the testbed's boarding pavilion B at full detail, nothing decimated, re-baked or downscaled "
      "(28,190 source triangles + the four recessed downlight fittings, 576; 4096 px base and normal, 2048 px metal-rough; no LODs: Sky reviews compression). Provenance art/pavilion/PROVENANCE.json. ")
SPEC = {
    "BoardingPavilion_TB_Square": dict(use=["shelter:square:testbed"], sight="open", budget=28800, order=0, script="pavilion.py", note=_P +
        "Scale 0.70 of the 12 m canopy: 8.4 x 2.14 x 2.59 m, long along X, front (open side to the kerb and the car) toward +Z; the testbed's getaway shelter = the block's square bay (structure square-boarding-pavilion). "
        "Has its own four supports (two at +-3.22): drop PavilionPier_Square and BoardingPavilion_Square_v2 where it is placed. Pivot footprint centre."),
    "BoardingPavilion_TB_Office": dict(use=["shelter:office:testbed"], sight="open", budget=28800, order=1, script="pavilion.py", note=_P +
        "Scale 1.25: 15.0 x 3.83 x 4.62 m, turned 90 degrees so it runs along Y like BoardingPavilion_Office (its front, the open side, toward +X); the testbed's official-bay shelter. "
        "Own supports (two at +-5.75 along Y): drop PavilionPier_Office and BoardingPavilion_Office where it is placed. Pivot footprint centre."),
}
SPEC["AtomFountain_Testbed"] = dict(use=["plaza:fountain:testbed"], sight="open", budget=41900, order=2, script="fountain_tb.py", note="Brief 028 (testbed port, Sky: \"and the fountain from testbed....?\"): the testbed's atom fountain at its finest level, nothing decimated or downscaled: "
    "basin = the testbed's own geometry baked from fountain-geometry.js (21,888 tris: teal plinth and fluted drum, 32 carved marble coping blocks, champagne beads, waterline light ring, 12 jets with nozzles and landing ripples, pool water with its meniscus) + "
    "the Blender sculpture from source-art/CivicFountain/atom-sculpture-v1.blend (20,000: silver globe and trumpet pedestal, three gold orbits). Ambient marble (CC0), ceramic, polish maps at the testbed's resolution. 6.6 m across, 4.08 m high, pivot the pool centre; "
    "swap onto civic/atom-fountain (circle r 3.3, centre (6.5, -30)) in place of AtomFountain, mode fit (diameter 6.6 as now). Movable parts: DK_AtomFountain_Testbed_Orbit0..2 turn about the vertical axis through the globe (2.33 m) at the testbed's rates "
    "(art/fountain/PROVENANCE.json). Not ported here (Glint/Latch): the animated water shader (ripples, caustics, jets flow), clearcoat, night lights. Gold is the testbed's champagne; the street rule is brass: Sky decides. The basin's rim is 0.52 m: the planter rings sit outside r 3.3 as before.")
SPEC["WashingtoniaPalm_Street"] = dict(use=["tree"], scale=[0.9, 1.1], sight=None, budget=19300, order=3, script="palms_avenue.py", note="Brief 028 (Splice, 20 Oct: the 803 avenue trees fell back to procedural because CoconutPalm_Street's 5.25 m crown does not fit the 4 m slot): the avenue palm, a slender Washingtonia for streetscape.trees.species.washingtonia (top 16 m, spread 4 m): 15.6 m tall, crown 3.58 x 3.39 m (built bounds), straight 13.9 m ringed shaft 0.16 to 0.25 m, shaggy skirt of dead leaf-bases, a dense head of 82 fan fronds. Full detail, no LODs (nolod). Pivot: trunk foot; the head leans +0.12 m toward +X (turn by the seed yaw). Fits the slot at scale 1.02.")
SPEC["RecordsEntrance_Official_Open"] = dict(use=["entrance:records-official:open"], sight="glazed", budget=2000, order=4, script="records_open.py", note="Brief 028: RecordsEntrance_Official without its 3 x 6 m marquee (civic_records.py's own doors, pilasters and banners, same numbers), for the place where BoardingPavilion_TB_Office covers the official bay. Same pivot and yaw as RecordsEntrance_Official. Only if Waymark and Splice take the shelter placement of briefs/028/office.")
SPEC.update(json.loads((Path(__file__).parent / "art/proposal028.extra.json").read_text()) if (Path(__file__).parent / "art/proposal028.extra.json").exists() else {})


def files_of(a, existing):
    files = []

    def gen(role, rel):
        p = OUT / rel
        files.append({"root": "generated", "role": role, "path": rel, "sha256": sha(p.read_bytes()), "bytes": p.stat().st_size})
    gen("fbx", a["fbx"])
    for m in a["materials"]:
        for sfx, role in ((".mat", "material"), (".mat.meta", "material-meta")):
            rel = f"Materials/{m}{sfx}"
            if m.startswith(GEN):
                gen(role, rel)
                if sfx == ".mat":
                    row = MATS[m]
                    names = [row["pbr"][k] for k in ("albedo", "normal", "mask") if row.get("pbr", {}).get(k)] if row.get("pbr") else ([row["texture"]] if row["texture"] else [])
                    for t in names:
                        gen("texture", f"Textures/{t}")
                        gen("texture-meta", f"Textures/{t}.meta")
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
    return {"id": name, "category": a["category"], "size": a["size"], **piv, "front": "+Z", "tris": a["tris"], "materials": a["materials"],
            "lods": [{"file": l["file"], "tris": l["tris"], "screen": sc} for l, sc in zip(a.get("lods", []), (0.008, 0.003))], "lodCategory": {"Plants": "trees", "Skyport": "signatures"}.get(a["category"], "street"), "sourceScript": "tools/decokit/" + spec["script"], "files": files_of(a, existing),
            "tags": {"use": spec["use"], "scale": spec.get("scale", [1, 1]), "squash": 1, "wave": 2, **({"sight": spec["sight"]} if spec.get("sight") else {})}, "status": "candidate", "source": "generated"}


def main():
    kit = json.loads(KIT.read_text())
    rows = [row(n, kit["assets"]) for n in sorted(SPEC, key=lambda n: SPEC[n]["order"]) if n in MAN]
    (OUT / "catalogue.proposal.json").write_text(json.dumps(dict(
        schema="suborned.kit/1 (proposal: KitAsset rows, status candidate)", brief="028 Vela", generator="tools/decokit (Blender 5.2.2), --out out/decokit-028",
        roots={"generated": "out/decokit-028 (this proposal's generator output; adopting = same relative paths under out/decokit)"},
        notes={n: SPEC[n]["note"] for n in SPEC if n in MAN}, assets=rows), indent=1) + "\n")
    for r in rows:
        print(f"{r['id']}: {r['tris']} tris, size {r['size']}, pivot {r['pivot']}, {len(r['files'])} files")
    if "--write-kit" in sys.argv:
        merge = f"""const p='{KIT}',k=JSON.parse(await Bun.file(p).text()),rows=JSON.parse(await Bun.file('{OUT}/catalogue.proposal.json').text()).assets;
for(const r of rows){{const i=k.assets.findIndex(a=>a.id===r.id);if(i>=0){{const o=k.assets[i],d=o.files.filter(f=>f.root==='generated'&&r.files.some(g=>g.path===f.path&&g.sha256!==f.sha256));if(d.length)console.log('STALE '+r.id+': '+d.map(f=>f.path.split('/').pop()).join(', '));}}else{{let j=k.assets.length;while(j>0&&k.assets[j-1].source==='generated'&&k.assets[j-1].id.localeCompare(r.id)>0)j--;k.assets.splice(j,0,r);}}}}
await Bun.write(p,JSON.stringify(k,null,1)+'\\n');console.log('kit.json: '+rows.map(r=>r.id).join(', '));"""
        subprocess.run(["bun", "-e", merge], check=True)


if __name__ == "__main__":
    main()
