#!/usr/bin/env python3
"""Brief 072 (Sky, 2026-10-28: "yeah, I more colors needed"; Vela): the second palette push, on top of 072-G (apply.py), idempotently.
Everything 072-G added stays (oxblood plinths, blush and powder walls, the shop colours, the accent weights' reds and blues); this widens the range:
four new wall families (butter, lilac, apricot, seagreen), the warm neutrals and the two blues pulled apart, more plinth hues (rose on mint, cerulean,
plum, jade, tangerine), five new enamels and world accent roles (tangerine, plum, cerulean, jade = teal-green, lime), family roofs tinted, cream's weight down.
The civic block keeps seafoam + deep teal (areas.civic untouched); the road stays powder-blue enamel (not in these files); signals own no colour.
Writes facade.json, look.json, looks/harbour.json and the style contract (palette_world roles, rule, materials, concept_prompt.city, notes, version 0.23.0;
palette_world.source.sha256 refreshed from looks/harbour.json). The world accent weights are tools/world/gen/colour.ts (edited in code).
Run: python3 -I briefs/072/style/applied2/src/apply2.py"""
import hashlib, json, re
from pathlib import Path

REPO = Path(__file__).resolve().parents[5]
W = REPO / "unity/Assets/Suborned/Editor/World"
FACADE, LOOK, HARBOUR, STYLE = W / "facade.json", W / "look.json", W / "looks/harbour.json", REPO / "contracts/style/suborned.style.json"

TANGERINE, PLUM, CERULEAN, JADE, LIME = "#F07A28", "#7A3B78", "#1F8FC4", "#1E9C78", "#B5D334"

SWATCHES = {  # changed (existing keys)
    "sand_w": "#DDBE8A",      # a step deeper and more golden: 5.2 dE from cream was the same wall at play zoom (now 8.8)
    "blush_w": "#F4C3C8",     # pinker (rose blush): 15 dE from peach, was 6.6
    "sky_w": "#9CCDEE",       # a clear sky blue: 9.6 dE from powder, was 4.0
    "mint_w": "#C8E8C6",      # a little greener (pistachio mint), so seagreen can sit beside it
    "mint_p": "#D9548A",      # rose plinth on mint (the 1950s mint + pink); was #2a9d96, a second teal beside cream's
    "cream_r": "#F7ECD6", "peach_r": "#FBDFCB", "mint_r": "#DEF1DC", "sky_r": "#D6EAF8", "sand_r": "#EFDFC0",
    "blush_r": "#FADCDF", "powder_r": "#E2E9F8",
    "t_launder": "#3FA7D6", "t_radio": TANGERINE, "t_chandler": "#1E8C6E",
}
NEW = {  # added: the four new families, then the new enamels
    "butter_w": "#F7E38C", "butter_p": CERULEAN, "butter_r": "#FBF0C6",
    "lilac_w": "#D8C8EC", "lilac_p": PLUM, "lilac_r": "#EEE6F7",
    "apricot_w": "#F9B977", "apricot_p": "#1E8C6E", "apricot_r": "#FCE0C0",
    "seagreen_w": "#9AD2C6", "seagreen_p": "#E2661F", "seagreen_r": "#D5EEE8",
    "e_tangerine": TANGERINE, "e_plum": PLUM, "e_cerulean": CERULEAN, "e_jade": JADE, "e_lime": LIME,
}
FAMILIES_ADD = {"butter": ["butter_w", "butter_p", "butter_r"], "lilac": ["lilac_w", "lilac_p", "lilac_r"],
                "apricot": ["apricot_w", "apricot_p", "apricot_r"], "seagreen": ["seagreen_w", "seagreen_p", "seagreen_r"]}
