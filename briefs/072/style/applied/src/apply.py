#!/usr/bin/env python3
"""Brief 072-G item 1 (Vela): apply Sky's approved and pushed palette ("yep, more, more reds / blues allowed as well") to the colour sources, idempotently.
Writes facade.json, look.json, looks/harbour.json and the style contract (palette_world, materials, concept_prompt, notes, version; palette_world.source.sha256
refreshed from looks/harbour.json). The kit side (tools/decokit mats.py, records_tower.py) and the world accents (tools/world/gen/colour.ts) are edited in code;
civic-look.json goes through tools/decokit/lookcolours072.py. Run: python3 -I briefs/072/style/applied/src/apply.py"""
import hashlib, json, re
from pathlib import Path

REPO = Path(__file__).resolve().parents[5]
W = REPO / "unity/Assets/Suborned/Editor/World"
FACADE, LOOK, HARBOUR, STYLE = W / "facade.json", W / "look.json", W / "looks/harbour.json", REPO / "contracts/style/suborned.style.json"

BRASS, BRASS_LIGHT = "#C29A55", "#D6B676"
RED, ROYAL, COBALT = "#C42A1F", "#2C56A6", "#2F84E8"

SWATCHES = {
    # walls (072-B W-*): warmer and a little deeper, so stone, enamel and trim separate
    "cream_w": "#EBDABA", "peach_w": "#F2C4A4", "mint_w": "#C0E3D0", "sky_w": "#BCD9EE", "sand_w": "#E3CA9E", "ivory_w": "#EFE8D6", "civic_w": "#EDE5D2",
    # plinths pushed (072-G): peach takes a deep red-orange coral, sky a royal blue, sand an oxblood (not brass: a 0.5 m plinth band is a large brass surface)
    "peach_p": "#E8735A", "sky_p": ROYAL, "sand_p": "#8E2F25",
    # two new families (072-G): a blush stone with enamel-red plinth, a powder-blue render with royal plinth
    "blush_w": "#F3CBBE", "blush_p": RED, "blush_r": "#FBE5DC", "powder_w": "#CCDCF0", "powder_p": ROYAL, "powder_r": "#E8EFF8",
    # brass-gold (B5, B6)
    "gold": BRASS, "civic_trim": BRASS, "t_hotel": "#C9A46A",
    # blue-teal glass (G2)
    "glass_clear": "#2C6478", "glass_t": "#A7CAD8", "glass_dark": "#233F50",
    # DecoKit enamels (A1) and the push's reds and blues
    "e_mustard": "#EEA82D", "e_coral_deep": "#E07560", "e_royal": ROYAL, "e_teal_deep": "#1F6C67",
    "e_red": RED, "e_redorange": "#E8553A", "e_oxblood": "#8E2F25", "e_powder": "#86ADD3",
    # trades pushed (072-G): diner red-orange, record shop enamel red, cinema crimson marquee, travel bureau royal, garage cobalt
    "t_diner": "#E8553A", "t_record": RED, "t_cinema": "#A8322B", "t_travel": ROYAL, "t_garage": "#3D72C8",
}
FAMILIES_ADD = {"blush": ["blush_w", "blush_p", "blush_r"], "powder": ["powder_w", "powder_p", "powder_r"]}
AREAS = {  # family weights added, enamel added (072-B A2 + the 072-G push), civic replaced (C1)
    "lantern":   ({"blush": 2}, ["e_mustard", "e_coral_deep", "e_red", "e_royal"]),
    "docks":     ({"powder": 2}, ["e_mustard", "e_royal", "e_red", "e_powder"]),
    "crescent":  ({"blush": 1, "powder": 1}, ["e_royal", "e_coral_deep", "e_sky"]),
    "records":   ({"powder": 1}, ["e_mustard", "e_royal", "e_oxblood"]),
    "harbour":   ({"blush": 1, "powder": 2}, ["e_mustard", "e_coral_deep", "e_red", "e_royal"]),
    "lamplight": ({"blush": 2}, ["e_mustard", "e_coral_deep", "e_redorange", "e_cobalt"]),
    "gardens":   ({"powder": 1}, ["e_mustard", "e_royal", "e_coral_deep"]),
}
CIVIC_ENAMEL = ["e_seafoam", "e_teal_deep"]
MATERIALS = {"matte": 0.22, "glossy": 0.72}

