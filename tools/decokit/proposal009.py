#!/usr/bin/env python3
"""Brief 009 A delivery data (no Blender): reads out/decokit-009 (the generator's manifest, materials, measured.json) and writes
  catalogue.proposal.json   one KitAsset-shaped row per piece (contracts/world/kit.schema.ts fields) + parts, shadow flags, budget, source script, sha256
  crossing-colours.json     every material the pieces draw: unlit hex, role, dE2000 to cones / guard / rivals, the one display exception
and prints the district totals (from the world export's crossings). Nothing under out/decokit/, contracts/ or the store is written.
Run: python3 tools/decokit/proposal009.py [--only A,B --tag field]   (--only: just those rows; --tag: files become catalogue.proposal.<tag>.json and crossing-colours.<tag>.json)"""

import hashlib
import json
import math
import sys
from pathlib import Path

HERE = Path(__file__).resolve().parent
REPO = HERE.parents[1]
sys.path.insert(0, str(HERE))
from deltae import de  # noqa: E402

OUT = REPO / "out/decokit-009"
STYLE = json.loads((REPO / "contracts/style/suborned.style.json").read_text())
SIGNALS = {k: v.upper() for k, v in STYLE["signal_colours"].items() if k != "rule"}
MEAS = json.loads((OUT / "measured.json").read_text())
MAN = {a["name"]: a for a in json.loads((OUT / "decokit_manifest.json").read_text())["assets"]}
MATS = {m["name"]: m for m in json.loads((OUT / "decokit_materials.json").read_text())["materials"]}
sha = lambda b: hashlib.sha256(b).hexdigest()  # noqa: E731
hexof = lambda rgb: "#%02X%02X%02X" % tuple(round(c * 255) for c in rgb)  # noqa: E731
r3 = lambda v: round(v + 0.0, 3)  # noqa: E731

ROLE = {
    "DK_Chrome": ("orb body, finial, nozzle", "kit chrome"),
    "DK_Brass_Civic": ("frame line: display bezels, bar and dot frames, hairlines, kerb stud (small metal only, SF-21)", "civic brass #99784D"),
    "DK_Enamel_Charcoal": ("display plate (dark glass), under the pictograms", "kit enamel"),
    "DK_Cross_Light": ("seafoam glow: light bars, dots, halo ring, emitter lens", "kit seafoam #72B5A7, emissive 2.5"),
    "DK_Cross_Walk": ("walk pictogram on both faces", "civic pale seafoam #9BCFC2 (e_seafoam_pale), emissive 2.4"),
    "DK_Cross_Stop": ("stop pictogram on both faces: Sky's exception, small emissive display only, never a surface", "signal red, emissive 2.4"),
    "DK_Cross_Beam": ("light beam, translucent glow (alpha 0.28)", "pale seafoam #9BCFC2"),
    "DK_Cross_Halo": ("halo disc under the beam, translucent glow (alpha 0.40)", "kit seafoam #72B5A7"),
    "DK_Inlay_Seafoam": ("kerb inlay arc band, glazed faience (as the civic paving inlay)", "kit seafoam #72B5A7"),
    "DK_Cross_Pool": ("light spilt on the road: the lit strip's glow quad (kind glow, translucent; Latch draws it additive and double-sided, Glint sets the strength)", "kit seafoam #72B5A7, glow alpha 0.40"),
}
EXEMPT = {"DK_Cross_Stop"}
PX = 1080 / 28  # play zoom: orthographic size 14 at 1080 p

def tris(name, obj, lvl="L0"):
    return MEAS[name]["levels"][lvl]["objects"][obj]["tris"]