AREAS = {  # family weights replaced (cream 4 -> 2 almost everywhere), enamel added
    "lantern":   ({"peach": 3, "cream": 2, "sand": 2, "blush": 2, "apricot": 3, "butter": 2, "lilac": 2, "seagreen": 1, "mint": 1}, ["e_tangerine", "e_plum", "e_cerulean", "flamingo"]),
    "docks":     ({"sky": 2, "cream": 2, "mint": 2, "sand": 2, "powder": 2, "seagreen": 3, "butter": 2, "apricot": 1}, ["e_cerulean", "e_tangerine", "e_jade"]),
    "crescent":  ({"mint": 2, "sky": 2, "peach": 2, "cream": 2, "blush": 2, "powder": 1, "lilac": 3, "butter": 2, "seagreen": 1}, ["e_plum", "e_jade", "e_lime", "e_tangerine"]),
    "records":   ({"ivory": 6, "cream": 2, "powder": 1, "butter": 1, "seagreen": 1}, ["e_cerulean", "e_plum"]),
    "harbour":   ({"cream": 2, "sky": 2, "mint": 2, "peach": 2, "sand": 1, "blush": 1, "powder": 2, "seagreen": 2, "butter": 2, "apricot": 2, "lilac": 1}, ["e_tangerine", "e_cerulean", "e_jade", "e_plum"]),
    "lamplight": ({"cream": 2, "peach": 2, "sand": 2, "mint": 1, "blush": 2, "apricot": 3, "lilac": 2, "butter": 2}, ["e_plum", "e_tangerine", "e_lime", "e_jade"]),
    "gardens":   ({"mint": 3, "cream": 2, "sky": 2, "powder": 1, "seagreen": 2, "butter": 2, "lilac": 2, "apricot": 1}, ["e_lime", "e_jade", "e_tangerine", "e_plum"]),
}
LOOK_HEX = {"accent_tangerine": TANGERINE, "accent_plum": PLUM, "accent_cerulean": CERULEAN, "accent_jade": JADE, "accent_lime": LIME}
SMOOTH = {"accent_tangerine": 0.5, "accent_plum": 0.5, "accent_cerulean": 0.55, "accent_jade": 0.55, "accent_lime": 0.45}

FACADE_NOTE_ADD = (" Brief 072, second push (Sky, 2026-10-28: \"yeah, I more colors needed\"): four new wall families, butter (#f7e38c, cerulean plinth), lilac (#d8c8ec, plum plinth), "
    "apricot (#f9b977, jade plinth) and seagreen (#9ad2c6, tangerine plinth); sand_w deeper (#ddbe8a), blush_w pinker (#f4c3c8), sky_w a clear sky blue (#9ccdee), mint_w greener (#c8e8c6) "
    "with a rose plinth (#d9548a), so neighbouring walls sit at least about 9 dE2000 apart (cream and ivory excepted); family roofs one tint deeper; cream's weight 4 -> 2; "
    "new enamels e_tangerine, e_plum, e_cerulean, e_jade, e_lime in the areas' sets (civic keeps seafoam + deep teal); trades launderette cerulean, radio-tv tangerine, chandler jade. "
    "Values: briefs/072/style/rules.json `applied2`.")


def sub1(text, pattern, repl, flags=0):
    out, n = re.subn(pattern, repl, text, count=1, flags=flags)
    assert n == 1, pattern
    return out