LOOK_HEX = {  # harbour.<role>
    "limestone": "#F0E3CA", "residential": "#F3E3C6", "mixed": "#F7EAD2", "commercial": "#F5D3B5", "frontage": "#F0E0C3", "civic": "#F7DFA8", "roof": "#F6EBD6",
    "trim": BRASS, "accent_gold": BRASS, "accent_coral": "#F28A70", "accent_peach": "#F8B898",
    "accent_red": RED, "accent_royal": ROYAL, "accent_cobalt": COBALT,
}
NEW_ACCENTS = ("accent_red", "accent_royal", "accent_cobalt")

FACADE_NOTE_ADD = (" Brief 072-G (Sky, 2026-10-27, on the 072-B proposal: \"yep, more, more reds / blues allowed as well\"; the Office's deep teal band \"yep\"; the powder-blue road kept): "
    "walls warmer and a little deeper (cream_w #EBDABA, peach_w #F2C4A4 ...); brass-gold #C29A55 for gold, civic_trim and gold_trim (t_hotel #C9A46A); glass is dark teal to blue-teal with sky reflections "
    "(glass_clear #2C6478, glass_dark #233F50), still no curtain-wall towers in the play streets; DecoKit's enamels pickable (e_mustard, e_coral_deep, e_royal, e_teal_deep) and the push's reds and blues "
    "(e_red #C42A1F, e_redorange #E8553A, e_oxblood #8E2F25, e_powder #86ADD3) in every area's enamel set; plinths peach_p red-orange coral, sky_p royal, sand_p oxblood (not brass: a plinth band is a large surface); "
    "two new families, blush (pale coral stone, enamel-red plinth) and powder (powder-blue render, royal plinth); trades diner, record shop, cinema, travel bureau and garage in reds and blues; "
    "civic enamel seafoam + deep teal (C1). Matte 0.22, glossy 0.72 (a soft sheen). Values: briefs/072/style/rules.json `applied`.")


def dump(path, obj):
    path.write_text(json.dumps(obj, indent=2, ensure_ascii=False) + "\n")


def sub1(text, pattern, repl, flags=0):
    out, n = re.subn(pattern, repl, text, count=1, flags=flags)
    assert n == 1, pattern
    return out


def facade():
    """facade.json is hand-formatted (one line per family of swatches, one per area): edit the text, keep the layout."""
    t = FACADE.read_text()
    f0 = json.loads(t)
    for k, hx in SWATCHES.items():
        if k in f0["swatches"]:
            t = sub1(t, r'("%s": ")#[0-9a-fA-F]{6}(")' % re.escape(k), r"\g<1>%s\g<2>" % hx.lower())
    new = [k for k in SWATCHES if k not in f0["swatches"]]
    if new:
        line = lambda ks: "    " + ", ".join('"%s": "%s"' % (k, SWATCHES[k].lower()) for k in ks) + ","  # noqa: E731
        fams = [k for k in new if k.split("_")[0] in FAMILIES_ADD]
        rest = [k for k in new if k not in fams]
        block = "\n".join([line([k for k in fams if k.startswith(f + "_")]) for f in FAMILIES_ADD] + [line(rest)])
        t = sub1(t, r'(\n    "ivory_w": [^\n]*\n)', lambda m: m.group(1) + block + "\n")
    add = [n for n in FAMILIES_ADD if n not in f0["families"]]
    if add:
        row = "    " + ", ".join('"%s": [%s]' % (n, ", ".join('"%s"' % x for x in FAMILIES_ADD[n])) for n in add)
        t = sub1(t, r'(\n  \},\n  "areas": \{)', lambda m: "," + "\n" + row + m.group(1))
    for a, (fams, en) in AREAS.items():
        cur = f0["areas"][a]
        fam = {**cur["families"], **fams}
        ens = [e for e in cur["enamel"] if e not in en] + en
        body = '"families": { %s }, "enamel": [%s]' % (", ".join('"%s": %d' % kv for kv in fam.items()), ", ".join('"%s"' % e for e in ens))
        t = sub1(t, r'("%s":\s*\{ )"families": \{[^}]*\},\s*"enamel": \[[^\]]*\]' % a, lambda m: m.group(1) + body)
    t = sub1(t, r'("civic":\s*\{ "families": \{ "civic": 1 \},\s*"enamel": )\[[^\]]*\]', lambda m: m.group(1) + "[%s]" % ", ".join('"%s"' % e for e in CIVIC_ENAMEL))
    t = sub1(t, r'"materials": \{ "matte": [\d.]+, "glossy": [\d.]+ \}', '"materials": { "matte": %s, "glossy": %s }' % (MATERIALS["matte"], MATERIALS["glossy"]))
    t = sub1(t, r'("paletteOverride": \{ "gold_trim": ")#[0-9a-fA-F]{6}', r"\g<1>%s" % BRASS.lower())
    f = json.loads(t)
    note = f["note"].replace("Glass is clear (grammar \u00a73: no blue curtain-wall tint)", "Glass was clear (grammar \u00a73: no blue curtain-wall tint) until brief 072-G")
    if "Brief 072-G" not in note:
        note += FACADE_NOTE_ADD
    if note != f["note"]:
        t = t.replace(json.dumps(f["note"], ensure_ascii=False), json.dumps(note, ensure_ascii=False), 1)
    f = json.loads(t)
    assert f["note"] == note
    missing = [s for fam in f["families"].values() for s in fam if s not in f["swatches"]] + [e for a in f["areas"].values() for e in a["enamel"] if e not in f["swatches"]]
    assert not missing, missing
    assert len(f["swatches"]) <= 256 and all(f["swatches"][k].lower() == v.lower() for k, v in SWATCHES.items())
    FACADE.write_text(t)