SHADOW = "off"
PARTS = {
    "SignalEye": [
        ("DK_SignalEye", "body: chrome orb cut flat at the two display planes, finial, nozzle, brass bezels, dark plates, emitter lens", "on", "visible", f"the one caster ({tris('SignalEye', 'DK_SignalEye')} tris); its L1 body ({tris('SignalEye', 'DK_SignalEye_L1', 'L1')} tris) is a cheap shadows-only proxy if Glint wants one"),
        ("DK_SignalEye_Walk", "walking figure, both faces (DK_Cross_Walk)", SHADOW, "visible", "Latch shows exactly one of Walk / Stop"),
        ("DK_SignalEye_Stop", "standing figure, both faces (DK_Cross_Stop, red)", SHADOW, "hidden", "enable instead of Walk"),
        ("DK_SignalEye_Beam", "open cone, ground to nozzle (DK_Cross_Beam, kind glow)", SHADOW, "visible", "additive, double-sided; UV u round, v 0 ground to 1 nozzle (for a fade)"),
        ("DK_SignalEye_Halo", "ground ring (DK_Cross_Light) and translucent disc (DK_Cross_Halo)", SHADOW, "visible", "disc UV = x, y in metres from the beam centre (radial falloff = length(uv))"),
    ],
}
SPEC = {
    "SignalEye": dict(use=["signal:eye"], budget=700, sight="open", src="crossing_eye.py", order=2,
                      note="Floating signal orb (sphere, not saucer). Pivot = the ground point under the beam = halo centre. Orb centre 2.70 m up (hover), nozzle 2.21 m, finial 3.23 m; orb 0.84 m across, display plates 0.51 m, figures 0.41 m tall; halo 1.12 m across. "
                           "Display A faces -Y (the piece's front), B faces +Y: yaw so A looks along the walking direction to the far kerb. Body, Walk and Stop share the origin: they move together by a z offset (bob) or turn together about the vertical axis; Beam and Halo are round, so they need neither. "
                           "Overhead keep-clear: a disc of radius 0.42 m at z 2.21 to 3.23 over the pivot; the ground footprint is the halo, radius 0.56 m."),
    "KerbStud": dict(use=["stud:kerb"], budget=100, sight="open", src="crossing_eye.py", order=3,
                     note="Brass dome stud, 0.40 m across the flange, 84 mm high, open underneath. Pivot = centre, on the paving. Along the kerb edge on the pavement side, and under a beam's foot as in crossing-future-play."),
    "LightBar_4": dict(use=["crossing:bar"], budget=40, sight="open", src="crossing_flat.py", order=4,
                       note="Flush light bar across the strip: 4.00 x 0.44 m overall (every crossing strip is 4 m wide), lens 3.90 x 0.34 m, 5 cm brass frame; long axis X, centred: yaw 90 to lie across the walking direction, pitch 1.0 m along it. Lens 4 mm and frame 6 mm above the road; faces up, no sides, nothing below the pivot plane."),
    "LightDotRow_2": dict(use=["crossing:dots"], budget=100, sight="open", src="crossing_flat.py", order=5,
                          note="2 m row of four seafoam dots (0.22 m) at a 0.5 m pitch, each in a 4 cm brass ring, between two 3 cm brass hairlines 0.4 m apart; long axis X, centred. Open ends: modules butt end to end with no seam. 4 mm / 6 mm proud."),
    "LightDotRow_1": dict(use=["crossing:dots"], budget=50, sight="open", src="crossing_flat.py", order=6,
                          note="1 m end module of LightDotRow_2 (two dots, same pitch and hairlines): a row of any whole metre is 2 m modules plus at most one of these."),
    "CrossEdgeLine_2": dict(use=["crossing:edge"], budget=10, sight="open", src="crossing_flat.py", order=8,
                            note="Brass frame line for one long edge of the crossing strip: 2.00 x 0.04 m, one quad, 6 mm proud, faces up, nothing below the pivot plane; long axis X, centred, yaw to the walking direction; ends butt with no seam. Place at y = +-2.0 (the strip's edge line) and move the dot rows in to y = +-1.75 so their outer hairline (1.965) clears it. Separate from the dot row on purpose: it serves the bar variant too, runs the strip's whole length whatever the row layout, and is a quad per 2 m."),
    "CrossEdgeLine_1": dict(use=["crossing:edge"], budget=10, sight="open", src="crossing_flat.py", order=9,
                            note="1 m end module of CrossEdgeLine_2 (1.00 x 0.04 m): a line of any whole metre is 2 m modules plus at most one of these."),
    "LightField_2": dict(use=["crossing:field"], budget=4, sight="open", src="crossing_opts.py", order=10,
                         note="The lit strip (Sky's pick, 15 Oct, brief 009 option d1): 2 m of soft seafoam light across the whole strip, one 2.0 x 3.9 m quad of kind glow (DK_Cross_Pool: translucent, additive, double-sided), 2 mm above the road, "
                              "no sides, faces up, nothing below the pivot plane; long axis X, centred, ends butt with no seam. Lay it end to end along the strip's centre line, between the brass edge lines (y = +-2.0; the quad spans y = +-1.95), "
                              "from the same end (0.5 m off each kerb) as the dot lines, UNDER the dot rows (lens 4 mm) and the brass hairlines (6 mm); the dots stay as they are. "
                              "SHADER (Latch): UV is the face's box projection, u = metres along the strip from the piece's centre, v = metres across from the centre line; fade = (1 - |v| / 1.95) ^ 1.5, so the quad is full on the centre line and nothing at the edge lines. "
                              "The beam's halo already fades by radius; this is the same material with |v| in place of length(uv). STRENGTH (Glint): the options sheet drew it as the dot colour (154, 219, 198) at 0.34 of full, added to the road, the fade above, no bloom; "
                              "that read as a frosted lit lane. It does not cast or receive shadows. Light state: the quad is the strip's walk cue, live 3D (lit on walk, dark on stop is Latch's to switch)."),
    "LightField_1": dict(use=["crossing:field"], budget=4, sight="open", src="crossing_opts.py", order=11,
                         note="1 m end module of LightField_2 (1.0 x 3.9 m): a field of any whole metre is 2 m modules plus at most one of these. Same glow, UVs and fade."),
    "CrossInlayArc_R24_W08": dict(use=["inlay:crossing-arc"], budget=70, sight="open", src="crossing_flat.py", order=7,
                                  note="Quarter ring of seafoam faience, centreline R 2.4 m, band 0.8 m (R 2.0 to 2.8) between 3 cm brass hairlines (outer edge R 2.83); pivot = arc centre, sweeps +X to +Y, 4 mm / 6 mm proud. Placed so the outer edge is tangent to the kerb line and to the strip's edge line: centre = mouth corner + (2.83, 2.83) into the pavement, quarter-turn yaw (180 NE, 90 SE, 270 NW, 0 SW in the render009 frame); a pair rounds both corners of a crossing's mouth."),
}


