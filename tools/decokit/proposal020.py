#!/usr/bin/env python3
"""Brief 020 B and C delivery data (no Blender): reads out/decokit-020 (the generator's manifest and materials) and contracts/world/kit.json and writes
  out/decokit-020/catalogue.proposal.json   KitAsset rows (contracts/world/kit.schema.ts) for CafeBay_v2 and CafeInterior (B), Sedan_Official, Sedan_Getaway, the _Play slim copies and SkyRailStation_Blank (C), status candidate, every file with its sha256
and, with --write-kit, adds the two rows to contracts/world/kit.json (nothing else in it changes). Materials the kit already has keep the file rows they have there;
the new ones (DK_Cafe_*, DK_Glass_Clear, DK_Glass_Sheen), their textures and the pieces' models and LODs are `root: generated` under out/decokit-020 (adopting = the same
relative paths under out/decokit, which `bun run decokit --only CafeBay_v2,CafeInterior` writes).
Run: python3 -I tools/decokit/proposal020.py [--write-kit]"""

import hashlib
import json
import math
import subprocess
import sys
from pathlib import Path

REPO = Path(__file__).resolve().parents[2]
OUT = REPO / "out/decokit-020"
KIT = REPO / "contracts/world/kit.json"
MAN = {a["name"]: a for a in json.loads((OUT / "decokit_manifest.json").read_text())["assets"]}
MATS = {m["name"]: m for m in json.loads((OUT / "decokit_materials.json").read_text())["materials"]}
sha = lambda b: hashlib.sha256(b).hexdigest()  # noqa: E731
r3 = lambda v: round(v + 0.0, 3)  # noqa: E731

SPEC = {
    "CafeBay_v2": dict(use=["frontage:cafe-bay"], sight="glazed", budget=8000, order=0,
                       note="Replaces CafeBay (same pivot: centre of the flat front on the ring line, block (-10.7, -29), yaw 90; same footprint and the generator's mass from z 4.7). Clear glazing (DK_Glass_Clear, alpha 0.12) in thin chrome mullions with brass sill, transom and head lines and reflection streaks (DK_Glass_Sheen); glass door leaves; cream piers; a seafoam belt z 4.40 to 4.70 under the upper mass; the coral and ivory striped awning 1.7 m out (the 008 canopy reached 2.45 m and hid the room from the play camera), edge z 4.02, scalloped valance to 3.78; the blade sign as CafeBay. Must have its companions CafeInterior (the room) and CafeTerrace (the wall)."),
    "CafeInterior": dict(use=["frontage:cafe-interior"], sight="glazed", budget=9000, order=1,
                         note="Companion of CafeBay_v2 (same pivot and yaw: civic-look.json `companions`). The room behind the glass and the walls round it: floor 0.04 m over the pavement (DK_Cafe_Floor checker), ceiling at 4.6 with an amber cove, back wall (back-bar painting and botanical mural on flat quads) at y 10.9 to 11.3 with the kitchen door at x 3 (block (-22.4, -26)), two side walls (x +-10.6 to 11.0) each with a clear window y 6.5 to 10.1, cream stone with the seafoam base and belt outside; a window row of tables and chairs, a second row, the counter with stools, two petrol-velvet booths, opal pendants, potted plants. Everything inside is an emit material drawn lit (strength 0.45 to 0.85 DK_Cafe_*, pendants 5): it reads through the glass with no light inside. It is dressing seen through glass, never entered. Draws the ground storey's side and back walls, which no piece drew before."),
}


PLAY = {"RobotPorter": "stand-in for the response unit and the patrol robot (a tracked porter with a load: reads as a machine on a job; Latch keeps the unlit signal lamp)",
        "ServiceRobot": "the small wheeled rover: dock robots, vending robot, a second response-unit shape", "RobotWaiter": "city-life robot (café, shops)",
        "RobotButler": "the foreground civic robot (adopted at 16k triangles; this is its play-zoom copy)", "BoxTruck": "parked or passing service truck", "CoffeeCart": "the street food kiosk (kiosk-food)", "BusShelter": "tram-shelter (20 in the world)", "CurvedKiosk": "newsstand (15 in the world)", "TramCanopy": "the wide tram-stop canopy (tram-shelter, second shape)"}
SLIM = {"FanPalm": "tub-palm, a 4.2k piece placed 189 times: 20 to 24 leaf cards of 6 quads, a 4-band trunk, about 1k triangles", "Planter_Urn": "planter: the urn at 10 sides and 14 foliage cards, about 1k triangles"}
for _b, _why in PLAY.items():
    SPEC[_b + "_Play"] = dict(base=_b, budget=None, order=10, note=f"{_b} with the same builder and an L0 triangle cap (play-zoom slim copy, as CafeSet_Tulip_Play): same pivot, size and parts. {_why}.")