def look_text():
    """look.json is indent-2 with inline `details` rows: edit the text."""
    t = LOOK.read_text()
    d = json.loads(t)
    for k, hx in LOOK_HEX.items():
        if "harbour." + k in d["palette"]:
            t = sub1(t, r'("harbour\.%s": \{\s*"hex": ")#[0-9a-fA-F]{6}' % re.escape(k), r"\g<1>%s" % hx.lower())
    for k in NEW_ACCENTS:
        if k not in d["surfaces"]:
            t = sub1(t, r'(\n    "accent_peach": \{[^}]*\},)', lambda m: m.group(1) + '\n    "%s": {\n      "detail": "enamel",\n      "palette": "harbour.%s",\n      "value": 1,\n      "smoothness": 0.5\n    },' % (k, k))
        if "harbour." + k not in d["palette"]:
            t = sub1(t, r'(\n    "harbour\.accent_peach": \{[^}]*\},)', lambda m: m.group(1) + '\n    "harbour.%s": {\n      "hex": "%s"\n    },' % (k, LOOK_HEX[k].lower()))
    d = json.loads(t)
    assert all(d["palette"]["harbour." + k]["hex"].lower() == v.lower() for k, v in LOOK_HEX.items())
    LOOK.write_text(t)


def harbour():
    d = json.loads(HARBOUR.read_text())
    for k, hx in LOOK_HEX.items():
        d["palette"].setdefault("harbour." + k, {})["hex"] = hx.lower()
    surf = d["surfaces"]
    for k in NEW_ACCENTS:
        surf.setdefault(k, {"palette": "harbour." + k, "value": 1, "smoothness": 0.5})
    dump(HARBOUR, d)