def files_of(a, name):
    files = []

    def add(role, rel):
        p = OUT / rel
        files.append({"root": "generated", "role": role, "path": rel, "sha256": sha(p.read_bytes()), "bytes": p.stat().st_size})
    add("fbx", a["fbx"])
    for m in a["materials"]:
        for sfx, role in ((".mat", "material"), (".mat.meta", "material-meta")):
            if (OUT / f"Materials/{m}{sfx}").exists():
                add(role, f"Materials/{m}{sfx}")
    for l in a.get("lods", []):
        add("lod", l["file"])
    return files


def row(name):
    a, spec, M = MAN[name], SPEC[name], MEAS[name]["levels"]
    b = a["bounds"]
    c = [r3((b[0] + b[1]) / 2), r3((b[2] + b[3]) / 2)]
    piv = {"pivot": "origin-ground", "pivotOffset": c} if math.hypot(*c) > 0.05 else {"pivot": "footprint-centre-ground"}
    top = max(o["max"][2] for o in M["L0"]["objects"].values())
    parts = [dict(object=o, role=r, castShadows=cast, default=d, note=n, tris=dict(L0=M["L0"]["objects"][o]["tris"], L1=M["L1"]["objects"][o.replace("DK_SignalEye", "DK_SignalEye_L1", 1)]["tris"]))
             for o, r, cast, d, n in PARTS.get(name, [])]
    return {"id": name, "category": a["category"], "size": [a["size"][0], r3(top), a["size"][2]], **piv, "front": "+Z", "tris": a["tris"], "materials": a["materials"],
            **({"parts": parts} if parts else {}), "lods": [{"file": l["file"], "tris": l["tris"], "screen": 0.008} for l in a["lods"]], "lodCategory": "street", "files": files_of(a, name),
            "tags": {"use": spec["use"], "scale": [1, 1], "squash": 1, "wave": 2, "sight": spec["sight"]}, "status": "candidate", "source": "generated", "sourceScript": f"tools/decokit/{spec['src']}",
            "budget": {"tris": spec["budget"], "ok": a["tris"] <= spec["budget"]}, "heightAbovePivot": r3(top), "note": spec["note"]}