def facade():
    t = FACADE.read_text()
    f0 = json.loads(t)
    for k, hx in SWATCHES.items():
        t = sub1(t, r'("%s": ")#[0-9a-fA-F]{6}(")' % re.escape(k), r"\g<1>%s\g<2>" % hx.lower())
    new = [k for k in NEW if k not in f0["swatches"]]
    if new:
        line = lambda ks: "    " + ", ".join('"%s": "%s"' % (k, NEW[k].lower()) for k in ks) + ","  # noqa: E731
        fams = [line([k for k in new if k.startswith(f + "_")]) for f in FAMILIES_ADD if any(k.startswith(f + "_") for k in new)]
        rest = [k for k in new if not any(k.startswith(f + "_") for f in FAMILIES_ADD)]
        block = "\n".join(fams + ([line(rest)] if rest else []))
        t = sub1(t, r'(\n    "powder_w": [^\n]*\n)', lambda m: m.group(1) + block + "\n")
    for k, hx in NEW.items():
        t = sub1(t, r'("%s": ")#[0-9a-fA-F]{6}(")' % re.escape(k), r"\g<1>%s\g<2>" % hx.lower())
    add = [n for n in FAMILIES_ADD if n not in f0["families"]]
    if add:
        row = "    " + ", ".join('"%s": [%s]' % (n, ", ".join('"%s"' % x for x in FAMILIES_ADD[n])) for n in add)
        t = sub1(t, r'(\n  \},\n  "areas": \{)', lambda m: "," + "\n" + row + m.group(1))
    for a, (fams, en) in AREAS.items():
        cur = json.loads(t)["areas"][a]
        ens = [e for e in cur["enamel"] if e not in en] + en
        body = '"families": { %s }, "enamel": [%s]' % (", ".join('"%s": %d' % kv for kv in fams.items()), ", ".join('"%s"' % e for e in ens))
        t = sub1(t, r'("%s":\s*\{ )"families": \{[^}]*\},\s*"enamel": \[[^\]]*\]' % a, lambda m: m.group(1) + body)
    f = json.loads(t)
    if "second push" not in f["note"]:
        t = t.replace(json.dumps(f["note"], ensure_ascii=False), json.dumps(f["note"] + FACADE_NOTE_ADD, ensure_ascii=False), 1)
    f = json.loads(t)
    missing = [s for fam in f["families"].values() for s in fam if s not in f["swatches"]] + [e for a in f["areas"].values() for e in a["enamel"] if e not in f["swatches"]]
    assert not missing, missing
    assert len(f["swatches"]) <= 256
    assert all(f["swatches"][k].lower() == v.lower() for k, v in {**SWATCHES, **NEW}.items())
    assert f["areas"]["civic"]["enamel"] == ["e_seafoam", "e_teal_deep"]
    FACADE.write_text(t)


def look_text():
    t = LOOK.read_text()
    for k, hx in reversed(list(LOOK_HEX.items())):  # each goes in right after accent_red: reversed keeps the table's order
        d = json.loads(t)
        if k not in d["surfaces"]:
            t = sub1(t, r'(\n    "accent_red": \{[^}]*\},)', lambda m: m.group(1) + '\n    "%s": {\n      "detail": "enamel",\n      "palette": "harbour.%s",\n      "value": 1,\n      "smoothness": %s\n    },' % (k, k, SMOOTH[k]))
        if "harbour." + k not in d["palette"]:
            t = sub1(t, r'(\n    "harbour\.accent_red": \{[^}]*\},)', lambda m: m.group(1) + '\n    "harbour.%s": {\n      "hex": "%s"\n    },' % (k, hx.lower()))
        else:
            t = sub1(t, r'("harbour\.%s": \{\s*"hex": ")#[0-9a-fA-F]{6}' % re.escape(k), r"\g<1>%s" % hx.lower())
    d = json.loads(t)
    assert all(d["palette"]["harbour." + k]["hex"] == v.lower() for k, v in LOOK_HEX.items()) and all(k in d["surfaces"] for k in LOOK_HEX)
    LOOK.write_text(t)


def harbour():
    d = json.loads(HARBOUR.read_text())
    for k, hx in LOOK_HEX.items():
        d["palette"].setdefault("harbour." + k, {})["hex"] = hx.lower()
        d["surfaces"].setdefault(k, {"palette": "harbour." + k, "value": 1, "smoothness": SMOOTH[k]})
    HARBOUR.write_text(json.dumps(d, indent=2, ensure_ascii=False) + "\n")