def style():
    s = json.loads(STYLE.read_text())
    pw = s["palette_world"]
    for k, hx in LOOK_HEX.items():
        if k in pw["roles"]:
            pw["roles"][k]["hex"] = hx.lower()
        else:
            pw["roles"][k] = {"hex": hx.lower(), "group": "accent"}
    pw["surface_overrides"]["gold_trim"] = {"hex": BRASS.lower(), "why": "Since v0.22.0 (brief 072-G) world surfaces use brass-gold #c29a55, the same as roles.trim, roles.accent_gold and facade.json paletteOverride.gold_trim (the sign boards). Kit: DK_Accent_Gold and DK_Brass_Civic #C29A55, DK_Gold #D6AE62 (DecoKit's), DK_Gold17 #D6B676 (tools/decokit mats.py); the crowns keep DK_Crown_Gold #E3D681 and DK_Crown_Gold17 #C9C08E."}
    pw["rule"] = ("Pastel cream, peach and warm limestone base for ground, stone and bodies (pale blush and powder-blue renders too); saturated enamel accents (cobalt, royal, sky blue, "
                  "enamel red, coral, red-orange, oxblood, flamingo, mustard, sunshine, mint, turquoise, teal) on panels, spandrels, bands, awnings, signs, roofs and trim, never a whole building's body; "
                  "reds and blues are welcome world accents (signals don't own red, amber or violet); blue-teal glass with sky reflections, no curtain walls in the play streets; lagoon water; coral transit; "
                  "a powder-blue enamel road. Warm brass-gold at street level and on facades: frames, beads, bands, fins and small metal up to about 15 cm; skyline crowns keep gold; never a large gold or brass panel.")
    pw["status"] = "current, tuneable: Sky approved the 072-B loosening and pushed it (2026-10-27: 'yep, more, more reds / blues allowed as well'); judged in the world"
    s["materials"] = [
        "honed cream, peach and warm limestone",
        "flamed grey granite kerb",
        "saturated vitreous enamel panels (cobalt, royal blue, enamel red, coral, mustard, flamingo, sunshine, mint, turquoise)",
        "teal, deep teal and mint vitreous enamel",
        "chrome and brushed silver",
        "white vinyl and lame",
        "clear acrylic",
        "blue-teal glass with sky reflections",
        "warm brass-gold trim, frames and bands at street level and on facades, gold on skyline crowns",
        "powder-blue enamel road",
    ]
    cp = s["concept_prompt"]
    cp["city"] = cp["city"].replace("thin brushed-brass hairline trim, coral and peach accents", "brushed brass-gold trim lines and bands, coral, peach and mustard accents, enamel red and royal blue accents")
    cp["avoid"] = ["large gold panels" if a == "gold surfaces" else a for a in cp["avoid"]]
    sha = hashlib.sha256(HARBOUR.read_bytes()).hexdigest()
    pw["source"]["sha256"] = sha
    note = ("v0.22.0 (2026-10-27): Sky (brief 072-G, on Vela's 072-B proposal briefs/072/style/proposal.html and rules.json): 'yep, more, more reds / blues allowed as well'; "
            "the Records Office's deep teal storey band 'yep'; the road 'keep powdblue - feels more modern'. palette_world re-recorded from looks/harbour.json (sha256 refreshed): "
            "limestone #f8eed9 -> #f0e3ca, residential #f8eedb -> #f3e3c6, mixed #fdf5e4 -> #f7ead2, commercial #f9e2cc -> #f5d3b5, frontage #f6ecd8 -> #f0e0c3, civic #fbe8b8 -> #f7dfa8, "
            "roof #fff6e6 -> #f6ebd6 (off near-white toward warm limestone); trim and accent_gold #99784d -> brass-gold #c29a55 (surface_overrides.gold_trim the same); accent_coral #fc9c84 -> #f28a70, "
            "accent_peach #fcc4ac -> #f8b898; new world accents accent_red #c42a1f (DecoKit's enamel red), accent_royal #2c56a6 (DecoKit's royal), accent_cobalt #2f84e8 (tools/world/gen/colour.ts weights). "
            "The rule widens the accent set to DecoKit's and the push (royal, sky blue, enamel red, red-orange, oxblood, mustard, teal), allows brass-gold bands up to ~15 cm (never a large panel) "
            "and blue-teal glass (still no curtain walls). materials and concept_prompt city/avoid follow. Kit (tools/decokit mats.py): the four gate-era colours back to DecoKit's "
            "(DK_Enamel_Mustard #EEA82D, DK_Gold #D6AE62 as satin brass, DK_Enamel_Red #C42A1F, DK_Signal_Red #FF3B26), brass rows #C29A55 / light #D6B676 at (rough 0.4, metal 0.6), "
            "the window film glaze #3F7A8E a0.65. Facade (facade.json): warmer walls, blush and powder families, red and blue plinths, DecoKit enamels and the push's reds and blues in every area, "
            "civic seafoam + deep teal; the Records Office's storey band (5.0 to 5.9 m) is DecoKit's deep teal DK_Enamel_Teal (tools/decokit/records_tower.py); civic-look.json through tools/decokit/lookcolours072.py. "
            "The road stays powder-blue enamel. Final values: briefs/072/style/rules.json `applied`. GDD §15 to follow (Aster).")
    s["notes"] = [n for n in s["notes"] if not n.startswith("v0.22.0")] + [note]
    s["version"], s["date"] = "0.22.0", "2026-10-27"
    dump(STYLE, s)
    return sha


if __name__ == "__main__":
    facade()
    look_text()
    harbour()
    print("palette_world.source.sha256", style())