def colours(rows):
    used = sorted({m for r in rows for m in r["materials"]})
    out = []
    for m in used:
        r = MATS[m]
        h = hexof(r["color"])
        d = {k: round(de(h, v), 1) for k, v in SIGNALS.items()}
        out.append(dict(material=m, kind=r["kind"], unlit=h, emission=r["emission"] or None, alpha=r["alpha"] if r["kind"] == "glow" else None, role=ROLE[m][0], source=ROLE[m][1], dE2000=d, min=min(d.values()),
                        used_by=[x["id"] for x in rows if m in x["materials"]], gate="exception (display pictogram)" if m in EXEMPT else ("pass" if min(d.values()) >= 20 else "FAIL")))
    return out


def district():
    w = json.loads((REPO / "out/world/harbour-district.world.json").read_text())["crossings"]
    ln = [math.dist(*c["line"]) for c in w]
    bars = sum(round(x) for x in ln)
    mods2 = sum(int(x // 2) * 3 + (1 if x % 2 >= 0.5 else 0) * 0 for x in ln)  # three rows of 2 m modules (the 1 m ends are counted below)
    ends = sum(3 if (x % 2) >= 1 else 0 for x in ln)
    return dict(crossings=len(w), metres=round(sum(ln), 1), bars=bars, orbs_2_per_crossing=2 * len(w), orbs_4_per_crossing=4 * len(w), dot_rows_2m=mods2, dot_rows_1m=ends, **field_modules())


def field_modules():
    """The lit strip lies along one line per crossing (the dot rows' spans on the left line): its modules, from the world export (read only)."""
    items = json.loads((REPO / "out/world/harbour-district.world.json").read_text())["street"]
    left = [s for s in items if s["kind"] == "crossing-dots" and s["id"].split("/")[-1].startswith("l")]
    n2, n1 = sum(1 for s in left if s["size"][0] == 2), sum(1 for s in left if s["size"][0] == 1)
    return dict(field_2m=n2, field_1m=n1, field_tris=2 * (n2 + n1))


def main():
    arg = lambda k: sys.argv[sys.argv.index(k) + 1] if k in sys.argv else None  # noqa: E731
    rows = [row(n) for n in (arg("--only").split(",") if arg("--only") else SPEC)]
    sfx = f".{arg('--tag')}" if arg("--tag") else ""
    cols = colours(rows)
    (OUT / f"catalogue.proposal{sfx}.json").write_text(json.dumps(dict(
        schema="suborned.kit/1 (proposal: KitAsset rows, status candidate)", brief="009 A", generator="tools/decokit (Blender 5.2.2), --out out/decokit-009",
        roots={"generated": "out/decokit-009 (this proposal's generator output; adopting = same relative paths under out/decokit)"},
        note="Rows follow contracts/world/kit.schema.ts (KitAsset) plus sight, sourceScript, budget, heightAbovePivot (the manifest's size[1] is the bounding box, which for the flat pieces starts at z 0.004) and parts as objects "
             "(object, role, castShadows, default, tris L0/L1). Kinds: DK_Cross_Beam, DK_Cross_Halo and DK_Cross_Pool (the lit strip) are kind `glow` (translucent light volumes): no .mat is written, Latch draws them additive and double-sided. kit.json is not touched.",
        assets=rows), indent=1) + "\n")
    (OUT / f"crossing-colours{sfx}.json").write_text(json.dumps(dict(
        schema="suborned.crossing-colours/1", brief="009 A", date="2026-10-14", signals=SIGNALS,
        rule="dE2000 >= 20 from every signal colour on the unlit palette (base colour of the material, emission off), except the stop pictogram: Sky's exception of 2026-10-14 (small emissive display pictograms only, never a surface; the cones keep their shape and motion cue). Owed: GDD s10 / s15 and the style contract.",
        materials=cols, below_20=[c["material"] for c in cols if c["min"] < 20], district=district()), indent=1) + "\n")
    for r in rows:
        print(f"{r['id']}: {r['tris']} tris (budget {r['budget']['tris']}, {'ok' if r['budget']['ok'] else 'OVER'}), L1 {r['lods'][0]['tris']}, size {r['size']}, pivot {r['pivot']}")
    for c in cols:
        print(f"  {c['material']:20s} {c['unlit']} min dE {c['min']:5.1f} {c['gate']}")
    print("district:", district())


if __name__ == "__main__":
    main()