def style():
    s = json.loads(STYLE.read_text())
    pw = s["palette_world"]
    for k, hx in LOOK_HEX.items():
        pw["roles"][k] = {"hex": hx.lower(), "group": "accent"}
    pw["rule"] = ("Pastel stone and render bodies: cream, peach and warm limestone, and since 072's second push butter yellow, mint, sea green, sky blue, powder blue, lilac, "
                  "apricot and blush renders, so neighbours read different at play zoom; saturated enamel accents (cobalt, royal, cerulean, sky blue, enamel red, coral, red-orange, tangerine, "
                  "oxblood, plum, flamingo, rose, mustard, sunshine, lime, mint, jade, turquoise, teal) on panels, plinths, spandrels, bands, awnings, signs, roofs and trim, never a whole "
                  "building's body; pastel walls with saturated accents, never garish; signals don't own red, amber or violet; blue-teal glass with sky reflections, no curtain walls in the "
                  "play streets; lagoon water; coral transit; a powder-blue enamel road; the civic block keeps cream stone with seafoam and deep teal. Warm brass-gold at street level and on "
                  "facades: frames, beads, bands, fins and small metal up to about 15 cm; skyline crowns keep gold; never a large gold or brass panel.")
    pw["status"] = ("current, tuneable: Sky approved the 072-B loosening and pushed it twice (2026-10-27: 'yep, more, more reds / blues allowed as well'; "
                    "2026-10-28: 'yeah, I more colors needed'); judged in the world")
    s["materials"] = [
        "honed cream, peach and warm limestone",
        "pastel renders: butter yellow, mint, sea green, sky blue, powder blue, lilac, apricot, blush",
        "flamed grey granite kerb",
        "saturated vitreous enamel panels (cobalt, royal blue, cerulean, enamel red, coral, tangerine, plum, mustard, flamingo, sunshine, lime, mint, jade, turquoise)",
        "teal, deep teal and mint vitreous enamel",
        "chrome and brushed silver",
        "white vinyl and lame",
        "clear acrylic",
        "blue-teal glass with sky reflections",
        "warm brass-gold trim, frames and bands at street level and on facades, gold on skyline crowns",
        "powder-blue enamel road",
    ]
    cp = s["concept_prompt"]
    old = "Cream limestone and ivory enamel buildings with seafoam-teal faience bands"
    if old in cp["city"]:
        cp["city"] = cp["city"].replace(old, "Cream limestone and pastel-rendered buildings (butter yellow, mint, sea green, sky blue, lilac, apricot, blush), each neighbour a different colour, with seafoam-teal faience bands")
    cp["city"] = cp["city"].replace("coral, peach and mustard accents, enamel red and royal blue accents", "coral, peach, mustard, tangerine, plum, lime and cerulean accents, enamel red and royal blue accents")
    sha = hashlib.sha256(HARBOUR.read_bytes()).hexdigest()
    pw["source"]["sha256"] = sha
    note = ("v0.23.0 (2026-10-28): Sky (brief 072, on the applied 072-G palette): 'yeah, I more colors needed'. Everything 072-G added stays; the range widens (Vela, "
            "briefs/072/style/rules.json `applied2`): world accent roles accent_tangerine #f07a28, accent_plum #7a3b78, accent_cerulean #1f8fc4, accent_jade #1e9c78 (teal-green), "
            "accent_lime #b5d334 join (looks/harbour.json and look.json, sha256 refreshed; tools/world/gen/colour.ts weights: teal 13, coral 12, red 9, royal 9, tangerine 9, turquoise 7, "
            "cerulean 7, cobalt 6, flamingo 6, plum 6, mint 5, jade 5, peach 3, lime 3; fins and flags take the new roles too). facade.json: four new wall families "
            "(butter #f7e38c, lilac #d8c8ec, apricot #f9b977, seagreen #9ad2c6) with cerulean, plum, jade and tangerine plinths; sand_w #ddbe8a, blush_w #f4c3c8, sky_w #9ccdee, "
            "mint_w #c8e8c6 (rose plinth #d9548a) pulled apart; family roofs one tint deeper; cream's area weight 4 -> 2; five new enamels in every area's set except civic "
            "(seafoam + deep teal kept). The rule names the renders and the wider accents; materials and concept_prompt.city follow. Road unchanged (powder-blue enamel). GDD §15 to follow (Aster).")
    s["notes"] = [n for n in s["notes"] if not n.startswith("v0.23.0")] + [note]
    s["version"], s["date"] = "0.23.0", "2026-10-28"
    STYLE.write_text(json.dumps(s, indent=2, ensure_ascii=False) + "\n")
    return sha


if __name__ == "__main__":
    facade()
    look_text()
    harbour()
    print("palette_world.source.sha256", style())
