#!/usr/bin/env python3
"""Brief 072-G (27 Oct, Vela): write the applied palette push into contracts/world/civic-look.json, colour values and colour rules only (as lookcolours008.py did on 14 Oct).

Sky on the 072-B proposal (briefs/072/style/rules.json): "yep, more, more reds / blues allowed as well"; the Records Office's deep teal band "yep".
  palette.materials  every row whose material is a flat colour in the generator's table (out/decokit/decokit_materials.json: no texture) takes that colour as its unlit hex,
                     so the rows say what the regenerated .mat files draw (kit.test.ts holds the brass rows to the .mat). Textured rows (linear-light means) are left as they are.
  roles              hairline (brass-gold #C29A55, bands up to 15 cm, at most 12 % of an elevation), deep (storey bands, fascia lines and reveals up to 0.9 m: the Office band),
                     glass (dark teal to blue-teal), banner and lamp (the signal-gate reasons dropped: Sky, 2026-10-16), decision (a 072-G line), parts.RecordsTower.
Nothing else in the file is touched (pieces, swaps, scales, place, bands, budget, slab, beds, sources). Splice owns the file; this script is the colour importer.
Run after `bun run decokit --mats-only`: python3 -I tools/decokit/lookcolours072.py [--check]   (idempotent; --check exits 1 if the file would change)"""
import json
import sys
from pathlib import Path

REPO = Path(__file__).resolve().parents[2]
LOOK = REPO / "contracts/world/civic-look.json"
MATS = REPO / "out/decokit/decokit_materials.json"
BRASS, GOLD_DK, BRASS_LIGHT = "#C29A55", "#D6AE62", "#D6B676"
G = "072-G (Sky, 27 Oct: 'yep, more, more reds / blues allowed as well')"


def hexof(c):
    return "#" + "".join(f"{round(x * 255):02X}" for x in c[:3])


def roles(r):
    R = r["roles"]
    h = R["hairline"]
    h["hex"] = [BRASS]
    h["facade_swatch"] = {"gold": BRASS, "paletteOverride.gold_trim": BRASS}
    h["rule"] = ("bead, frame, mullion, line or band up to 15 cm; at most 12% of an elevation; never a large panel. Brass-gold " + BRASS + " (" + G + "; DK_Gold is DecoKit's "
                 + GOLD_DK + ", DK_Gold17 " + BRASS_LIGHT + "). Was dark brass #99784D, held there only by the signal-colour gate dropped on 2026-10-16")
    h["use"] = "glazing frames, door frames, fascia lines, marquee edge, banner rods, window grid, the Office band's hairlines"
    d = R["deep"]
    d["rule"] = ("storey bands, fascia lines and reveals up to 0.9 m; never a whole field. The Records Office's storey band (5.0 to 5.9 m, RecordsTower) is DecoKit's deep teal, "
                 "as the old DecoKit civic office (" + G + ", Q1b 'yep'); the sign body, lamp sleeve, cushions and the 8 cm window reveal stay")
    d["use"] = "the Office storey band, sign body, cushions, lamp sleeve, window reveal"
    R["glass"]["rule"] = "dark teal to blue-teal with sky reflections (facade.json glass_clear #2C6478; the kit film DK_GlassDark glaze #3F7A8E); no curtain wall"
    R["banner"]["rule"] = ("entrance banners: seafoam field, ivory comet (banner_comet_seafoam_v1.png, derive008.py); lamp pennants: seafoam and the contract coral. Coral, flamingo, "
                           "peach and red-orange are all allowed (the signal-colour gate that kept DK_Fabric_Coral off this block was dropped on 2026-10-16); the block keeps seafoam to follow the paintings")
    R["lamp"]["rule"] = "DK_Lamp_Deco #FFF1C4, the paler lamp, chosen for the civic pieces' colour (the old reason, a distance from the guard colour, is gone since 2026-10-16)"
    line = (" " + G + ": the hairline is brass-gold " + BRASS + " and may be a band up to 15 cm; deep teal is also the Records Office's storey band (up to 0.9 m, never a field); "
            "glass dark teal to blue-teal; the signal-gate reasons are history.")
    if "072-G" not in r["decision"]:
        r["decision"] += line
    r["parts"]["RecordsTower"] = {"stone": "wing, pilasters, towers, crowns (DK_Limestone, DK_Enamel_Ivory)", "band": "pilaster flutes, parapet band, tower fins, crown ring (seafoam)",
                                  "deep": "the storey band 5.0 to 5.9 m round the wing and both towers (DK_Enamel_Teal), the service door",
                                  "hairline": "the band's two hairlines, parapet and crown lines, pilaster caps (brass-gold)", "glass": "the giant order's glazing, stair slots, rotunda (DK_GlassDark)",
                                  "metal": "mullions, rotunda ribs, spheres (chrome)"}


def main():
    check = "--check" in sys.argv
    text = LOOK.read_text()
    look = json.loads(text)
    flat = {m["name"]: hexof(m["color"]) for m in json.loads(MATS.read_text())["materials"] if not m.get("texture") and m.get("color")}
    changed = []
    for row in look["palette"]["materials"]:
        new = flat.get(row["material"])
        if new and row["unlit"].upper() != new:
            changed.append(f"{row['material']} {row['unlit']} -> {new}")
            row["unlit"] = new
    roles(look["roles"])
    out = json.dumps(look, indent=1) + "\n"  # the file is ASCII-escaped, indent 1
    if check:
        print("civic-look.json", "would change" if out != text else "up to date", *changed, sep="\n  ")
        sys.exit(1 if out != text else 0)
    LOOK.write_text(out)
    print(f"civic-look.json: roles hairline, deep, glass, banner, lamp, decision, parts.RecordsTower; palette rows changed {len(changed)}", *changed, sep="\n  ")


if __name__ == "__main__":
    main()