for _b, _why in SLIM.items():
    SPEC[_b + "_Play"] = dict(base=_b, budget=1400, order=11, note=f"{_b} rebuilt lean (street020.py, not a decimation: the leaf and foliage cards would tear): same pivot, size and materials, {_why}.")
SPEC["PneumaticPost_Play"] = dict(base="PneumaticPost", budget=1400, order=12, note="The tube-mail post cut to the plan's mailbox: 0.6 x 0.6 x 1.3 m (the post is 2.34 m with its chrome stack): red pillar with a rocket-nose cap, POST plaque, capsule hatch, amber beacon on top; no stack, no return pipe. Front -Y.")
SPEC["PhoneBooth_Single"] = dict(use=["phone-booth"], sight="glazed", budget=2000, order=13, note="One Googie capsule, 0.91 x 0.91 x 2.5 m (the plan's K6 kiosk 1.0 x 1.0 x 2.5, cap 2.7): coral drum, glazed cabin in chrome mullions, a seafoam half-drum hood and a lit lamp inside, ivory cap with a seafoam band, chrome starburst finial. Replaces the 2.58 m twin PhoneBooth where the plan has a 1.1 m slot (68 booths, two 2 m apart at tram stops). Opening faces -Y.")
SPEC["Newsstand"] = dict(use=["newsstand"], sight="glazed", budget=3500, order=14, note="3.0 x 1.5 x 2.8 m, the plan's newsstand (CurvedKiosk is 6.1 x 4.4 x 6.5 and is scaled 0.43): stadium-plan kiosk in the CurvedKiosk's ivory, royal blue, mustard and chrome; glazed on three sides over a teal interior, a counter ledge and a magazine rack on the front (-Y), vendor's door at the back, a mustard fin to 2.8 m at the right end.")
SPEC["Sedan_Official"] = dict(use=["vehicle:car-official"], sight="open", budget=4000, order=5, note="The official car (Vale's): a 1950s-shaped sedan, 1.84 x 4.6 x 1.45 m, nose -Y, pivot the footprint centre on the ground: drops in for the 1.8 x 1.4 x 4.6 box of HostViews.cs `car/official`. Deep teal body, ivory roof, glass cabin, rear fins, chrome bumpers and flank line, whitewall wheels.")
SPEC["Sedan_Getaway"] = dict(use=["vehicle:car-getaway"], sight="open", budget=4000, order=6, note="The getaway car: Sedan_Official in coral with a seafoam roof (`car/getaway`).")
SPEC["SkyRailStation_Blank"] = dict(use=["landmark"], sight=None, budget=None, order=20, note="SkyRailStation without HARBOUR on the dorsal fin and the hanging boards (the name was baked into the kit piece; a station's name is the plan's, set by a sign). Same geometry, 18k triangles: a candidate for the halts, not used by the world today (it places SkyRailStation_Twin and _Decks).")


def files_of(a, name, existing):
    """The row's files: models and LODs, the new materials and their textures from out/decokit-020; shared materials as the kit has them."""
    files = []

    def gen(role, rel):
        p = OUT / rel
        files.append({"root": "generated", "role": role, "path": rel, "sha256": sha(p.read_bytes()), "bytes": p.stat().st_size})
    gen("fbx", a["fbx"])
    for m in a["materials"]:
        for sfx, role in ((".mat", "material"), (".mat.meta", "material-meta")):
            rel = f"Materials/{m}{sfx}"
            if m.startswith(("DK_Cafe_", "DK_Glass_Clear", "DK_Glass_Sheen")):
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
            "sourceScript": "tools/decokit/cafe_grand.py", "files": files_of(a, name, existing),  # key order as tools/kit/catalogue.ts rebuilds a generated row (kit.test.ts)
            "tags": tags, "status": "candidate", "source": "generated"}


def main():
    kit = json.loads(KIT.read_text())
    rows = [row(n, kit["assets"]) for n in sorted(SPEC, key=lambda n: SPEC[n]["order"]) if n in MAN]
    art = json.loads((REPO / "tools/decokit/art/cafe/PROVENANCE.json").read_text())
    (OUT / "catalogue.proposal.json").write_text(json.dumps(dict(
        schema="suborned.kit/1 (proposal: KitAsset rows, status candidate)", brief="020 B", generator="tools/decokit (Blender 5.2.2), --out out/decokit-020",
        roots={"generated": "out/decokit-020 (this proposal's generator output; adopting = same relative paths under out/decokit)"},
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
